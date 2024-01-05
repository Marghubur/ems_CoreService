using ServiceLayer.Code.HostedServiceJobs;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IAutoTriggerService
    {
        Task RunJobAsync();
        Task RunLeaveAccrualJobAsync();
        Task RunTimesheetJobAsync();
        Task RunPayrollJobAsync();
    }
}
