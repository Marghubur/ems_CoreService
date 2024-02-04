using ServiceLayer.Interface;
using System.Threading.Tasks;

namespace ServiceLayer.Code.HostedServiceJobs
{
    public class PayrollCycleJob: IPayrollCycleJob
    {
        private readonly IPayrollService _payrollService;

        public PayrollCycleJob(IPayrollService payrollService)
        {
            _payrollService = payrollService;
        }

        public async Task RunPayrollAsync(int i)
        {
            await _payrollService.RunPayrollCycle(i);
        }
    }
}
