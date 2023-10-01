using CoreBottomHalf.Modal;
using ModalLayer.Modal.Leaves;
using System;
using System.Collections.Generic;

namespace ModalLayer.Modal
{
    public class AttendenceDetail : AttendanceDetailJson
    {
        public long AttendanceId { set; get; } = 0;
        public long UserId { get; set; }
        public int UserTypeId { get; set; }
        public double BillingHours { get; set; }
        public int IsTimeAttendacneApproved { get; set; } = 0;
        public long LeaveId { set; get; }
        public int AttendenceStatus { get; set; }
        public int GrossMinutes { get; set; } // total gross minutes per day
        public int LunchBreanInMinutes { get; set; } // 1 = full day, 2 = first half and 3 = second half
        public DateTime SubmittedOn { get; set; }
        public int ForYear { get; set; }
        public int ForMonth { get; set; }
        public DateTime? AttendenceFromDay { get; set; }
        public DateTime? AttendenceToDay { get; set; }
        public long SubmittedBy { get; set; }
        public long EmployeeUid { get; set; }
        public long ClientId { get; set; }
        public int TotalDays { get; set; }
        public int DaysPending { get; set; }
        public bool IsActiveDay { get; set; }
        public int CompanyId { get; set; }
        public List<string> EmailList { get; set; }
    }

    public class AttendanceWithClientDetail
    {
        public List<AttendanceJson> AttendacneDetails { set; get; }
        public long AttendanceId { set; get; }
        public Employee EmployeeDetail { set; get; }
    }

    public class AttendanceDetailBuildModal
    {
        public int attendanceSubmissionLimit { get; set; }
        public Attendance attendance { set; get; }
        public Employee employee { set; get; }
        public int SessionType { set; get; } = 1;
        public DateTime firstDate { set; get; }
        public DateTime presentDate { set; get; }
        public List<Calendar> calendars { set; get; }
        public ShiftDetail shiftDetail { set; get; }
        public LeaveDetail leaveDetail { set; get; }
        public List<ComplaintOrRequest> compalintOrRequests { set; get; }
    }

    public class AttendanceJson
    {
        public int AttendenceDetailId { set; get; }
        public bool IsHoliday { set; get; }
        public bool IsOnLeave { set; get; }
        public bool IsWeekend { set; get; }
        public DateTime AttendanceDay { set; get; }
        public string LogOn { set; get; }
        public string LogOff { set; get; }
        public int PresentDayStatus { set; get; }
        public string UserComments { set; get; }
        public string ApprovedName { set; get; }
        public long ApprovedBy { set; get; }
        public int SessionType { set; get; }
        public int TotalMinutes { set; get; }
        public bool IsOpen { set; get; }
        public string Emails { set; get; }
        public int WorkTypeId { get; set; }
    }

    public class AttendanceDetailJson : UserMangerCommonDetail
    {
        public long AttendanceId { get; set; }
    }
}
