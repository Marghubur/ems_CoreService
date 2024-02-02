using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Interface;
using System;
using System.Threading.Tasks;

namespace ServiceLayer.Code.HostedServiceJobs
{
    public class WeeklyTimesheetCreationJob
    {
        private readonly ITimesheetService _timesheetService;

        public WeeklyTimesheetCreationJob(ITimesheetService timesheetService)
        {
            _timesheetService = timesheetService;
        }

        public async Task RunDailyTimesheetCreationJob(DateTime startDate, DateTime? endDate, bool isCronJob)
        {
            if (isCronJob)
            {
                if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Saturday)
                {
                    await _timesheetService.RunWeeklyTimesheetCreation(startDate.AddDays(2), null);
                }
            }
            else
            {
                if (endDate != null && endDate?.DayOfWeek != DayOfWeek.Saturday)
                    throw new Exception("Invalid end date selected. End date must be sunday");

                await _timesheetService.RunWeeklyTimesheetCreation(startDate, endDate);
            }

            await Task.CompletedTask;
        }
    }
}
