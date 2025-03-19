using EMailService.Modal;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IServiceJobStatusService
    {
        Task<ServiceJobStatus> GetServiceJobStatusService(int serviceJobStatusId);
        Task<int> AddServiceJobStatusService(string serviceName);
        Task<int> UpdateServiceJobStatusService(int serviceJobStatusId);
    }
}
