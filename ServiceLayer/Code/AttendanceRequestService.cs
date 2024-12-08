using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Interface;
using Bt.Lib.Common.Service.KafkaService.interfaces;
using Bt.Lib.Common.Service.Model;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EMailService.Modal;
using Microsoft.Extensions.Logging;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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
        private readonly IKafkaProducerService _kafkaProducerService;

        public AttendanceRequestService(IDb db,
            ITimezoneConverter timezoneConverter,
            CurrentSession currentSession,
            ILogger<AttendanceRequestService> logger,
            IKafkaProducerService kafkaProducerService)
        {
            _db = db;
            _timezoneConverter = timezoneConverter;
            _currentSession = currentSession;
            _logger = logger;
            _kafkaProducerService = kafkaProducerService;
        }

        private RequestModel GetEmployeeRequestedDataService(long employeeId, string procedure, ItemStatus itemStatus = ItemStatus.Pending)
        {
            if (employeeId < 0)
                throw new HiringBellException("Invalid employee id.");

            DateTime date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var FromDate = _timezoneConverter.ToUtcTime(date.AddMonths(-1), _currentSession.TimeZone);
            var ToDate = _timezoneConverter.ToUtcTime(date.AddMonths(1).AddDays(-1), _currentSession.TimeZone);
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

        public async Task<dynamic> ApproveAttendanceService(List<DailyAttendance> dailyAttendances, int filterId = ApplicationConstants.Only)
        {
            await UpdateAttendanceDetail(dailyAttendances, ItemStatus.Approved);
            var attr = dailyAttendances.FirstOrDefault();

            Attendance attendance = new Attendance
            {
                ForMonth = DateTime.Now.Month,
                ForYear = DateTime.Now.Year,
                ReportingManagerId = _currentSession.CurrentUserDetail.UserId,
                PageIndex = attr.PageIndex,
                PresentDayStatus = filterId,
                EmployeeId = attr.EmployeeId,
                TotalDays = attr.TotalDays
            };

            return await this.GetAttendanceRequestDataService(attendance);
        }

        public async Task<dynamic> RejectAttendanceService(List<DailyAttendance> dailyAttendances, int filterId = ApplicationConstants.Only)
        {
            await UpdateAttendanceDetail(dailyAttendances, ItemStatus.Rejected);
            var attr = dailyAttendances.FirstOrDefault();

            Attendance attendance = new Attendance
            {
                ForMonth = DateTime.Now.Month,
                ForYear = DateTime.Now.Year,
                ReportingManagerId = _currentSession.CurrentUserDetail.UserId,
                PageIndex = attr.PageIndex,
                PresentDayStatus = filterId,
                EmployeeId = attr.EmployeeId,
                TotalDays = attr.TotalDays
            };

            return await this.GetAttendanceRequestDataService(attendance);
        }

        public async Task UpdateAttendanceDetail(List<DailyAttendance> dailyAttendances, ItemStatus status)
        {
            try
            {
                foreach (var dailyAttendance in dailyAttendances)
                {
                    if (dailyAttendance.AttendanceId <= 0)
                        throw new HiringBellException("Invalid attendance day selected");

                    var attendance = _db.Get<DailyAttendance>(Procedures.Attendance_Get_ById, new
                    {
                        dailyAttendance.AttendanceId
                    });

                    if (attendance == null)
                        throw new HiringBellException("Invalid attendance day selected");

                    await updateDailyAttendanceData(status, attendance, dailyAttendance.UserComment);

                    var Result = await _db.ExecuteAsync(Procedures.Attendance_Update_Request, new
                    {
                        attendance.AttendanceId,
                        attendance.ReviewerId,
                        attendance.ReviewerEmail,
                        attendance.ReviewerName,
                        attendance.AttendanceStatus,
                        attendance.Comments,
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
                        Message = dailyAttendance.UserComment,
                        RequestType = dailyAttendance.WorkTypeId == WorkType.WORKFROMHOME ? ApplicationConstants.WorkFromHome : ApplicationConstants.WorkFromOffice,
                        ToAddress = new List<string> { attendance.EmployeeEmail },
                        kafkaServiceName = KafkaServiceName.Attendance,
                        LocalConnectionString = _currentSession.LocalConnectionString,
                        CompanyId = _currentSession.CurrentUserDetail.CompanyId
                    };

                    await _kafkaProducerService.SendEmailNotification(attendanceRequestModal, KafkaTopicNames.ATTENDANCE_REQUEST_ACTION);
                }

                await Task.CompletedTask;
            }
            catch (Exception e)
            {
                _logger.LogError($"Server error: {e.Message}");
                throw HiringBellException.ThrowBadRequest($"Server error: Please contact to admin.");
            }
        }

        private async Task updateDailyAttendanceData(ItemStatus status, DailyAttendance attendance, string comment)
        {
            attendance.AttendanceStatus = (int)status;
            attendance.ReviewerId = _currentSession.CurrentUserDetail.UserId;
            attendance.ReviewerEmail = _currentSession.CurrentUserDetail.EmailId;
            attendance.ReviewerName = _currentSession.CurrentUserDetail.FirstName + " " + _currentSession.CurrentUserDetail.LastName;

            if (status == ItemStatus.Rejected)
            {
                var existingComments = new List<AttendanceComment>();
                AttendanceComment attendanceComment = new AttendanceComment
                {
                    EmployeeId = _currentSession.CurrentUserDetail.UserId,
                    Email = _currentSession.CurrentUserDetail.EmailId,
                    Name = _currentSession.CurrentUserDetail.FirstName + " " + _currentSession.CurrentUserDetail.LastName,
                    Comment = comment
                };

                if (!string.IsNullOrEmpty(attendanceComment.Comment) && attendanceComment.Comment == "[]")
                    existingComments = JsonConvert.DeserializeObject<List<AttendanceComment>>(attendance.Comments);

                existingComments.Add(attendanceComment);
                attendance.Comments = JsonConvert.SerializeObject(existingComments);
            }

            await Task.CompletedTask;
        }

        public async Task<dynamic> GetAttendanceRequestDataService(Attendance attendance)
        {
            bool isWeeklyAttendance = true;
            if (attendance.ReportingManagerId == 0)
                throw new HiringBellException("Invalid reporting manager");

            if (attendance.ForMonth == 0)
                throw new HiringBellException("Month is invalid");

            if (attendance.ForYear == 0)
                throw new HiringBellException("Year is invalid");

            DateTime FromDate, ToDate;
            GetAttendanceFromAndToDate(attendance, out FromDate, out ToDate);

            var result = _db.GetList<DailyAttendance>(Procedures.Attendance_Requests_By_Filter, new
            {
                attendance.ReportingManagerId,
                FromDate,
                ToDate,
                AttendanceStatus = attendance.PresentDayStatus
            });

            if (result.Count == 0)
                return null;

            List<AutoCompleteEmployees> autoCompleteEmployees = GetEmployeeAutoComplete(result);
            if (isWeeklyAttendance)
            {
                var filteredAttendance = FilterAndPagingWeeklyAttendanceRecord(attendance, result);
                return await Task.FromResult(new { FilteredAttendance = filteredAttendance, AutoCompleteEmployees = autoCompleteEmployees });
            }
            else
            {
                var filteredAttendance = FilterAndPagingDailyAttendanceRecord(attendance, result);
                return await Task.FromResult(new { FilteredAttendance = filteredAttendance, AutoCompleteEmployees = autoCompleteEmployees });
            }
        }

        private List<AutoCompleteEmployees> GetEmployeeAutoComplete(List<DailyAttendance> result)
        {
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
            return autoCompleteEmployees;
        }

        private void GetAttendanceFromAndToDate(Attendance attendance, out DateTime FromDate, out DateTime ToDate)
        {
            FromDate = default;
            ToDate = default;

            if (attendance.TotalDays > 0)
            {
                var currentDate = DateTime.Now;
                var date = new DateTime(currentDate.Year, currentDate.Month, 1);
                ToDate = _timezoneConverter.ToUtcTime(date, _currentSession.TimeZone);
                FromDate = ToDate.AddDays(-1 * attendance.TotalDays);
            }
            else
            {
                var date = new DateTime(attendance.ForYear, attendance.ForMonth, 1).AddMonths(-1);
                if (date.DayOfWeek != DayOfWeek.Monday)
                {
                    int days = (int)date.DayOfWeek - (int)DayOfWeek.Monday;
                    if (days < 0)
                        days += 7;

                    date = date.AddDays(-days);
                }

                FromDate = _timezoneConverter.ToUtcTime(date, _currentSession.TimeZone);
                ToDate = _timezoneConverter.ToUtcTime(date.AddMonths(2).AddDays(-1), _currentSession.TimeZone);
            }
        }

        private List<DailyAttendance> FilterAndPagingDailyAttendanceRecord(Attendance attendance, List<DailyAttendance> attendanceRequest)
        {
            int recordsPerPage = 10;
            int totalRecord = 0;
            attendanceRequest = FilterAttendanceRecord(attendance, attendanceRequest);

            totalRecord = attendanceRequest.Count;
            if (attendance.PageIndex > 0)
                attendanceRequest = attendanceRequest.Skip((attendance.PageIndex - 1) * recordsPerPage).Take(recordsPerPage).ToList();

            attendanceRequest.ForEach(i => i.Total = totalRecord);
            return attendanceRequest;
        }

        private Dictionary<long, List<DailyAttendance>> FilterAndPagingWeeklyAttendanceRecord(Attendance attendance, List<DailyAttendance> attendanceRequest)
        {
            int recordsPerPage = 10;
            attendanceRequest = FilterAttendanceRecord(attendance, attendanceRequest);

            Dictionary<long, List<DailyAttendance>> attendanceGroupedRequest = default;
            if (attendance.PageIndex > 0)
            {
                var groupRecord = attendanceRequest.GroupBy(x => x.EmployeeId).SelectMany(i => i.OrderBy(x => _timezoneConverter.ToTimeZoneDateTime(x.AttendanceDate, _currentSession.TimeZone))
                .GroupBy(r => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(_timezoneConverter.ToTimeZoneDateTime(r.AttendanceDate, _currentSession.TimeZone), CalendarWeekRule.FirstDay, DayOfWeek.Monday)));
                var currentPageAttendance = groupRecord.Skip((attendance.PageIndex - 1) * recordsPerPage).Take(recordsPerPage);
                attendanceGroupedRequest = currentPageAttendance.ToDictionary(x => x.First().AttendanceId, x => x.ToList());
                foreach (var record in attendanceGroupedRequest)
                {
                    record.Value.ForEach(x =>
                    {
                        x.Total = groupRecord.Count();
                    });
                }
            }

            return attendanceGroupedRequest;
        }

        private List<DailyAttendance> FilterAttendanceRecord(Attendance attendance, List<DailyAttendance> attendanceRequest)
        {
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