using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Interface;
using EMailService.Modal;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer
{
    public class CompanyCalendar : ICompanyCalendar
    {
        private List<CompanyCalendarDetail> _calendars;
        private ShiftDetail _shiftDetail;
        private readonly CurrentSession _currentSession;
        private readonly IDb _db;
        private readonly CurrentSession _session;
        private readonly ITimezoneConverter _timezoneConverter;
        private readonly IUtilityService _utilityService;
        public CompanyCalendar(IDb db,
            CurrentSession session,
            CurrentSession currentSession,
            ITimezoneConverter timezoneConverter,
            IShiftService shiftService,
            IUtilityService utilityService)
        {
            _db = db;
            _session = session;
            _currentSession = currentSession;
            _timezoneConverter = timezoneConverter;
            _utilityService = utilityService;
        }

        public void LoadHolidayCalendar()
        {
            if (_calendars == null)
            {
                _calendars = _db.GetList<CompanyCalendarDetail>(Procedures.Company_Calendar_Get_By_Company, new { _session.CurrentUserDetail.CompanyId });
            }
        }

        public async Task<bool> IsHoliday(DateTime date)
        {
            bool flag = false;

            LoadHolidayCalendar();
            //var records = _calendars.FirstOrDefault(x => x.StartDate.Date.Subtract(date.Date).TotalDays <= 0
            //                && x.EndDate.Date.Subtract(date.Date).TotalDays >= 0);

            var records = _calendars.Find(x => x.CalendarDate.Date.Subtract(date.Date).TotalDays == 0);
            if (records != null)
                flag = true;

            return await Task.FromResult(flag);
        }

        private bool CheckIsWeekend(DateTime date)
        {
            var flag = false;
            var zoneDate = _timezoneConverter.ToTimeZoneDateTime(date, _currentSession.TimeZone);
            switch (zoneDate.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    if (!_shiftDetail.IsSun)
                        flag = true;
                    break;
                case DayOfWeek.Monday:
                    if (!_shiftDetail.IsMon)
                        flag = true;
                    break;
                case DayOfWeek.Tuesday:
                    if (!_shiftDetail.IsTue)
                        flag = true;
                    break;
                case DayOfWeek.Wednesday:
                    if (!_shiftDetail.IsWed)
                        flag = true;
                    break;
                case DayOfWeek.Thursday:
                    if (!_shiftDetail.IsThu)
                        flag = true;
                    break;
                case DayOfWeek.Friday:
                    if (!_shiftDetail.IsFri)
                        flag = true;
                    break;
                case DayOfWeek.Saturday:
                    if (!_shiftDetail.IsSat)
                        flag = true;
                    break;
            }
            return flag;
        }

        public int CountHolidaysBeforeDate(DateTime date, ShiftDetail shiftDetail)
        {
            _shiftDetail = shiftDetail;
            int totalDays = 0;
            date = date.AddDays(-1);

            LoadHolidayCalendar();
            //var holiday = _calendars.Find(i => i.EndDate.Date.Subtract(date.Date).TotalDays == 0);
            //while (holiday != null)
            //{
            //    while (date.Date.Subtract(holiday.StartDate.Date).TotalDays >= 0)
            //    {
            //        // check date is weekoff or not
            //        // if yes do nothing
            //        // else increament
            //        if (!CheckIsWeekend(date))
            //            totalDays++;

            //        date = date.AddDays(-1);
            //    }

            //    holiday = _calendars.Find(i => i.EndDate.Date.Subtract(date.Date).TotalDays == 0);
            //}

            var holiday = _calendars.Find(i => i.CalendarDate.Date.Subtract(date.Date).TotalDays == 0);
            if (holiday != null && !CheckIsWeekend(date))
                totalDays++;

            return totalDays;
        }

        public int CountHolidaysAfterDate(DateTime date, ShiftDetail shiftDetail)
        {
            _shiftDetail = shiftDetail;
            int totalDays = 0;
            date = date.AddDays(1);

            LoadHolidayCalendar();
            //var holiday = _calendars.Find(i => i.StartDate.Date.Subtract(date.Date).TotalDays == 0);
            //while (holiday != null)
            //{
            //    while (date.Date.Subtract(holiday.EndDate.Date).TotalDays <= 0)
            //    {
            //        // check date is weekoff or not
            //        // if yes do nothing
            //        // else increament
            //        if (!CheckIsWeekend(date))
            //            totalDays++;

            //        date = date.AddDays(1);
            //    }

            //    holiday = _calendars.Find(i => i.StartDate.Date.Subtract(date.Date).TotalDays == 0);
            //}
            var holiday = _calendars.Find(i => i.CalendarDate.Date.Subtract(date.Date).TotalDays == 0);
            if (holiday != null && !CheckIsWeekend(date))
                totalDays++;

            return totalDays;
        }

        public async Task<bool> IsHolidayBetweenTwoDates(DateTime fromDate, DateTime toDate)
        {
            bool flag = false;

            LoadHolidayCalendar();
            //var records = _calendars.Where(x => x.StartDate.Date >= fromDate.Date && x.EndDate.Date <= toDate.Date);
            var records = _calendars.Where(x => x.CalendarDate.Date.Subtract(fromDate.Date).TotalDays >= 0 && x.CalendarDate.Date.Subtract(toDate.Date).TotalDays <= 0);
            if (records.Any())
                flag = true;

            return await Task.FromResult(flag);
        }

        public async Task<int> GetHolidayBetweenTwoDates(DateTime fromDate, DateTime toDate)
        {
            LoadHolidayCalendar();
            //var holidays = _calendars.Count(x => (x.StartDate.Date >= fromDate.Date && x.EndDate.Date <= fromDate.Date));
            var holidays = _calendars.Count(x => x.CalendarDate.Date.Subtract(fromDate.Date).TotalDays >= 0 && x.CalendarDate.Date.Subtract(toDate.Date).TotalDays <= 0);

            return await Task.FromResult(holidays);
        }

        public async Task<decimal> GetHolidayCountInMonth(int month, int year)
        {
            decimal totalDays = 0;
            DateTime fromDate = new DateTime(year, month, 1);
            DateTime toDate = fromDate.AddMonths(1).AddDays(-1);
            LoadHolidayCalendar();

            //int fullDayHoliday = _calendars.Count(x => (x.StartDate.Date >= fromDate.Date && x.EndDate.Date <= fromDate.Date) && x.IsHalfDay);
            //int halfDayHoliday = _calendars.Count(x => (x.StartDate.Date >= fromDate.Date && x.EndDate.Date <= fromDate.Date) && !x.IsHalfDay);

            int fullDayHoliday = _calendars.Count(x => x.CalendarDate.Date.Subtract(fromDate.Date).TotalDays >= 0 && x.CalendarDate.Date.Subtract(toDate.Date).TotalDays <= 0 && x.IsHalfDay);
            int halfDayHoliday = _calendars.Count(x => x.CalendarDate.Date.Subtract(fromDate.Date).TotalDays >= 0 && x.CalendarDate.Date.Subtract(toDate.Date).TotalDays <= 0 && !x.IsHalfDay);

            totalDays = (decimal)(fullDayHoliday + (halfDayHoliday * 0.5));

            return await Task.FromResult(totalDays);
        }

        public async Task<bool> IsWeekOff(DateTime date)
        {
            bool flag = false;
            if (date.DayOfWeek == DayOfWeek.Sunday || date.DayOfWeek == DayOfWeek.Saturday)
                flag = true;

            return await Task.FromResult(flag);
        }

        public async Task<bool> IsWeekOffBetweenTwoDates(DateTime fromDate, DateTime toDate)
        {
            bool flag = false;
            while (fromDate.Date <= toDate.Date)
            {
                if (fromDate.DayOfWeek == DayOfWeek.Saturday || fromDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    flag = true;
                    break;
                }
                fromDate.AddDays(1);
            }
            return await Task.FromResult(flag);
        }

        public async Task<List<DateTime>> GetWeekOffBetweenTwoDates(DateTime fromDate, DateTime toDate)
        {
            List<DateTime> holidays = new List<DateTime>();
            while (fromDate.Date <= toDate.Date)
            {
                if (fromDate.DayOfWeek == DayOfWeek.Saturday || fromDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    holidays.Add(fromDate);
                }
                fromDate.AddDays(1);
            }
            return await Task.FromResult(holidays);
        }

        public async Task<int> CountWeekOffBetweenTwoDates(DateTime fromDate, DateTime toDate, ShiftDetail shiftDetail)
        {
            int count = 0;
            while (fromDate.Date <= toDate.Date)
            {
                var zoneDate = _timezoneConverter.ToTimeZoneDateTime(fromDate, _currentSession.TimeZone);
                switch (zoneDate.DayOfWeek)
                {
                    case DayOfWeek.Sunday:
                        if (!shiftDetail.IsSun)
                            count++;
                        break;
                    case DayOfWeek.Monday:
                        if (!shiftDetail.IsMon)
                            count++;
                        break;
                    case DayOfWeek.Tuesday:
                        if (!shiftDetail.IsTue)
                            count++;
                        break;
                    case DayOfWeek.Wednesday:
                        if (!shiftDetail.IsWed)
                            count++;
                        break;
                    case DayOfWeek.Thursday:
                        if (!shiftDetail.IsThu)
                            count++;
                        break;
                    case DayOfWeek.Friday:
                        if (!shiftDetail.IsFri)
                            count++;
                        break;
                    case DayOfWeek.Saturday:
                        if (!shiftDetail.IsSat)
                            count++;
                        break;
                }
                fromDate = fromDate.AddDays(1);
            }
            return await Task.FromResult(count);
        }

        public List<CompanyCalendarDetail> GetAllHolidayService(FilterModel filterModel)
        {
            filterModel.SearchString = filterModel.SearchString + $" and CompanyId = {_currentSession.CurrentUserDetail.CompanyId} and IsHoliday = true and Year = {DateTime.UtcNow.Year}";
            var result = _db.GetList<CompanyCalendarDetail>(Procedures.Company_Calender_Getby_Filter, new
            {
                filterModel.SearchString,
                filterModel.PageIndex,
                filterModel.PageSize,
                filterModel.SortBy
            });
            return result;
        }

        public List<CompanyCalendarDetail> HolidayInsertUpdateService(CompanyCalendarDetail calendar)
        {
            ValidateCalender(calendar);
            var result = _db.GetList<CompanyCalendarDetail>(Procedures.COMPANY_CALENDAR_ALL_COMPANY, new { _currentSession.CurrentUserDetail.CompanyId });
            if (!result.Any())
                throw HiringBellException.ThrowBadRequest("Company calendar not found. Please contact to admin");

            var existCalendar = result.Find(x => _timezoneConverter.ToTimeZoneDateTime(x.CalendarDate, _currentSession.TimeZone).Date.Subtract(_timezoneConverter.ToTimeZoneDateTime(calendar.CalendarDate, _currentSession.TimeZone).Date).TotalDays == 0);
            if (existCalendar == null)
                throw HiringBellException.ThrowBadRequest("Fail to get holiday detail.");

            existCalendar.HolidayName = calendar.HolidayName;
            existCalendar.IsHoliday = calendar.IsHoliday;
            existCalendar.IsHalfDay = calendar.IsHalfDay;
            existCalendar.DescriptionNote = calendar.DescriptionNote;
            existCalendar.IsPublicHoliday = calendar.IsPublicHoliday;
            existCalendar.DepartmentId = calendar.DepartmentId;

            var value = _db.Execute<CompanyCalendarDetail>(Procedures.Company_Calendar_Insupd, new
            {
                existCalendar.CompanyCalendarId,
                existCalendar.CompanyId,
                existCalendar.CalendarDate,
                existCalendar.EventId,
                existCalendar.IsHoliday,
                existCalendar.IsHalfDay,
                existCalendar.HolidayName,
                existCalendar.DayOfWeekNumber,
                existCalendar.DayOfWeek,
                existCalendar.DescriptionNote,
                existCalendar.DepartmentId,
                existCalendar.Year,
                existCalendar.IsPublicHoliday,
                AdminId = _currentSession.CurrentUserDetail.UserId
            }, true);

            if (string.IsNullOrEmpty(value))
                throw HiringBellException.ThrowBadRequest("Fail to insert/ update holiday");

            FilterModel filterModel = new FilterModel
            {
                SearchString = $"1=1 and CompanyId={calendar.CompanyId}"
            };
            return GetAllHolidayService(filterModel);
        }

        private void ValidateCalender(CompanyCalendarDetail calendar)
        {
            if (string.IsNullOrEmpty(calendar.DescriptionNote))
                throw HiringBellException.ThrowBadRequest("Description note is null or empty");

            if (string.IsNullOrEmpty(calendar.HolidayName))
                throw HiringBellException.ThrowBadRequest("Event name is null or empty");

            if (calendar.CalendarDate == null)
                throw HiringBellException.ThrowBadRequest("Select a valid holiday date");
        }

        public List<CompanyCalendarDetail> DeleteHolidayService(long CompanyCalendarId)
        {
            if (CompanyCalendarId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid holiday selected. Please select a vlid holiday");

            var result = _db.Execute<CompanyCalendarDetail>(Procedures.Company_Calender_Delete_By_Calenderid, new { CompanyCalendarId = CompanyCalendarId }, true);
            if (string.IsNullOrEmpty(result))
                throw HiringBellException.ThrowBadRequest("Fail to delete holiday");

            FilterModel filterModel = new FilterModel
            {
                SearchString = $"1=1 and CompanyId={_session.CurrentUserDetail.CompanyId}"
            };
            return GetAllHolidayService(filterModel);
        }

        public async Task<List<CompanyCalendarDetail>> ReadHolidayDataService(IFormFileCollection files)
        {
            try
            {
                var uploadedHolidayData = await _utilityService.ReadExcelData<CompanyCalendarDetail>(files);
                var result = await UpdateHolidayData(uploadedHolidayData);
                return result;
            }
            catch
            {
                throw;
            }
        }

        private async Task<List<CompanyCalendarDetail>> UpdateHolidayData(List<CompanyCalendarDetail> uploadedHolidayData)
        {
            var result = _db.GetList<CompanyCalendarDetail>(Procedures.Company_Calendar_Get_By_Company, new { CompanyId = _currentSession.CurrentUserDetail.CompanyId });
            if (!result.Any())
                throw HiringBellException.ThrowBadRequest("Company calendar not found. Please contact to admin");

            foreach (CompanyCalendarDetail calendar in uploadedHolidayData)
            {
                ValidateCalender(calendar);

                var existingCalendar = result.Find(x => _timezoneConverter.ToTimeZoneDateTime(x.CalendarDate, _currentSession.TimeZone).Date.Subtract(_timezoneConverter.ToTimeZoneDateTime(calendar.CalendarDate, _currentSession.TimeZone).Date).TotalDays == 0);
                if (existingCalendar == null)
                    throw HiringBellException.ThrowBadRequest("Fail to get holiday detail.");

                existingCalendar.HolidayName = calendar.HolidayName.ToUpper();
                existingCalendar.DescriptionNote = calendar.DescriptionNote.ToUpper();
                existingCalendar.IsHoliday = calendar.IsHoliday;
                existingCalendar.IsHalfDay = calendar.IsHalfDay;
                existingCalendar.IsPublicHoliday = calendar.IsPublicHoliday;
                existingCalendar.DepartmentId = calendar.DepartmentId;
                existingCalendar.AdminId = _currentSession.CurrentUserDetail.UserId;

                var value = _db.Execute<CompanyCalendarDetail>(Procedures.Company_Calendar_Insupd, new
                {
                    existingCalendar.CompanyCalendarId,
                    existingCalendar.CompanyId,
                    existingCalendar.CalendarDate,
                    existingCalendar.EventId,
                    existingCalendar.IsHoliday,
                    existingCalendar.IsHalfDay,
                    existingCalendar.HolidayName,
                    existingCalendar.DayOfWeekNumber,
                    existingCalendar.DayOfWeek,
                    existingCalendar.DescriptionNote,
                    existingCalendar.DepartmentId,
                    existingCalendar.Year,
                    existingCalendar.IsPublicHoliday,
                    AdminId = _currentSession.CurrentUserDetail.UserId
                }, true);
                if (string.IsNullOrEmpty(value))
                    throw HiringBellException.ThrowBadRequest("Fail to insert/ update holiday");
            }

            FilterModel filterModel = new FilterModel
            {
                SearchString = $"1=1 and CompanyId={_currentSession.CurrentUserDetail.CompanyId}"
            };
            var data = GetAllHolidayService(filterModel);

            return await Task.FromResult(data);
        }
    }
}