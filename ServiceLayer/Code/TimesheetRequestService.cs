using BottomhalfCore.DatabaseLayer.Common.Code;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Code.SendEmail;
using ServiceLayer.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class TimesheetRequestService : ITimesheetRequestService
    {
        private readonly IDb _db;
        private readonly CurrentSession _currentSession;
        private readonly ApprovalEmailService _approvalEmailService;

        public TimesheetRequestService(IDb db,
            ApprovalEmailService approvalEmailService,
            CurrentSession currentSession)
        {
            _db = db;
            _currentSession = currentSession;
            _approvalEmailService = approvalEmailService;
        }

        public async Task<List<TimesheetDetail>> RejectTimesheetService(int timesheetId, TimesheetDetail timesheetDetail, int filterId = ApplicationConstants.Only)
        {
            await UpdateTimesheetRequest(timesheetId, ItemStatus.Rejected);
            return await this.GetTimesheetRequestDataService(timesheetDetail);
        }

        public async Task<List<TimesheetDetail>> ApprovalTimesheetService(int timesheetId, TimesheetDetail timesheetDetail, int filterId = ApplicationConstants.Only)
        {
            await UpdateTimesheetRequest(timesheetId, ItemStatus.Approved);
            return await this.GetTimesheetRequestDataService(timesheetDetail);
        }

        public async Task UpdateTimesheetRequest(int timesheetId, ItemStatus itemStatus)
        {
            if (timesheetId <= 0)
                throw new HiringBellException("Invalid attendance day selected");

            TimesheetDetail timesheet = _db.Get<TimesheetDetail>("sp_employee_timesheet_getby_timesheetid", new
            {
                TimesheetId = timesheetId
            });

            if (timesheet == null)
                throw HiringBellException.ThrowBadRequest("Invalid timesheet found. Please contact admin.");

            // this call is used for only upadate AttendanceDetail json object
            var Result = _db.Execute<TimesheetDetail>("sp_timesheet_upd_by_id", new
            {
                timesheet.TimesheetId,
                TimesheetStatus = (int)itemStatus,
                timesheet.UserComments,
                IsSubmitted = itemStatus == ItemStatus.Approved ? true : false,
                AdminId = _currentSession.CurrentUserDetail.UserId
            }, true);

            if (string.IsNullOrEmpty(Result))
                throw new HiringBellException("Unable to update attendance status");

            var timesheetDetails = JsonConvert.DeserializeObject<List<TimesheetDetail>>(timesheet.TimesheetWeeklyJson);
            await _approvalEmailService.TimesheetApprovalStatusSendEmail(timesheet, timesheetDetails, itemStatus);
            await Task.CompletedTask;
        }

        public List<DailyTimesheetDetail> ReAssigneTimesheetService(List<DailyTimesheetDetail> dailyTimesheetDetails, int filterId = ApplicationConstants.Only)
        {
            return null;
        }

        public async Task<List<TimesheetDetail>> GetTimesheetRequestDataService(TimesheetDetail timesheetDetail)
        {
            if (timesheetDetail.ReportingManagerId == 0)
                throw new HiringBellException("Invalid reporting manager");

            if (timesheetDetail.ForYear == 0)
                throw new HiringBellException("Year is invalid");

            var result = _db.GetList<TimesheetDetail>("sp_timesheet_requests_by_filter", new
            {
                timesheetDetail.ReportingManagerId,
                timesheetDetail.ForYear,
                timesheetDetail.TimesheetStatus,
                timesheetDetail.EmployeeId,
                timesheetDetail.PageIndex
            });

            return await Task.FromResult(result);
        }
    }
}
