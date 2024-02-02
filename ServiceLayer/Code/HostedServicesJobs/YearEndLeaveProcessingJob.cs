using Microsoft.Extensions.DependencyInjection;
using ModalLayer.Modal.Accounts;
using ServiceLayer.Interface;
using System;
using System.Threading.Tasks;

namespace ServiceLayer.Code.HostedServicesJobs
{
    public class YearEndLeaveProcessingJob
    {
        public async static Task RunLeaveEndYearAsync(IServiceProvider _serviceProvider, CompanySetting companySetting)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IRunLeaveEndYearService runLeaveEndYearService = scope.ServiceProvider.GetRequiredService<IRunLeaveEndYearService>();
                await runLeaveEndYearService.RunYearEndLeaveProcessingAsync(companySetting);
            }
        }
    }
}