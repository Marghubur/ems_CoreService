using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using Bot.CoreBottomHalf.CommonModal.Enums;
using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using Bot.CoreBottomHalf.CommonModal.Leave;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using Bt.Lib.Common.Service.MicroserviceHttpRequest;
using Bt.Lib.Common.Service.Model;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EMailService.Modal;
using EMailService.Service;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using ModalLayer.Modal.Leaves;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DailyAttendance = ModalLayer.Modal.DailyAttendance;

namespace ServiceLayer.Code
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IDb _db;
        private readonly CurrentSession _currentSession;
        private readonly ITimezoneConverter _timezoneConverter;
        private readonly ICompanyService _companyService;
        private readonly IEMailManager _eMailManager;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly ICommonService _commonService;
        private readonly IUtilityService _utilityService;
        private readonly MicroserviceRegistry _microserviceUrlLogs;
        private readonly RequestMicroservice _requestMicroservice;
        public AttendanceService(IDb db,
            ITimezoneConverter timezoneConverter,
            CurrentSession currentSession,
            ICompanyService companyService,
            IEMailManager eMailManager,
            FileLocationDetail fileLocationDetail,
            ICommonService commonService,
            IUtilityService utilityService,
            MicroserviceRegistry microserviceUrlLogs,
            RequestMicroservice requestMicroservice)
        {
            _db = db;
            _companyService = companyService;
            _currentSession = currentSession;
            _timezoneConverter = timezoneConverter;
            _eMailManager = eMailManager;
            _fileLocationDetail = fileLocationDetail;
            _commonService = commonService;
            _utilityService = utilityService;
            _microserviceUrlLogs = microserviceUrlLogs;
            _requestMicroservice = requestMicroservice;
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

        private async Task<List<AttendanceJson>> CreateAttendanceTillDate(AttendanceDetailBuildModal attendanceModal)
        {
            List<AttendanceJson> attendenceDetails = new List<AttendanceJson>();
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

                attendenceDetails.Add(new AttendanceJson
                {
                    AttendenceDetailId = workingDate.Day,
                    AttendanceDay = workingDate,
                    LogOn = officetime,
                    LogOff = logoff,
                    PresentDayStatus = presentDayStatus,
                    UserComments = string.Empty,
                    ApprovedName = string.Empty,
                    ApprovedBy = 0,
                    SessionType = 1,
                    TotalMinutes = totalMinute,
                    IsOpen = i >= days ? true : false,
                    IsHoliday = isHoliday,
                    IsOnLeave = false,
                    IsWeekend = isWeekend,
                    WorkTypeId = (int)WorkType.EMPTY
                });

                i++;
            }

            var result = await _db.ExecuteAsync(Procedures.Attendance_Insupd, new
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

        public async Task GenerateAttendanceService(AttendenceDetail attendenceDetail)
        {
            var attendanceStartDate = _timezoneConverter.ToTimeZoneDateTime((DateTime)attendenceDetail.AttendenceFromDay, _currentSession.TimeZone);
            var attendanceEndDate = _timezoneConverter.ToTimeZoneDateTime((DateTime)attendenceDetail.AttendenceToDay, _currentSession.TimeZone);

            attendanceStartDate = _timezoneConverter.ToUtcTime(attendanceStartDate, _currentSession.TimeZone);
            attendanceEndDate = _timezoneConverter.ToUtcTime(attendanceEndDate, _currentSession.TimeZone);

            await _db.ExecuteAsync("sp_daily_attendance_ins_advance", new
            {
                FromDate = attendanceStartDate,
                ToDate = attendanceEndDate,
                AttendanceStatus = attendenceDetail.AttendenceStatus
            });

            await Task.CompletedTask;
        }

        public async Task<AttendanceWithClientDetail> GetAttendanceByUserId(Attendance attendance)
        {
            if (attendance.EmployeeId == 0)
                throw HiringBellException.ThrowBadRequest("Employee id is invalid");

            List<AttendanceJson> attendenceDetails;
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

            //var attendances = attendenceDetails.OrderBy(i => i.AttendanceDay)
            //                    .TakeWhile(x => DateTime.Now.Date.Subtract(x.AttendanceDay.Date).TotalDays >= 0)
            //                    .OrderByDescending(i => i.AttendanceDay)
            //                    .ToList();

            var attendances = attendenceDetails.OrderByDescending(i => i.AttendanceDay).ToList();

            var leave = _db.GetList<LeaveRequestNotification>(Procedures.Leave_Request_Notification_Get_By_Empid, new
            {
                EmployeeId = attendance.EmployeeId,
                RequestStatusId = (int)ItemStatus.Approved
            });

            int daysLimit = attendanceDetailBuildModal.attendanceSubmissionLimit + 1;
            if (attendanceDetailBuildModal.attendance.ForMonth == DateTime.UtcNow.Month || DateTime.UtcNow.Day < daysLimit)
            {
                foreach (var item in attendances)
                {
                    if (!CheckWeekend(attendanceDetailBuildModal.shiftDetail, item.AttendanceDay))
                    {
                        if (daysLimit > 0 && DateTime.UtcNow.Subtract(item.AttendanceDay).TotalDays >= 0)
                        {
                            item.IsOpen = true;
                            daysLimit--;
                        }
                        else
                            item.IsOpen = false;
                    }
                    else
                    {
                        if (daysLimit > 0 && DateTime.UtcNow.Subtract(item.AttendanceDay).Days >= 0)
                            daysLimit--;
                    }
                }
            }
            else
            {
                attendances.ForEach(x =>
                {
                    if (x.PresentDayStatus == (int)ItemStatus.NotSubmitted && x.PresentDayStatus != (int)DayStatus.Weekend && x.PresentDayStatus != (int)DayStatus.Weekend)
                        x.IsOpen = false;
                });
            }
            if (leave != null && leave.Count > 0)
            {
                foreach (var item in attendances)
                {
                    var leaveDetail = leave.Find(x => _timezoneConverter.ToTimeZoneDateTime(x.FromDate, _currentSession.TimeZone).Date.Subtract(item.AttendanceDay.Date).TotalDays <= 0
                                                    && _timezoneConverter.ToTimeZoneDateTime(x.ToDate, _currentSession.TimeZone).Date.Subtract(item.AttendanceDay.Date).TotalDays >= 0);
                    if (leaveDetail != null && leaveDetail.RequestStatusId == (int)ItemStatus.Approved)
                    {
                        item.IsOnLeave = true;
                        item.IsOpen = false;
                    }
                }
            }

            if (attendanceDetailBuildModal.calendars != null && attendanceDetailBuildModal.calendars.Count > 0)
            {
                foreach (var item in attendances)
                {
                    var holiday = attendanceDetailBuildModal.calendars.Find(x => _timezoneConverter.ToTimeZoneDateTime(x.StartDate, _currentSession.TimeZone).Date.Subtract(item.AttendanceDay.Date).TotalDays <= 0
                                                    && _timezoneConverter.ToTimeZoneDateTime(x.EndDate, _currentSession.TimeZone).Date.Subtract(item.AttendanceDay.Date).TotalDays >= 0);
                    if (holiday != null)
                    {
                        item.IsHoliday = true;
                        item.IsOpen = true;
                        if (holiday.IsHalfDay)
                        {
                            item.IsHalfDay = true;
                            item.LogOff = "04:00";
                            item.LogOn = "04:00";
                        }
                    }
                }
            }

            return new AttendanceWithClientDetail
            {
                EmployeeDetail = attendanceDetailBuildModal.employee,
                AttendanceId = attendanceDetailBuildModal.attendance.AttendanceId,
                AttendacneDetails = attendances.OrderBy(i => i.AttendanceDay).ToList(),
                //Projects = attendanceDetailBuildModal.projects
            };
        }

        private AttendanceDetailBuildModal GetAttendanceDetail(Attendance attendance, out List<AttendanceJson> attendenceDetails)
        {
            var now = _timezoneConverter.ToTimeZoneDateTime((DateTime)attendance.AttendanceDay, _currentSession.TimeZone);
            if (now.Day != 1)
                throw HiringBellException.ThrowBadRequest("Invalid from date submitted.");

            attendenceDetails = new List<AttendanceJson>();
            AttendanceDetailBuildModal attendanceDetailBuildModal = new AttendanceDetailBuildModal();
            attendanceDetailBuildModal.firstDate = (DateTime)attendance.AttendanceDay;

            if (attendance.ForMonth <= 0)
                throw new HiringBellException("Invalid month num. passed.", nameof(attendance.ForMonth), attendance.ForMonth.ToString());

            var Result = _db.FetchDataSet(Procedures.Attendance_Get, new
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
            //attendanceDetailBuildModal.projects = Converter.ToList<Project>(Result.Tables[5]);

            if (!ApplicationConstants.ContainSingleRow(Result.Tables[1]))
                throw new HiringBellException("Err!! fail to get employee detail. Plaese contact to admin.");

            attendanceDetailBuildModal.employee = Converter.ToType<Employee>(Result.Tables[1]);

            if (ApplicationConstants.ContainSingleRow(Result.Tables[0]) &&
                !string.IsNullOrEmpty(Result.Tables[0].Rows[0]["AttendanceDetail"].ToString()))
            {
                attendanceDetailBuildModal.attendance = Converter.ToType<Attendance>(Result.Tables[0]);
                attendenceDetails = JsonConvert.DeserializeObject<List<AttendanceJson>>(attendanceDetailBuildModal.attendance.AttendanceDetail);
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

            attendanceDetailBuildModal.calendars = Converter.ToList<ModalLayer.Calendar>(Result.Tables[2]);
            return attendanceDetailBuildModal;
        }

        public List<AttendenceDetail> GetAllPendingAttendanceByUserIdService(long employeeId, int UserTypeId, long clientId)
        {
            List<AttendenceDetail> attendanceSet = new List<AttendenceDetail>();
            DateTime current = DateTime.UtcNow;

            var currentAttendance = _db.Get<Attendance>(Procedures.Attendance_Detall_Pending, new
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

        public async Task<AttendanceJson> SubmitAttendanceService(Attendance attendance)
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
            var attendanceList = new List<AttendanceJson>();

            // check back date limit to allow attendance
            await AttendanceBackdayLimit(attendance.AttendanceDay);

            // check for leave, holiday and weekends
            await this.IsGivenDateAllowed(attendance.AttendanceDay);

            var presentAttendance = _db.Get<Attendance>(Procedures.Attendance_Get_ById, new { AttendanceId = attendance.AttendanceId });
            if (presentAttendance == null || string.IsNullOrEmpty(presentAttendance.AttendanceDetail))
                throw HiringBellException.ThrowBadRequest("Fail to get attendance detail");

            var employee = _db.Get<Employee>(Procedures.Employees_ById, new { EmployeeId = _currentSession.CurrentUserDetail.ReportingManagerId, IsActive = 1 });
            if (employee == null)
                throw HiringBellException.ThrowBadRequest("Fail to get manager detail");

            attendanceList = JsonConvert
                .DeserializeObject<List<AttendanceJson>>(presentAttendance.AttendanceDetail);

            presentAttendance.AttendanceDay = attendance.AttendanceDay;
            var workingattendance = attendanceList.Find(x => x.AttendenceDetailId == attendance.AttendenceDetailId);
            await CheckAndCreateAttendance(workingattendance);
            workingattendance.UserComments = attendance.UserComments;
            // check for halfday or fullday.
            await CheckHalfdayAndFullday(workingattendance, attendance, presentAttendance.EmployeeId);

            int pendingDays = attendanceList.Count(x => x.PresentDayStatus == (int)ItemStatus.Pending);
            presentAttendance.DaysPending = pendingDays;
            presentAttendance.TotalHoursBurend = pendingDays * dailyWorkingHours;
            workingattendance.WorkTypeId = (int)attendance.WorkTypeId;
            attendance.PendingRequestCount = ++attendance.PendingRequestCount;

            Result = _db.Execute<Attendance>(Procedures.Attendance_Insupd, new
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

            AttendanceRequestModal attendanceRequestModal = new AttendanceRequestModal
            {
                ActionType = ApplicationConstants.Submitted,
                CompanyName = _currentSession.CurrentUserDetail.CompanyName,
                DayCount = 1,
                DeveloperName = presentAttendance.EmployeeName,
                FromDate = _timezoneConverter.ToTimeZoneDateTime(presentAttendance.AttendanceDay, _currentSession.TimeZone),
                ManagerName = presentAttendance.ManagerName,
                Message = presentAttendance.UserComments,
                RequestType = attendance.WorkTypeId == (int)WorkType.WORKFROMHOME ? ApplicationConstants.WorkFromHome : ApplicationConstants.WorkFromOffice,
                ToAddress = new List<string> { employee.Email },
                kafkaServiceName = KafkaServiceName.Attendance,
                LocalConnectionString = _currentSession.LocalConnectionString,
                CompanyId = _currentSession.CurrentUserDetail.CompanyId
            };

            await _utilityService.SendNotification(attendanceRequestModal, KafkaTopicNames.ATTENDANCE_REQUEST_ACTION);

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

            var result = _db.GetList<ComplaintOrRequest>(Procedures.Complaint_Or_Request_Get_By_Employeeid, new
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

            var result = _db.GetList<ComplaintOrRequest>(Procedures.Complaint_Or_Request_Get_By_Employeeid, new
            {
                filter.SearchString,
                filter.SortBy,
                filter.PageSize,
                filter.PageIndex
            });

            return await Task.FromResult(result);
        }

        private async Task<AttendanceRequestModal> InsertUpdateAttendanceRequest(ComplaintOrRequestWithEmail compalintOrRequestWithEmail, int attendanceId)
        {
            //Attendance attendance = null;
            var attendanceIds = compalintOrRequestWithEmail.CompalintOrRequestList.Select(x => x.AttendanceId).ToList();
            var resultSet = _db.GetDataSet(Procedures.Attendance_Employee_Detail_Id, new
            {
                EmployeeId = _currentSession.CurrentUserDetail.ReportingManagerId,
                AttendanceIds = JsonConvert.SerializeObject(attendanceIds)
            });

            if (resultSet.Tables.Count != 3)
                throw HiringBellException.ThrowBadRequest("Fail to get attendance detail. Please contact to admin.");

            //attendance = Converter.ToType<Attendance>(resultSet.Tables[0]);
            //if (attendance == null)
            //    throw HiringBellException.ThrowBadRequest("Invalid attendance detail found. Please apply with proper data.");

            //if (string.IsNullOrEmpty(attendance.AttendanceDetail) || attendance.AttendanceDetail == "[]")
            //    throw HiringBellException.ThrowBadRequest("Invalid attendance detail found. Please contact to admin.");

            //List<AttendanceJson> attrDetails = JsonConvert.DeserializeObject<List<AttendanceJson>>(attendance.AttendanceDetail);

            List<DailyAttendance> dailyAttendances = Converter.ToList<DailyAttendance>(resultSet.Tables[0]);
            if (dailyAttendances.Count == 0)
                throw HiringBellException.ThrowBadRequest("Attendance detail not found. Please contact to admin");

            Employee managerDetail = Converter.ToType<Employee>(resultSet.Tables[1]);
            if (managerDetail == null)
                throw HiringBellException.ThrowBadRequest("Employee detail not found. Please contact to admin");

            List<ComplaintOrRequest> complaintOrRequests = Converter.ToList<ComplaintOrRequest>(resultSet.Tables[2]);
            if (complaintOrRequests == null)
                throw HiringBellException.ThrowBadRequest("Invalid attendance detail found. Please apply with proper data.");

            //compalintOrRequestWithEmail.CompalintOrRequestList.ForEach(x =>
            //{
            //    var item = dailyAttendances.Find(i => i.AttendenceDetailId == x.TargetOffset);
            //    if (item == null)
            //        throw HiringBellException.ThrowBadRequest("Found invalid data. Please contact to admin.");

            //    var target = complaintOrRequests.Find(i => i.TargetOffset == x.TargetOffset);
            //    if (target != null)
            //    {
            //        x.ComplaintOrRequestId = target.ComplaintOrRequestId;
            //    }

            //    item.PresentDayStatus = (int)ItemStatus.MissingAttendanceRequest;
            //});
            //var attrDetailJson = JsonConvert.SerializeObject(attrDetails);

            int workTypeId = 0;
            compalintOrRequestWithEmail.CompalintOrRequestList.ForEach(x =>
            {
                var item = dailyAttendances.Find(i => i.AttendanceId == x.AttendanceId);
                if (item == null)
                    throw HiringBellException.ThrowBadRequest("Found invalid data. Please contact to admin.");

                var target = complaintOrRequests.Find(i => i.TargetId == x.TargetId);
                if (target != null)
                    x.ComplaintOrRequestId = target.ComplaintOrRequestId;

                x.AttendanceDate = item.AttendanceDate;
                workTypeId = (int)item.WorkTypeId;
            });

            //var records = (from n in compalintOrRequestWithEmail.CompalintOrRequestList
            //               select new
            //               {
            //                   AttendanceId = attendanceId,
            //                   AttendanceDetail = attrDetailJson,
            //                   ComplaintOrRequestId = n.ComplaintOrRequestId,
            //                   RequestTypeId = (int)RequestType.Attendance,
            //                   TargetId = attendanceId,
            //                   TargetOffset = n.TargetOffset,
            //                   EmployeeId = _currentSession.CurrentUserDetail.UserId,
            //                   EmployeeName = _currentSession.CurrentUserDetail.FullName,
            //                   Email = _currentSession.CurrentUserDetail.EmailId,
            //                   Mobile = _currentSession.CurrentUserDetail.Mobile,
            //                   ManagerId = _currentSession.CurrentUserDetail.ReportingManagerId,
            //                   ManagerName = managerDetail.FirstName + " " + managerDetail.LastName,
            //                   ManagerEmail = managerDetail.Email,
            //                   ManagerMobile = managerDetail.Mobile,
            //                   EmployeeMessage = n.EmployeeMessage,
            //                   ManagerComments = string.Empty,
            //                   CurrentStatus = (int)ItemStatus.Pending,
            //                   RequestedOn = DateTime.UtcNow,
            //                   AttendanceDate = n.AttendanceDate,
            //                   LeaveFromDate = DateTime.UtcNow,
            //                   LeaveToDate = DateTime.UtcNow,
            //                   Notify = JsonConvert.SerializeObject(n.NotifyList),
            //                   ExecutedByManager = n.ExecutedByManager,
            //                   ExecuterId = n.ExecuterId,
            //                   ExecuterName = n.ExecuterName,
            //                   ExecuterEmail = n.ExecuterEmail
            //               }).ToList();

            var records = (from n in compalintOrRequestWithEmail.CompalintOrRequestList
                           select new
                           {
                               AttendanceId = n.AttendanceId,
                               AttendanceStatus = (int)ItemStatus.MissingAttendanceRequest,
                               TotalMinutes = 480,
                               ComplaintOrRequestId = n.ComplaintOrRequestId,
                               RequestTypeId = (int)RequestType.Attendance,
                               TargetId = n.AttendanceId,
                               TargetOffset = n.TargetOffset,
                               EmployeeId = _currentSession.CurrentUserDetail.UserId,
                               EmployeeName = _currentSession.CurrentUserDetail.FullName,
                               Email = _currentSession.CurrentUserDetail.EmailId,
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

            var result = await _db.BulkExecuteAsync(Procedures.Complaint_Or_Request_InsUpdate, records, true);
            if (result != records.Count)
                throw HiringBellException.ThrowBadRequest("Fail to insert the record");

            List<string> allAttendanceDates = new List<string>();
            compalintOrRequestWithEmail.CompalintOrRequestList.ForEach(x =>
            {
                allAttendanceDates.Add(_timezoneConverter.ToTimeZoneDateTime(x.AttendanceDate, _currentSession.TimeZone).ToString("dddd, dd MMMM yyyy"));
            });

            AttendanceRequestModal attendanceRequestModal = new AttendanceRequestModal
            {
                ActionType = ApplicationConstants.Submitted,
                CompanyName = _currentSession.CurrentUserDetail.CompanyName,
                DeveloperName = _currentSession.CurrentUserDetail.FullName,
                ManagerName = managerDetail.FirstName + " " + managerDetail.LastName,
                Message = compalintOrRequestWithEmail.EmailBody,
                RequestType = workTypeId == (int)WorkType.WORKFROMHOME ? ApplicationConstants.WorkFromHome : ApplicationConstants.WorkFromOffice,
                ToAddress = new List<string> { managerDetail.Email },
                kafkaServiceName = KafkaServiceName.BlockAttendance,
                Body = string.Join(", ", allAttendanceDates),
                Note = compalintOrRequestWithEmail.CompalintOrRequestList[0].EmployeeMessage,
                LocalConnectionString = _currentSession.LocalConnectionString,
                CompanyId = _currentSession.CurrentUserDetail.CompanyId
            };
            return attendanceRequestModal;
        }

        public async Task<string> RaiseMissingAttendanceRequestService(ComplaintOrRequestWithEmail complaintOrRequestWithEmail)
        {
            if (complaintOrRequestWithEmail == null || complaintOrRequestWithEmail.CompalintOrRequestList.Count == 0)
                throw HiringBellException.ThrowBadRequest("Invalid request data passed. Please check your form again.");

            //if (complaintOrRequestWithEmail.AttendanceId <= 0)
            //    throw HiringBellException.ThrowBadRequest("Invalid request data passed. Please check your form again.");

            var alreadyApplied = complaintOrRequestWithEmail.CompalintOrRequestList
                .FindAll(x => x.CurrentStatus == (int)ItemStatus.MissingAttendanceRequest);

            if (alreadyApplied.Count > 0)
                throw HiringBellException.ThrowBadRequest("You already raise the request");

            //var anyRecord = complaintOrRequestWithEmail.CompalintOrRequestList.Any(x => x.TargetOffset == 0);
            var anyRecord = complaintOrRequestWithEmail.CompalintOrRequestList.Any(x => x.AttendanceId == 0);
            if (anyRecord)
                throw HiringBellException.ThrowBadRequest("Invalid data passed. Please contact to admin.");

            AttendanceRequestModal attendanceRequestModal = await InsertUpdateAttendanceRequest(complaintOrRequestWithEmail, complaintOrRequestWithEmail.AttendanceId);

            await _utilityService.SendNotification(attendanceRequestModal, KafkaTopicNames.ATTENDANCE_REQUEST_ACTION);
            //await this.AttendaceApprovalStatusSendEmail(complaintOrRequestWithEmail);
            return "Attendance raised successfully";
        }

        public AttendanceWithClientDetail EnablePermission(AttendenceDetail attendenceDetail)
        {
            if (attendenceDetail.ForMonth <= 0)
                throw new HiringBellException("Invalid month num. passed.", nameof(attendenceDetail.ForMonth), attendenceDetail.ForMonth.ToString());

            if (Convert.ToDateTime(attendenceDetail.AttendenceFromDay).Subtract(DateTime.UtcNow).TotalDays > 0)
            {
                throw new HiringBellException("Ohh!!!. Future dates are now allowed.");
            }


            var Result = _db.FetchDataSet(Procedures.Attendance_Get, new
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
            var result = _db.GetList<Attendance>(Procedures.Employee_Performance_Get, new
            {
                EmployeeId = attendanceDetail.EmployeeUid,
                UserTypeId = attendanceDetail.UserTypeId,
                ForYear = attendanceDetail.ForYear
            });

            var monthlyAttendance = result.Find(x => x.ForMonth == attendanceDetail.ForMonth);

            return new { MonthlyAttendance = monthlyAttendance, YearlyAttendance = result };
        }

        private async Task CheckAndCreateAttendance(AttendanceJson workingAttendance)
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

        private async Task CheckHalfdayAndFullday(AttendanceJson workingAttendance, Attendance attendance, long employeeId)
        {
            ShiftDetail shiftDetail = _db.Get<ShiftDetail>(Procedures.Work_Shifts_Getby_Empid, new { EmployeeId = employeeId });
            if (shiftDetail == null)
                throw HiringBellException.ThrowBadRequest("Employee shift detail not found. Please contact to admin");

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
                workingAttendance.TotalMinutes = shiftDetail.Duration / 2;
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
                workingAttendance.TotalMinutes = shiftDetail.Duration;
            }
            await Task.CompletedTask;
        }

        public string ConvertToMin(int mins)
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

        public async Task<DailyAttendance> AdjustAttendanceService(Attendance attendance)
        {
            if (attendance.AttendanceId == 0)
                throw HiringBellException.ThrowBadRequest("Invalid record send for applying.");

            //var attendancemonth = _timezoneConverter.ToIstTime(attendance.AttendanceDay).Month;
            //var attendanceyear = _timezoneConverter.ToIstTime(attendance.AttendanceDay).Year;

            // this value should come from database as configured by user.
            //int dailyWorkingHours = 8;
            //var attendanceList = new List<AttendanceJson>();

            // check for leave, holiday and weekends
            //await this.IsGivenDateAllowed(attendance.AttendanceDay);

            //var presentAttendance = _db.Get<Attendance>(Procedures.Attendance_Get_ById, new { AttendanceId = attendance.AttendanceId });
            //if (presentAttendance == null || string.IsNullOrEmpty(presentAttendance.AttendanceDetail))
            //    throw HiringBellException.ThrowBadRequest("Fail to get attendance detail");

            //attendanceList = JsonConvert
            //    .DeserializeObject<List<AttendanceJson>>(presentAttendance.AttendanceDetail);

            var workingattendance = _db.Get<DailyAttendance>(Procedures.Attendance_Get_ById, new { AttendanceId = attendance.AttendanceId });

            //var workingattendance = attendanceList.Find(x => x.AttendenceDetailId == attendance.AttendenceDetailId);
            //workingattendance.UserComments = attendance.UserComments;
            //workingattendance.PresentDayStatus = (int)ItemStatus.Approved;
            //int pendingDays = attendanceList.Count(x => x.PresentDayStatus == (int)ItemStatus.Pending);
            //presentAttendance.DaysPending = pendingDays;
            //presentAttendance.TotalHoursBurend = pendingDays * dailyWorkingHours;
            //workingattendance.WorkTypeId = (int)WorkType.WORKFROMHOME;
            if (workingattendance == null)
                throw HiringBellException.ThrowBadRequest("Fail to get attendance detail");

            workingattendance.Comments = attendance.UserComments;
            workingattendance.AttendanceStatus = (int)ItemStatus.Approved;
            workingattendance.TotalMinutes = 480;
            workingattendance.WorkTypeId = WorkType.WORKFROMHOME;

            string Result = _db.Execute<DailyAttendance>(Procedures.DAILY_ATTENDANCE_INS_UPD_WEEKLY, new
            {
                AttendanceId = workingattendance.AttendanceId,
                EmployeeId = workingattendance.EmployeeId,
                EmployeeName = workingattendance.EmployeeName,
                EmployeeEmail = workingattendance.EmployeeEmail,
                ReviewerId = workingattendance.ReviewerId,
                ReviewerName = workingattendance.ReviewerName,
                ReviewerEmail = workingattendance.ReviewerEmail,
                ProjectId = workingattendance.ProjectId,
                TaskId = workingattendance.TaskId,
                TaskType = workingattendance.TaskType,
                LogOn = workingattendance.LogOn,
                LogOff = workingattendance.LogOff,
                TotalMinutes = workingattendance.TotalMinutes,
                Comments = string.IsNullOrEmpty(workingattendance.Comments) ? "[]" : JsonConvert.SerializeObject(workingattendance.Comments),
                AttendanceStatus = workingattendance.AttendanceStatus,
                WeekOfYear = workingattendance.WeekOfYear,
                AttendanceDate = workingattendance.AttendanceDate,
                WorkTypeId = workingattendance.WorkTypeId,
                IsOnLeave = workingattendance.IsOnLeave,
                LeaveId = workingattendance.LeaveId,
                CreatedBy = _currentSession.CurrentUserDetail.UserId
            }, true);

            //string Result = _db.Execute<Attendance>(Procedures.Attendance_Insupd, new
            //{
            //    AttendanceId = presentAttendance.AttendanceId,
            //    AttendanceDetail = JsonConvert.SerializeObject(attendanceList),
            //    UserTypeId = presentAttendance.UserTypeId,
            //    EmployeeId = presentAttendance.EmployeeId,
            //    TotalDays = presentAttendance.TotalDays,
            //    TotalWeekDays = presentAttendance.TotalWeekDays,
            //    DaysPending = presentAttendance.DaysPending,
            //    TotalBurnedMinutes = presentAttendance.TotalHoursBurend,
            //    ForYear = presentAttendance.ForYear,
            //    ForMonth = presentAttendance.ForMonth,
            //    UserId = _currentSession.CurrentUserDetail.EmployeeId,
            //    PendingRequestCount = ++presentAttendance.PendingRequestCount,
            //    EmployeeName = presentAttendance.EmployeeName,
            //    Email = presentAttendance.Email,
            //    Mobile = presentAttendance.Mobile,
            //    ReportingManagerId = presentAttendance.ReportingManagerId,
            //    ManagerName = presentAttendance.ManagerName
            //}, true);

            if (string.IsNullOrEmpty(Result))
                throw new HiringBellException("Unable submit the attendace");

            AttendanceRequestModal attendanceRequestModal = new AttendanceRequestModal
            {
                ActionType = ApplicationConstants.Approved,
                CompanyName = _currentSession.CurrentUserDetail.CompanyName,
                DayCount = 1,
                DeveloperName = workingattendance.EmployeeName,
                FromDate = _timezoneConverter.ToTimeZoneDateTime(attendance.AttendanceDay, _currentSession.TimeZone),
                ManagerName = _currentSession.CurrentUserDetail.FullName,
                Message = workingattendance.Comments,
                RequestType = attendance.WorkTypeId == (int)WorkType.WORKFROMHOME ? ApplicationConstants.WorkFromHome : ApplicationConstants.WorkFromOffice,
                ToAddress = new List<string> { workingattendance.EmployeeEmail },
                kafkaServiceName = KafkaServiceName.Attendance,
                LocalConnectionString = _currentSession.LocalConnectionString,
                CompanyId = _currentSession.CurrentUserDetail.CompanyId
            };

            await _utilityService.SendNotification(attendanceRequestModal, KafkaTopicNames.ATTENDANCE_REQUEST_ACTION);

            return workingattendance;
        }

        public Task<List<LOPAdjustmentDetail>> GetLOPAdjustmentService(int month, int year)
        {
            List<LOPAdjustmentDetail> lOPAdjustmentDetails = new List<LOPAdjustmentDetail>();
            var date = new DateTime(year, month, 1);
            var FromDate = _timezoneConverter.ToUtcTime(date, _currentSession.TimeZone);
            var ToDate = _timezoneConverter.ToUtcTime(date.AddMonths(1).AddDays(-1), _currentSession.TimeZone);

            var ds = _db.GetDataSet(Procedures.Leave_And_Lop_Get, new
            {
                FromDate,
                ToDate,
                _currentSession.CurrentUserDetail.CompanyId
            }, false);

            if (ds == null && ds.Tables.Count != 3)
                throw HiringBellException.ThrowBadRequest("LOP detail not found. Please contact to admin.");

            if (ds.Tables[1].Rows.Count == 0)
                throw new HiringBellException("Fail to get employee attendance details. Please contact to admin.");

            if (ds.Tables[2].Rows.Count == 0)
                throw new HiringBellException("Fail to get attendance setting details. Please contact to admin.");

            AttendanceSetting attendanceSetting = Converter.ToType<AttendanceSetting>(ds.Tables[2]);
            List<LeaveRequestNotification> leaveRequestNotifications = Converter.ToList<LeaveRequestNotification>(ds.Tables[0]);
            List<DailyAttendance> dailyAttendances = Converter.ToList<DailyAttendance>(ds.Tables[1]);

            int daysLimit = attendanceSetting.BackDateLimitToApply + 1;
            var employees = dailyAttendances.GroupBy(x => x.EmployeeId).Select(g => g.First()).ToList();
            employees.ForEach(x =>
            {
                List<LeaveRequestNotification> leaves = null;
                if (leaveRequestNotifications.Count > 0)
                    leaves = leaveRequestNotifications.FindAll(i => i.EmployeeId == x.EmployeeId && i.RequestStatusId == (int)ItemStatus.Approved);

                DateTime lastAppliedDate = DateTime.UtcNow.AddDays(-daysLimit);
                List<DailyAttendance> lopAttendnace = dailyAttendances.FindAll(a => a.EmployeeId == x.EmployeeId
                                                                                        && !a.IsOnLeave
                                                                                        && ((a.AttendanceStatus == (int)ItemStatus.Approved && a.TotalMinutes == 0)
                                                                                        || (a.AttendanceStatus != (int)ItemStatus.Approved
                                                                                        && a.AttendanceStatus != (int)ItemStatus.Canceled)));
                if (lopAttendnace != null && lopAttendnace.Count > 0)
                {
                    lOPAdjustmentDetails.Add(new LOPAdjustmentDetail
                    {
                        ActualLOP = lopAttendnace.Count,
                        Email = x.EmployeeEmail,
                        EmployeeId = x.EmployeeId,
                        EmployeeName = x.EmployeeName,
                        BlockedDates = lopAttendnace.Select(i => i.AttendanceDate).ToList()
                    });
                }
            });


            //List<Attendance> attendance = Converter.ToList<Attendance>(ds.Tables[1]);
            //attendance.ForEach(x =>
            //{
            //    int daysLimit = attendanceSetting.BackDateLimitToApply + 1;
            //    List<AttendanceJson> attendanceDetail = JsonConvert.DeserializeObject<List<AttendanceJson>>(x.AttendanceDetail);
            //    List<LeaveRequestNotification> leaves = null;
            //    if (leaveRequestNotifications.Count > 0)
            //        leaves = leaveRequestNotifications.FindAll(i => i.EmployeeId == x.EmployeeId && i.RequestStatusId == (int)ItemStatus.Approved);

            //    DateTime lastAppliedDate = DateTime.UtcNow.AddDays(-daysLimit);
            //    //attendanceDetail.ForEach(i =>
            //    //{
            //    //    if (leaves != null && leaves.Count > 0)
            //    //    {
            //    //        var leaveDetail = leaves.Find(x => x.FromDate.Date.Subtract(i.AttendanceDay.Date).TotalDays <= 0 && x.ToDate.Date.Subtract(i.AttendanceDay.Date).TotalDays >= 0);
            //    //        if (leaveDetail != null && leaveDetail.RequestStatusId == (int)ItemStatus.Approved)
            //    //        {
            //    //            i.IsOnLeave = true;
            //    //            i.IsOpen = false;
            //    //        }
            //    //    }

            //    //    // if (!i.IsOnLeave && i.PresentDayStatus != 3 && i.PresentDayStatus != 4 && i.AttendanceDay.Date.Subtract(lastAppliedDate.Date).TotalDays <= 0)
            //    //    if (!i.IsOnLeave && i.PresentDayStatus != 9)
            //    //        i.IsOpen = false;

            //    //});

            //    List<AttendanceJson> blockedAttendance = attendanceDetail.FindAll(a => a.PresentDayStatus != (int)ItemStatus.Approved && a.PresentDayStatus != (int)ItemStatus.Canceled);
            //    if (blockedAttendance != null && blockedAttendance.Count > 0)
            //    {
            //        lOPAdjustmentDetails.Add(new LOPAdjustmentDetail
            //        {
            //            ActualLOP = blockedAttendance.Count,
            //            Email = x.Email,
            //            EmployeeId = x.EmployeeId,
            //            EmployeeName = x.EmployeeName,
            //            BlockedDates = blockedAttendance.Select(x => x.AttendanceDay).ToList()
            //        });
            //    }
            //});

            return Task.FromResult(lOPAdjustmentDetails);
        }

        public async Task<List<ComplaintOrRequest>> ApproveRaisedAttendanceRequestService(List<ComplaintOrRequest> complaintOrRequests)
        {
            return await UpdateRequestRaised(complaintOrRequests, (int)ItemStatus.Approved);
        }

        public async Task<List<ComplaintOrRequest>> RejectRaisedAttendanceRequestService(List<ComplaintOrRequest> complaintOrRequests)
        {
            return await UpdateRequestRaised(complaintOrRequests, (int)ItemStatus.Rejected);
        }

        private async Task<DailyAttendance> GetCurrentAttendanceRequestData(List<ComplaintOrRequest> complaintOrRequests, int itemStatus)
        {
            var first = complaintOrRequests.First();
            var attendance = _db.Get<DailyAttendance>(Procedures.Attendance_Get_ById, new
            {
                AttendanceId = first.TargetId
            });

            if (attendance == null)
                throw HiringBellException.ThrowBadRequest("Fail to get attendance detail");

            attendance.AttendanceStatus = itemStatus;
            attendance.ReviewerName = _currentSession.CurrentUserDetail.FullName;
            attendance.ReviewerId = _currentSession.CurrentUserDetail.UserId;
            attendance.ReviewerEmail = _currentSession.CurrentUserDetail.EmailId;
            attendance.TotalMinutes = 480;

            //var resultSet = _db.GetDataSet(Procedures.Attendance_Get_ById, new
            //{
            //    AttendanceId = first.TargetId
            //});

            //if (resultSet.Tables.Count != 1)
            //    throw HiringBellException.ThrowBadRequest("Fail to get current attendance detail.");

            //var attendance = Converter.ToType<Attendance>(resultSet.Tables[0]);
            //if (attendance == null)
            //    throw HiringBellException.ThrowBadRequest("Fail to get current attendance detail.");

            //if (string.IsNullOrEmpty(attendance.AttendanceDetail) || attendance.AttendanceDetail == "[]")
            //    throw HiringBellException.ThrowBadRequest("Invalid attendance detail found. Please contact to admin.");

            //var attendanceDetail = JsonConvert.DeserializeObject<List<AttendanceDetailJson>>(attendance.AttendanceDetail);
            //var currentAttr = attendanceDetail.Find(x => x.AttendenceDetailId == first.TargetOffset);
            //if (currentAttr == null)
            //    throw HiringBellException.ThrowBadRequest("Invalid attendance detail found. Please contact to admin.");

            //currentAttr.PresentDayStatus = itemStatus;
            //currentAttr.ApprovedName = _currentSession.CurrentUserDetail.FullName;
            //currentAttr.ApprovedBy = _currentSession.CurrentUserDetail.UserId;
            ////var logoff = currentAttr.LogOff;
            ////var logofftime = logoff.Replace(":", ".");
            ////decimal time = decimal.Parse(logofftime);
            ////var totaltime = (int)((time * 60) * 2);
            //currentAttr.LogOff = currentAttr.LogOff;
            //currentAttr.LogOn = currentAttr.LogOn;
            //currentAttr.SessionType = currentAttr.SessionType;
            //currentAttr.TotalMinutes = currentAttr.TotalMinutes;
            //attendance.AttendanceDetail = JsonConvert.SerializeObject(attendanceDetail);

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
            //if (_currentSession.CurrentUserDetail.UserId == attendance.ReportingManagerId)
            //    isManager = true;

            if (_currentSession.CurrentUserDetail.EmailId == attendance.ManagerEmail)
                isManager = true;

            //var items = (from n in complaintOrRequests
            //             select new
            //             {
            //                 ComplaintOrRequestId = n.ComplaintOrRequestId,
            //                 ExecutedByManager = isManager,
            //                 ExecuterId = _currentSession.CurrentUserDetail.UserId,
            //                 ExecuterName = _currentSession.CurrentUserDetail.FullName,
            //                 ExecuterEmail = _currentSession.CurrentUserDetail.EmailId,
            //                 ManagerComments = n.ManagerComments,
            //                 StatusId = itemStatus,
            //                 AttendanceId = attendance.AttendanceId,
            //                 AttendanceDetail = attendance.AttendanceDetail,
            //                 UserId = _currentSession.CurrentUserDetail.UserId
            //             }).ToList();

            var items = (from n in complaintOrRequests
                         select new
                         {
                             ComplaintOrRequestId = n.ComplaintOrRequestId,
                             ExecutedByManager = isManager,
                             ExecuterId = _currentSession.CurrentUserDetail.UserId,
                             ExecuterName = _currentSession.CurrentUserDetail.FullName,
                             ExecuterEmail = _currentSession.CurrentUserDetail.EmailId,
                             ManagerComments = n.ManagerComments,
                             StatusId = itemStatus,
                             AttendanceId = attendance.AttendanceId,
                             AttendanceStatus = attendance.AttendanceStatus,
                             ReviewerName = attendance.ReviewerName,
                             ReviewerId = attendance.ReviewerId,
                             ReviewerEmail = attendance.ReviewerEmail,
                             TotalMinutes = attendance.TotalMinutes,
                             UserId = _currentSession.CurrentUserDetail.UserId
                         }).ToList();


            var result = await _db.BulkExecuteAsync(Procedures.Complaint_Or_Request_Update_Status, items);
            if (result != items.Count)
                throw HiringBellException.ThrowBadRequest("Failed to update complain request");

            var filter = new FilterModel();
            filter.SearchString = string.Empty;
            return await GetMissingAttendanceApprovalRequestService(filter);
        }

        public async Task GenerateMonthlyAttendance()
        {
            DateTime lastAttendanceDate = new DateTime(2024, 1, 29);
            DateTime firstDayOfNextMonth = new DateTime(lastAttendanceDate.Year, lastAttendanceDate.Month, 1).AddMonths(1);
            DateTime lastDayOfNextMonth = firstDayOfNextMonth.AddMonths(1).AddDays(-1);

            if (lastDayOfNextMonth.DayOfWeek != DayOfWeek.Sunday)
            {
                int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)lastDayOfNextMonth.DayOfWeek - 7) % 7;
                lastDayOfNextMonth = lastDayOfNextMonth.AddDays(daysUntilSunday);
            }
            var attendance = new AttendenceDetail
            {
                AttendenceFromDay = lastAttendanceDate.AddDays(1),
                AttendenceToDay = lastDayOfNextMonth,
                AttendenceStatus = (int)ItemStatus.NotSubmitted
            };

            await GenerateAttendanceService(attendance);

            await Task.CompletedTask;
        }


        #region ATTENDANCE_NEW_CLASS

        public async Task<dynamic> LoadAttendanceConfigDataService(long EmployeeId)
        {
            if (EmployeeId == 0)
                throw HiringBellException.ThrowBadRequest("Invalid employee id passed");

            return await GetAttendanceConfiData(EmployeeId);
        }

        private async Task<dynamic> GetAttendanceConfiData(long EmployeeId)
        {
            var ds = await _db.GetDataSetAsync(Procedures.DAILY_ATTENDANCE_CONFIG_DATA, new
            {
                EmployeeId
            });

            if (ds.Tables.Count != 5)
                throw HiringBellException.ThrowBadRequest("Employee and project detail not found.");

            if (ds.Tables[0].Rows.Count == 0)
                throw HiringBellException.ThrowBadRequest("Attendance detail not found.");

            if (ds.Tables[3].Rows.Count == 0)
                throw HiringBellException.ThrowBadRequest("Employee detail not found.");

            var companySetting = Converter.ToType<CompanySetting>(ds.Tables[3]);
            var dailyAttendances = ProcessAttendanceRecords(ds);
            var requestAttendance = FilterAttendancesByStatus(dailyAttendances, AttendanceEnum.NotSubmitted, companySetting.AttendanceType);

            return new
            {
                AttendanceType = companySetting.AttendanceType,
                Projects = Converter.ToList<Project>(ds.Tables[4]),
                ShiftDetail = Converter.ToType<ShiftDetail>(ds.Tables[2]),
                RequestAttendance = requestAttendance,
                AttendanceLogs = GetAttendanceLogs(dailyAttendances, companySetting.AttendanceType),
                PendingRequest = FilterPendingAttendance(dailyAttendances, companySetting.AttendanceType),
                Weeks = companySetting.AttendanceType ? await GenerateWeeks(requestAttendance) : null
            };
        }

        private async Task<List<WeekDates>> GenerateWeeks(Dictionary<long, List<DailyAttendance>> dailyAttendances)
        {
            var attendanceWeeks = new List<WeekDates>();
            foreach (var item in dailyAttendances)
            {
                var startDate = _timezoneConverter.ToTimeZoneDateTime(item.Value.First().AttendanceDate, _currentSession.TimeZone);
                var endDate = _timezoneConverter.ToTimeZoneDateTime(item.Value.Last().AttendanceDate, _currentSession.TimeZone);

                attendanceWeeks.Add(new WeekDates
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    WeekIndex = GetWeekIndex(startDate)
                });
            }

            return await Task.FromResult(attendanceWeeks);
        }

        private dynamic FilterPendingAttendance(List<DailyAttendance> dailyAttendances, bool attendanceType)
        {
            dailyAttendances = dailyAttendances.FindAll(x => x.AttendanceStatus == (int)AttendanceEnum.Submitted || x.AttendanceStatus == 8);

            if (attendanceType)
                return GetGroupAttendance(dailyAttendances);
            else
                return dailyAttendances;
        }

        private Dictionary<long, List<DailyAttendance>> GetGroupAttendance(List<DailyAttendance> dailyAttendances)
        {
            var groupedByWeek = dailyAttendances
               .Select(attendance =>
               {
                   var localDate = _timezoneConverter.ToTimeZoneDateTime(attendance.AttendanceDate, _currentSession.TimeZone);

                   var isoWeek = ISOWeek.GetWeekOfYear(localDate);
                   var isoYear = localDate.Month == 12 && isoWeek == 1
                       ? localDate.Year + 1
                       : (localDate.Month == 1 && isoWeek >= 52 ? localDate.Year - 1 : localDate.Year);

                   return new
                   {
                       Attendance = attendance,
                       WeekKey = $"{isoYear}-{isoWeek}",
                       LocalDate = localDate
                   };
               })
               .GroupBy(x => x.WeekKey)
               .OrderBy(x => x.Key);

            var result = new Dictionary<long, List<DailyAttendance>>();

            foreach (var weekGroup in groupedByWeek)
            {
                var minId = weekGroup.Min(x => x.Attendance.AttendanceId);
                result[minId] = weekGroup.OrderBy(x => x.LocalDate).Select(x => x.Attendance).ToList();
            }

            return result;
        }

        private dynamic FilterAttendancesByStatus(List<DailyAttendance> attendances, AttendanceEnum status, bool attendanceType)
        {

            attendances = attendances.FindAll(x => x.AttendanceStatus == (int)AttendanceEnum.NotSubmitted
                                        && x.AttendanceStatus != (int)AttendanceEnum.WeekOff
                                        && !x.IsOnLeave && !x.IsHoliday);

            if (attendanceType)
                return GetGroupAttendance(attendances);
            else
                return attendances;
        }

        private dynamic GetAttendanceLogs(List<DailyAttendance> dailyAttendances, bool attendanceType)
        {
            dailyAttendances = dailyAttendances.FindAll(x => x.AttendanceStatus == (int)AttendanceEnum.Approved
                                                    || x.AttendanceStatus == (int)AttendanceEnum.Rejected
                                                    || x.AttendanceStatus == (int)AttendanceEnum.WeekOff
                                                    || (x.IsHoliday && x.HolidayId > 0)
                                                    || x.IsOnLeave);

            if (attendanceType)
                return GetGroupAttendance(dailyAttendances);
            else
                return dailyAttendances;
        }

        private List<DailyAttendance> ProcessAttendanceRecords(DataSet dataset)
        {
            var dailyAttendances = Converter.ToList<DailyAttendance>(dataset.Tables[0]);
            var leaveRequests = Converter.ToList<LeaveRequestNotification>(dataset.Tables[1]);
            var shiftDetail = Converter.ToType<ShiftDetail>(dataset.Tables[2]);

            return GetDailyAttendanceUpdateDetail(dailyAttendances, leaveRequests, shiftDetail);
        }

        private List<DailyAttendance> GetDailyAttendanceUpdateDetail(List<DailyAttendance> dailyAttendances, List<LeaveRequestNotification> LeaveRequestDetail, ShiftDetail shiftDetail)
        {
            dailyAttendances = dailyAttendances.OrderBy(i => i.AttendanceDate).ToList();

            if (LeaveRequestDetail.Count > 0)
            {
                foreach (var item in dailyAttendances)
                {
                    var leaveDetail = LeaveRequestDetail
                                        .Find(
                                                x => _timezoneConverter.ToTimeZoneDateTime(x.FromDate, _currentSession.TimeZone).Date
                                                        .Subtract(item.AttendanceDate.Date).TotalDays <= 0
                                                    && _timezoneConverter.ToTimeZoneDateTime(x.ToDate, _currentSession.TimeZone).Date
                                                        .Subtract(item.AttendanceDate.Date).TotalDays >= 0
                                             );

                    if (leaveDetail != null && leaveDetail.RequestStatusId == (int)ItemStatus.Approved)
                    {
                        item.IsOnLeave = true;
                    }
                }
            }

            foreach (var item in dailyAttendances)
            {
                var attendanceDate = _timezoneConverter.ToTimeZoneDateTime(item.AttendanceDate, _currentSession.TimeZone);
                item.AttendanceStatus = CheckWeekend(shiftDetail, attendanceDate)
                                        ? (int)AttendanceEnum.WeekOff
                                        : (item.AttendanceStatus != (int)AttendanceEnum.WeekOff ? item.AttendanceStatus : (int)AttendanceEnum.NotSubmitted);
            }

            return dailyAttendances;
        }

        private int GetWeekIndex(DateTime date)
        {
            var firstDayOfYear = new DateTime(date.Year, 1, 1);
            var startOfWeek = firstDayOfYear.AddDays(-(int)firstDayOfYear.DayOfWeek + (int)DayOfWeek.Monday);

            return (int)((date - startOfWeek).TotalDays / 7) + 1;
        }

        public async Task<AttendanceWithClientDetail> GetDailyAttendanceByUserIdService(WeekDates weekDates)
        {
            AttendanceWithClientDetail detail = await GetUserAttendance(weekDates);

            detail.DailyAttendances = detail.DailyAttendances.OrderBy(i => i.AttendanceDate).ToList();

            if (detail.LeaveRequestDetail.Count > 0)
            {
                foreach (var item in detail.DailyAttendances)
                {
                    var leaveDetail = detail.LeaveRequestDetail
                                        .Find(
                                                x => _timezoneConverter.ToTimeZoneDateTime(x.FromDate, _currentSession.TimeZone).Date
                                                        .Subtract(item.AttendanceDate.Date).TotalDays <= 0
                                                    && _timezoneConverter.ToTimeZoneDateTime(x.ToDate, _currentSession.TimeZone).Date
                                                        .Subtract(item.AttendanceDate.Date).TotalDays >= 0
                                             );

                    if (leaveDetail != null && leaveDetail.RequestStatusId == (int)ItemStatus.Approved)
                    {
                        item.IsOnLeave = true;
                    }
                }
            }

            return detail;
        }

        private async Task<Tuple<DateTime, DateTime>> BuildDates()
        {
            DateTime PresentDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day);

            DateTime FromDate = new DateTime(PresentDate.Year, PresentDate.Month, 1).AddMonths(-1);
            FromDate = _timezoneConverter.FirstDayOfPresentWeek(FromDate, _currentSession.TimeZone);
            DateTime ToDate = _timezoneConverter.LastDayOfPresentWeek(PresentDate, _currentSession.TimeZone);

            return await Task.FromResult(Tuple.Create(FromDate, ToDate));
        }

        private async Task<AttendanceWithClientDetail> GetUserAttendance(WeekDates weekDates)
        {
            AttendanceWithClientDetail attendanceWithClientDetail = new AttendanceWithClientDetail();
            var attendanceDs = await _db.GetDataSetAsync(Procedures.DAILY_ATTENDANCE_BY_USER, new
            {
                FromDate = weekDates.StartDate.AddDays(-1),
                ToDate = weekDates.EndDate,
                EmployeeId = _currentSession.CurrentUserDetail.UserId
            });

            ValidateAttendanceResult(attendanceDs);

            attendanceWithClientDetail.DailyAttendances = Converter.ToList<DailyAttendance>(attendanceDs.Tables[0]);
            attendanceWithClientDetail.LeaveRequestDetail = Converter.ToList<LeaveRequestNotification>(attendanceDs.Tables[1]);
            attendanceWithClientDetail.EmployeeDetail = Converter.ToType<Employee>(attendanceDs.Tables[2]);
            attendanceWithClientDetail.EmployeeShift = Converter.ToType<ShiftDetail>(attendanceDs.Tables[3]);

            UpdateShiftInAttendance(attendanceWithClientDetail);
            return attendanceWithClientDetail;
        }

        private void UpdateShiftInAttendance(AttendanceWithClientDetail attendanceWithClientDetail)
        {
            foreach (var item in attendanceWithClientDetail.DailyAttendances)
            {
                var attendanceDate = _timezoneConverter.ToTimeZoneDateTime(item.AttendanceDate, _currentSession.TimeZone);
                item.AttendanceStatus = CheckWeekend(attendanceWithClientDetail.EmployeeShift, attendanceDate)
                    ? (int)AttendanceEnum.WeekOff
                    : (item.AttendanceStatus != (int)AttendanceEnum.WeekOff ? item.AttendanceStatus : (int)AttendanceEnum.NotSubmitted);
            }
        }

        private void ValidateAttendanceResult(DataSet attendanceDs)
        {
            if (attendanceDs.Tables.Count != 4)
                throw HiringBellException.ThrowBadRequest("Attendance detail not found.");

            if (attendanceDs.Tables[0].Rows.Count == 0)
                throw HiringBellException.ThrowBadRequest("Attendance detail not found.");

            if (attendanceDs.Tables[2].Rows.Count == 0)
                throw HiringBellException.ThrowBadRequest("Employee detail not found.");

            if (attendanceDs.Tables[3].Rows.Count == 0)
                throw HiringBellException.ThrowBadRequest("Shift detail not found.");
        }

        public async Task<dynamic> SaveDailyAttendanceService(List<DailyAttendance> attendances)
        {
            if (attendances.Count == 0)
                throw HiringBellException.ThrowBadRequest("Attendance record not found");

            return await UpdateDailyAttendanceService(attendances, ItemStatus.Saved);
        }

        public async Task<dynamic> SubmitDailyAttendanceService(List<DailyAttendance> attendances)
        {
            if (attendances.Count == 0)
                throw HiringBellException.ThrowBadRequest("Attendance record not found");

            return await UpdateDailyAttendanceService(attendances, ItemStatus.Submitted);
        }

        private async Task<dynamic> UpdateDailyAttendanceService(List<DailyAttendance> attendances, ItemStatus status)
        {
            try
            {
                attendances = await PrepareAttendanceForUpdate(attendances, status);
                var result = await _db.BulkExecuteAsync(Procedures.DAILY_ATTENDANCE_INS_UPD_WEEKLY, (
                from n in attendances
                select new
                {
                    n.AttendanceId,
                    n.EmployeeId,
                    n.EmployeeName,
                    n.EmployeeEmail,
                    n.ReviewerId,
                    n.ReviewerName,
                    n.ReviewerEmail,
                    n.ProjectId,
                    n.TaskId,
                    n.TaskType,
                    n.LogOn,
                    n.LogOff,
                    n.TotalMinutes,
                    n.Comments,
                    n.AttendanceStatus,
                    n.WeekOfYear,
                    n.AttendanceDate,
                    WorkTypeId = (int)n.WorkTypeId,
                    n.IsOnLeave,
                    n.LeaveId,
                    n.CreatedBy
                }).ToList(), true);

                if (result != attendances.Count)
                    throw HiringBellException.ThrowBadRequest("Fail to update attendance request");

                AttendanceRequestModal attendanceRequestModal = new AttendanceRequestModal
                {
                    ActionType = ApplicationConstants.Submitted,
                    CompanyName = _currentSession.CurrentUserDetail.CompanyName,
                    DayCount = 1,
                    DeveloperName = attendances[0].EmployeeName,
                    FromDate = _timezoneConverter.ToTimeZoneDateTime(attendances[0].AttendanceDate, _currentSession.TimeZone),
                    ToDate = _timezoneConverter.ToTimeZoneDateTime(attendances.Last().AttendanceDate, _currentSession.TimeZone),
                    ManagerName = _currentSession.CurrentUserDetail.FullName,
                    Message = attendances[0].Comments,
                    RequestType = (int)attendances[0].WorkTypeId == (int)WorkType.WORKFROMHOME ? ApplicationConstants.WorkFromHome : ApplicationConstants.WorkFromOffice,
                    ToAddress = new List<string> { _currentSession.CurrentUserDetail.ManagerEmailId },
                    kafkaServiceName = KafkaServiceName.Attendance,
                    LocalConnectionString = _currentSession.LocalConnectionString,
                    CompanyId = _currentSession.CurrentUserDetail.CompanyId
                };

                await _utilityService.SendNotification(attendanceRequestModal, KafkaTopicNames.ATTENDANCE_REQUEST_ACTION);

                return await GetAttendanceConfiData(attendances[0].EmployeeId);
            }
            catch (Exception e)
            {
                throw HiringBellException.ThrowBadRequest(e.Message);
            }
        }

        private async Task<List<DailyAttendance>> PrepareAttendanceForUpdate(List<DailyAttendance> attendances, ItemStatus status)
        {
            attendances = attendances.OrderBy(x => x.AttendanceDate).ToList();
            DateTime startDate = attendances.First().AttendanceDate;
            DateTime endDate = attendances.Last().AttendanceDate;

            var attendanceDs = await _db.GetDataSetAsync(Procedures.DAILY_ATTENDANCE_BET_DATES_EMPID, new
            {
                FromDate = startDate,
                ToDate = endDate,
                EmployeeId = _currentSession.CurrentUserDetail.UserId
            });

            if (attendanceDs.Tables.Count != 2)
                throw HiringBellException.ThrowBadRequest("Attendance and Shift detail invalid");

            List<DailyAttendance> dailyAttendance = Converter.ToList<DailyAttendance>(attendanceDs.Tables[0]);
            ShiftDetail workShift = Converter.ToType<ShiftDetail>(attendanceDs.Tables[1]);
            return await updateAttendanceRecord(dailyAttendance, attendances, workShift, status);
        }

        private async Task<List<DailyAttendance>> updateAttendanceRecord(List<DailyAttendance> dailyAttendances, List<DailyAttendance> attendances,
                                                                        ShiftDetail shiftDetail, ItemStatus status)
        {
            foreach (var attendance in dailyAttendances)
            {
                if (attendance.AttendanceId == 0)
                    throw HiringBellException.ThrowBadRequest("Invalid record send for applying.");

                if (attendance.AttendanceDate.Year <= 1900)
                    throw HiringBellException.ThrowBadRequest("Fail to get attendance detail");

                var attr = attendances.Find(x => x.AttendanceId == attendance.AttendanceId);
                if (attr == null)
                    throw HiringBellException.ThrowBadRequest($"Attendance not found for date: {attendance.AttendanceDate}");

                attendance.ProjectId = attr.ProjectId;
                attendance.TaskId = attr.TaskId;
                attendance.TaskType = attr.TaskType;
                attendance.LogOn = attr.LogOn;
                attendance.LogOff = attr.LogOff;
                attendance.TotalMinutes = attr.TotalMinutes;
                attendance.Comments = attr.Comments;
                attendance.AttendanceStatus = (int)status;
                attendance.WorkTypeId = attr.WorkTypeId;
                attendance.IsOnLeave = attr.IsOnLeave;
                attendance.LeaveId = attr.LeaveId;

                // check for leave
                await this.CheckIsOnPaidLeave(attendance, shiftDetail);
            }

            return dailyAttendances;
        }

        private bool CheckIsHoliday(DateTime date, List<ModalLayer.Calendar> calendars)
        {
            bool flag = false;

            var records = calendars.FirstOrDefault(x => x.StartDate.Date >= date.Date && x.EndDate.Date <= date.Date);
            if (records != null)
                flag = true;

            return flag;
        }

        private async Task<DailyAttendance> CheckIsOnPaidLeave(DailyAttendance dailyAttendance, ShiftDetail shiftDetail)
        {
            // check if from date is holiday
            if (dailyAttendance.IsHoliday)
            {
                dailyAttendance.TotalMinutes = shiftDetail.Duration;
            }
            else if (dailyAttendance.IsOnLeave) // check if already on leave
            {
                var leaveType = _db.Get<LeavePlanType>(Procedures.LEAVE_PLAN_TYPE_BY_LEAVEID, new
                {
                    dailyAttendance.LeaveId
                });

                if (leaveType == null)
                    throw HiringBellException.ThrowBadRequest("Invalid leave id");

                if (leaveType.IsPaidLeave)
                    dailyAttendance.TotalMinutes = shiftDetail.Duration;
                else
                    dailyAttendance.TotalMinutes = 0;
            }
            else // check shift weekends
            {
                var attendanceDate = _timezoneConverter.ToTimeZoneDateTime(dailyAttendance.AttendanceDate, _currentSession.TimeZone);
                dailyAttendance.IsWeekend = CheckWeekend(shiftDetail, attendanceDate);
                if (dailyAttendance.IsWeekend)
                {
                    dailyAttendance.TotalMinutes = shiftDetail.Duration;
                    dailyAttendance.AttendanceStatus = (int)DayStatus.Weekend;
                }
            }

            return await Task.FromResult(dailyAttendance);
        }

        public async Task<Dictionary<long, List<DailyAttendance>>> GetAttendancePageService(FilterModel filterModel)
        {
            if (filterModel.ForMonth == 0)
                throw HiringBellException.ThrowBadRequest("Invalid month selected");

            if (filterModel.ForYear == 0)
                throw HiringBellException.ThrowBadRequest("Invalid year selected");

            var date = new DateTime(filterModel.ForYear, filterModel.ForMonth, 1);
            var fromDate = _timezoneConverter.ToUtcTime(date, _currentSession.TimeZone);
            var toDate = _timezoneConverter.ToUtcTime(date.AddMonths(1).AddDays(-1), _currentSession.TimeZone);
            var dailyAttendances = await GetFilteredDetailAttendanceService(filterModel, fromDate, toDate);
            return dailyAttendances.OrderBy(x => x.AttendanceDate)
                                                        .GroupBy(x => x.EmployeeId)
                                                        .ToDictionary(a => a.Key, a => a.ToList());
        }

        public async Task<Dictionary<long, List<DailyAttendance>>> GetRecentWeeklyAttendanceService(FilterModel filterModel)
        {
            (DateTime fromDate, DateTime toDate) = await BuildDates();
            fromDate = fromDate.AddDays(-1);
            var dailyAttendances = await GetFilteredDetailAttendanceService(filterModel, fromDate, toDate);
            var groupRecord = dailyAttendances.OrderBy(x => _timezoneConverter.ToTimeZoneDateTime(x.AttendanceDate, _currentSession.TimeZone))
                .GroupBy(r => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(_timezoneConverter.ToTimeZoneDateTime(r.AttendanceDate, _currentSession.TimeZone), CalendarWeekRule.FirstDay, DayOfWeek.Monday));

            var attendanceGroupedRequest = groupRecord.ToDictionary(x => x.First().AttendanceId, x => x.ToList());
            foreach (var record in attendanceGroupedRequest)
            {
                record.Value.ForEach(x =>
                {
                    x.Total = groupRecord.Count();
                });
            }

            return attendanceGroupedRequest;
        }

        public async Task<List<DailyAttendance>> GetRecentDailyAttendanceService(FilterModel filterModel)
        {
            DateTime toDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day);

            DateTime fromDate = toDate.AddMonths(-1);
            fromDate = fromDate.AddDays(1);
            //fromDate = _timezoneConverter.FirstDayOfPresentWeek(fromDate, _currentSession.TimeZone);

            var dailyAttendances = await GetFilteredDetailAttendanceService(filterModel, fromDate, toDate);

            return dailyAttendances;
        }

        private async Task<List<DailyAttendance>> GetFilteredDetailAttendanceService(FilterModel filterModel, DateTime fromDate, DateTime toDate)
        {
            if (filterModel.EmployeeId == 0)
                throw HiringBellException.ThrowBadRequest("Invalid user selected");
            filterModel.SearchString = filterModel.SearchString + $" and AttendanceDate between '{fromDate.ToString("yyyy-MM-dd HH:mm:ss")}' and '{toDate.ToString("yyyy-MM-dd HH:mm:ss")}'";
            (List<DailyAttendance> dailyAttendances, List<LeaveRequestNotification> leaveRequestDetail) = _db.GetList<DailyAttendance, LeaveRequestNotification>(Procedures.DAILY_ATTENDANCE_FILTER, new
            {
                filterModel.SearchString,
                filterModel.PageSize,
                filterModel.PageIndex,
                filterModel.SortBy,
                filterModel.EmployeeId
            });

            if (dailyAttendances.Count == 0)
                throw HiringBellException.ThrowBadRequest("Attendance detail not found");

            if (leaveRequestDetail.Count > 0)
            {
                foreach (var item in dailyAttendances)
                {
                    var leaveDetail = leaveRequestDetail
                                        .Find(
                                                x => x.FromDate.Date
                                                        .Subtract(item.AttendanceDate.Date).TotalDays <= 0
                                                    && x.ToDate.Date
                                                        .Subtract(item.AttendanceDate.Date).TotalDays >= 0
                                             );

                    if (leaveDetail != null && leaveDetail.RequestStatusId == (int)ItemStatus.Approved)
                    {
                        item.IsOnLeave = true;
                    }
                }
            }
            return await Task.FromResult(dailyAttendances);
        }

        public async Task UploadMonthlyAttendanceExcelService(IFormFileCollection files)
        {
            var attendanceData = await _commonService.ReadExcelData(files);
            var monthlyRecord = GetMonthlyAttendanceRecord(attendanceData);
            List<DailyAttendance> attendances = GenerateMonthlyAttendanceDetail(monthlyRecord); //GenerateMonthlyAttendanceRecord(monthlyRecord);

            await UploadBulkDailyAttendanceDetail(attendances);
        }

        private List<MonthlyAttendanceDetail> GetMonthlyAttendanceRecord(DataTable dataTable)
        {
            List<MonthlyAttendanceDetail> monthlyAttendanceDetail = new List<MonthlyAttendanceDetail>();
            foreach (DataRow row in dataTable.Rows)
            {
                MonthlyAttendanceDetail attendance = ValidateAttendanceBasicDetail(row);

                int daysInMonth = DateTime.DaysInMonth(attendance.Year, attendance.Month);
                for (int day = 1; day <= daysInMonth; day++)
                {
                    string dayColumnName = day.ToString();
                    if (dataTable.Columns.Contains(dayColumnName))
                    {
                        var dayValue = row[dayColumnName].ToString();
                        if (string.IsNullOrEmpty(dayValue) || dayValue.Equals("p", StringComparison.OrdinalIgnoreCase)
                            || dayValue.Equals("a", StringComparison.OrdinalIgnoreCase))
                        {
                            attendance.DailyData.Add(day, dayValue);
                        }
                        else
                        {
                            throw HiringBellException.ThrowBadRequest($"Invalid day value {dayValue} for employee {attendance.Name}");
                        }
                    }
                }

                monthlyAttendanceDetail.Add(attendance);
            }

            return monthlyAttendanceDetail;
        }

        private MonthlyAttendanceDetail ValidateAttendanceBasicDetail(DataRow row)
        {
            if (row["Name"] == null || string.IsNullOrEmpty(row["Name"].ToString()))
                throw HiringBellException.ThrowBadRequest("Employee Name is null. Please provide a valid Employee Name.");

            string employeeName = row["Name"].ToString();

            if (row["EmployeeId"] == null || string.IsNullOrEmpty(row["EmployeeId"].ToString()))
                throw HiringBellException.ThrowBadRequest($"Employee ID is null for employee name '{employeeName}'. Please provide a valid Employee Id.");

            if (row["Month"] == null || string.IsNullOrEmpty(row["Month"].ToString()))
                throw HiringBellException.ThrowBadRequest($"Month number is null for employee name '{employeeName}'. Please provide a valid Month number.");

            if (row["Year"] == null || string.IsNullOrEmpty(row["Year"].ToString()))
                throw HiringBellException.ThrowBadRequest($"Year is null for employee name '{employeeName}'. Please provide a valid Year.");

            long.TryParse(row["EmployeeId"].ToString(), out long employeeId);
            if (employeeId == 0)
                throw HiringBellException.ThrowBadRequest($"Employee ID is invalid for employee name '{employeeName}'");

            int.TryParse(row["Month"].ToString(), out int month);
            if (month < 1 || month > 12)
                throw HiringBellException.ThrowBadRequest($"Month number is invalid for employee name '{employeeName}'");

            int.TryParse(row["Year"].ToString(), out int year);
            if (year < 1)
                throw HiringBellException.ThrowBadRequest($"Year is invalid for employee name '{employeeName}'");

            return new MonthlyAttendanceDetail
            {
                EmployeeId = employeeId,
                Name = employeeName,
                Month = month,
                Year = year
            };
        }

        //private List<DailyAttendance> GenerateMonthlyAttendanceRecord(List<MonthlyAttendanceDetail> monthlyAttendanceDetails)
        //{
        //    List<DailyAttendance> dailyAttendances = new List<DailyAttendance>();

        //    monthlyAttendanceDetails.ForEach(x =>
        //    {
        //        DailyAttendanceBuilder dailyAttendanceBuilder = GetDailyAttendanceDetail(x.EmployeeId, x.Month, x.Year, out List<DailyAttendance> attendanceDetails);

        //        foreach (var item in attendanceDetails)
        //        {
        //            var attedanceDate = _timezoneConverter.ToTimeZoneDateTime(item.AttendanceDate, _currentSession.TimeZone);
        //            if (attedanceDate.Month == x.Month)
        //            {
        //                x.DailyData.TryGetValue(attedanceDate.Day, out string value);
        //                item.WorkTypeId = WorkType.WORKFROMOFFICE;
        //                item.AttendanceStatus = GetAttendanceDayStatus(value, dailyAttendanceBuilder, attedanceDate);

        //                var leaveDetail = dailyAttendanceBuilder.leaveDetails.Find(i => i.FromDate.Date.Subtract(item.AttendanceDate.Date).TotalDays <= 0
        //                                                                                && i.ToDate.Date.Subtract(item.AttendanceDate.Date).TotalDays >= 0);

        //                if (leaveDetail != null)
        //                {
        //                    item.IsOnLeave = true;
        //                    item.LeaveId = leaveDetail.LeaveTypeId;
        //                }
        //            }
        //        };

        //        dailyAttendances.AddRange(attendanceDetails);
        //    });

        //    return dailyAttendances;
        //}

        private List<DailyAttendance> GenerateMonthlyAttendanceDetail(List<MonthlyAttendanceDetail> monthlyAttendanceDetails)
        {
            List<DailyAttendance> dailyAttendances = new List<DailyAttendance>();

            monthlyAttendanceDetails.ForEach(x =>
            {
                DailyAttendanceBuilder dailyAttendanceBuilder = GetDailyAttendanceDetail(x.EmployeeId, x.Month, x.Year, out List<DailyAttendance> attendanceDetails);
                if (dailyAttendanceBuilder.employee.CreatedOn.Year > x.Year && dailyAttendanceBuilder.employee.CreatedOn.Month > x.Month)
                    throw HiringBellException.ThrowBadRequest($"Attendance for Employee '{x.Name}' cannot be uploaded before their joining date ({dailyAttendanceBuilder.employee.CreatedOn.ToString("dd-MM-yyyy")}).");

                foreach (var item in x.DailyData)
                {
                    var attendance = new DailyAttendance();
                    if (attendanceDetails.Any())
                    {
                        attendance = attendanceDetails.Find(i => item.Key == _timezoneConverter.ToTimeZoneDateTime(i.AttendanceDate, _currentSession.TimeZone).Day);
                        if (attendance != null)
                        {
                            attendance.WorkTypeId = WorkType.WORKFROMOFFICE;
                            var attedanceDate = _timezoneConverter.ToTimeZoneDateTime(attendance.AttendanceDate, _currentSession.TimeZone);
                            attendance.AttendanceStatus = GetAttendanceDayStatus(item.Value, dailyAttendanceBuilder, attedanceDate);
                        }
                        else
                        {
                            attendance = BuildNewAttendance(x, dailyAttendanceBuilder, item);
                        }

                        var leaveDetail = dailyAttendanceBuilder.leaveDetails.Find(i => i.FromDate.Date.Subtract(attendance.AttendanceDate.Date).TotalDays <= 0
                                                                                        && i.ToDate.Date.Subtract(attendance.AttendanceDate.Date).TotalDays >= 0);

                        if (leaveDetail != null)
                        {
                            attendance.IsOnLeave = true;
                            attendance.LeaveId = leaveDetail.LeaveTypeId;
                        }
                    }
                    else
                    {
                        attendance = BuildNewAttendance(x, dailyAttendanceBuilder, item);

                        var leaveDetail = dailyAttendanceBuilder.leaveDetails.Find(i => i.FromDate.Date.Subtract(attendance.AttendanceDate.Date).TotalDays <= 0
                                                                                        && i.ToDate.Date.Subtract(attendance.AttendanceDate.Date).TotalDays >= 0);

                        if (leaveDetail != null)
                        {
                            attendance.IsOnLeave = true;
                            attendance.LeaveId = leaveDetail.LeaveTypeId;
                        }
                    }

                    dailyAttendances.Add(attendance);
                }
            });

            return dailyAttendances;
        }

        private DailyAttendance BuildNewAttendance(MonthlyAttendanceDetail monthlyAttendance, DailyAttendanceBuilder dailyAttendanceBuilder, KeyValuePair<int, string> item)
        {
            var date = new DateTime(monthlyAttendance.Year, monthlyAttendance.Month, item.Key, 18, 30, 0, DateTimeKind.Unspecified).AddDays(-1);
            var attedanceDate = _timezoneConverter.ToTimeZoneDateTime(date, _currentSession.TimeZone);

            return new DailyAttendance
            {
                AttendanceId = 0,
                EmployeeId = dailyAttendanceBuilder.employee.EmployeeUid,
                EmployeeEmail = dailyAttendanceBuilder.employee.Email,
                ReviewerId = 0,
                ProjectId = 0,
                TaskId = 0,
                TaskType = 0,
                LogOn = "00:00:00",
                LogOff = "00:00:00",
                TotalMinutes = dailyAttendanceBuilder.shiftDetail.Duration,
                Comments = "[]",
                WorkTypeId = WorkType.WORKFROMOFFICE,
                IsOnLeave = false,
                LeaveId = 0,
                AttendanceStatus = GetAttendanceDayStatus(item.Value, dailyAttendanceBuilder, attedanceDate),
                AttendanceDate = _timezoneConverter.ToUtcTime(attedanceDate, _currentSession.TimeZone),
                WeekOfYear = ISOWeek.GetWeekOfYear(attedanceDate)
            };
        }

        private DailyAttendanceBuilder GetDailyAttendanceDetail(long employeeId, int month, int year, out List<DailyAttendance> dailyAttendances)
        {
            dailyAttendances = new List<DailyAttendance>();
            DailyAttendanceBuilder dailyAttendanceBuilder = new DailyAttendanceBuilder();

            var fromDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified).AddDays(-1);
            var toDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified).AddMonths(1);
            var Result = _db.FetchDataSet(Procedures.DAILY_ATTENDANCE_GET, new
            {
                EmployeeId = employeeId,
                FromDate = fromDate,
                ToDate = toDate,
                _currentSession.CurrentUserDetail.CompanyId,
            });

            if (Result.Tables.Count != 5)
                throw HiringBellException.ThrowBadRequest("Fail to get attendance detail. Please contact to admin.");

            //if (Result.Tables[0].Rows.Count == 0)
            //    throw HiringBellException.ThrowBadRequest("Attendance not found. Please contact to admin");

            if (Result.Tables[3].Rows.Count != 1)
                throw HiringBellException.ThrowBadRequest("Company regular shift is not configured. Please complete company setting first.");

            dailyAttendanceBuilder.shiftDetail = Converter.ToType<ShiftDetail>(Result.Tables[3]);
            dailyAttendances = Converter.ToList<DailyAttendance>(Result.Tables[0]);

            if (!ApplicationConstants.ContainSingleRow(Result.Tables[1]))
                throw new HiringBellException("Err!! fail to get employee detail. Plaese contact to admin.");

            dailyAttendanceBuilder.employee = Converter.ToType<Employee>(Result.Tables[1]);
            dailyAttendanceBuilder.leaveDetails = Converter.ToList<LeaveRequestNotification>(Result.Tables[4]);
            dailyAttendanceBuilder.calendars = Converter.ToList<ModalLayer.Calendar>(Result.Tables[2]);

            return dailyAttendanceBuilder;
        }

        private int GetAttendanceDayStatus(string value, DailyAttendanceBuilder dailyAttendanceBuilder, DateTime attedanceDate)
        {
            int status = 0;
            var isHoliday = dailyAttendanceBuilder.calendars.Any() && CheckIsHoliday(attedanceDate, dailyAttendanceBuilder.calendars);
            var isWeekend = CheckWeekend(dailyAttendanceBuilder.shiftDetail, attedanceDate);

            if (isWeekend)
                status = (int)AttendanceEnum.WeekOff;
            else if (isHoliday)
                status = (int)AttendanceEnum.Holiday;
            else if (value.Equals("p", StringComparison.OrdinalIgnoreCase))
                status = (int)AttendanceEnum.Approved;
            else
                status = (int)AttendanceEnum.NotSubmitted;

            return status;
        }

        private async Task UploadBulkDailyAttendanceDetail(List<DailyAttendance> attendances)
        {
            var records = (from n in attendances
                           select new
                           {
                               n.AttendanceId,
                               n.EmployeeId,
                               n.EmployeeName,
                               n.EmployeeEmail,
                               n.ReviewerId,
                               n.ReviewerName,
                               n.ReviewerEmail,
                               n.ProjectId,
                               n.TaskId,
                               n.TaskType,
                               n.LogOn,
                               n.LogOff,
                               n.TotalMinutes,
                               n.Comments,
                               n.AttendanceStatus,
                               n.WeekOfYear,
                               n.AttendanceDate,
                               WorkTypeId = (int)n.WorkTypeId,
                               n.IsOnLeave,
                               n.LeaveId,
                               CreatedBy = _currentSession.CurrentUserDetail.UserId
                           }).ToList();

            var result = await _db.BulkExecuteAsync(Procedures.DAILY_ATTENDANCE_INS_UPD_WEEKLY, records, true);

            if (result != records.Count)
                throw HiringBellException.ThrowBadRequest("Fail to insert the record");
        }

        public async Task UploadDailyBiometricAttendanceExcelService(IFormFileCollection files)
        {
            var dailyAttendanceData = await _commonService.ReadExcelData(files);
            var attendanceRecord = GetDailyAttendanceRecord(dailyAttendanceData);
            List<DailyAttendance> attendances = GenerateDailyAttendanceRecord(attendanceRecord);

            await UploadBulkDailyAttendanceDetail(attendances);
        }

        private List<DailyAttendance> GenerateDailyAttendanceRecord(List<DailyBiometricAttendance> dailyBiometricAttendances)
        {
            var firstBiometricAttendance = dailyBiometricAttendances.First();
            DailyAttendanceBuilder dailyAttendanceBuilder = GetDailyAttendanceDetail(firstBiometricAttendance.EmployeeId, firstBiometricAttendance.Date.Month, firstBiometricAttendance.Date.Year, out List<DailyAttendance> attendanceDetails);

            foreach (var item in attendanceDetails)
            {
                var attedanceDate = _timezoneConverter.ToTimeZoneDateTime(item.AttendanceDate, _currentSession.TimeZone);
                var currentBiometricAttendance = dailyBiometricAttendances.Find(x => x.Date.Date.Subtract(attedanceDate.Date).TotalDays == 0);
                if (firstBiometricAttendance.Date.Month == attedanceDate.Month)
                {
                    if (currentBiometricAttendance != null)
                    {
                        string value = currentBiometricAttendance.TotalWorkingMinutes > 0 ? "p" : "";

                        item.TotalMinutes = currentBiometricAttendance.TotalWorkingMinutes;
                        item.LogOn = currentBiometricAttendance.Punch_In;
                        item.LogOff = currentBiometricAttendance.Punch_Out;
                        item.WorkTypeId = WorkType.WORKFROMOFFICE;
                        item.AttendanceStatus = GetAttendanceDayStatus(value, dailyAttendanceBuilder, attedanceDate);
                    }
                    else
                    {
                        item.TotalMinutes = 0;
                        item.LogOn = "00:00:00";
                        item.LogOff = "00:00:00";
                        item.WorkTypeId = WorkType.WORKFROMOFFICE;
                        item.AttendanceStatus = (int)AttendanceEnum.NotSubmitted;
                    }

                    var leaveDetail = dailyAttendanceBuilder.leaveDetails.Find(x => x.FromDate.Date.Subtract(item.AttendanceDate.Date).TotalDays <= 0
                                                                                    && x.ToDate.Date.Subtract(item.AttendanceDate.Date).TotalDays >= 0);

                    if (leaveDetail != null)
                    {
                        item.IsOnLeave = true;
                        item.LeaveId = leaveDetail.LeaveTypeId;
                    }
                }
            };

            return attendanceDetails;
        }

        private int GetWorkingMinute(string punchIn, string punchOut)
        {
            TimeSpan startTime = TimeSpan.Parse(punchIn);
            TimeSpan endTime = TimeSpan.Parse(punchOut);

            return (int)(endTime - startTime).TotalMinutes;
        }

        private List<DailyBiometricAttendance> GetDailyAttendanceRecord(DataTable dataTable)
        {
            var dailyBiometricAttendances = new List<DailyBiometricAttendance>();
            var headers = dataTable.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToList();
            var punchColumnsIndex = new List<int>();

            for (int i = 0; i < headers.Count; i++)
            {
                var headerValue = headers[i].ToString();
                if (headerValue.Contains("Punch_In", StringComparison.OrdinalIgnoreCase) || headerValue.Contains("Punch_Out", StringComparison.OrdinalIgnoreCase))
                    punchColumnsIndex.Add(i);
            }

            foreach (DataRow row in dataTable.Rows)
            {
                var dailyBiometricAttendance = new DailyBiometricAttendance
                {
                    EmployeeId = long.Parse(row["EmployeeId"].ToString()),
                    Name = row["Name"].ToString(),
                    Date = DateTime.SpecifyKind(Convert.ToDateTime(row["Date"].ToString()), DateTimeKind.Unspecified)
                };

                for (int j = 0; j < punchColumnsIndex.Count; j += 2)
                {
                    if (j + 1 >= punchColumnsIndex.Count)
                        break;

                    int punchInIndex = punchColumnsIndex[j];
                    int punchOutIndex = punchColumnsIndex[j + 1];

                    var punchInValue = row[punchInIndex].ToString();
                    var punchOutValue = row[punchOutIndex].ToString();

                    dailyBiometricAttendance.PunchTimes.Add(new PunchTime
                    {
                        Punch_In = punchInValue,
                        Punch_Out = punchOutValue
                    });

                    dailyBiometricAttendance.TotalWorkingMinutes += GetWorkingMinute(punchInValue, punchOutValue);
                }

                dailyBiometricAttendance.Punch_In = dailyBiometricAttendance.PunchTimes[0].Punch_In;
                dailyBiometricAttendance.Punch_Out = dailyBiometricAttendance.PunchTimes.Last().Punch_Out;


                dailyBiometricAttendances.Add(dailyBiometricAttendance);
            }

            return dailyBiometricAttendances;
        }

        public async Task<byte[]> DownloadAttendanceExcelWithDataService()
        {
            var employees = _db.GetList<Employee>(Procedures.EMPLOYEES_ACTIVE_ALL);

            List<dynamic> employeeRecord = new List<dynamic>();
            var currentDate = DateTime.UtcNow;
            int daysInMonth = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);

            foreach (var employee in employees)
            {
                Dictionary<string, object> data = new Dictionary<string, object>();

                data.Add("EmployeeId", employee.EmployeeUid);
                data.Add("Name", employee.FirstName + " " + employee.LastName);
                data.Add("Month", currentDate.Month);
                data.Add("Year", currentDate.Year);
                for (int i = 1; i <= daysInMonth; i++)
                {
                    data.Add($"{i}", "p");
                }

                employeeRecord.Add(data);
            }

            var url = $"{_microserviceUrlLogs.GenerateExelWithHeader}";

            var microserviceRequest = MicroserviceRequest.Builder(url);
            microserviceRequest
            .SetPayload(employeeRecord)
            .SetDbConfigModal(_requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString))
            .SetConnectionString(_currentSession.LocalConnectionString)
            .SetCompanyCode(_currentSession.CompanyCode)
            .SetToken(_currentSession.Authorization);

            return await _requestMicroservice.PostRequest<byte[]>(microserviceRequest);
        }
        #endregion

        #region Un-used Code
        //private async Task GenerateAttendanceForEachEmployee(DateTime attendanceStartDate, DateTime attendanceEndDate, long employeeId, int status)
        //{
        //    while (attendanceStartDate.Subtract(attendanceEndDate).TotalDays < 0)
        //    {
        //        Attendance attendance = new Attendance
        //        {
        //            AttendanceDay = attendanceStartDate,
        //            ForMonth = attendanceStartDate.Month,
        //            ForYear = attendanceStartDate.Year,
        //            EmployeeId = employeeId
        //        };

        //        var record = _db.Get<Attendance>(Procedures.Attendance_Get_By_Empid, new
        //        {
        //            EmployeeId = employeeId,
        //            ForYear = attendanceStartDate.Year,
        //            ForMonth = attendanceStartDate.Month
        //        });

        //        if (record == null || string.IsNullOrEmpty(record.AttendanceDetail) || record.AttendanceDetail == "[]")
        //        {
        //            List<AttendanceJson> attendenceDetails;
        //            var attendanceModal = GetAttendanceDetail(attendance, out attendenceDetails);

        //            attendanceModal.attendanceSubmissionLimit = 2;
        //            await BuildApprovedAttendance(attendanceModal, attendanceModal.employee.CreatedOn, status);

        //            attendanceStartDate = attendanceStartDate.AddMonths(1);
        //        }
        //        else
        //        {
        //            break;
        //        }
        //    }
        //    await Task.CompletedTask;
        //}

        //private async Task<List<AttendanceJson>> BuildApprovedAttendance(AttendanceDetailBuildModal attendanceModal, DateTime dateOfJoining, int status)
        //{
        //    List<AttendanceJson> attendenceDetails = new List<AttendanceJson>();
        //    var timezoneFirstDate = _timezoneConverter.ToTimeZoneDateTime(attendanceModal.firstDate, _currentSession.TimeZone);
        //    int totalNumOfDaysInPresentMonth = DateTime.DaysInMonth(timezoneFirstDate.Year, timezoneFirstDate.Month);
        //    if (dateOfJoining.Month > timezoneFirstDate.Month && dateOfJoining.Year >= timezoneFirstDate.Year)
        //        return null;

        //    double days = 0;
        //    var barrierDate = GetBarrierDate(attendanceModal.attendanceSubmissionLimit);
        //    if (timezoneFirstDate.Day > 1)
        //        totalNumOfDaysInPresentMonth = totalNumOfDaysInPresentMonth - timezoneFirstDate.Day;

        //    int weekDays = 0;
        //    int totalMinute = 0;
        //    int i = 0;
        //    DateTime workingDate = timezoneFirstDate;
        //    while (i < totalNumOfDaysInPresentMonth)
        //    {
        //        workingDate = timezoneFirstDate.AddDays(i);
        //        var isHoliday = CheckIsHoliday(workingDate, attendanceModal.calendars);
        //        var isWeekend = CheckWeekend(attendanceModal.shiftDetail, workingDate);
        //        var officetime = attendanceModal.shiftDetail.OfficeTime;
        //        var logoff = CalculateLogOff(attendanceModal.shiftDetail.OfficeTime, attendanceModal.shiftDetail.LunchDuration);

        //        days = barrierDate.Date.Subtract(workingDate.Date).TotalDays;
        //        totalMinute = attendanceModal.shiftDetail.Duration;
        //        var presentDayStatus = (int)DayStatus.Empty;
        //        if (isHoliday || isWeekend)
        //        {
        //            officetime = "00:00";
        //            logoff = "00:00";
        //            totalMinute = 0;
        //        }

        //        if (isHoliday)
        //            presentDayStatus = (int)DayStatus.Holiday;
        //        else if (isWeekend)
        //            presentDayStatus = (int)DayStatus.Weekend;
        //        else
        //        {
        //            presentDayStatus = status;
        //        }

        //        attendenceDetails.Add(new AttendanceJson
        //        {
        //            AttendenceDetailId = workingDate.Day,
        //            IsHoliday = isHoliday,
        //            IsOnLeave = false,
        //            IsWeekend = isWeekend,
        //            AttendanceDay = workingDate,
        //            LogOn = officetime,
        //            LogOff = logoff,
        //            PresentDayStatus = presentDayStatus,
        //            UserComments = string.Empty,
        //            ApprovedName = string.Empty,
        //            ApprovedBy = 0,
        //            SessionType = 1,
        //            TotalMinutes = totalMinute,
        //            IsOpen = i >= days ? true : false,
        //            WorkTypeId = (int)WorkType.WORKFROMHOME
        //        });

        //        i++;
        //    }

        //    var result = await _db.ExecuteAsync(Procedures.Attendance_Insupd, new
        //    {
        //        AttendanceId = 0,
        //        AttendanceDetail = JsonConvert.SerializeObject(attendenceDetails),
        //        UserTypeId = (int)UserType.Employee,
        //        EmployeeId = attendanceModal.employee.EmployeeUid,
        //        TotalDays = totalNumOfDaysInPresentMonth,
        //        TotalWeekDays = weekDays,
        //        DaysPending = totalNumOfDaysInPresentMonth,
        //        TotalBurnedMinutes = 0,
        //        ForYear = attendanceModal.firstDate.AddDays(1).Year,
        //        ForMonth = attendanceModal.firstDate.AddDays(1).Month,
        //        _currentSession.CurrentUserDetail.UserId,
        //        PendingRequestCount = 0,
        //        attendanceModal.employee.ReportingManagerId,
        //        attendanceModal.employee.ManagerName,
        //        attendanceModal.employee.Mobile,
        //        attendanceModal.employee.Email,
        //        EmployeeName = attendanceModal.employee.FirstName + " " + attendanceModal.employee.LastName,
        //        AttendenceStatus = (int)DayStatus.WorkFromOffice,
        //        BillingHours = 0,
        //        ClientId = 0,
        //        LunchBreanInMinutes = attendanceModal.shiftDetail.LunchDuration
        //    }, true);

        //    if (string.IsNullOrEmpty(result.statusMessage))
        //        throw HiringBellException.ThrowBadRequest("Got server error. Please contact to admin.");

        //    attendanceModal.attendance.AttendanceId = Convert.ToInt64(result.statusMessage);
        //    return attendenceDetails;
        //}

        //private string UpdateOrInsertAttendanceDetail(List<AttendenceDetail> finalAttendanceSet, Attendance currentAttendance, string procedure)
        //{
        //    var firstAttn = finalAttendanceSet.FirstOrDefault();

        //    var AttendaceDetail = JsonConvert.SerializeObject((from n in finalAttendanceSet
        //                                                       select new
        //                                                       {
        //                                                           TotalMinutes = n.TotalMinutes,
        //                                                           UserTypeId = n.UserTypeId,
        //                                                           PresentDayStatus = n.PresentDayStatus,
        //                                                           EmployeeUid = n.EmployeeUid,
        //                                                           AttendanceId = n.AttendanceId,
        //                                                           UserComments = n.UserComments,
        //                                                           AttendanceDay = n.AttendanceDay,
        //                                                           AttendenceStatus = n.AttendenceStatus
        //                                                       }));

        //    double MonthsMinutes = 0;
        //    currentAttendance.DaysPending = 0;
        //    finalAttendanceSet.ForEach(x =>
        //    {
        //        MonthsMinutes += x.TotalMinutes;
        //        if (x.AttendenceStatus == 8)
        //            currentAttendance.DaysPending++;
        //    });

        //    var Result = _db.Execute<string>(procedure, new
        //    {
        //        AttendanceId = currentAttendance.AttendanceId,
        //        EmployeeId = currentAttendance.EmployeeId,
        //        UserTypeId = currentAttendance.UserTypeId,
        //        AttendanceDetail = AttendaceDetail,
        //        TotalDays = currentAttendance.TotalDays,
        //        TotalWeekDays = currentAttendance.TotalWeekDays,
        //        DaysPending = currentAttendance.DaysPending,
        //        TotalBurnedMinutes = MonthsMinutes,
        //        ForYear = currentAttendance.ForYear,
        //        ForMonth = currentAttendance.ForMonth,
        //        UserId = _currentSession.CurrentUserDetail.UserId
        //    }, true);

        //    if (string.IsNullOrEmpty(Result))
        //        throw new HiringBellException("Fail to insert or update attendance detail. Pleasa contact to admin.");

        //    return Result;
        //}

        //private void ValidateDateOfAttendanceSubmission(DateTime firstDate, DateTime lastDate)
        //{
        //    DateTime now = DateTime.Now;
        //    DateTime presentDate = _timezoneConverter.GetUtcDateTime(now.Year, now.Month, now.Day);

        //    // handling future date
        //    if (presentDate.Subtract(lastDate).TotalDays > 0)
        //    {
        //        throw new HiringBellException("Future date's are not allowed.");
        //    }
        //    // handling past date
        //    else if (presentDate.Subtract(firstDate).TotalDays < 0)
        //    {
        //        if (_currentSession.CurrentUserDetail.RoleId != (int)UserType.Admin)
        //        {
        //            throw new HiringBellException("Past week's are not allowed.");
        //        }
        //    }
        //}

        //private async Task<TemplateReplaceModal> GetAttendanceApprovalTemplate(ComplaintOrRequest compalintOrRequest)
        //{
        //    var templateReplaceModal = new TemplateReplaceModal
        //    {
        //        DeveloperName = compalintOrRequest.EmployeeName,
        //        RequestType = ApplicationConstants.WorkFromHome,
        //        ToAddress = new List<string> { compalintOrRequest.Email },
        //        ActionType = "Requested",
        //        FromDate = compalintOrRequest.AttendanceDate,
        //        ToDate = compalintOrRequest.AttendanceDate,
        //        ManagerName = compalintOrRequest.ManagerName,
        //        Message = string.IsNullOrEmpty(compalintOrRequest.ManagerComments)
        //            ? "NA"
        //            : compalintOrRequest.ManagerComments,
        //    };

        //    if (compalintOrRequest.NotifyList != null && compalintOrRequest.NotifyList.Count > 0)
        //    {
        //        foreach (var email in compalintOrRequest.NotifyList)
        //        {
        //            templateReplaceModal.ToAddress.Add(email.Email);
        //        }
        //    }
        //    return await Task.FromResult(templateReplaceModal);
        //}

        //public async Task AttendaceApprovalStatusSendEmail(ComplaintOrRequestWithEmail compalintOrRequestWithEmail)
        //{
        //    var templateReplaceModal = new TemplateReplaceModal();
        //    if (compalintOrRequestWithEmail.CompalintOrRequestList.First().NotifyList != null && compalintOrRequestWithEmail.CompalintOrRequestList.First().NotifyList.Count > 0)
        //    {
        //        templateReplaceModal.ToAddress = new List<string>();
        //        foreach (var email in compalintOrRequestWithEmail.CompalintOrRequestList.First().NotifyList)
        //        {
        //            templateReplaceModal.ToAddress.Add(email.Email);
        //        }
        //    }

        //    await SendEmailWithTemplate(compalintOrRequestWithEmail, templateReplaceModal);
        //}

        //private async Task<EmailSenderModal> SendEmailWithTemplate(ComplaintOrRequestWithEmail compalintOrRequestWithEmail, TemplateReplaceModal templateReplaceModal)
        //{
        //    templateReplaceModal.BodyContent = compalintOrRequestWithEmail.EmailBody;
        //    var emailSenderModal = await ReplaceActualData(templateReplaceModal, compalintOrRequestWithEmail);

        //    await _eMailManager.SendMailAsync(emailSenderModal);
        //    return await Task.FromResult(emailSenderModal);
        //}

        //private async Task<EmailSenderModal> ReplaceActualData(TemplateReplaceModal templateReplaceModal, ComplaintOrRequestWithEmail compalintOrRequestWithEmail)
        //{
        //    EmailSenderModal emailSenderModal = null;
        //    var attendance = compalintOrRequestWithEmail.CompalintOrRequestList.First();
        //    if (templateReplaceModal != null)
        //    {
        //        var totalDays = compalintOrRequestWithEmail.CompalintOrRequestList.Count;
        //        string subject = $"{totalDays} Days Blocked Attendance Approval Status";
        //        string body = compalintOrRequestWithEmail.EmailBody;

        //        StringBuilder builder = new StringBuilder();
        //        builder.Append("<div style=\"border-bottom:1px solid black; margin-top: 14px; margin-bottom:5px\">" + "" + "</div>");
        //        builder.AppendLine();
        //        builder.AppendLine();
        //        builder.Append("<div>" + "Thanks and Regard" + "</div>");
        //        builder.Append("<div>" + attendance.EmployeeName + "</div>");
        //        builder.Append("<div>" + attendance.Mobile + "</div>");

        //        var logoPath = Path.Combine(_fileLocationDetail.RootPath, _fileLocationDetail.LogoPath, ApplicationConstants.HiringBellLogoSmall);
        //        if (File.Exists(logoPath))
        //        {
        //            builder.Append($"<div><img src=\"cid:{ApplicationConstants.LogoContentId}\" style=\"width: 10rem;margin-top: 1rem;\"></div>");
        //        }

        //        emailSenderModal = new EmailSenderModal
        //        {
        //            To = templateReplaceModal.ToAddress,
        //            Subject = subject,
        //            Body = string.Concat(body, builder.ToString()),
        //        };
        //    }

        //    emailSenderModal.Title = $"{attendance.EmployeeName} requested for approved blocked attendance.";

        //    return await Task.FromResult(emailSenderModal);
        //}

        //private async Task CreatePresentDayAttendance(AttendenceDetail attendenceDetail, DateTime workingTimezoneDate)
        //{
        //    var totalDays = DateTime.DaysInMonth(workingTimezoneDate.Year, workingTimezoneDate.Month);

        //    attendenceDetail.IsActiveDay = false;
        //    attendenceDetail.TotalDays = totalDays;
        //    attendenceDetail.AttendanceId = 0;
        //    attendenceDetail.AttendenceStatus = (int)DayStatus.WorkFromOffice;
        //    attendenceDetail.BillingHours = 480;
        //    attendenceDetail.ClientId = attendenceDetail.ClientId;
        //    attendenceDetail.DaysPending = totalDays;
        //    attendenceDetail.EmployeeUid = attendenceDetail.EmployeeUid;
        //    attendenceDetail.ForMonth = workingTimezoneDate.Month;
        //    attendenceDetail.ForYear = workingTimezoneDate.Year;
        //    attendenceDetail.TotalMinutes = 480;
        //    attendenceDetail.IsHoliday = (workingTimezoneDate.DayOfWeek == DayOfWeek.Saturday
        //                    ||
        //                workingTimezoneDate.DayOfWeek == DayOfWeek.Sunday) ? true : false;
        //    attendenceDetail.IsOnLeave = false;
        //    attendenceDetail.LeaveId = 0;
        //    attendenceDetail.PresentDayStatus = (int)ItemStatus.Pending;
        //    attendenceDetail.UserTypeId = (int)UserType.Employee;
        //    attendenceDetail.IsOpen = true;
        //    CalculateLogOffTime(attendenceDetail);
        //    await Task.CompletedTask;
        //}

        //private void CalculateLogOffTime(AttendenceDetail attendenceDetail)
        //{
        //    var logontime = attendenceDetail.LogOn.Replace(":", ".");
        //    decimal logon = decimal.Parse(logontime);
        //    var totaltime = 0;
        //    if (attendenceDetail.SessionType == 1)
        //    {
        //        totaltime = (int)(logon * 60 - attendenceDetail.LunchBreanInMinutes);
        //        var time = ConvertToMin(totaltime);
        //        attendenceDetail.LogOff = time.ToString();
        //    }
        //    else
        //    {
        //        totaltime = (int)(logon * 60 - attendenceDetail.LunchBreanInMinutes) / 2;
        //        var time = ConvertToMin(totaltime);
        //        attendenceDetail.LogOn = time.ToString();
        //        attendenceDetail.LogOff = time.ToString();
        //    }
        //}

        //private WeekDates getCurrentWeekStartEndDate()
        //{
        //    DateTime today = DateTime.UtcNow;

        //    int daysToSubtract = today.DayOfWeek - DayOfWeek.Monday;
        //    if (daysToSubtract < 0)
        //        daysToSubtract += 7;

        //    DateTime weekStart = today.AddDays(-daysToSubtract);
        //    DateTime weekEnd = weekStart.AddDays(6);

        //    WeekDates weekDates = new WeekDates
        //    {
        //        StartDate = weekStart,
        //        EndDate = weekEnd
        //    };

        //    return weekDates;
        //}

        //private async Task<List<WeekDates>> GenerateWeeks()
        //{
        //    (DateTime FromDate, DateTime ToDate) = await BuildDates();

        //    if (ToDate.Date.Subtract(FromDate.Date).TotalDays < 0)
        //        throw HiringBellException.ThrowBadRequest("Invalid date calculation for week generation. Please contact to admin.");

        //    var attendanceWeeks = new List<WeekDates>();
        //    DateTime startDate = FromDate;
        //    DateTime movingDate = FromDate;

        //    int weekIndex = GetWeekIndex(startDate);
        //    while (movingDate.Date.Subtract(ToDate.Date).TotalDays <= 0)
        //    {
        //        if (movingDate.DayOfWeek == DayOfWeek.Sunday)
        //        {
        //            attendanceWeeks.Add(new WeekDates
        //            {
        //                StartDate = startDate,
        //                EndDate = movingDate,
        //                WeekIndex = weekIndex++
        //            });

        //            startDate = movingDate.AddDays(1);
        //        }

        //        movingDate = movingDate.AddDays(1);
        //    }

        //    attendanceWeeks = attendanceWeeks.OrderByDescending(x => x.WeekIndex).ToList();

        //    return await Task.FromResult(attendanceWeeks);
        //}

        //private string ConvertPunchTime(string punchTime)
        //{
        //    TimeSpan timeSpan = TimeSpan.Parse(punchTime);
        //    return timeSpan.ToString(@"hh\:mm\:ss");
        //}
        #endregion
    }
}