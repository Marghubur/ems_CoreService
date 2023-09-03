using Microsoft.Extensions.DependencyInjection;
using ModalLayer.Modal.Accounts;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OnlineDataBuilder.HostedService.Services
{
    public class LeaveAccrualJob
    {
        public async static Task<List<CompanySetting>> LeaveAccrualAsync(IServiceProvider _serviceProvider)
        {
            List<CompanySetting> CompanySettingList = null;
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                ILeaveCalculation _leaveCalculation = scope.ServiceProvider.GetRequiredService<ILeaveCalculation>();
                CompanySettingList = await _leaveCalculation.StartAccrualCycle();
            }

            return CompanySettingList;
        }
    }
}
