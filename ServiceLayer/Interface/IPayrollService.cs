using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IPayrollService
    {
        Task RunPayrollCycle(int i);
    }
}
