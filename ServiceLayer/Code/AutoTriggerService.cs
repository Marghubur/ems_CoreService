using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using Confluent.Kafka;
using EMailService.Modal;
using EMailService.Modal.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ServiceLayer.Code.HostedServiceJobs;
using ServiceLayer.Code.HostedServicesJobs;
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
        private readonly ILogger<AutoTriggerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly MasterDatabase _masterDatabase;
        private readonly KafkaServiceConfigExtend _kafkaServiceConfig;
        private readonly ITimezoneConverter _timezoneConverter;

        public AutoTriggerService(ILogger<AutoTriggerService> logger,
            IServiceProvider serviceProvider,
            IOptions<MasterDatabase> options,
            IOptions<KafkaServiceConfigExtend> kafkaOptions,
            ITimezoneConverter timezoneConverter)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _masterDatabase = options.Value;
            _kafkaServiceConfig = kafkaOptions.Value;
            _timezoneConverter = timezoneConverter;
        }

        public async Task ScheduledJobManager()
        {
            var config = new ConsumerConfig
            {
                GroupId = _kafkaServiceConfig.GroupId,
                BootstrapServers = $"{_kafkaServiceConfig.ServiceName}:{_kafkaServiceConfig.Port}"
            };

            _logger.LogInformation($"[Kafka] Start listning kafka topic: {_kafkaServiceConfig.HourlyJobTopic}");
            using (var consumer = new ConsumerBuilder<Null, string>(config).Build())
            {
                consumer.Subscribe(_kafkaServiceConfig.HourlyJobTopic);
                while (true)
                {
                    try
                    {
                        _logger.LogInformation($"[Kafka] Waiting on topic: {_kafkaServiceConfig.HourlyJobTopic}");
                        var message = consumer.Consume();

                        if (message != null)
                        {
                            _logger.LogInformation(message.Message.Value);
                            KafkaPayload kafkaPayload = JsonConvert.DeserializeObject<KafkaPayload>(message.Message.Value);

                            if (kafkaPayload != null)
                            {
                                await RunJobAsync(kafkaPayload);
                            }
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

        public async Task RunJobAsync(KafkaPayload kafkaPayload)
        {
            // Load all database configuration from master database
            List<DbConfigModal> dbConfig = await LoadDatabaseConfiguration();

            dbConfig.ForEach(async x =>
            {
                List<CompanySetting> companySettings = await LoadCompanySettings(x);

                // execute jobs
                switch (kafkaPayload.ServiceName)
                {
                    case nameof(ScheduledJobServiceName.DAILY): //daily job
                        break;
                    case nameof(ScheduledJobServiceName.WEEKLY): // weekly job
                        companySettings.ForEach(async i => await ExecuteTimesheetJobAsync(i));
                        break;
                    case nameof(ScheduledJobServiceName.MONTHLY): // monthly job
                        companySettings.ForEach(async i =>
                        {
                            await ExecuteLeaveAccrualJobAsync(i);
                            await ExecutePayrollJobsAsync(i);
                        });
                        break;
                    case nameof(ScheduledJobServiceName.YEARLY): // yearly job
                        companySettings.ForEach(async i => await ExecuteYearEndLeaveProcessingJobsAsync(i));
                        break;

                    case nameof(ScheduledJobServiceName.DAILYWEEKLY):
                        break;
                    case nameof(ScheduledJobServiceName.DAILYMONTHLY):
                        break;
                    case nameof(ScheduledJobServiceName.DAILYYEARLY):
                        break;
                    case nameof(ScheduledJobServiceName.WEEKLYMONTHLY):
                        break;
                    case nameof(ScheduledJobServiceName.WEEKLYYEARLY):
                        break;
                    case nameof(ScheduledJobServiceName.MONTHLYYEARLY):
                        break;
                    case nameof(ScheduledJobServiceName.YEARLYMONTHLY):
                        break;
                    case nameof(ScheduledJobServiceName.DAILYWEEKLYMONTHLY):
                        break;
                    case nameof(ScheduledJobServiceName.DAILYWEEKLYYEARLY):
                        break;
                    case nameof(ScheduledJobServiceName.WEEKLYMONTHLYYEARLY):
                        break;
                    case nameof(ScheduledJobServiceName.DAILYWEEKLYMONTHLYYEARLY):
                        break;
                }
            });
        }

        private async Task ExecuteLeaveAccrualJobAsync(CompanySetting companySetting)
        {
            _logger.LogInformation("Leave Accrual cron job started.");
            await RunLeaveAccrualJobAsync();
        }

        private async Task ExecuteTimesheetJobAsync(CompanySetting companySetting)
        {
            await RunTimesheetJobAsync(companySetting, DateTime.UtcNow, null, true);
            _logger.LogInformation("Timesheet creation cron job started.");
        }

        private async Task ExecutePayrollJobsAsync(CompanySetting companySetting)
        {
            _logger.LogInformation("Payroll cron job started.");
            await RunPayrollJobAsync();
        }

        private async Task ExecuteYearEndLeaveProcessingJobsAsync(CompanySetting companySetting)
        {
            _logger.LogInformation("Leave year end cron job started.");
            await RunLeaveYearEndJobAsync(companySetting);
        }

        private async Task<List<CompanySetting>> LoadCompanySettings(DbConfigModal x)
        {
            List<CompanySetting> companySettings = new List<CompanySetting>();

            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IDb db = scope.ServiceProvider.GetRequiredService<IDb>();
                db.SetupConnectionString($"server={x.Server};port={x.Port};database={x.Database};User Id={x.UserId};password={x.Password};Connection Timeout={x.ConnectionTimeout};Connection Lifetime={_masterDatabase.Connection_Lifetime};Min Pool Size={_masterDatabase.Min_Pool_Size};Max Pool Size={_masterDatabase.Max_Pool_Size};Pooling={_masterDatabase.Pooling};");
                companySettings = db.GetList<CompanySetting>(Procedures.Company_Setting_Get_All);
            }

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

        public async Task RunLeaveAccrualJobAsync()
        {
            await LeaveAccrualJob.LeaveAccrualAsync(_serviceProvider);
            _logger.LogInformation("Leave Accrual cron job ran successfully.");
        }

        public async Task RunTimesheetJobAsync(CompanySetting companySetting, DateTime startDate, DateTime? endDate, bool isCronJob)
        {
            var TimeZone = TZConvert.GetTimeZoneInfo(companySetting.TimezoneName);
            DateTime presentUtcDate = _timezoneConverter.ToSpecificTimezoneDateTime(TimeZone);

            if (startDate.DayOfWeek != DayOfWeek.Sunday)
                throw new Exception("Invalid start date selected. Start date must be monday");

            if (endDate != null && endDate?.DayOfWeek != DayOfWeek.Saturday)
                throw new Exception("Invalid end date selected. End date must be sunday");

            await WeeklyTimesheetCreationJob.RunDailyTimesheetCreationJob(_serviceProvider, startDate, endDate, isCronJob);
            _logger.LogInformation("Timesheet creation cron job ran successfully.");
        }
        public async Task RunPayrollJobAsync()
        {
            await PayrollCycleJob.RunPayrollAsync(_serviceProvider, 0);
            _logger.LogInformation("Payroll cron job ran successfully.");
        }

        public async Task RunLeaveYearEndJobAsync(CompanySetting companySetting)
        {
            await YearEndLeaveProcessingJob.RunLeaveEndYearAsync(_serviceProvider, companySetting);
            _logger.LogInformation("Leave year end  cron job ran successfully.");
        }
    }
}
