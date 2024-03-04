using System;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IPayrollService
    {
        Task RunPayrollCycle(DateTime runDate, bool reRunFlag = false);
    }
}
