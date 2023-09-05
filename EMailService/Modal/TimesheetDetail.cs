using CoreBottomHalf.Modal;
using System;
using System.Collections.Generic;

namespace ModalLayer.Modal
{
    public class TimesheetDetail : WeeklyTimesheetDetail
    {
        public long TimesheetId { set; get; } = 0;
        public long ClientId { get; set; }
        public string TimesheetWeeklyJson { get; set; }
        public int TotalWeekDays { get; set; }
        public int TotalWorkingDays { get; set; }
        public int TimesheetStatus { get; set; }
        public DateTime TimesheetStartDate { get; set; }
        public DateTime TimesheetEndDate { get; set; }
        public string UserComments { set; get; }
        public int ForYear { get; set; }
        public int ForMonth { get; set; }
        public DateTime SubmittedOn { get; set; }
        public long SubmittedBy { get; set; }
        public long ExecutedBy { get; set; }
        public bool IsSaved { get; set; }
        public bool IsSubmitted { get; set; }
        public List<WeeklyTimesheetDetail> TimesheetWeeklyData { get; set; }
    }

    public class WeeklyTimesheetDetail : UserMangerCommonDetail
    {
        public DayOfWeek WeekDay { get; set; }
        public DateTime PresentDate { get; set; }
        public int ExpectedBurnedMinutes { get; set; }
        public int ActualBurnedMinutes { get; set; }
        public bool IsHoliday { get; set; }
        public bool IsWeekEnd { get; set; }
        public bool IsOpen { get; set; }
    }

    public class DailyTimesheetDetail
    {
        public long TimesheetId { set; get; }
        public long EmployeeId { get; set; }
        public long ClientId { get; set; }
        public int UserTypeId { get; set; }
        public decimal TotalMinutes { get; set; }
        public bool IsHoliday { get; set; }
        public bool IsWeekEnd { get; set; }
        public int TimesheetStatus { get; set; }
        public DateTime PresentDate { get; set; }
        public string UserComments { get; set; }
        public string EmployeeName { set; get; }
        public string Email { set; get; }
        public string Mobile { set; get; }
        public long ReportingManagerId { set; get; }
        public string ManagerName { set; get; }
    }
}
