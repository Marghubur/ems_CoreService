using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Interface;
using System;
using System.Threading.Tasks;

namespace OnlineDataBuilder.HostedService.Services
{
    public class PayrollCycleJob
    {
        public async static Task RunPayrollAsync(IServiceProvider _serviceProvider, int i)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IPayrollService _payrollService = scope.ServiceProvider.GetRequiredService<IPayrollService>();
                await _payrollService.RunPayrollCycle(i);
            }
        }
    }
}
