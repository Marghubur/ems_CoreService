using EMailService.Modal.Jobs;
using ModalLayer.Modal.Accounts;
using System;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IAutoTriggerService
    {
        Task ExecuteLeaveAccrualJobAsync(CompanySetting companySetting, LeaveAccrualKafkaModel leaveAccrualKafkaModel);
        Task RunTimesheetJobAsync(CompanySetting companySetting, DateTime startDate, DateTime? endDate, bool isCronJob);
        Task RunPayrollJobAsync(DateTime? runDate);
        Task ScheduledJobManager();
        Task ExecuteYearlyLeaveRequestAccrualJobAsync(CompanySetting companySetting);
        Task RunAndBuilEmployeeSalaryAndDeclaration();
    }
}
