using Microsoft.Extensions.DependencyInjection;
using ModalLayer.Modal.Accounts;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Code.HostedServiceJobs
{
    public class AttendanceApprovalLevelJob
    {
        public async static Task UpgradeRequestLevel(IServiceProvider _serviceProvider, List<CompanySetting> companySettings)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                ILeaveRequestService _leaveRequestService = scope.ServiceProvider.GetRequiredService<ILeaveRequestService>();
                await _leaveRequestService.LeaveLeaveManagerMigration(companySettings);
            }
        }
    }
}
