using EMailService.Modal.DashboardCalculation;
using ModalLayer.Modal;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IDashboardService
    {
        Task<AdminDashboardResponse> GetSystemDashboardService(AttendenceDetail userDetails);
    }
}
