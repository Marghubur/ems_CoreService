using Bot.CoreBottomHalf.CommonModal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModalLayer.Modal;
using NCrontab;
using ServiceLayer.Interface;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OnlineDataBuilder.HostedService
{
    public class DailyStartHourJob : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DailyStartHourJob> _logger;
        private readonly CrontabSchedule _cron;
        private readonly IServiceProvider _serviceProvider;
        private readonly ApplicationConfiguration _applicationConfiguration;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly IAutoTriggerService _autoTriggerService;

        private int counter = 3;
        private int index = 1;
        DateTime _nextCron;

        public DailyStartHourJob(ILogger<DailyStartHourJob> logger,
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ApplicationConfiguration applicationConfiguration,
            FileLocationDetail fileLocationDetail,
            IAutoTriggerService autoTriggerService)
        {
            _logger = logger;
            _configuration = configuration;
            _fileLocationDetail = fileLocationDetail;
            _applicationConfiguration = applicationConfiguration;
            _serviceProvider = serviceProvider;

            _logger.LogInformation($"Cron value: {configuration.GetSection("DailyEarlyHourJob").Value}");

            _cron = CrontabSchedule.Parse(configuration.GetSection("DailyEarlyHourJob").Value,
                new CrontabSchedule.ParseOptions { IncludingSeconds = true });
            _nextCron = _cron.GetNextOccurrence(DateTime.UtcNow);
            _autoTriggerService = autoTriggerService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Enabling the logging.");
            EnableLoggin();

            while (!cancellationToken.IsCancellationRequested)
            {
                int value = WaitForNextCronValue();
                _logger.LogInformation($"Cron job will run: {_nextCron}. Utc time: {DateTime.UtcNow} Wait time in ms: {value}");

                await Task.Delay(value, cancellationToken);
                _logger.LogInformation($"Daily cron job started. Index = {index} at {DateTime.Now} (utc time: {DateTime.UtcNow})   ...............");

                await this.RunJobAsync();

                _logger.LogInformation($"Daily cron job ran successfully. Index = {index++} at {DateTime.Now} (utc time: {DateTime.UtcNow})   .................");
                _nextCron = _cron.GetNextOccurrence(DateTime.Now);
            }
        }

        private async Task RunJobAsync()
        {
            await _autoTriggerService.RunJobAsync();
        }

        private void EnableLoggin()
        {
            _applicationConfiguration.LoggingFilePath = _configuration.GetSection("ExceptionLoggingPath").Value;
            var flag = _configuration.GetSection("Logging:LogTransaction").Value;
            if (flag.ToLower() == "true")
                _applicationConfiguration.IsLoggingEnabled = true;
            else
                _applicationConfiguration.IsLoggingEnabled = false;

            _applicationConfiguration.LoggingFilePath = Path.Combine(
                _fileLocationDetail.RootPath,
            _applicationConfiguration.LoggingFilePath);

            if (!Directory.Exists(_applicationConfiguration.LoggingFilePath))
            {
                Directory.CreateDirectory(_applicationConfiguration.LoggingFilePath);
            }
        }

        private int WaitForNextCronValue() => Math.Max(0, (int)_nextCron.Subtract(DateTime.UtcNow).TotalMilliseconds);

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}
