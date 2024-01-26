using Bot.CoreBottomHalf.CommonModal;
using Microsoft.Extensions.Logging;
using ModalLayer.Modal.Leaves;
using System.Threading.Tasks;

namespace ServiceLayer.Code.Leaves
{
    public class Approval
    {
        private LeavePlanConfiguration _leavePlanConfiguration;
        private CurrentSession _currentSession;
        private readonly ILogger<Approval> _logger;
        public Approval(CurrentSession currentSession, ILogger<Approval> logger)
        {
            _currentSession = currentSession;
            _logger = logger;
        }

        public async Task CheckLeaveApproval(LeaveCalculationModal leaveCalculationModal)
        {
            _logger.LogInformation("Method: CheckLeaveApproval start");

            _leavePlanConfiguration = leaveCalculationModal.leavePlanConfiguration;
            //await CheckLeaveRequiredForApproval(leaveCalculationModal);

            if (_leavePlanConfiguration.leaveApproval.IsPauseForApprovalNotification)
                leaveCalculationModal.IsEmailNotificationPasued = true;
            else
                leaveCalculationModal.IsEmailNotificationPasued = false;

            _logger.LogInformation("Method: CheckLeaveApproval end");
            await Task.CompletedTask;
        }

        //private async Task CheckLeaveRequiredForApproval(LeaveCalculationModal leaveCalculationModal)
        //{
        //    if (_leavePlanConfiguration.leaveApproval.IsLeaveRequiredApproval)
        //    {
        //        var ApprovalRoleTypeId = _leavePlanConfiguration.leaveApproval.ApprovalChain[0].ApprovalRoleTypeId;
        //        if (ApprovalRoleTypeId == ApplicationConstants.ReportingManager)
        //            leaveCalculationModal.AssigneId = _currentSession.CurrentUserDetail.ReportingManagerId;
        //        else if (ApprovalRoleTypeId == ApplicationConstants.SeniorHRManager)
        //            leaveCalculationModal.AssigneId = 12;
        //        else
        //        {
        //            if (_leavePlanConfiguration.leaveApproval.IsRequiredAllLevelApproval)
        //                leaveCalculationModal.IsLeaveAutoApproval = true;
        //        }
        //    }
        //    await Task.CompletedTask;
        //}
    }
}
