using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Interface;
using System;
using System.Threading.Tasks;

namespace ServiceLayer.Code.HostedServicesJobs
{
    public class YearEndLeaveProcessingJob
    {
        public async static Task RunLeaveEndYearAsync(IServiceProvider _serviceProvider)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IRunLeaveEndYearService runLeaveEndYearService = scope.ServiceProvider.GetRequiredService<IRunLeaveEndYearService>();
                await runLeaveEndYearService.LoadDbConfiguration();
            }
        }
    }
}