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
        private readonly ILogger<DailyStartHourJob> _logger;
        private readonly IAutoTriggerService _autoTriggerService;

        public DailyStartHourJob(ILogger<DailyStartHourJob> logger, IAutoTriggerService autoTriggerService)
        {
            _logger = logger;
            _autoTriggerService = autoTriggerService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Enabling the logging.");
            await _autoTriggerService.ScheduledJobManager();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}
