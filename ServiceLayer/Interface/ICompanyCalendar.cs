using EMailService.Modal;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface ICompanyCalendar
    {
        Task<bool> IsHoliday(DateTime date);
        int CountHolidaysBeforeDate(DateTime date, ShiftDetail shiftDetail);
        int CountHolidaysAfterDate(DateTime date, ShiftDetail shiftDetail);
        Task<bool> IsHolidayBetweenTwoDates(DateTime fromDate, DateTime toDate);
        Task<int> GetHolidayBetweenTwoDates(DateTime fromDate, DateTime toDate);
        Task<bool> IsWeekOff(DateTime date);
        Task<bool> IsWeekOffBetweenTwoDates(DateTime fromDate, DateTime toDate);
        Task<List<DateTime>> GetWeekOffBetweenTwoDates(DateTime fromDate, DateTime toDate);
        List<CompanyCalendarDetail> GetAllHolidayService(FilterModel filterModel);
        Task<List<CompanyCalendarDetail>> HolidayInsertUpdateService(CompanyCalendarDetail calendar);
        List<CompanyCalendarDetail> DeleteHolidayService(long CompanyCalendarId);
        Task<int> CountWeekOffBetweenTwoDates(DateTime fromDate, DateTime toDate, ShiftDetail shiftDetail);
        Task<decimal> GetHolidayCountInMonth(int month, int year);
        Task<List<CompanyCalendarDetail>> ReadHolidayDataService(IFormFileCollection files);
    }
}
