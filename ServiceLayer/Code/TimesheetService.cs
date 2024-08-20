using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EMailService.Modal;
using ems_CommonUtility.KafkaService.interfaces;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Code.SendEmail;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class TimesheetService : ITimesheetService
    {
        private readonly IDb _db;
        private readonly ITimezoneConverter _timezoneConverter;
        private readonly CurrentSession _currentSession;
        private readonly TimesheetEmailService _timesheetEmailService;
        private readonly IKafkaNotificationService _kafkaNotificationService;

        public TimesheetService(
            IDb db,
            ITimezoneConverter timezoneConverter,
            CurrentSession currentSession,
            TimesheetEmailService timesheetEmailService,
            IKafkaNotificationService kafkaNotificationService)
        {
            _db = db;
            _timezoneConverter = timezoneConverter;
            _currentSession = currentSession;
            _timesheetEmailService = timesheetEmailService;
            _kafkaNotificationService = kafkaNotificationService;
        }

        #region NEW CODE

        public async Task RunWeeklyTimesheetCreation(DateTime TimesheetStartDate, DateTime? TimesheetEndDate)
        {
            try
            {
                var counts = await _db.ExecuteAsync(Procedures.TIMESHEET_RUNWEEKLY_DATA, new
                {
                    TimesheetStartDate,
                    TimesheetEndDate
                }, true);
            }
            catch
            {
                throw;
            }

            await Task.CompletedTask;
        }

        public List<TimesheetDetail> GetTimesheetByFilterService(TimesheetDetail timesheetDetail)
        {
            if (timesheetDetail.EmployeeId <= 0 || timesheetDetail.ClientId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid data used to get the records.");

            FilterModel filter = new FilterModel();
            filter.SearchString = $"1=1 and EmployeeId = {timesheetDetail.EmployeeId} and ClientId = {timesheetDetail.ClientId} ";

            if (timesheetDetail.TimesheetStatus == (int)ItemStatus.Pending)
                filter.SearchString += $"and TimesheetStatus = {timesheetDetail.TimesheetStatus} and IsSaved = false and IsSubmitted = false";

            else if (timesheetDetail.TimesheetStatus == (int)ItemStatus.Submitted)
                filter.SearchString += $"and IsSubmitted = true";

            else if (timesheetDetail.TimesheetStatus == (int)ItemStatus.Saved)
                filter.SearchString += $"and IsSaved = true";

            else if (timesheetDetail.TimesheetStatus == (int)ItemStatus.Rejected)
                filter.SearchString += $"and TimesheetStatus = {timesheetDetail.TimesheetStatus}";

            else if (timesheetDetail.TimesheetStatus == (int)ItemStatus.Approved)
                filter.SearchString += $"and TimesheetStatus = {timesheetDetail.TimesheetStatus}";

            var Result = _db.GetList<TimesheetDetail>(Procedures.EMPLOYEE_TIMESHEET_FILTER, new
            {
                filter.SearchString,
                timesheetDetail.PageIndex,
                filter.PageSize,
                filter.SortBy
            });

            if (Result == null)
                throw HiringBellException.ThrowBadRequest("Unable to get client detail. Please contact to admin.");

            return Result;
        }

        private async Task CreateTimesheetWeekDays(TimesheetDetail timesheetDetail, ShiftDetail shiftDetail)
        {
            List<TimesheetRequestModel> weeklyTimesheetDetails = new List<TimesheetRequestModel>();
            DateTime startDate = _timezoneConverter.ToTimeZoneDateTime(timesheetDetail.TimesheetStartDate, _currentSession.TimeZone);
            DateTime endDate = _timezoneConverter.ToTimeZoneDateTime(timesheetDetail.TimesheetEndDate, _currentSession.TimeZone);

            while (startDate.Date.Subtract(endDate.Date).TotalDays <= 0)
            {
                var item = timesheetDetail.TimesheetWeeklyData.Find(x => x.WeekDay == (int)startDate.DayOfWeek);
                if (item == null)
                {
                    var isweekened = false;
                    switch (startDate.DayOfWeek)
                    {
                        case DayOfWeek.Sunday:
                            isweekened = !shiftDetail.IsSun;
                            break;
                        case DayOfWeek.Monday:
                            isweekened = !shiftDetail.IsMon;
                            break;
                        case DayOfWeek.Tuesday:
                            isweekened = !shiftDetail.IsTue;
                            break;
                        case DayOfWeek.Wednesday:
                            isweekened = !shiftDetail.IsWed;
                            break;
                        case DayOfWeek.Thursday:
                            isweekened = !shiftDetail.IsThu;
                            break;
                        case DayOfWeek.Friday:
                            isweekened = !shiftDetail.IsFri;
                            break;
                        case DayOfWeek.Saturday:
                            isweekened = !shiftDetail.IsSat;
                            break;
                    }

                    weeklyTimesheetDetails.Add(new TimesheetRequestModel
                    {
                        WeekDay = (int)startDate.DayOfWeek,
                        PresentDate = startDate,
                        ActualBurnedMinutes = isweekened ? 0 : shiftDetail.Duration,
                        IsHoliday = false,
                        IsWeekEnd = isweekened,
                        ExpectedBurnedMinutes = isweekened ? 0 : shiftDetail.Duration,
                        IsOpen = true
                    });
                }
                else
                {
                    weeklyTimesheetDetails.Add(item);
                }

                startDate = startDate.AddDays(1);
            }

            timesheetDetail.TimesheetWeeklyData = weeklyTimesheetDetails.OrderBy(x => x.PresentDate).ToList();
            await Task.CompletedTask;
        }

        public async Task<TimesheetDetail> GetWeekTimesheetDataService(long TimesheetId)
        {
            if (TimesheetId <= 0)
                throw new HiringBellException("Invalid Timesheet id passed.");

            (TimesheetDetail timesheet, ShiftDetail shiftDetail) = _db.Get<TimesheetDetail, ShiftDetail>(Procedures.EMPLOYEE_TIMESHEET_SHIFT_GETBY_TIMESHEETID, new
            {
                TimesheetId
            });

            if (shiftDetail == null)
                throw HiringBellException.ThrowBadRequest("Shift detail not found");

            if (timesheet == null)
            {
                throw HiringBellException.ThrowBadRequest("Timesheet not found. Please contact to admin.");
            }
            else
            {
                if (!string.IsNullOrEmpty(timesheet.TimesheetWeeklyJson))
                {
                    timesheet.TimesheetWeeklyData = JsonConvert.DeserializeObject<List<TimesheetRequestModel>>(timesheet.TimesheetWeeklyJson);
                    timesheet.TimesheetWeeklyData.ForEach(x =>
                    {
                        x.PresentDate = _timezoneConverter.ToTimeZoneDateTime(x.PresentDate, _currentSession.TimeZone);
                    });
                }
                else
                    throw HiringBellException.ThrowBadRequest("Timesheet not found. Please contact to admin.");

            }

            await CreateTimesheetWeekDays(timesheet, shiftDetail);
            return timesheet;
        }

        private string UpdateOrInsertTimesheetDetail(TimesheetDetail timeSheetDetail, ShiftDetail shiftDetail)
        {
            int ExpectedBurnedMinutes = 0;
            int ActualBurnedMinutes = 0;
            timeSheetDetail.TimesheetWeeklyJson = JsonConvert.SerializeObject(timeSheetDetail.TimesheetWeeklyData);
            timeSheetDetail.TimesheetWeeklyData.ForEach(i =>
            {
                ExpectedBurnedMinutes += shiftDetail.Duration;
                ActualBurnedMinutes += i.ActualBurnedMinutes;
            });

            var result = _db.Execute<TimesheetDetail>(Procedures.TIMESHEET_INSUPD, new
            {
                timeSheetDetail.TimesheetId,
                timeSheetDetail.EmployeeId,
                timeSheetDetail.ClientId,
                timeSheetDetail.TimesheetWeeklyJson,
                ExpectedBurnedMinutes = ExpectedBurnedMinutes,
                ActualBurnedMinutes = ActualBurnedMinutes,
                TotalWeekDays = shiftDetail.TotalWorkingDays,
                TotalWorkingDays = timeSheetDetail.TimesheetWeeklyData.Count(i => i.ActualBurnedMinutes > 0),
                timeSheetDetail.TimesheetStatus,
                timeSheetDetail.TimesheetStartDate,
                timeSheetDetail.TimesheetEndDate,
                timeSheetDetail.UserComments,
                timeSheetDetail.ForYear,
                timeSheetDetail.IsSaved,
                timeSheetDetail.IsSubmitted,
                AdminId = _currentSession.CurrentUserDetail.UserId
            }, true);

            if (string.IsNullOrEmpty(result))
                return null;
            return result;
        }

        public async Task<TimesheetDetail> SubmitTimesheetService(TimesheetDetail timesheetDetail)
        {
            if (timesheetDetail == null || timesheetDetail.TimesheetWeeklyData == null || timesheetDetail.TimesheetWeeklyData.Count == 0)
                throw HiringBellException.ThrowBadRequest("Invalid data submitted. Please check you detail.");

            if (timesheetDetail.ClientId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid data submitted. Client id is not valid.");

            ShiftDetail shiftDetail = _db.Get<ShiftDetail>(Procedures.WORK_SHIFTS_BY_CLIENTID, new { ClientId = timesheetDetail.ClientId });

            timesheetDetail.TimesheetStatus = (int)ItemStatus.Submitted;
            timesheetDetail.IsSubmitted = true;
            timesheetDetail.IsSaved = false;
            var result = this.UpdateOrInsertTimesheetDetail(timesheetDetail, shiftDetail);
            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Unable to insert/update record. Please contact to admin.");

            TimesheetApprovalTemplateModel timesheetApprovalTemplateModel = await GetTemplate(timesheetDetail);
            timesheetApprovalTemplateModel.LocalConnectionString = _currentSession.LocalConnectionString;
            await _kafkaNotificationService.SendEmailNotification(timesheetApprovalTemplateModel);
            //await _timesheetEmailService.SendSubmitTimesheetEmail(timesheetDetail);
            return await Task.FromResult(timesheetDetail);
        }

        public async Task<TimesheetDetail> SaveTimesheetService(TimesheetDetail timesheetDetail)
        {
            if (timesheetDetail == null || timesheetDetail.TimesheetWeeklyData == null || timesheetDetail.TimesheetWeeklyData.Count == 0)
                throw HiringBellException.ThrowBadRequest("Invalid data submitted. Please check you detail.");

            if (timesheetDetail.ClientId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid data submitted. Client id is not valid.");

            ShiftDetail shiftDetail = _db.Get<ShiftDetail>(Procedures.WORK_SHIFTS_BY_CLIENTID, new { ClientId = timesheetDetail.ClientId });

            timesheetDetail.IsSubmitted = false;
            timesheetDetail.IsSaved = true;
            var result = this.UpdateOrInsertTimesheetDetail(timesheetDetail, shiftDetail);
            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Unable to insert/update record. Please contact to admin.");

            return await Task.FromResult(timesheetDetail);
        }

        public async Task<string> ExecuteActionOnTimesheetService(TimesheetDetail timesheetDetail)
        {
            if (timesheetDetail == null || timesheetDetail.TimesheetWeeklyData == null || timesheetDetail.TimesheetWeeklyData.Count == 0)
                throw HiringBellException.ThrowBadRequest("Invalid data submitted. Please check you detail.");

            if (timesheetDetail.ClientId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid data submitted. Client id is not valid.");

            ShiftDetail shiftDetail = _db.Get<ShiftDetail>(Procedures.WORK_SHIFTS_BY_CLIENTID, new { ClientId = timesheetDetail.ClientId });

            timesheetDetail.TimesheetStatus = (int)ItemStatus.Submitted;
            var result = this.UpdateOrInsertTimesheetDetail(timesheetDetail, shiftDetail);
            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Unable to insert/update record. Please contact to admin.");

            await _timesheetEmailService.SendSubmitTimesheetEmail(timesheetDetail);
            return await Task.FromResult("successfull");
        }

        #endregion

        public List<TimesheetDetail> GetPendingTimesheetByIdService(long employeeId, long clientId)
        {
            List<TimesheetDetail> timesheetDetail = new List<TimesheetDetail>();
            DateTime current = DateTime.UtcNow;

            var currentTimesheetDetail = _db.Get<TimesheetDetail>(Procedures.Employee_Timesheet_Get, new
            {
                EmployeeId = employeeId,
                ClientId = clientId,
                UserTypeId = _currentSession.CurrentUserDetail.RoleId,
                ForYear = current.Year,
                ForMonth = current.Month,
            });

            timesheetDetail = JsonConvert.DeserializeObject<List<TimesheetDetail>>(currentTimesheetDetail.TimesheetWeeklyJson);
            return timesheetDetail;
        }

        public List<DailyTimesheetDetail> GetEmployeeTimeSheetService(TimesheetDetail timesheetDetail)
        {
            int daysInMonth = DateTime.DaysInMonth(timesheetDetail.ForYear, timesheetDetail.ForMonth);
            var lastDate = new DateTime(timesheetDetail.ForYear, timesheetDetail.ForMonth, daysInMonth);
            var firstDate = new DateTime(timesheetDetail.ForYear, timesheetDetail.ForMonth, 1);
            List<TimesheetDetail> currentTimesheetDetail = _db.GetList<TimesheetDetail>(Procedures.Employee_Timesheet_Getby_Empid, new
            {
                timesheetDetail.EmployeeId,
                timesheetDetail.ForYear,
                timesheetDetail.ClientId,
                firstDate,
                lastDate
            });

            if (currentTimesheetDetail.Count <= 0)
                throw HiringBellException.ThrowBadRequest("Timesheet is not found");

            return BuildFinalTimesheet(currentTimesheetDetail);
        }

        public List<DailyTimesheetDetail> BuildFinalTimesheet(List<TimesheetDetail> timesheetDetail)
        {
            List<DailyTimesheetDetail> monthlyTimesheet = new List<DailyTimesheetDetail>();
            timesheetDetail.ForEach(x =>
            {
                if (string.IsNullOrEmpty(x.TimesheetWeeklyJson))
                    throw HiringBellException.ThrowBadRequest("Weeklytimesheet not found");

                var dailyTimesheet = JsonConvert.DeserializeObject<List<DailyTimesheetDetail>>(x.TimesheetWeeklyJson);
                dailyTimesheet.ForEach(i =>
                {
                    if (!i.IsHoliday && !i.IsWeekEnd && i.ActualBurnedMinutes == 0)
                        i.TimesheetStatus = (int)ItemStatus.Absent;
                    else
                        i.TimesheetStatus = x.TimesheetStatus;

                    i.TimesheetId = x.TimesheetId;
                    i.ClientId = x.ClientId;
                    i.EmployeeId = x.EmployeeId;
                });
                monthlyTimesheet.AddRange(dailyTimesheet);
            });

            return monthlyTimesheet;
        }

        public BillingDetail EditEmployeeBillDetailService(GenerateBillFileDetail fileDetail)
        {
            BillingDetail billingDetail = default(BillingDetail);
            var now = DateTime.UtcNow;
            int daysInMonth = DateTime.DaysInMonth(fileDetail.ForYear, fileDetail.ForMonth);
            var lastDate = new DateTime(fileDetail.ForYear, fileDetail.ForMonth, daysInMonth);
            var firstDate = new DateTime(fileDetail.ForYear, fileDetail.ForMonth, 1);

            var Result = _db.FetchDataSet(Procedures.EmployeeBillDetail_ById, new
            {
                CompanyId = _currentSession.CurrentUserDetail.CompanyId,
                EmployeeId = fileDetail.EmployeeId,
                ClientId = fileDetail.ClientId,
                FileId = fileDetail.FileId,
                FirstDate = firstDate,
                LastDate = lastDate,
                ForYear = fileDetail.ForYear
            });

            if (Result.Tables.Count != 4)
                throw HiringBellException.ThrowBadRequest("Server error. Unable to get detail.");

            billingDetail = new BillingDetail();
            billingDetail.FileDetail = Result.Tables[0];
            billingDetail.Employees = Result.Tables[1];

            List<TimesheetDetail> currentTimesheetDetail = Converter.ToList<TimesheetDetail>(Result.Tables[2]);
            billingDetail.TimesheetDetails = BuildFinalTimesheet(currentTimesheetDetail);
            billingDetail.Organizations = Result.Tables[3];

            return billingDetail;
        }

        private async Task<TimesheetApprovalTemplateModel> GetTemplate(TimesheetDetail timesheetDetail)
        {
            var fromDate = _timezoneConverter.ToTimeZoneDateTime((DateTime)timesheetDetail.TimesheetStartDate, _currentSession.TimeZone);
            var toDate = _timezoneConverter.ToTimeZoneDateTime((DateTime)timesheetDetail.TimesheetEndDate, _currentSession.TimeZone);
            long reportManagerId = 0;
            if (_currentSession.CurrentUserDetail.ReportingManagerId == 0)
                reportManagerId = 1;
            else
                reportManagerId = _currentSession.CurrentUserDetail.ReportingManagerId;
            FilterModel filterModel = new FilterModel
            {
                SearchString = $"1=1 and EmployeeUid = {reportManagerId}",
                SortBy = "",
                PageIndex = 1,
                PageSize = 10
            };

            var managerDetail = _db.Get<Employee>(Procedures.Employees_Get, filterModel);
            if (managerDetail == null)
                throw new Exception("No manager record found. Please add manager first.");

            var numOfDays = fromDate.Date.Subtract(toDate.Date).TotalDays + 1;

            TimesheetApprovalTemplateModel timesheetApprovalTemplateModel = new TimesheetApprovalTemplateModel
            {
                ActionType = ApplicationConstants.Submitted,
                CompanyName = _currentSession.CurrentUserDetail.CompanyName,
                DayCount = Convert.ToInt32(numOfDays),
                DeveloperName = _currentSession.CurrentUserDetail.FullName,
                FromDate = fromDate,
                ToDate = toDate,
                ManagerName = managerDetail.ManagerName,
                Message = string.IsNullOrEmpty(timesheetDetail.UserComments) ? "NA" : timesheetDetail.UserComments,
                ToAddress = new List<string> { managerDetail.Email },
                kafkaServiceName = KafkaServiceName.Timesheet,
                LocalConnectionString = _currentSession.LocalConnectionString,
                CompanyId = _currentSession.CurrentUserDetail.CompanyId
            };

            return await Task.FromResult(timesheetApprovalTemplateModel);
        }
    }
}
