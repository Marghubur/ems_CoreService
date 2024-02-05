using Bot.CoreBottomHalf.CommonModal.Enums;
using Bot.CoreBottomHalf.CommonModal.Kafka;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using Confluent.Kafka;
using EMailService.Modal;
using EMailService.Modal.CronJobs;
using EMailService.Modal.Jobs;
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
using System.Net;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace ServiceLayer.Code
{
    public class AutoTriggerService : IAutoTriggerService
    {
        private readonly ILogger<AutoTriggerService> _logger;
        private readonly MasterDatabase _masterDatabase;
        private readonly List<KafkaServiceConfig> _kafkaServiceConfig;
        private readonly ITimezoneConverter _timezoneConverter;
        private readonly IWeeklyTimesheetCreationJob _weeklyTimesheetCreationJob;
        private readonly ILeaveAccrualJob _leaveAccrualJob;
        private readonly YearEndCalculation _yearEndCalculation;
        private readonly IPayrollService _payrollService;
        private readonly IDb _db;

        public AutoTriggerService(ILogger<AutoTriggerService> logger,
            IOptions<MasterDatabase> options,
            IOptions<List<KafkaServiceConfig>> kafkaOptions,
            ITimezoneConverter timezoneConverter,
            IWeeklyTimesheetCreationJob weeklyTimesheetCreationJob,
            ILeaveAccrualJob leaveAccrualJob,
            IDb db,
            YearEndCalculation yearEndCalculation,
            IPayrollService payrollService)
        {
            _logger = logger;
            _masterDatabase = options.Value;
            _kafkaServiceConfig = kafkaOptions.Value;
            _timezoneConverter = timezoneConverter;
            _weeklyTimesheetCreationJob = weeklyTimesheetCreationJob;
            _db = db;
            _leaveAccrualJob = leaveAccrualJob;
            _yearEndCalculation = yearEndCalculation;
            _payrollService = payrollService;
        }

        public async Task ScheduledJobManager()
        {
            var kafkaConfig = _kafkaServiceConfig.Find(x => x.Topic == LocalConstants.DailyJobsManager);
            if (kafkaConfig == null)
            {
                throw new HiringBellException($"No configuration found for the kafka", "service name", LocalConstants.DailyJobsManager, HttpStatusCode.InternalServerError);
            }

            var config = new ConsumerConfig
            {
                GroupId = kafkaConfig.GroupId,
                BootstrapServers = $"{kafkaConfig.ServiceName}:{kafkaConfig.Port}"
            };

            _logger.LogInformation($"[Kafka] Start listning kafka topic: {kafkaConfig.Topic}");
            using (var consumer = new ConsumerBuilder<Null, string>(config).Build())
            {
                consumer.Subscribe(kafkaConfig.Topic);
                while (true)
                {
                    try
                    {
                        _logger.LogInformation($"[Kafka] Waiting on topic: {kafkaConfig.Topic}");
                        var message = consumer.Consume();

                        _logger.LogInformation($"[Kafka] Message recieved: {message}");
                        if (message != null && !string.IsNullOrEmpty(message.Message.Value))
                        {
                            _logger.LogInformation(message.Message.Value);
                            await RunJobAsync(message.Message.Value);

                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[Kafka Error]: Got exception - {ex.Message}");
                    }

                    await Task.CompletedTask;
                }
            }
        }

        public async Task RunJobAsync(string payload)
        {
            KafkaPayload kafkaPayload = JsonConvert.DeserializeObject<KafkaPayload>(payload);

            // Load all database configuration from master database
            List<DbConfigModal> dbConfig = await LoadDatabaseConfiguration();

            dbConfig.ForEach(async x =>
            {
                List<CompanySetting> companySettings = await LoadCompanySettings(x);

                // execute jobs
                switch (kafkaPayload.ServiceName)
                {
                    case nameof(ScheduledJobServiceName.MONTHLYLEAVEACCRUAL):
                        companySettings.ForEach(async i =>
                        {
                            LeaveAccrualKafkaModel leaveAccrualKafkaModel = JsonConvert.DeserializeObject<LeaveAccrualKafkaModel>(payload);
                            await ExecuteLeaveAccrualJobAsync(i, leaveAccrualKafkaModel);
                        });
                        break;
                    case nameof(ScheduledJobServiceName.WEEKLYTIMESHEET):
                        companySettings.ForEach(async i => await RunTimesheetJobAsync(i, DateTime.UtcNow, null, true));
                        break;
                    case nameof(ScheduledJobServiceName.MONTHLYPAYROLL):
                        companySettings.ForEach(async i => await RunPayrollJobAsync());
                        break;
                    case nameof(ScheduledJobServiceName.YEARENDLEAVEPROCESSING):
                        companySettings.ForEach(async i =>
                        {
                            LeaveYearEndCalculationKafkaModel data = JsonConvert.DeserializeObject<LeaveYearEndCalculationKafkaModel>(kafkaPayload.Message);
                            await RunLeaveYearEndJobAsync(i, data);
                        });
                        break;
                }
            });
        }

        public async Task ExecuteLeaveAccrualJobAsync(CompanySetting companySetting, LeaveAccrualKafkaModel leaveAccrualKafkaModel)
        {
            _logger.LogInformation("Leave Accrual cron job started.");
            await _leaveAccrualJob.LeaveAccrualAsync(companySetting, leaveAccrualKafkaModel);
        }

        private async Task<List<CompanySetting>> LoadCompanySettings(DbConfigModal x)
        {
            _db.SetupConnectionString($"server={x.Server};port={x.Port};database={x.Database};User Id={x.UserId};password={x.Password};Connection Timeout={x.ConnectionTimeout};Connection Lifetime={_masterDatabase.Connection_Lifetime};Min Pool Size={_masterDatabase.Min_Pool_Size};Max Pool Size={_masterDatabase.Max_Pool_Size};Pooling={_masterDatabase.Pooling};");
            List<CompanySetting> companySettings = _db.GetList<CompanySetting>(Procedures.Company_Setting_Get_All);

            return await Task.FromResult(companySettings);
        }

        private async Task<List<DbConfigModal>> LoadDatabaseConfiguration()
        {
            List<DbConfigModal> databaseConfiguration = new List<DbConfigModal>();
            string cs = $"server={_masterDatabase.Server};port={_masterDatabase.Port};database={_masterDatabase.Database};User Id={_masterDatabase.User_Id};password={_masterDatabase.Password};Connection Timeout={_masterDatabase.Connection_Timeout};Connection Lifetime={_masterDatabase.Connection_Lifetime};Min Pool Size={_masterDatabase.Min_Pool_Size};Max Pool Size={_masterDatabase.Max_Pool_Size};Pooling={_masterDatabase.Pooling};";
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
                            databaseConfiguration = Converter.ToList<DbConfigModal>(dataSet.Tables[0]);
                        }
                    }
                    catch
                    {
                        throw;
                    }
                }
            }

            return await Task.FromResult(databaseConfiguration);
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
            _logger.LogInformation("Timesheet creation cron job ran successfully.");
        }

        public async Task RunPayrollJobAsync()
        {
            await _payrollService.RunPayrollCycle(0);
            _logger.LogInformation("Payroll cron job ran successfully.");
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
            _logger.LogInformation("Leave year end  cron job ran successfully.");
        }
    }
}