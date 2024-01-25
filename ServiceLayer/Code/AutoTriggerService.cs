using Microsoft.Extensions.Logging;
using ServiceLayer.Code.HostedServiceJobs;
using ServiceLayer.Code.HostedServicesJobs;
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

            _logger.LogInformation("Leave year end cron job started.");
            await RunLeaveYearEndJobAsync();
        }

        public async Task RunLeaveAccrualJobAsync()
        {
            await LeaveAccrualJob.LeaveAccrualAsync(_serviceProvider);
            _logger.LogInformation("Leave Accrual cron job ran successfully.");
        }

        public async Task RunTimesheetJobAsync(DateTime startDate, DateTime? endDate, bool isCronJob)
        {
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

        public async Task RunLeaveYearEndJobAsync()
        {
            await YearEndLeaveProcessingJob.RunLeaveEndYearAsync(_serviceProvider);
            _logger.LogInformation("Leave year end  cron job ran successfully.");
        }
    }
}
