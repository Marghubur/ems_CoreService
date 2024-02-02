using ModalLayer.Modal.Accounts;
using System;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IAutoTriggerService
    {
        Task RunLeaveAccrualJobAsync();
        Task RunTimesheetJobAsync(CompanySetting companySetting, DateTime startDate, DateTime? endDate, bool isCronJob);
        Task RunPayrollJobAsync();
        Task ScheduledJobManager();
    }
}
