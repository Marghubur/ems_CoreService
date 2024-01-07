using System;

namespace EMailService.Modal
{
    public class TimesheetRequestModel
    {
        public bool IsOpen { set; get; }
        public int WeekDay { set; get; }
        public bool IsHoliday { set; get; }
        public bool IsWeekEnd { set; get; }
        public DateTime PresentDate { set; get; }
        public int ActualBurnedMinutes { set; get; }
        public int ExpectedBurnedMinutes { set; get; }
    }
}
