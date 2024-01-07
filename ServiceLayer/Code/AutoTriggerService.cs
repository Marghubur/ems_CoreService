using Microsoft.Extensions.Logging;
using ServiceLayer.Code.HostedServiceJobs;
using ServiceLayer.Interface;
using System;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class AutoTriggerService : IAutoTriggerService
    {
        private readonly ILogger<AutoTriggerService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public AutoTriggerService(ILogger<AutoTriggerService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task RunJobAsync()
        {
            _logger.LogInformation("Leave Accrual cron job started.");
            await RunLeaveAccrualJobAsync();

            await RunTimesheetJobAsync(DateTime.UtcNow, null, true);
            _logger.LogInformation("Timesheet creation cron job started.");

            _logger.LogInformation("Payroll cron job started.");
            await RunPayrollJobAsync();
        }

        public async Task RunLeaveAccrualJobAsync()
        {
            await LeaveAccrualJob.LeaveAccrualAsync(_serviceProvider);
            _logger.LogInformation("Leave Accrual cron job ran successfully.");
        }

        public async Task RunTimesheetJobAsync(DateTime startDate, DateTime? endDate, bool isCronJob)
        {
            await WeeklyTimesheetCreationJob.RunDailyTimesheetCreationJob(_serviceProvider, startDate, endDate, isCronJob);
            _logger.LogInformation("Timesheet creation cron job ran successfully.");
        }

        public async Task RunPayrollJobAsync()
        {
            await PayrollCycleJob.RunPayrollAsync(_serviceProvider, 0);
            _logger.LogInformation("Payroll cron job ran successfully.");
        }
    }
}
