using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Interface;
using EMailService.Service;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Code.SendEmail;
using ServiceLayer.Interface;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class TimesheetRequestService : ITimesheetRequestService
    {
        private readonly IDb _db;
        private readonly ITimezoneConverter _timezoneConverter;
        private readonly CurrentSession _currentSession;
        private readonly IAttendanceRequestService _attendanceRequestService;
        private readonly IEMailManager _eMailManager;
        private readonly IEmailService _emailService;
        private readonly ICommonService _commonService;
        private readonly ApprovalEmailService _approvalEmailService;

        public TimesheetRequestService(IDb db,
            ITimezoneConverter timezoneConverter,
            ApprovalEmailService approvalEmailService,
            CurrentSession currentSession,
            IEmailService emailService,
            IAttendanceRequestService attendanceRequestService,
            ICommonService commonService,
            IEMailManager eMailManager)
        {
            _db = db;
            _timezoneConverter = timezoneConverter;
            _currentSession = currentSession;
            _attendanceRequestService = attendanceRequestService;
            _eMailManager = eMailManager;
            _commonService = commonService;
            _emailService = emailService;
            _approvalEmailService = approvalEmailService;
        }

        public async Task<RequestModel> RejectTimesheetService(int timesheetId, int filterId = ApplicationConstants.Only)
        {
            await UpdateTimesheetRequest(timesheetId, ItemStatus.Rejected);
            return _attendanceRequestService.GetRequestPageData(_currentSession.CurrentUserDetail.UserId, filterId);
        }

        public async Task<RequestModel> ApprovalTimesheetService(int timesheetId, int filterId = ApplicationConstants.Only)
        {
            await UpdateTimesheetRequest(timesheetId, ItemStatus.Approved);
            return _attendanceRequestService.GetRequestPageData(_currentSession.CurrentUserDetail.UserId, filterId);
        }

        public async Task<RequestModel> UpdateTimesheetRequest(int timesheetId, ItemStatus itemStatus)
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
            return _attendanceRequestService.FetchPendingRequestService(_currentSession.CurrentUserDetail.ReportingManagerId);
        }

        public List<DailyTimesheetDetail> ReAssigneTimesheetService(List<DailyTimesheetDetail> dailyTimesheetDetails, int filterId = ApplicationConstants.Only)
        {
            return null;
        }
    }
}
