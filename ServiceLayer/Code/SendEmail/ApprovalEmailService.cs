using Bot.CoreBottomHalf.CommonModal;
using ModalLayer.Modal;
using ModalLayer.Modal.Leaves;
using ServiceLayer.Interface;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Code.SendEmail
{
    public class ApprovalEmailService
    {
        private readonly IEmailService _emailService;
        private readonly CurrentSession _currentSession;

        public ApprovalEmailService(IEmailService emailService, CurrentSession currentSession)
        {
            _emailService = emailService;
            _currentSession = currentSession;
        }

        #region ATTENDANCE APPROVAL

        private async Task<TemplateReplaceModal> GetAttendanceApprovalTemplate(Attendance attendanceDetails, ItemStatus status)
        {
            var templateReplaceModal = new TemplateReplaceModal
            {
                DeveloperName = attendanceDetails.EmployeeName,
                RequestType = ApplicationConstants.WorkFromHome,
                ToAddress = new List<string> { attendanceDetails.Email },
                ActionType = status.ToString(),
                FromDate = attendanceDetails.AttendanceDay,
                ToDate = attendanceDetails.AttendanceDay,
                ManagerName = _currentSession.CurrentUserDetail.FullName,
                Message = string.IsNullOrEmpty(attendanceDetails.UserComments)
                    ? "NA"
                    : attendanceDetails.UserComments,
            };

            return await Task.FromResult(templateReplaceModal);
        }

        public async Task AttendaceApprovalStatusSendEmail(Attendance attendanceDetails, ItemStatus status)
        {
            var templateReplaceModal = await GetAttendanceApprovalTemplate(attendanceDetails, status);
            await _emailService.SendEmailWithTemplate(ApplicationConstants.AttendanceApprovalStatusEmailTemplate, templateReplaceModal);
        }

        #endregion

        #region LEAVE APPROVAL

        private async Task<TemplateReplaceModal> GetLeaveApprovalTemplate(LeaveRequestDetail leaveRequestDetail, ItemStatus status, string SendTo)
        {
            var templateReplaceModal = new TemplateReplaceModal
            {
                DeveloperName = leaveRequestDetail.FirstName + " " + leaveRequestDetail.LastName,
                RequestType = ApplicationConstants.Leave,
                ToAddress = new List<string> { SendTo },
                ActionType = status.ToString(),
                FromDate = leaveRequestDetail.LeaveFromDay,
                ToDate = leaveRequestDetail.LeaveToDay,
                LeaveType = leaveRequestDetail.LeaveToDay.ToString(),
                ManagerName = _currentSession.CurrentUserDetail.FullName,
                Message = string.IsNullOrEmpty(leaveRequestDetail.Reason)
                                    ? "NA"
                                    : leaveRequestDetail.Reason
            };

            return await Task.FromResult(templateReplaceModal);
        }

        public async Task LeaveApprovalStatusSendEmail(LeaveRequestDetail leaveRequestDetail, ItemStatus status)
        {
            var templateReplaceModal = await GetLeaveApprovalTemplate(leaveRequestDetail, status, leaveRequestDetail.Email);
            await _emailService.SendEmailWithTemplate(ApplicationConstants.AttendanceApprovalStatusEmailTemplate, templateReplaceModal);
        }

        public async Task ManagerApprovalMigrationEmail(LeaveRequestDetail leaveRequestDetail, string SendTo)
        {
            var templateReplaceModal = await GetLeaveApprovalTemplate(leaveRequestDetail, ItemStatus.Generated, SendTo);
            await _emailService.SendEmailWithTemplate(ApplicationConstants.MigrateApprovalToNewLevel, templateReplaceModal);
        }

        #endregion

        #region TIMESHEET APPROVAL

        private async Task<TemplateReplaceModal> GetTimesheetApprovalTemplate(TimesheetDetail timesheet, List<TimesheetDetail> timesheets, ItemStatus status)
        {
            var sortedTimesheetByDate = timesheets.OrderByDescending(x => x.PresentDate);
            var templateReplaceModal = new TemplateReplaceModal
            {
                DeveloperName = timesheet.FirstName + " " + timesheet.LastName,
                RequestType = ApplicationConstants.Timesheet,
                ToAddress = new List<string> { timesheet.Email },
                ActionType = status.ToString(),
                FromDate = sortedTimesheetByDate.First().PresentDate,
                ToDate = sortedTimesheetByDate.Last().PresentDate,
                LeaveType = null,
                ManagerName = _currentSession.CurrentUserDetail.FullName,
                Message = string.IsNullOrEmpty(timesheet.UserComments)
                            ? "NA"
                            : timesheet.UserComments,
            };

            return await Task.FromResult(templateReplaceModal);
        }

        public async Task TimesheetApprovalStatusSendEmail(TimesheetDetail timesheet, List<TimesheetDetail> timesheets, ItemStatus status)
        {
            var templateReplaceModal = await GetTimesheetApprovalTemplate(timesheet, timesheets, status);
            await _emailService.SendEmailWithTemplate(ApplicationConstants.TimesheetApprovalStatusEmailTemplate, templateReplaceModal);
        }

        #endregion
    }
}
