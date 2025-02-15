using ModalLayer;
using System;

namespace EMailService.Modal
{
    public class CompanyCalendarDetail: CreationInfo
    {
        public long CompanyCalendarId { get; set; }
        public int CompanyId { get; set; }
        public DateTime CalendarDate { get; set; }
        public int EventId { get; set; } = 0;
        public bool IsHoliday { get; set; }
        public string HolidayName { get; set; }
        public bool IsHalfDay { get; set; }
        public int DayOfWeekNumber { get; set; }
        public string DayOfWeek { get; set; }
        public string DescriptionNote { get; set; }
        public int DepartmentId { get; set; }
        public int Year { get; set; }
        public bool IsPublicHoliday { get; set; }
        public int RowIndex { get; set; }
        public int Total { get; set; }
    }
}
