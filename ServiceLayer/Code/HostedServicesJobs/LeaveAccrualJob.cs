using EMailService.Modal.Leaves;
using Microsoft.Extensions.DependencyInjection;
using ModalLayer.Modal.Accounts;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Code.HostedServiceJobs
{
    public class LeaveAccrualJob
    {
        public async static Task<List<CompanySetting>> LeaveAccrualAsync(IServiceProvider _serviceProvider)
        {
            List<CompanySetting> CompanySettingList = null;
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                ILeaveCalculation _leaveCalculation = scope.ServiceProvider.GetRequiredService<ILeaveCalculation>();
                RunAccrualModel runAccrualModel = new RunAccrualModel
                {
                    RunTillMonthOfPresnetYear = true,
                    EmployeeId = 0,
                    IsSingleRun = false
                };

                CompanySettingList = await _leaveCalculation.StartAccrualCycle(runAccrualModel);
            }

            return CompanySettingList;
        }
    }
}
