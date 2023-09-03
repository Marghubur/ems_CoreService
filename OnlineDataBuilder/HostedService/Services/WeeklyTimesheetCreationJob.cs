using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Interface;
using System;
using System.Threading.Tasks;

namespace OnlineDataBuilder.HostedService.Services
{
    public class WeeklyTimesheetCreationJob
    {
        public async static Task RunDailyTimesheetCreationJob(IServiceProvider _serviceProvider)
        {
            if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Saturday)
            {
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    var service = scope.ServiceProvider.GetRequiredService<ITimesheetService>();
                    await service.RunWeeklyTimesheetCreation(DateTime.UtcNow.AddDays(2));
                }
            }

            await Task.CompletedTask;
        }
    }
}
