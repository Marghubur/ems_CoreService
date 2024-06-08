using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Interface;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EMailService.Modal;
using Microsoft.Extensions.Logging;
using ModalLayer.Modal;
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

            DateTime date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var FromDate = _timezoneConverter.ToUtcTime(date.AddMonths(-1));
            var ToDate = _timezoneConverter.ToUtcTime(date.AddMonths(1).AddDays(-1));
            if (itemStatus == ItemStatus.NotGenerated)
                itemStatus = 0;

            var resultSet = _db.FetchDataSet(procedure, new
            {
                ManagerId = employeeId,
                StatusId = (int)itemStatus,
                FromDate,
                ToDate
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
            return GetEmployeeRequestedDataService(employeeId, Procedures.LEAVE_TIMESHEET_AND_ATTENDANCE_REQUESTS_GET_BY_ROLE);
        }

        public RequestModel FetchPendingRequestService(long employeeId, ItemStatus itemStatus = ItemStatus.Pending)
        {
            return GetEmployeeRequestedDataService(employeeId, Procedures.LEAVE_TIMESHEET_AND_ATTENDANCE_REQUESTS_GET, itemStatus);
        }

        public async Task<dynamic> ApproveAttendanceService(DailyAttendance dailyAttendance, int filterId = ApplicationConstants.Only)
        {
            await UpdateAttendanceDetail(dailyAttendance, ItemStatus.Approved);
            Attendance attendance = new Attendance
            {
                ForMonth = dailyAttendance.AttendanceDate.Month,
                ForYear = dailyAttendance.AttendanceDate.Year,
                ReportingManagerId = _currentSession.CurrentUserDetail.UserId,
                PageIndex = dailyAttendance.PageIndex,
                PresentDayStatus = filterId,
                EmployeeId = dailyAttendance.EmployeeId,
                TotalDays = dailyAttendance.TotalDays
            };
            return await this.GetAttendenceRequestDataServive(attendance);
        }

        public async Task<dynamic> RejectAttendanceService(DailyAttendance dailyAttendance, int filterId = ApplicationConstants.Only)
        {
            await UpdateAttendanceDetail(dailyAttendance, ItemStatus.Rejected);
            Attendance attendance = new Attendance
            {
                ForMonth = DateTime.Now.Month,
                ForYear = DateTime.Now.Year,
                ReportingManagerId = _currentSession.CurrentUserDetail.UserId,
                PageIndex = dailyAttendance.PageIndex,
                PresentDayStatus = filterId,
                EmployeeId = dailyAttendance.EmployeeId,
                TotalDays = dailyAttendance.TotalDays
            };
            return await this.GetAttendenceRequestDataServive(attendance);
        }

        public async Task UpdateAttendanceDetail(DailyAttendance dailyAttendance, ItemStatus status)
        {
            try
            {
                if (dailyAttendance.AttendanceId <= 0)
                    throw new HiringBellException("Invalid attendance day selected");

                var attendance = _db.Get<DailyAttendance>(Procedures.Attendance_Get_ById, new
                {
                    dailyAttendance.AttendanceId
                });

                if (attendance == null)
                    throw new HiringBellException("Invalid attendance day selected");

                attendance.AttendanceStatus = (int)status;
                attendance.ReviewerId = _currentSession.CurrentUserDetail.UserId;
                attendance.ReviewerEmail = _currentSession.CurrentUserDetail.EmailId;
                attendance.ReviewerName = _currentSession.CurrentUserDetail.FirstName + " " + _currentSession.CurrentUserDetail.LastName;
                var Result = await _db.ExecuteAsync(Procedures.Attendance_Update_Request, new
                {
                    attendance.AttendanceId,
                    attendance.ReviewerId,
                    attendance.ReviewerEmail,
                    attendance.ReviewerName,
                    attendance.AttendanceStatus,
                    _currentSession.CurrentUserDetail.UserId
                }, true);

                if (string.IsNullOrEmpty(Result.statusMessage))
                {
                    throw HiringBellException.ThrowBadRequest("Unable to update attendance status");
                }

                AttendanceRequestModal attendanceRequestModal = new AttendanceRequestModal
                {
                    ActionType = status == ItemStatus.Approved ? ApplicationConstants.Approved : ApplicationConstants.Rejected,
                    CompanyName = _currentSession.CurrentUserDetail.CompanyName,
                    DayCount = 1,
                    DeveloperName = dailyAttendance.EmployeeName,
                    FromDate = dailyAttendance.AttendanceDate,
                    ManagerName = dailyAttendance.ManagerName,
                    Message = dailyAttendance.Comments,
                    RequestType = dailyAttendance.WorkTypeId == WorkType.WORKFROMHOME ? ApplicationConstants.WorkFromHome : ApplicationConstants.WorkFromOffice,
                    ToAddress = new List<string> { attendance.EmployeeEmail },
                    kafkaServiceName = KafkaServiceName.Attendance,
                    LocalConnectionString = _currentSession.LocalConnectionString,
                    CompanyId = _currentSession.CurrentUserDetail.CompanyId
                };

                await _kafkaNotificationService.SendEmailNotification(attendanceRequestModal);

                await Task.CompletedTask;
            }
            catch (Exception e)
            {
                _logger.LogError($"Server error: {e.Message}");
                throw HiringBellException.ThrowBadRequest($"Server error: Please contact to admin.");
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

            var date = new DateTime(attendance.ForYear, attendance.ForMonth, 1).AddMonths(-1);
            var FromDate = _timezoneConverter.ToUtcTime(date);
            var ToDate = _timezoneConverter.ToUtcTime(date.AddMonths(2).AddDays(-1));

            var result = _db.GetList<DailyAttendance>(Procedures.Attendance_Requests_By_Filter, new
            {
                attendance.ReportingManagerId,
                FromDate,
                ToDate
            });

            if (result.Count == 0)
                return null;

            List<AutoCompleteEmployees> autoCompleteEmployees = new List<AutoCompleteEmployees>();
            result.ForEach(x =>
            {
                if (autoCompleteEmployees.Find(i => i.value == x.EmployeeId) == null)
                {
                    autoCompleteEmployees.Add(new AutoCompleteEmployees
                    {
                        email = x.EmployeeEmail,
                        text = x.EmployeeName,
                        value = x.EmployeeId,
                        selected = false
                    });
                }
            });
            List<DailyAttendance> filteredAttendance = FilterAndPagingAttendanceRecord(attendance, result);
            return await Task.FromResult(new { FilteredAttendance = filteredAttendance, AutoCompleteEmployees = autoCompleteEmployees });
        }

        private List<DailyAttendance> FilterAndPagingAttendanceRecord(Attendance attendance, List<DailyAttendance> attendanceRequest)
        {
            int recordsPerPage = 10;
            int totalRecord = 0;
            attendanceRequest = attendanceRequest.OrderBy(x => x.AttendanceDate).ToList();
            if (attendance.PresentDayStatus != 0)
                attendanceRequest = attendanceRequest.FindAll(x => x.AttendanceStatus == attendance.PresentDayStatus);

            if (attendance.EmployeeId != 0)
                attendanceRequest = attendanceRequest.FindAll(x => x.EmployeeId == attendance.EmployeeId);

            if (attendance.TotalDays > 0)
            {
                DateTime lastDate = DateTime.Now.AddDays(attendance.TotalDays * -1);
                attendanceRequest = attendanceRequest.FindAll(x => x.AttendanceDate.Subtract(lastDate).TotalDays >= 0);
            }

            totalRecord = attendanceRequest.Count;
            if (attendance.PageIndex > 0)
                attendanceRequest = attendanceRequest.Skip((attendance.PageIndex - 1) * recordsPerPage).Take(recordsPerPage).ToList();

            attendanceRequest.ForEach(i => i.Total = totalRecord);
            return attendanceRequest;
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
