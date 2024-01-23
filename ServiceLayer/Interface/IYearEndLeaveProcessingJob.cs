using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IYearEndLeaveProcessingJob
    {
        Task LoadDbConfiguration();
    }
}
