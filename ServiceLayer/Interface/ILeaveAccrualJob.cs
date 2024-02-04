using EMailService.Modal.Jobs;
using ModalLayer.Modal.Accounts;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface ILeaveAccrualJob
    {
        Task LeaveAccrualAsync(CompanySetting companySetting, LeaveAccrualKafkaModel leaveAccrualKafkaModel);
    }
}
