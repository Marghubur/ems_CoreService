using System;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IWeeklyTimesheetCreationJob
    {
        Task RunDailyTimesheetCreationJob(DateTime startDate, DateTime? endDate, bool isCronJob);
    }
}
