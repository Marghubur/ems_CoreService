using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using Confluent.Kafka;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EMailService.Service;
using ModalLayer;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Code.SendEmail;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace ServiceLayer.Code
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IDb _db;
        private readonly CurrentSession _currentSession;
        private readonly ITimezoneConverter _timezoneConverter;
        private readonly AttendanceEmailService _attendanceEmailService;
        private readonly ICompanyService _companyService;
        private readonly IEMailManager _eMailManager;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly ProducerConfig _producerConfig;
        private readonly KafkaNotificationService _kafkaNotificationService;

        public AttendanceService(IDb db,
            ITimezoneConverter timezoneConverter,
            CurrentSession currentSession,
            ICompanyService companyService,
            AttendanceEmailService attendanceEmailService,
            IEMailManager eMailManager,
            FileLocationDetail fileLocationDetail,
            ProducerConfig producerConfig,
            KafkaNotificationService kafkaNotificationService)
        {
            _db = db;
            _companyService = companyService;
            _currentSession = currentSession;
            _timezoneConverter = timezoneConverter;
            _attendanceEmailService = attendanceEmailService;
            _eMailManager = eMailManager;
            _fileLocationDetail = fileLocationDetail;
            _producerConfig = producerConfig;
            _kafkaNotificationService = kafkaNotificationService;
        }

        private DateTime GetBarrierDate(int limit)
        {
            int i = limit;
            DateTime todayDate = DateTime.UtcNow.Date;
            while (true && i > 0)
            {
                todayDate = todayDate.AddDays(-1);
                switch (todayDate.DayOfWeek)
                {
                    case DayOfWeek.Saturday:
                    case DayOfWeek.Sunday:
                        break;
                    default:
                        i--;
                        break;
                }

                if (i == 0)
                    break;
            }

            if (i > 0)
                todayDate = todayDate.AddDays(-1 * i);

            return todayDate;
        }

        private async Task<List<AttendanceDetailJson>> BuildApprovedAttendance(AttendanceDetailBuildModal attendanceModal, DateTime dateOfJoining)
        {
            List<AttendanceDetailJson> attendenceDetails = new List<AttendanceDetailJson>();
            var timezoneFirstDate = _timezoneConverter.ToTimeZoneDateTime(attendanceModal.firstDate, _currentSession.TimeZone);
            int totalNumOfDaysInPresentMonth = DateTime.DaysInMonth(timezoneFirstDate.Year, timezoneFirstDate.Month);

            double days = 0;
            var barrierDate = GetBarrierDate(attendanceModal.attendanceSubmissionLimit);
            if (timezoneFirstDate.Day > 1)
                totalNumOfDaysInPresentMonth = totalNumOfDaysInPresentMonth - timezoneFirstDate.Day;

            int weekDays = 0;
            int totalMinute = 0;
            int i = 0;
            DateTime workingDate = timezoneFirstDate;
            while (i < totalNumOfDaysInPresentMonth)
            {
                workingDate = timezoneFirstDate.AddDays(i);
                var isHoliday = CheckIsHoliday(workingDate, attendanceModal.calendars);
                var isWeekend = CheckWeekend(attendanceModal.shiftDetail, workingDate);
                var officetime = attendanceModal.shiftDetail.OfficeTime;
                var logoff = CalculateLogOff(attendanceModal.shiftDetail.OfficeTime, attendanceModal.shiftDetail.LunchDuration);

                days = barrierDate.Date.Subtract(workingDate.Date).TotalDays;
                totalMinute = attendanceModal.shiftDetail.Duration;
                var presentDayStatus = (int)DayStatus.Empty;
                if (isHoliday || isWeekend)
                {
                    officetime = "00:00";
                    logoff = "00:00";
                    totalMinute = 0;
                }

                if (isHoliday)
                    presentDayStatus = (int)DayStatus.Holiday;
                else if (isWeekend)
                    presentDayStatus = (int)DayStatus.Weekend;
                else
                {
                    if (workingDate.Month == dateOfJoining.Month && workingDate.Year == dateOfJoining.Year)
                    {
                        if (workingDate.Day >= dateOfJoining.Day)
                            presentDayStatus = (int)ItemStatus.Approved;
                        else
                            presentDayStatus = (int)ItemStatus.NotSubmitted;
                    }
                    else
                    {
                        presentDayStatus = (int)ItemStatus.Approved;
                    }
                }

                attendenceDetails.Add(new AttendanceDetailJson
                {
                    AttendenceDetailId = workingDate.Day,
                    IsHoliday = isHoliday,
                    IsOnLeave = false,
                    IsWeekend = isWeekend,
                    AttendanceDay = workingDate,
                    LogOn = officetime,
                    LogOff = logoff,
                    PresentDayStatus = presentDayStatus,
                    UserComments = string.Empty,
                    ApprovedName = String.Empty,
                    ApprovedBy = 0,
                    SessionType = 1,
                    TotalMinutes = totalMinute,
                    IsOpen = i >= days ? true : false,
                    Emails = "[]"
                });

                i++;
            }

            var result = await _db.ExecuteAsync("sp_attendance_insupd", new
            {
                AttendanceId = 0,
                AttendanceDetail = JsonConvert.SerializeObject(attendenceDetails),
                UserTypeId = (int)UserType.Employee,
                EmployeeId = attendanceModal.employee.EmployeeUid,
                TotalDays = totalNumOfDaysInPresentMonth,
                TotalWeekDays = weekDays,
                DaysPending = totalNumOfDaysInPresentMonth,
                TotalBurnedMinutes = 0,
                ForYear = attendanceModal.firstDate.AddDays(1).Year,
                ForMonth = attendanceModal.firstDate.AddDays(1).Month,
                UserId = _currentSession.CurrentUserDetail.UserId,
                PendingRequestCount = 0,
                ReportingManagerId = attendanceModal.employee.ReportingManagerId,
                ManagerName = _currentSession.CurrentUserDetail.ManagerName,
                Mobile = attendanceModal.employee.Mobile,
                Email = attendanceModal.employee.Email,
                EmployeeName = attendanceModal.employee.FirstName + " " + attendanceModal.employee.LastName,
                AttendenceStatus = (int)DayStatus.WorkFromOffice,
                BillingHours = 0,
                ClientId = 0,
                LunchBreanInMinutes = attendanceModal.shiftDetail.LunchDuration
            }, true);

            if (string.IsNullOrEmpty(result.statusMessage))
                throw HiringBellException.ThrowBadRequest("Got server error. Please contact to admin.");
            attendanceModal.attendance.AttendanceId = Convert.ToInt64(result.statusMessage);
            return attendenceDetails;
        }

        private async Task<List<AttendanceDetailJson>> CreateAttendanceTillDate(AttendanceDetailBuildModal attendanceModal)
        {
            List<AttendanceDetailJson> attendenceDetails = new List<AttendanceDetailJson>();
            var timezoneFirstDate = _timezoneConverter.ToTimeZoneDateTime(attendanceModal.firstDate, _currentSession.TimeZone);
            int totalNumOfDaysInPresentMonth = DateTime.DaysInMonth(timezoneFirstDate.Year, timezoneFirstDate.Month);

            double days = 0;
            var barrierDate = GetBarrierDate(attendanceModal.attendanceSubmissionLimit);
            if (timezoneFirstDate.Day > 1)
                totalNumOfDaysInPresentMonth = totalNumOfDaysInPresentMonth - timezoneFirstDate.Day;

            int weekDays = 0;
            int totalMinute = 0;
            int i = 0;
            DateTime workingDate = timezoneFirstDate;
            while (i < totalNumOfDaysInPresentMonth)
            {
                workingDate = timezoneFirstDate.AddDays(i);
                var isHoliday = CheckIsHoliday(workingDate, attendanceModal.calendars);
                var isWeekend = CheckWeekend(attendanceModal.shiftDetail, workingDate);
                var officetime = attendanceModal.shiftDetail.OfficeTime;
                var logoff = CalculateLogOff(attendanceModal.shiftDetail.OfficeTime, attendanceModal.shiftDetail.LunchDuration);
                days = barrierDate.Date.Subtract(workingDate.Date).TotalDays;
                totalMinute = attendanceModal.shiftDetail.Duration;
                var presentDayStatus = (int)DayStatus.Empty;
                if (isHoliday || isWeekend)
                {
                    officetime = "00:00";
                    logoff = "00:00";
                    totalMinute = 0;
                }

                var appliedFlag = attendanceModal.compalintOrRequests
                                    .Any(x => x.AttendanceDate.Date.Subtract(attendanceModal.firstDate.AddDays(i).Date)
                                    .TotalDays == 0);
                if (isHoliday)
                    presentDayStatus = (int)DayStatus.Holiday;
                else if (isWeekend)
                    presentDayStatus = (int)DayStatus.Weekend;
                else if (appliedFlag)
                    presentDayStatus = (int)ItemStatus.MissingAttendanceRequest;

                attendenceDetails.Add(new AttendanceDetailJson
                {
                    AttendenceDetailId = workingDate.Day,
                    AttendanceDay = workingDate,
                    LogOn = officetime,
                    LogOff = logoff,
                    PresentDayStatus = presentDayStatus,
                    UserComments = string.Empty,
                    ApprovedName = String.Empty,
                    ApprovedBy = 0,
                    SessionType = 1,
                    TotalMinutes = totalMinute,
                    IsOpen = i >= days ? true : false
                });

                i++;
            }

            var result = await _db.ExecuteAsync("sp_attendance_insupd", new
            {
                AttendanceId = 0,
                AttendanceDetail = JsonConvert.SerializeObject(attendenceDetails),
                UserTypeId = (int)UserType.Employee,
                EmployeeId = attendanceModal.employee.EmployeeUid,
                TotalDays = totalNumOfDaysInPresentMonth,
                TotalWeekDays = weekDays,
                DaysPending = totalNumOfDaysInPresentMonth,
                TotalBurnedMinutes = 0,
                ForYear = attendanceModal.firstDate.AddDays(1).Year,
                ForMonth = attendanceModal.firstDate.AddDays(1).Month,
                UserId = _currentSession.CurrentUserDetail.UserId,
                PendingRequestCount = 0,
                ReportingManagerId = attendanceModal.employee.ReportingManagerId,
                ManagerName = _currentSession.CurrentUserDetail.ManagerName,
                Mobile = attendanceModal.employee.Mobile,
                Email = attendanceModal.employee.Email,
                EmployeeName = attendanceModal.employee.FirstName + " " + attendanceModal.employee.LastName,
                AttendenceStatus = (int)DayStatus.WorkFromOffice,
                BillingHours = 0,
                ClientId = 0,
                LunchBreanInMinutes = attendanceModal.shiftDetail.LunchDuration
            }, true);

            if (string.IsNullOrEmpty(result.statusMessage))
                throw HiringBellException.ThrowBadRequest("Got server error. Please contact to admin.");
            attendanceModal.attendance.AttendanceId = Convert.ToInt64(result.statusMessage);
            return attendenceDetails;
        }

        private string CalculateLogOff(string OfficeTime, int LunchDuration)
        {
            var logontime = OfficeTime.Replace(":", ".");
            decimal logon = decimal.Parse(logontime);
            var totaltime = 0;
            totaltime = (int)(logon * 60 - LunchDuration);
            return ConvertToMin(totaltime);
        }

        private bool CheckWeekend(ShiftDetail shiftDetail, DateTime date)
        {
            bool flag = false;
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    flag = !shiftDetail.IsMon;
                    break;
                case DayOfWeek.Tuesday:
                    flag = !shiftDetail.IsTue;
                    break;
                case DayOfWeek.Wednesday:
                    flag = !shiftDetail.IsWed;
                    break;
                case DayOfWeek.Thursday:
                    flag = !shiftDetail.IsThu;
                    break;
                case DayOfWeek.Friday:
                    flag = !shiftDetail.IsFri;
                    break;
                case DayOfWeek.Saturday:
                    flag = !shiftDetail.IsSat;
                    break;
                case DayOfWeek.Sunday:
                    flag = !shiftDetail.IsSun;
                    break;
            }

            return flag;
        }

        private bool CheckIsHoliday(DateTime date, List<Calendar> calendars)
        {
            bool flag = false;

            var records = calendars.FirstOrDefault(x => x.StartDate.Date >= date.Date && x.EndDate.Date <= date.Date);
            if (records != null)
                flag = true;

            return flag;
        }

        public async Task GenerateAttendanceService()
        {
            List<Employee> employees = _db.GetList<Employee>("SP_Employee_GetAll", new
            {
                SearchString = "1=1",
                SortBy = string.Empty,
                PageIndex = 1,
                PageSize = 500
            });

            _currentSession.TimeZone = TZConvert.GetTimeZoneInfo("India Standard Time");
            _currentSession.CurrentUserDetail.CompanyId = 1;
            DateTime workingDate = DateTime.UtcNow;
            foreach (var employee in employees)
            {
                workingDate = employee.CreatedOn;

                if (workingDate.Day != 1)
                    workingDate = workingDate.AddDays(-1 * workingDate.Day + 1);

                var presentMonth = DateTime.UtcNow.AddMonths(1);
                if (presentMonth.Day != 1)
                    presentMonth = presentMonth.AddDays(-1 * presentMonth.Day + 1);

                while (workingDate.Date.Subtract(presentMonth.Date).TotalDays < 0)
                {
                    Attendance attendance = new Attendance
                    {
                        AttendanceDay = workingDate,
                        ForMonth = workingDate.Month,
                        ForYear = workingDate.Year,
                        EmployeeId = employee.EmployeeUid
                    };

                    var record = _db.Get<Attendance>("sp_attendance_get_by_empid", new
                    {
                        EmployeeId = employee.EmployeeUid,
                        ForYear = workingDate.Year,
                        ForMonth = workingDate.Month
                    });

                    if (record == null || string.IsNullOrEmpty(record.AttendanceDetail) || record.AttendanceDetail == "[]")
                    {
                        List<AttendanceDetailJson> attendenceDetails;
                        var attendanceModal = GetAttendanceDetail(attendance, out attendenceDetails);

                        attendanceModal.attendanceSubmissionLimit = 2;
                        await BuildApprovedAttendance(attendanceModal, employee.CreatedOn);

                        workingDate = workingDate.AddMonths(1);

                        if (workingDate.Day != 1)
                            workingDate = workingDate.AddDays(-1 * workingDate.Day + 1);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            await Task.CompletedTask;
        }

        public async Task<AttendanceWithClientDetail> GetAttendanceByUserId(Attendance attendance)
        {
            if (attendance.EmployeeId == 0)
                throw HiringBellException.ThrowBadRequest("EMployee id is invalid");

            List<AttendanceDetailJson> attendenceDetails;
            AttendanceDetailBuildModal attendanceDetailBuildModal = GetAttendanceDetail(attendance, out attendenceDetails);

            attendanceDetailBuildModal.attendanceSubmissionLimit = attendanceDetailBuildModal.employee.AttendanceSubmissionLimit;
            if (attendanceDetailBuildModal.attendance.AttendanceDetail == null || attendanceDetailBuildModal.attendance.AttendanceDetail == "[]" ||
                attendanceDetailBuildModal.attendance.AttendanceDetail.Count() == 0)
            {
                attendanceDetailBuildModal.firstDate = (DateTime)attendance.AttendanceDay;
                var nowDate = attendance.AttendanceDay.AddDays(1);

                if (nowDate.Year == attendanceDetailBuildModal.employee.CreatedOn.Year && nowDate.Month == attendanceDetailBuildModal.employee.CreatedOn.Month)
                {
                    var days = attendanceDetailBuildModal.employee.CreatedOn.Date.Subtract(nowDate.Date).TotalDays;
                    attendanceDetailBuildModal.firstDate = attendanceDetailBuildModal.firstDate.AddDays(days);
                }

                attendenceDetails = await CreateAttendanceTillDate(attendanceDetailBuildModal);
            }

            var attendances = attendenceDetails.OrderBy(i => i.AttendanceDay)
                                .TakeWhile(x => DateTime.Now.Date.Subtract(x.AttendanceDay.Date).TotalDays >= 0)
                                .OrderByDescending(i => i.AttendanceDay)
                                .ToList();

            //attendances = attendances.OrderByDescending(i => i.AttendanceDay).ToList();
            var leave = _db.GetList<LeaveRequestNotification>("sp_leave_request_notification_get_by_empid", new
            {
                EmployeeId = attendance.EmployeeId,
                RequestStatusId = (int)ItemStatus.Approved
            });

            int daysLimit = attendanceDetailBuildModal.attendanceSubmissionLimit + 1;
            if (attendances.Count == DateTime.UtcNow.Day || DateTime.UtcNow.Day < daysLimit)
            {
                //if (DateTime.UtcNow.Day < daysLimit)
                //    daysLimit = daysLimit - DateTime.UtcNow.Day;

                foreach (var item in attendances)
                {
                    if (!CheckWeekend(attendanceDetailBuildModal.shiftDetail, item.AttendanceDay))
                    {
                        if (daysLimit > 0)
                            item.IsOpen = true;
                        else
                            item.IsOpen = false;

                    }
                    daysLimit--;
                }

            }
            if (leave != null && leave.Count > 0)
            {
                foreach (var item in attendances)
                {
                    var leaveDetail = leave.Find(x => x.FromDate.Date.Subtract(item.AttendanceDay.Date).TotalDays <= 0 && x.ToDate.Date.Subtract(item.AttendanceDay.Date).TotalDays >= 0);
                    if (leaveDetail != null && leaveDetail.RequestStatusId == (int)ItemStatus.Approved)
                    {
                        item.IsOnLeave = true;
                        item.IsOpen = false;
                    }
                }
            }

            return new AttendanceWithClientDetail
            {
                EmployeeDetail = attendanceDetailBuildModal.employee,
                AttendanceId = attendanceDetailBuildModal.attendance.AttendanceId,
                AttendacneDetails = attendances.OrderBy(i => i.AttendanceDay).ToList()
            };
        }

        private AttendanceDetailBuildModal GetAttendanceDetail(Attendance attendance, out List<AttendanceDetailJson> attendenceDetails)
        {
            var now = _timezoneConverter.ToTimeZoneDateTime((DateTime)attendance.AttendanceDay, _currentSession.TimeZone);
            if (now.Day != 1)
                throw HiringBellException.ThrowBadRequest("Invalid from date submitted.");

            attendenceDetails = new List<AttendanceDetailJson>();
            AttendanceDetailBuildModal attendanceDetailBuildModal = new AttendanceDetailBuildModal();
            attendanceDetailBuildModal.firstDate = (DateTime)attendance.AttendanceDay;

            if (attendance.ForMonth <= 0)
                throw new HiringBellException("Invalid month num. passed.", nameof(attendance.ForMonth), attendance.ForMonth.ToString());

            var Result = _db.FetchDataSet("sp_attendance_get", new
            {
                EmployeeId = attendance.EmployeeId,
                StartDate = attendance.AttendanceDay,
                ForYear = attendance.ForYear,
                ForMonth = attendance.ForMonth,
                CompanyId = _currentSession.CurrentUserDetail.CompanyId,
                RequestTypeId = (int)RequestType.Attendance
            });

            if (Result.Tables.Count != 5)
                throw HiringBellException.ThrowBadRequest("Fail to get attendance detail. Please contact to admin.");

            if (Result.Tables[3].Rows.Count != 1)
                throw HiringBellException.ThrowBadRequest("Company regular shift is not configured. Please complete company setting first.");

            attendanceDetailBuildModal.shiftDetail = Converter.ToType<ShiftDetail>(Result.Tables[3]);
            attendanceDetailBuildModal.compalintOrRequests = Converter.ToList<ComplaintOrRequest>(Result.Tables[4]);

            if (!ApplicationConstants.ContainSingleRow(Result.Tables[1]))
                throw new HiringBellException("Err!! fail to get employee detail. Plaese contact to admin.");

            attendanceDetailBuildModal.employee = Converter.ToType<Employee>(Result.Tables[1]);

            if (ApplicationConstants.ContainSingleRow(Result.Tables[0]) &&
                !string.IsNullOrEmpty(Result.Tables[0].Rows[0]["AttendanceDetail"].ToString()))
            {
                attendanceDetailBuildModal.attendance = Converter.ToType<Attendance>(Result.Tables[0]);
                attendenceDetails = JsonConvert.DeserializeObject<List<AttendanceDetailJson>>(attendanceDetailBuildModal.attendance.AttendanceDetail);
            }
            else
            {
                attendanceDetailBuildModal.attendance = new Attendance
                {
                    AttendanceDetail = "[]",
                    AttendanceId = 0,
                    EmployeeId = attendance.EmployeeId,
                };

            }

            attendanceDetailBuildModal.calendars = Converter.ToList<Calendar>(Result.Tables[2]);
            return attendanceDetailBuildModal;
        }

        public List<AttendenceDetail> GetAllPendingAttendanceByUserIdService(long employeeId, int UserTypeId, long clientId)
        {
            List<AttendenceDetail> attendanceSet = new List<AttendenceDetail>();
            DateTime current = DateTime.UtcNow;

            var currentAttendance = _db.Get<Attendance>("sp_attendance_detall_pending", new
            {
                EmployeeId = employeeId,
                UserTypeId = UserTypeId == 0 ? _currentSession.CurrentUserDetail.UserTypeId : UserTypeId,
                ForYear = current.Year,
                ForMonth = current.Month
            });

            if (currentAttendance == null)
                throw new HiringBellException("Fail to get attendance detail. Please contact to admin.");

            attendanceSet = JsonConvert.DeserializeObject<List<AttendenceDetail>>(currentAttendance.AttendanceDetail);
            return attendanceSet;
        }

        public async Task<AttendanceDetailJson> SubmitAttendanceService(Attendance attendance)
        {
            string Result = string.Empty;

            if (attendance.AttendanceId == 0)
                throw HiringBellException.ThrowBadRequest("Invalid record send for applying.");

            if (attendance.AttendanceDay == null)
                throw HiringBellException.ThrowBadRequest("Fail to get attendance detail");

            var attendancemonth = _timezoneConverter.ToIstTime(attendance.AttendanceDay).Month;
            var attendanceyear = _timezoneConverter.ToIstTime(attendance.AttendanceDay).Year;

            // this value should come from database as configured by user.
            int dailyWorkingHours = 8;
            var attendanceList = new List<AttendanceDetailJson>();

            // check back date limit to allow attendance
            await AttendanceBackdayLimit(attendance.AttendanceDay);

            // check for leave, holiday and weekends
            await this.IsGivenDateAllowed(attendance.AttendanceDay);

            var presentAttendance = _db.Get<Attendance>("sp_attendance_get_byid", new { AttendanceId = attendance.AttendanceId });
            if (presentAttendance == null || string.IsNullOrEmpty(presentAttendance.AttendanceDetail))
                throw HiringBellException.ThrowBadRequest("Fail to get attendance detail");

            var employee = _db.Get<Employee>("SP_Employees_ById", new { EmployeeId = _currentSession.CurrentUserDetail.ReportingManagerId, IsActive = 1 });
            if (employee == null)
                throw HiringBellException.ThrowBadRequest("Fail to get manager detail");

            attendanceList = JsonConvert
                .DeserializeObject<List<AttendanceDetailJson>>(presentAttendance.AttendanceDetail);

            presentAttendance.AttendanceDay = attendance.AttendanceDay;
            var workingattendance = attendanceList.Find(x => x.AttendenceDetailId == attendance.AttendenceDetailId);
            await this.CheckAndCreateAttendance(workingattendance);
            workingattendance.UserComments = attendance.UserComments;
            int pendingDays = attendanceList.Count(x => x.PresentDayStatus == (int)ItemStatus.Pending);
            presentAttendance.DaysPending = pendingDays;
            presentAttendance.TotalHoursBurend = pendingDays * dailyWorkingHours;
            attendance.PendingRequestCount = ++attendance.PendingRequestCount;
            // check for halfday or fullday.
            await this.CheckHalfdayAndFullday(workingattendance, attendance);

            string ProjectName = string.Empty;
            Result = _db.Execute<Attendance>("sp_attendance_insupd", new
            {
                AttendanceId = presentAttendance.AttendanceId,
                AttendanceDetail = JsonConvert.SerializeObject(attendanceList),
                UserTypeId = _currentSession.CurrentUserDetail.UserTypeId,
                EmployeeId = presentAttendance.EmployeeId,
                TotalDays = presentAttendance.TotalDays,
                TotalWeekDays = presentAttendance.TotalWeekDays,
                DaysPending = presentAttendance.DaysPending,
                TotalBurnedMinutes = presentAttendance.TotalHoursBurend,
                ForYear = presentAttendance.ForYear,
                ForMonth = presentAttendance.ForMonth,
                UserId = _currentSession.CurrentUserDetail.UserId,
                PendingRequestCount = ++presentAttendance.PendingRequestCount,
                EmployeeName = presentAttendance.EmployeeName,
                Email = presentAttendance.Email,
                Mobile = presentAttendance.Mobile,
                ReportingManagerId = presentAttendance.ReportingManagerId,
                ManagerName = presentAttendance.ManagerName
            }, true);

            if (string.IsNullOrEmpty(Result))
                throw new HiringBellException("Unable submit the attendace");

            Result = ApplicationConstants.Updated;
            var result = JsonConvert.SerializeObject(presentAttendance);

            AttendanceRequestModal attendanceRequestModal = new AttendanceRequestModal
            {
                ActionType = ApplicationConstants.Submitted,
                CompanyName = _currentSession.CurrentUserDetail.CompanyName,
                DayCount = presentAttendance.TotalDays,
                DeveloperName = presentAttendance.EmployeeName,
                FromDate = presentAttendance.AttendanceDay,
                ManagerName = presentAttendance.ManagerName,
                Message = presentAttendance.UserComments,
                RequestType = attendance.WorkTypeId == WorkType.WORKFROMHOME ? ApplicationConstants.WorkFromHome : ApplicationConstants.WorkFromOffice,
                ToAddress = new List<string> { employee.Email },
                kafkaServiceName = KafkaServiceName.Attendance
            };

            await _kafkaNotificationService.SendEmailNotification(attendanceRequestModal);

            return workingattendance;
        }

        private async Task AttendanceBackdayLimit(DateTime AttendanceDay)
        {
            var companySetting = await _companyService.GetCompanySettingByCompanyId(_currentSession.CurrentUserDetail.CompanyId);
            DateTime barrierDate = this.GetBarrierDate(companySetting.AttendanceSubmissionLimit);

            var zoneBaseDate = _timezoneConverter.ToTimeZoneDateTime(barrierDate, _currentSession.TimeZone);
            var attendanceDay = _timezoneConverter.ToTimeZoneDateTime(AttendanceDay, _currentSession.TimeZone);

            if (attendanceDay.Date.Subtract(zoneBaseDate.Date).TotalDays < 0)
                throw new HiringBellException("Ops!!! You are not allow to submit this date attendace. Please raise a request to your direct manager.");

            await Task.CompletedTask;
        }

        public async Task<List<ComplaintOrRequest>> GetMissingAttendanceRequestService(FilterModel filter)
        {
            if (string.IsNullOrEmpty(filter.SearchString) || filter.EmployeeId > 0)
                filter.SearchString = $"1=1 and RequestTypeId = {(int)RequestType.Attendance} and EmployeeId = {filter.EmployeeId}";

            var result = _db.GetList<ComplaintOrRequest>("sp_complaint_or_request_get_by_employeeid", new
            {
                filter.SearchString,
                filter.SortBy,
                filter.PageSize,
                filter.PageIndex
            });

            return await Task.FromResult(result);
        }

        public async Task<List<ComplaintOrRequest>> GetMissingAttendanceApprovalRequestService(FilterModel filter)
        {
            if (string.IsNullOrEmpty(filter.SearchString))
                filter.SearchString = $"1=1 and RequestTypeId = {(int)RequestType.Attendance} and ManagerId = {_currentSession.CurrentUserDetail.UserId}";
            if (filter.EmployeeId > 0)
                filter.SearchString += $" and EmployeeId = {filter.EmployeeId}";

            var result = _db.GetList<ComplaintOrRequest>("sp_complaint_or_request_get_by_employeeid", new
            {
                filter.SearchString,
                filter.SortBy,
                filter.PageSize,
                filter.PageIndex
            });

            return await Task.FromResult(result);
        }

        private async Task<string> InsertUpdateAttendanceRequest(ComplaintOrRequestWithEmail compalintOrRequestWithEmail, int attendanceId)
        {
            Attendance attendance = null;
            Employee managerDetail = null;
            List<ComplaintOrRequest> complaintOrRequests = new List<ComplaintOrRequest>();

            var resultSet = _db.GetDataSet("sp_attendance_employee_detail_id", new
            {
                EmployeeId = _currentSession.CurrentUserDetail.ReportingManagerId,
                AttendanceId = attendanceId
            });

            if (resultSet.Tables.Count != 3)
                throw HiringBellException.ThrowBadRequest("Fail to get attendance detail. Please contact to admin.");

            attendance = Converter.ToType<Attendance>(resultSet.Tables[0]);
            if (attendance == null)
                throw HiringBellException.ThrowBadRequest("Inlvalid attendance detail found. Please apply with proper data.");

            managerDetail = Converter.ToType<Employee>(resultSet.Tables[1]);
            if (managerDetail == null)
                throw HiringBellException.ThrowBadRequest("Employee deatil not found. Please contact to admin");

            complaintOrRequests = Converter.ToList<ComplaintOrRequest>(resultSet.Tables[2]);
            if (complaintOrRequests == null)
                throw HiringBellException.ThrowBadRequest("Inlvalid attendance detail found. Please apply with proper data.");

            if (string.IsNullOrEmpty(attendance.AttendanceDetail) || attendance.AttendanceDetail == "[]")
                throw HiringBellException.ThrowBadRequest("Inlvalid attendance detail found. Please contact to admin.");

            List<AttendanceDetailJson> attrDetails = JsonConvert.DeserializeObject<List<AttendanceDetailJson>>(attendance.AttendanceDetail);
            compalintOrRequestWithEmail.CompalintOrRequestList.ForEach(x =>
            {
                var item = attrDetails.Find(i => i.AttendenceDetailId == x.TargetOffset);
                if (item == null)
                    throw HiringBellException.ThrowBadRequest("Found invalid data. Please contact to admin.");

                var target = complaintOrRequests.Find(i => i.TargetOffset == x.TargetOffset);
                if (target != null)
                {
                    x.ComplaintOrRequestId = target.ComplaintOrRequestId;
                }

                item.PresentDayStatus = (int)ItemStatus.MissingAttendanceRequest;
            });

            var attrDetailJson = JsonConvert.SerializeObject(attrDetails);
            var records = (from n in compalintOrRequestWithEmail.CompalintOrRequestList
                           select new
                           {
                               AttendanceId = attendanceId,
                               AttendanceDetail = attrDetailJson,
                               ComplaintOrRequestId = n.ComplaintOrRequestId,
                               RequestTypeId = (int)RequestType.Attendance,
                               TargetId = attendanceId,
                               TargetOffset = n.TargetOffset,
                               EmployeeId = _currentSession.CurrentUserDetail.UserId,
                               EmployeeName = _currentSession.CurrentUserDetail.FullName,
                               Email = _currentSession.CurrentUserDetail.Email,
                               Mobile = _currentSession.CurrentUserDetail.Mobile,
                               ManagerId = _currentSession.CurrentUserDetail.ReportingManagerId,
                               ManagerName = managerDetail.FirstName + " " + managerDetail.LastName,
                               ManagerEmail = managerDetail.Email,
                               ManagerMobile = managerDetail.Mobile,
                               EmployeeMessage = n.EmployeeMessage,
                               ManagerComments = string.Empty,
                               CurrentStatus = (int)ItemStatus.Pending,
                               RequestedOn = DateTime.UtcNow,
                               AttendanceDate = n.AttendanceDate,
                               LeaveFromDate = DateTime.UtcNow,
                               LeaveToDate = DateTime.UtcNow,
                               Notify = JsonConvert.SerializeObject(n.NotifyList),
                               ExecutedByManager = n.ExecutedByManager,
                               ExecuterId = n.ExecuterId,
                               ExecuterName = n.ExecuterName,
                               ExecuterEmail = n.ExecuterEmail
                           }).ToList();

            var result = await _db.BulkExecuteAsync("sp_complaint_or_request_InsUpdate", records, true);
            return result.ToString();
        }

        public async Task<string> RaiseMissingAttendanceRequestService(ComplaintOrRequestWithEmail complaintOrRequestWithEmail)
        {
            if (complaintOrRequestWithEmail == null || complaintOrRequestWithEmail.CompalintOrRequestList.Count == 0)
                throw HiringBellException.ThrowBadRequest("Invalid request data passed. Please check your form again.");

            if (complaintOrRequestWithEmail.AttendanceId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid request data passed. Please check your form again.");

            var alreadyApplied = complaintOrRequestWithEmail.CompalintOrRequestList
                .FindAll(x => x.CurrentStatus == (int)ItemStatus.MissingAttendanceRequest);

            if (alreadyApplied.Count > 0)
                throw HiringBellException.ThrowBadRequest("You already raise the request");

            var anyRecord = complaintOrRequestWithEmail.CompalintOrRequestList.Any(x => x.TargetOffset == 0);
            if (anyRecord)
                throw HiringBellException.ThrowBadRequest("Invalid data passed. Please contact to admin.");

            var Result = await InsertUpdateAttendanceRequest(complaintOrRequestWithEmail, complaintOrRequestWithEmail.AttendanceId);
            await this.AttendaceApprovalStatusSendEmail(complaintOrRequestWithEmail);
            return Result;
        }

        public AttendanceWithClientDetail EnablePermission(AttendenceDetail attendenceDetail)
        {
            if (attendenceDetail.ForMonth <= 0)
                throw new HiringBellException("Invalid month num. passed.", nameof(attendenceDetail.ForMonth), attendenceDetail.ForMonth.ToString());

            if (Convert.ToDateTime(attendenceDetail.AttendenceFromDay).Subtract(DateTime.UtcNow).TotalDays > 0)
            {
                throw new HiringBellException("Ohh!!!. Future dates are now allowed.");
            }


            var Result = _db.FetchDataSet("sp_attendance_get", new
            {
                EmployeeId = attendenceDetail.EmployeeUid,
                ClientId = attendenceDetail.ClientId,
                UserTypeId = attendenceDetail.UserTypeId,
                ForYear = attendenceDetail.ForYear,
                ForMonth = attendenceDetail.ForMonth
            });

            return null;
        }

        public dynamic GetEmployeePerformanceService(AttendenceDetail attendanceDetail)
        {
            if (attendanceDetail.EmployeeUid <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid employee. Please login again");
            var result = _db.GetList<Attendance>("sp_employee_performance_get", new
            {
                EmployeeId = attendanceDetail.EmployeeUid,
                UserTypeId = attendanceDetail.UserTypeId,
                ForYear = attendanceDetail.ForYear
            });

            var monthlyAttendance = result.Find(x => x.ForMonth == attendanceDetail.ForMonth);

            return new { MonthlyAttendance = monthlyAttendance, YearlyAttendance = result };
        }

        private async Task CheckAndCreateAttendance(AttendanceDetailJson workingAttendance)
        {
            switch (workingAttendance.PresentDayStatus)
            {
                case (int)ItemStatus.Approved:
                    throw new HiringBellException($"Attendance for: {workingAttendance.AttendanceDay.ToString("dd MMM, yyyy")} " +
                        $"already has been {nameof(ItemStatus.Approved)}");
                case (int)ItemStatus.Pending:
                    throw new HiringBellException($"Attendance for: {workingAttendance.AttendanceDay.ToString("dd MMM, yyyy")} " +
                        $"currently is in {nameof(ItemStatus.Approved)} state.");
            }

            workingAttendance.PresentDayStatus = (int)ItemStatus.Pending;
            await Task.CompletedTask;
        }

        private async Task CheckHalfdayAndFullday(AttendanceDetailJson workingAttendance, Attendance attendance)
        {
            if (attendance.SessionType > 1 && workingAttendance.SessionType == 1)
            {
                var logoff = workingAttendance.LogOff;
                var logofftime = logoff.Replace(":", ".");
                decimal time = decimal.Parse(logofftime);
                var totaltime = (int)((time * 60) / 2);
                logoff = ConvertToMin(totaltime);
                workingAttendance.LogOff = logoff;
                workingAttendance.LogOn = logoff;
                workingAttendance.SessionType = attendance.SessionType;
                workingAttendance.TotalMinutes = workingAttendance.TotalMinutes / 2;
            }
            else if (attendance.SessionType == 1 && workingAttendance.SessionType > 1)
            {
                var logoff = workingAttendance.LogOff;
                var logofftime = logoff.Replace(":", ".");
                decimal time = decimal.Parse(logofftime);
                var totaltime = (int)((time * 60) * 2);
                workingAttendance.LogOff = ConvertToMin(totaltime);
                workingAttendance.LogOn = ConvertToMin(totaltime + 60);
                workingAttendance.SessionType = attendance.SessionType;
                workingAttendance.TotalMinutes = workingAttendance.TotalMinutes * 2;
            }
            await Task.CompletedTask;
        }

        private async Task CreatePresentDayAttendance(AttendenceDetail attendenceDetail, DateTime workingTimezoneDate)
        {
            var totalDays = DateTime.DaysInMonth(workingTimezoneDate.Year, workingTimezoneDate.Month);

            attendenceDetail.IsActiveDay = false;
            attendenceDetail.TotalDays = totalDays;
            attendenceDetail.AttendanceId = 0;
            attendenceDetail.AttendenceStatus = (int)DayStatus.WorkFromOffice;
            attendenceDetail.BillingHours = 480;
            attendenceDetail.ClientId = attendenceDetail.ClientId;
            attendenceDetail.DaysPending = totalDays;
            attendenceDetail.EmployeeUid = attendenceDetail.EmployeeUid;
            attendenceDetail.ForMonth = workingTimezoneDate.Month;
            attendenceDetail.ForYear = workingTimezoneDate.Year;
            attendenceDetail.TotalMinutes = 480;
            attendenceDetail.IsHoliday = (workingTimezoneDate.DayOfWeek == DayOfWeek.Saturday
                            ||
                        workingTimezoneDate.DayOfWeek == DayOfWeek.Sunday) ? true : false;
            attendenceDetail.IsOnLeave = false;
            attendenceDetail.LeaveId = 0;
            attendenceDetail.PresentDayStatus = (int)ItemStatus.Pending;
            attendenceDetail.UserTypeId = (int)UserType.Employee;
            attendenceDetail.IsOpen = true;
            CalculateLogOffTime(attendenceDetail);
            await Task.CompletedTask;
        }

        private void CalculateLogOffTime(AttendenceDetail attendenceDetail)
        {
            var logontime = attendenceDetail.LogOn.Replace(":", ".");
            decimal logon = decimal.Parse(logontime);
            var totaltime = 0;
            if (attendenceDetail.SessionType == 1)
            {
                totaltime = (int)(logon * 60 - attendenceDetail.LunchBreanInMinutes);
                var time = ConvertToMin(totaltime);
                attendenceDetail.LogOff = time.ToString();
            }
            else
            {
                totaltime = (int)(logon * 60 - attendenceDetail.LunchBreanInMinutes) / 2;
                var time = ConvertToMin(totaltime);
                attendenceDetail.LogOn = time.ToString();
                attendenceDetail.LogOff = time.ToString();
            }
        }

        public String ConvertToMin(int mins)
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

        private async Task IsGivenDateAllowed(DateTime workingDate)
        {
            // check if from date is holiday

            // check if already on leave

            // check if from date already applied for leave

            // check shift weekends

            await Task.CompletedTask;
        }

        private string UpdateOrInsertAttendanceDetail(List<AttendenceDetail> finalAttendanceSet, Attendance currentAttendance, string procedure)
        {
            var firstAttn = finalAttendanceSet.FirstOrDefault();

            var AttendaceDetail = JsonConvert.SerializeObject((from n in finalAttendanceSet
                                                               select new
                                                               {
                                                                   TotalMinutes = n.TotalMinutes,
                                                                   UserTypeId = n.UserTypeId,
                                                                   PresentDayStatus = n.PresentDayStatus,
                                                                   EmployeeUid = n.EmployeeUid,
                                                                   AttendanceId = n.AttendanceId,
                                                                   UserComments = n.UserComments,
                                                                   AttendanceDay = n.AttendanceDay,
                                                                   AttendenceStatus = n.AttendenceStatus
                                                               }));

            double MonthsMinutes = 0;
            currentAttendance.DaysPending = 0;
            finalAttendanceSet.ForEach(x =>
            {
                MonthsMinutes += x.TotalMinutes;
                if (x.AttendenceStatus == 8)
                    currentAttendance.DaysPending++;
            });

            var Result = _db.Execute<string>(procedure, new
            {
                AttendanceId = currentAttendance.AttendanceId,
                EmployeeId = currentAttendance.EmployeeId,
                UserTypeId = currentAttendance.UserTypeId,
                AttendanceDetail = AttendaceDetail,
                TotalDays = currentAttendance.TotalDays,
                TotalWeekDays = currentAttendance.TotalWeekDays,
                DaysPending = currentAttendance.DaysPending,
                TotalBurnedMinutes = MonthsMinutes,
                ForYear = currentAttendance.ForYear,
                ForMonth = currentAttendance.ForMonth,
                UserId = _currentSession.CurrentUserDetail.UserId
            }, true);

            if (string.IsNullOrEmpty(Result))
                throw new HiringBellException("Fail to insert or update attendance detail. Pleasa contact to admin.");

            return Result;
        }

        private void ValidateDateOfAttendanceSubmission(DateTime firstDate, DateTime lastDate)
        {
            DateTime now = DateTime.Now;
            DateTime presentDate = _timezoneConverter.GetUtcDateTime(now.Year, now.Month, now.Day);

            // handling future date
            if (presentDate.Subtract(lastDate).TotalDays > 0)
            {
                throw new HiringBellException("Future date's are not allowed.");
            }
            // handling past date
            else if (presentDate.Subtract(firstDate).TotalDays < 0)
            {
                if (_currentSession.CurrentUserDetail.RoleId != (int)UserType.Admin)
                {
                    throw new HiringBellException("Past week's are not allowed.");
                }
            }
        }

        private async Task<TemplateReplaceModal> GetAttendanceApprovalTemplate(ComplaintOrRequest compalintOrRequest)
        {
            var templateReplaceModal = new TemplateReplaceModal
            {
                DeveloperName = compalintOrRequest.EmployeeName,
                RequestType = ApplicationConstants.WorkFromHome,
                ToAddress = new List<string> { compalintOrRequest.Email },
                ActionType = "Requested",
                FromDate = compalintOrRequest.AttendanceDate,
                ToDate = compalintOrRequest.AttendanceDate,
                ManagerName = compalintOrRequest.ManagerName,
                Message = string.IsNullOrEmpty(compalintOrRequest.ManagerComments)
                    ? "NA"
                    : compalintOrRequest.ManagerComments,
            };

            if (compalintOrRequest.NotifyList != null && compalintOrRequest.NotifyList.Count > 0)
            {
                foreach (var email in compalintOrRequest.NotifyList)
                {
                    templateReplaceModal.ToAddress.Add(email.Email);
                }
            }
            return await Task.FromResult(templateReplaceModal);
        }

        public async Task AttendaceApprovalStatusSendEmail(ComplaintOrRequestWithEmail compalintOrRequestWithEmail)
        {
            var templateReplaceModal = new TemplateReplaceModal();
            if (compalintOrRequestWithEmail.CompalintOrRequestList.First().NotifyList != null && compalintOrRequestWithEmail.CompalintOrRequestList.First().NotifyList.Count > 0)
            {
                templateReplaceModal.ToAddress = new List<string>();
                foreach (var email in compalintOrRequestWithEmail.CompalintOrRequestList.First().NotifyList)
                {
                    templateReplaceModal.ToAddress.Add(email.Email);
                }
            }

            await SendEmailWithTemplate(compalintOrRequestWithEmail, templateReplaceModal);
        }

        private async Task<EmailSenderModal> SendEmailWithTemplate(ComplaintOrRequestWithEmail compalintOrRequestWithEmail, TemplateReplaceModal templateReplaceModal)
        {
            templateReplaceModal.BodyContent = compalintOrRequestWithEmail.EmailBody;
            var emailSenderModal = await ReplaceActualData(templateReplaceModal, compalintOrRequestWithEmail);

            await _eMailManager.SendMailAsync(emailSenderModal);
            return await Task.FromResult(emailSenderModal);
        }

        private async Task<EmailSenderModal> ReplaceActualData(TemplateReplaceModal templateReplaceModal, ComplaintOrRequestWithEmail compalintOrRequestWithEmail)
        {
            EmailSenderModal emailSenderModal = null;
            var attendance = compalintOrRequestWithEmail.CompalintOrRequestList.First();
            if (templateReplaceModal != null)
            {
                var totalDays = compalintOrRequestWithEmail.CompalintOrRequestList.Count;
                string subject = $"{totalDays} Days Blocked Attendance Approval Status";
                string body = compalintOrRequestWithEmail.EmailBody;

                StringBuilder builder = new StringBuilder();
                builder.Append("<div style=\"border-bottom:1px solid black; margin-top: 14px; margin-bottom:5px\">" + "" + "</div>");
                builder.AppendLine();
                builder.AppendLine();
                builder.Append("<div>" + "Thanks and Regard" + "</div>");
                builder.Append("<div>" + attendance.EmployeeName + "</div>");
                builder.Append("<div>" + attendance.Mobile + "</div>");

                var logoPath = Path.Combine(_fileLocationDetail.RootPath, _fileLocationDetail.LogoPath, ApplicationConstants.HiringBellLogoSmall);
                if (File.Exists(logoPath))
                {
                    builder.Append($"<div><img src=\"cid:{ApplicationConstants.LogoContentId}\" style=\"width: 10rem;margin-top: 1rem;\"></div>");
                }

                emailSenderModal = new EmailSenderModal
                {
                    To = templateReplaceModal.ToAddress,
                    Subject = subject,
                    Body = string.Concat(body, builder.ToString()),
                };
            }

            emailSenderModal.Title = $"{attendance.EmployeeName} requested for approved blocked attendance.";

            return await Task.FromResult(emailSenderModal);
        }

        public async Task<List<ComplaintOrRequest>> ApproveRaisedAttendanceRequestService(List<ComplaintOrRequest> complaintOrRequests)
        {
            return await UpdateRequestRaised(complaintOrRequests, (int)ItemStatus.Approved);
        }

        public async Task<List<ComplaintOrRequest>> RejectRaisedAttendanceRequestService(List<ComplaintOrRequest> complaintOrRequests)
        {
            return await UpdateRequestRaised(complaintOrRequests, (int)ItemStatus.Rejected);
        }

        private async Task<Attendance> GetCurrentAttendanceRequestData(List<ComplaintOrRequest> complaintOrRequests, int itemStatus)
        {
            var first = complaintOrRequests.First();
            var resultSet = _db.GetDataSet("sp_attendance_get_byid", new
            {
                AttendanceId = first.TargetId
            });

            if (resultSet.Tables.Count != 1)
                throw HiringBellException.ThrowBadRequest("Fail to get current attendance detail.");

            var attendance = Converter.ToType<Attendance>(resultSet.Tables[0]);
            if (attendance == null)
                throw HiringBellException.ThrowBadRequest("Fail to get current attendance detail.");

            if (string.IsNullOrEmpty(attendance.AttendanceDetail) || attendance.AttendanceDetail == "[]")
                throw HiringBellException.ThrowBadRequest("Invalid attendance detail found. Please contact to admin.");

            var attendanceDetail = JsonConvert.DeserializeObject<List<AttendanceDetailJson>>(attendance.AttendanceDetail);
            var currentAttr = attendanceDetail.Find(x => x.AttendenceDetailId == first.TargetOffset);
            if (currentAttr == null)
                throw HiringBellException.ThrowBadRequest("Invalid attendance detail found. Please contact to admin.");

            currentAttr.PresentDayStatus = itemStatus;
            currentAttr.ApprovedName = _currentSession.CurrentUserDetail.FullName;
            currentAttr.ApprovedBy = _currentSession.CurrentUserDetail.UserId;
            //var logoff = currentAttr.LogOff;
            //var logofftime = logoff.Replace(":", ".");
            //decimal time = decimal.Parse(logofftime);
            //var totaltime = (int)((time * 60) * 2);
            currentAttr.LogOff = currentAttr.LogOff;
            currentAttr.LogOn = currentAttr.LogOn;
            currentAttr.SessionType = currentAttr.SessionType;
            currentAttr.TotalMinutes = currentAttr.TotalMinutes;
            attendance.AttendanceDetail = JsonConvert.SerializeObject(attendanceDetail);

            return await Task.FromResult(attendance);
        }

        private async Task<List<ComplaintOrRequest>> UpdateRequestRaised(List<ComplaintOrRequest> complaintOrRequests, int itemStatus)
        {
            if (complaintOrRequests == null || complaintOrRequests.Count() == 0 || complaintOrRequests.Any(x => x.ComplaintOrRequestId <= 0))
                throw HiringBellException.ThrowBadRequest("Invalid attendance selected. Please login again");

            complaintOrRequests.ForEach(i =>
            {
                if (i.TargetId == 0 || i.TargetOffset == 0 || i.EmployeeId == 0)
                    throw HiringBellException.ThrowBadRequest("Invalid attendance detail found. Please contact to admin.");
            });

            var attendance = await GetCurrentAttendanceRequestData(complaintOrRequests, itemStatus);

            bool isManager = false;
            if (_currentSession.CurrentUserDetail.UserId == attendance.ReportingManagerId)
                isManager = true;

            var items = (from n in complaintOrRequests
                         select new
                         {
                             ComplaintOrRequestId = n.ComplaintOrRequestId,
                             ExecutedByManager = isManager,
                             ExecuterId = _currentSession.CurrentUserDetail.UserId,
                             ExecuterName = _currentSession.CurrentUserDetail.FullName,
                             ExecuterEmail = _currentSession.CurrentUserDetail.Email,
                             ManagerComments = n.ManagerComments,
                             StatusId = itemStatus,
                             AttendanceId = attendance.AttendanceId,
                             AttendanceDetail = attendance.AttendanceDetail,
                             UserId = _currentSession.CurrentUserDetail.UserId
                         }).ToList();

            var result = await _db.BulkExecuteAsync("sp_complaint_or_request_update_status", items);


            var filter = new FilterModel();
            filter.SearchString = String.Empty;
            return await GetMissingAttendanceApprovalRequestService(filter);
        }

        public void PresnetAllAttendance()
        {

        }
    }
}