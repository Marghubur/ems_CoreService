using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using Bot.CoreBottomHalf.CommonModal.Kafka;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using Bt.Lib.Common.Service.Configserver;
using Bt.Lib.Common.Service.KafkaService.interfaces;
using Bt.Lib.Common.Service.MicroserviceHttpRequest;
using Bt.Lib.Common.Service.Model;
using Confluent.Kafka;
using EMailService.Modal;
using EMailService.Modal.CronJobs;
using EMailService.Modal.Jobs;
using EMailService.Modal.Payroll;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModalLayer;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ServiceLayer.Code.Leaves;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace ServiceLayer.Code
{
    public class AutoTriggerService : IAutoTriggerService
    {
        private readonly IDb _db;
        private readonly IKafkaConsumerService _kafkaConsumerService;
        private readonly IFetchGithubConfigurationService _fetchGithubConfigurationService;

        private readonly ILogger<AutoTriggerService> _logger;
        private readonly MasterDatabase _masterDatabase;
        private readonly List<KafkaServiceConfig> _kafkaServiceConfig;
        private readonly ITimezoneConverter _timezoneConverter;
        private readonly IWeeklyTimesheetCreationJob _weeklyTimesheetCreationJob;
        private readonly ILeaveAccrualJob _leaveAccrualJob;
        private readonly YearEndCalculation _yearEndCalculation;
        private readonly RequestMicroservice _requestMicroservice;
        private readonly MicroserviceRegistry _microserviceRegistry;
        private readonly IAttendanceService _attendanceService;

        public AutoTriggerService(ILogger<AutoTriggerService> logger,
            IOptions<MasterDatabase> options,
            IOptions<List<KafkaServiceConfig>> kafkaOptions,
            ITimezoneConverter timezoneConverter,
            IWeeklyTimesheetCreationJob weeklyTimesheetCreationJob,
            ILeaveAccrualJob leaveAccrualJob,
            IDb db,
            YearEndCalculation yearEndCalculation,
            IOptions<MicroserviceRegistry> microserviceOptions,
            RequestMicroservice requestMicroservice,
            IAttendanceService attendanceService,
            IFetchGithubConfigurationService fetchGithubConfigurationService,
            IKafkaConsumerService kafkaConsumerService)
        {
            _logger = logger;
            _masterDatabase = options.Value;
            _kafkaServiceConfig = kafkaOptions.Value;
            _timezoneConverter = timezoneConverter;
            _weeklyTimesheetCreationJob = weeklyTimesheetCreationJob;
            _db = db;
            _leaveAccrualJob = leaveAccrualJob;
            _yearEndCalculation = yearEndCalculation;
            _microserviceRegistry = microserviceOptions.Value;
            _requestMicroservice = requestMicroservice;
            _attendanceService = attendanceService;
            _fetchGithubConfigurationService = fetchGithubConfigurationService;
            _kafkaConsumerService = kafkaConsumerService;
            // _payrollService = payrollService;
        }

        public async Task ScheduledJobManager()
        {
            await _kafkaConsumerService.SubscribeTopic(RunJobAsync, KafkaTopicNames.DAILY_JOBS_MANAGER);
            await Task.CompletedTask;
        }

        public async Task RunJobAsync(ConsumeResult<Null, string> result)
        {
            KafkaPayload kafkaPayload = JsonConvert.DeserializeObject<KafkaPayload>(result.Message.Value);

            // Load all database configuration from master database
            List<DbConfigModal> dbConfig = await LoadDatabaseConfiguration();

            foreach (var x in dbConfig)
            {
                try
                {
                    CompanySetting companySettings = await LoadCompanySettings(x);

                    // execute jobs
                    switch (kafkaPayload.kafkaServiceName)
                    {
                        case KafkaServiceName.MonthlyLeaveAccrualJob:
                            LeaveAccrualKafkaModel leaveAccrualKafkaModel = JsonConvert.DeserializeObject<LeaveAccrualKafkaModel>(result.Message.Value);
                            await ExecuteLeaveAccrualJobAsync(companySettings, leaveAccrualKafkaModel);
                            break;
                        case KafkaServiceName.WeeklyTimesheetJob:
                            await RunTimesheetJobAsync(companySettings, DateTime.UtcNow, null, true);
                            break;
                        case KafkaServiceName.MonthlyPayrollJob:
                            PayrollMonthlyDetail payrollMonthlyDetail = JsonConvert.DeserializeObject<PayrollMonthlyDetail>(kafkaPayload.Message);
                            await RunPayrollJobAsync(payrollMonthlyDetail.PaymentRunDate);
                            break;
                        case KafkaServiceName.YearEndLeaveProcessingJob:
                            LeaveYearEndCalculationKafkaModel data = JsonConvert.DeserializeObject<LeaveYearEndCalculationKafkaModel>(kafkaPayload.Message);
                            await RunLeaveYearEndJobAsync(companySettings, data);
                            break;
                        case KafkaServiceName.NewRegistration:
                            await RunGenerateAttendanceAsync();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[DAIALY JOB ERROR]: Got error: {ex.Message}");
                }
            }
        }

        public async Task ExecuteLeaveAccrualJobAsync(CompanySetting companySetting, LeaveAccrualKafkaModel leaveAccrualKafkaModel)
        {
            _logger.LogInformation("Leave Accrual cron job started.");
            await _leaveAccrualJob.LeaveAccrualAsync(companySetting, leaveAccrualKafkaModel);
        }

        private async Task<CompanySetting> LoadCompanySettings(DbConfigModal x)
        {
            _db.SetupConnectionString($"server={x.Server};port={x.Port};database={x.Database};User Id={x.UserId};password={x.Password};Connection Timeout={x.ConnectionTimeout};Connection Lifetime={x.ConnectionLifetime};Min Pool Size={x.MinPoolSize};Max Pool Size={x.MaxPoolSize};Pooling={x.Pooling};");
            CompanySetting companySettings = _db.Get<CompanySetting>(Procedures.Company_Setting_Get_All);

            return await Task.FromResult(companySettings);
        }

        private async Task<List<DbConfigModal>> LoadDatabaseConfiguration()
        {
            // DatabaseConfiguration config = await _fetchGithubConfigurationService.GetDatabaseConfiguration();
            // string cs = $"server={config.Server};port={config.Port};database={config.Database};User Id={_masterDatabase.User_Id};password={_masterDatabase.Password};Connection Timeout={_masterDatabase.Connection_Timeout};Connection Lifetime={_masterDatabase.Connection_Lifetime};Min Pool Size={_masterDatabase.Min_Pool_Size};Max Pool Size={_masterDatabase.Max_Pool_Size};Pooling={_masterDatabase.Pooling};";
            
            string cs = DatabaseConfiguration.BuildConnectionString(_fetchGithubConfigurationService.GetConfiguration<DatabaseConfiguration>());
            
            List<DbConfigModal> dbConfiguration = new List<DbConfigModal>();
            using (var connection = new MySqlConnection(cs))
            {
                using (MySqlCommand command = new MySqlCommand())
                {
                    try
                    {
                        command.Connection = connection;
                        command.CommandType = CommandType.Text;
                        command.CommandText = "select * from database_connections";
                        using (MySqlDataAdapter dataAdapter = new MySqlDataAdapter())
                        {
                            var dataSet = new DataSet();
                            connection.Open();
                            dataAdapter.SelectCommand = command;
                            dataAdapter.Fill(dataSet);
                            if (dataSet.Tables == null || dataSet.Tables.Count != 1 || dataSet.Tables[0].Rows.Count == 0)
                                throw new Exception("Fail to load the master data");
                            dbConfiguration = Converter.ToList<DbConfigModal>(dataSet.Tables[0]);
                        }
                    }
                    catch
                    {
                        throw;
                    }
                }
            }

            return await Task.FromResult(dbConfiguration);
        }

        public async Task RunTimesheetJobAsync(CompanySetting companySetting, DateTime startDate, DateTime? endDate, bool isCronJob)
        {
            if (isCronJob)
            {
                var TimeZone = TZConvert.GetTimeZoneInfo(companySetting.TimezoneName);
                DateTime presentUtcDate = _timezoneConverter.ToSpecificTimezoneDateTime(TimeZone);

                startDate = presentUtcDate;
            }

            await _weeklyTimesheetCreationJob.RunDailyTimesheetCreationJob(startDate, endDate, isCronJob);
        }

        public async Task RunPayrollJobAsync(DateTime? runDate)
        {
            if (runDate == null)
            {
                throw HiringBellException.ThrowBadRequest("Invalid run date passed. Please check run date.");
            }

            // await _payrollService.RunPayrollCycle(runDate.Value);
            var date = runDate.Value;
            string url = $"{_microserviceRegistry.RunPayroll}/{true}";
            await _requestMicroservice.GetRequest<EmployeeCalculation>(MicroserviceRequest.Builder(url));
        }

        public async Task RunLeaveYearEndJobAsync(CompanySetting companySetting, LeaveYearEndCalculationKafkaModel data)
        {
            var TimeZone = TZConvert.GetTimeZoneInfo(companySetting.TimezoneName);
            DateTime presentUtcDate = _timezoneConverter.ToSpecificTimezoneDateTime(TimeZone);

            LeaveYearEnd leaveYearEnd = new LeaveYearEnd
            {
                Timezone = TimeZone,
                ProcessingDateTime = data?.RunDate == null ? presentUtcDate : data.RunDate
            };

            await _yearEndCalculation.RunLeaveYearEndCycle(leaveYearEnd);
        }

        public async Task RunGenerateAttendanceAsync()
        {
            await _attendanceService.GenerateMonthlyAttendance();
        }
    }
}