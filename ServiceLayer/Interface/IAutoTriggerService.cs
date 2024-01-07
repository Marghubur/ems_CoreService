using ServiceLayer.Code.HostedServiceJobs;
using System;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IAutoTriggerService
    {
        Task RunJobAsync();
        Task RunLeaveAccrualJobAsync();
        Task RunTimesheetJobAsync(DateTime startDate, DateTime? endDate, bool isCronJob);
        Task RunPayrollJobAsync();
    }
}
