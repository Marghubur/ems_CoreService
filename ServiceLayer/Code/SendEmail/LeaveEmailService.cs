using Bot.CoreBottomHalf.CommonModal;
using ModalLayer.Modal;
using ModalLayer.Modal.Leaves;
using ServiceLayer.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Code.SendEmail
{
    public class LeaveEmailService
    {
        private readonly IEmailService _emailService;
        private readonly CurrentSession _currentSession;

        public LeaveEmailService(IEmailService emailService, CurrentSession currentSession)
        {
            _emailService = emailService;
            _currentSession = currentSession;
        }

        private async Task<TemplateReplaceModal> GetLeaveRequestModal(LeaveCalculationModal leaveCalculationModal, string reason)
        {
            var templateReplaceModal = new TemplateReplaceModal
            {
                DeveloperName = leaveCalculationModal.employee.FirstName + " " + leaveCalculationModal.employee.LastName,
                RequestType = ApplicationConstants.LeaveRequest,
                ToAddress = new List<string> { leaveCalculationModal.AssigneeEmail },
                ActionType = ItemStatus.Pending.ToString(),
                FromDate = leaveCalculationModal.fromDate,
                ToDate = leaveCalculationModal.toDate,
                LeaveType = ApplicationConstants.LeaveRequest,
                ManagerName = _currentSession.CurrentUserDetail.FullName,
                Message = string.IsNullOrEmpty(reason)
                            ? "NA"
                            : reason,
            };

            return await Task.FromResult(templateReplaceModal);
        }

        public async Task LeaveRequestSendEmail(LeaveCalculationModal leaveCalculationModal, string reason)
        {
            var templateReplaceModal = await GetLeaveRequestModal(leaveCalculationModal, reason);
            await _emailService.SendEmailWithTemplate(ApplicationConstants.LeaveApplyEmailTemplate, templateReplaceModal);
            await Task.CompletedTask;
        }
    }
}
