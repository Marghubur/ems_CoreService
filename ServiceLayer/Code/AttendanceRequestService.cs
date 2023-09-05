using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Interface;
using Microsoft.Extensions.Logging;
using ModalLayer.Modal;
using ModalLayer.Modal.HtmlTemplateModel;
using Newtonsoft.Json;
using OpenXmlPowerTools;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class AttendanceRequestService : IAttendanceRequestService
    {
        private readonly IDb _db;
        private readonly ITimezoneConverter _timezoneConverter;
        private readonly CurrentSession _currentSession;
        private readonly ILogger<AttendanceRequestService> _logger;
        private readonly KafkaNotificationService _kafkaNotificationService;

        public AttendanceRequestService(IDb db,
            ITimezoneConverter timezoneConverter,
            CurrentSession currentSession,
            ILogger<AttendanceRequestService> logger,
            KafkaNotificationService kafkaNotificationService)
        {
            _db = db;
            _timezoneConverter = timezoneConverter;
            _currentSession = currentSession;
            _logger = logger;
            _kafkaNotificationService = kafkaNotificationService;
        }

        private RequestModel GetEmployeeRequestedDataService(long employeeId, string procedure, ItemStatus itemStatus = ItemStatus.Pending)
        {
            if (employeeId < 0)
                throw new HiringBellException("Invalid employee id.");

            DateTime now = _timezoneConverter.ToSpecificTimezoneDateTime(_currentSession.TimeZone);
            if (itemStatus == ItemStatus.NotGenerated)
                itemStatus = 0;

            var resultSet = _db.FetchDataSet(procedure, new
            {
                ManagerId = employeeId,
                StatusId = (int)itemStatus,
                ForYear = now.Year,
                ForMonth = now.Month
            });

            if (resultSet != null && resultSet.Tables.Count != 3)
                throw new HiringBellException("Fail to get approval request data for current user.");

            DataTable attendanceTable = null;
            DataTable timsesheetTable = null;
            DataTable leaveTable = null;
            if (resultSet.Tables[1].Rows.Count > 0)
                attendanceTable = resultSet.Tables[1];

            if (resultSet.Tables[2].Rows.Count > 0)
                timsesheetTable = resultSet.Tables[2];

            if (resultSet.Tables[0].Rows.Count > 0)
                leaveTable = resultSet.Tables[0];

            return new RequestModel
            {
                ApprovalRequest = leaveTable,
                AttendaceTable = attendanceTable,
                TimesheetTable = timsesheetTable
            };
        }

        public RequestModel GetRequestPageData(long employeeId, int filterId)
        {
            if (filterId == ApplicationConstants.Only)
                return FetchPendingRequestService(_currentSession.CurrentUserDetail.UserId);
            else
                return GetManagerAndUnAssignedRequestService(_currentSession.CurrentUserDetail.UserId);
        }

        public RequestModel GetManagerAndUnAssignedRequestService(long employeeId)
        {
            return GetEmployeeRequestedDataService(employeeId, "sp_leave_timesheet_and_attendance_requests_get_by_role");
        }

        public RequestModel FetchPendingRequestService(long employeeId, ItemStatus itemStatus = ItemStatus.Pending)
        {
            return GetEmployeeRequestedDataService(employeeId, "sp_leave_timesheet_and_attendance_requests_get", itemStatus);
        }

        public async Task<RequestModel> ApproveAttendanceService(Attendance attendanceDetail, int filterId = ApplicationConstants.Only)
        {
            await UpdateAttendanceDetail(attendanceDetail, ItemStatus.Approved);
            return this.GetRequestPageData(_currentSession.CurrentUserDetail.UserId, filterId);
        }

        public async Task<RequestModel> RejectAttendanceService(Attendance attendanceDetail, int filterId = ApplicationConstants.Only)
        {
            await UpdateAttendanceDetail(attendanceDetail, ItemStatus.Rejected);
            return this.GetRequestPageData(_currentSession.CurrentUserDetail.UserId, filterId);
        }

        public async Task<RequestModel> UpdateAttendanceDetail(Attendance attendanceDetail, ItemStatus status)
        {
            try
            {
                RequestModel requestModel = null;
                if (attendanceDetail.AttendanceId <= 0)
                    throw new HiringBellException("Invalid attendance day selected");

                if (attendanceDetail.AttendenceDetailId <= 0)
                    throw new HiringBellException("Invalid attendance day selected");

                var attendance = _db.Get<Attendance>("sp_attendance_get_byid", new
                {
                    AttendanceId = attendanceDetail.AttendanceId
                });

                if (attendance == null)
                    throw new HiringBellException("Invalid attendance day selected");
                if (attendance.PendingRequestCount > 0)
                    attendance.PendingRequestCount = --attendance.PendingRequestCount;

                var allAttendance = JsonConvert.DeserializeObject<List<AttendanceDetailJson>>(attendance.AttendanceDetail);
                var currentAttendance = allAttendance.Find(x => x.AttendenceDetailId == attendanceDetail.AttendenceDetailId);
                if (currentAttendance == null)
                    throw new HiringBellException("Unable to update present request. Please contact to admin.");

                _logger.LogInformation("Attendance: " + currentAttendance.AttendanceDay);

                currentAttendance.PresentDayStatus = (int)status;
                //ChnageSessionType(currentAttendance);
                var Result = _db.Execute<Attendance>("sp_attendance_update_request", new
                {
                    AttendanceId = attendanceDetail.AttendanceId,
                    AttendanceDetail = JsonConvert.SerializeObject(allAttendance),
                    attendance.PendingRequestCount,
                    UserId = _currentSession.CurrentUserDetail.UserId
                }, true);

                if (string.IsNullOrEmpty(Result))
                    throw new HiringBellException("Unable to update attendance status");
                else
                    requestModel = FetchPendingRequestService(_currentSession.CurrentUserDetail.UserId, ItemStatus.Pending);

                AttendanceRequestModal attendanceRequestModal = new AttendanceRequestModal
                {
                    ActionType = status == ItemStatus.Approved ? ApplicationConstants.Approved : ApplicationConstants.Rejected,
                    CompanyName = _currentSession.CurrentUserDetail.CompanyName,
                    DayCount = 1,
                    DeveloperName = attendanceDetail.EmployeeName,
                    FromDate = attendanceDetail.AttendanceDay,
                    ManagerName = attendanceDetail.ManagerName,
                    Message = attendanceDetail.UserComments,
                    RequestType = attendanceDetail.WorkTypeId == WorkType.WORKFROMHOME ? ApplicationConstants.WorkFromHome : ApplicationConstants.WorkFromOffice,
                    ToAddress = new List<string> { attendance.Email },
                    kafkaServiceName = KafkaServiceName.Attendance
                };

                await _kafkaNotificationService.SendEmailNotification(attendanceRequestModal);

                return await Task.FromResult(requestModel);
            }
            catch (Exception)
            {
                throw new HiringBellException("Encounter error while sending email notification.", System.Net.HttpStatusCode.NotFound);
            }
        }

        public async Task<dynamic> GetAttendenceRequestDataServive(Attendance attendance)
        {
            if (attendance.ReportingManagerId == 0)
                throw new HiringBellException("Invalid reporting manager");

            if (attendance.ForMonth == 0)
                throw new HiringBellException("Month is invalid");

            if (attendance.ForYear == 0)
                throw new HiringBellException("Year is invalid");

            var result = _db.GetList<Attendance>("sp_attendance_requests_by_filter", new
            {
                attendance.ReportingManagerId,
                attendance.ForMonth,
                attendance.ForYear
            });

            if (result.Count == 0)
                return null;

            List<AttendanceDetailJson> attendanceRequest = new List<AttendanceDetailJson>();
            List<AutoCompleteEmployees> autoCompleteEmployees = new List<AutoCompleteEmployees>();
            result.ForEach(x =>
            {
                if (x.AttendanceDetail == null || x.AttendanceDetail == "[]")
                    throw new HiringBellException("Attendance detail not founf");

                var attendanceDetail = JsonConvert.DeserializeObject<List<AttendanceDetailJson>>(x.AttendanceDetail);
                if (x.ForMonth == attendance.ForMonth)
                    attendanceDetail = attendanceDetail.Take(DateTime.Now.Day).ToList<AttendanceDetailJson>();

                attendanceRequest.AddRange(attendanceDetail);
                if (autoCompleteEmployees.Find(i => i.value == x.EmployeeId) == null)
                {
                    autoCompleteEmployees.Add(new AutoCompleteEmployees
                    {
                        email = x.Email,
                        text = x.EmployeeName,
                        value = x.EmployeeId,
                        selected = false
                    });
                }
            });

            int recordsPerPage = 10;
            int pageNumber = 1;
            List<AttendanceDetailJson> filteredAttendance = new List<AttendanceDetailJson>();
            if (pageNumber > 0)
                filteredAttendance = attendanceRequest.Skip((pageNumber - 1) * recordsPerPage).Take(recordsPerPage).ToList();

            return await Task.FromResult(new { FilteredAttendance = filteredAttendance, AutoCompleteEmployees = autoCompleteEmployees });
        }

        private void ChnageSessionType(AttendanceDetailJson currentAttr)
        {
            var logoff = currentAttr.LogOff;
            var logofftime = logoff.Replace(":", ".");
            decimal time = decimal.Parse(logofftime);
            if (currentAttr.SessionType == 1)
            {

            }
            else
            {
                var totaltime = (int)((time * 60) * 2);
                currentAttr.LogOff = ConvertToMin(totaltime);
                currentAttr.LogOn = ConvertToMin(totaltime + 60);
                currentAttr.SessionType = 1;
                currentAttr.TotalMinutes = currentAttr.TotalMinutes * 2;
            }
        }

        private String ConvertToMin(int mins)
        {
            int hours = ((mins - mins % 60) / 60);
            string min = ((mins - hours * 60)).ToString();
            string hrs = hours.ToString();
            if (hrs.Length == 1)
                hrs = "0" + hrs;
            if (min.Length == 1)
                min = min + "0";
            return "" + hrs + ":" + min;
        }

        public List<Attendance> ReAssigneAttendanceService(AttendenceDetail attendanceDetail)
        {
            return null;
        }
    }
}
