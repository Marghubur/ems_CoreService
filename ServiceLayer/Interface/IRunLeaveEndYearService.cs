using ModalLayer.Modal.Accounts;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IRunLeaveEndYearService
    {
        Task RunYearEndLeaveProcessingAsync(CompanySetting companySetting);
    }
}
