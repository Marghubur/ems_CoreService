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

    public class AttendanceDetailJson : UserMangerCommonDetail
    {
        public int AttendenceDetailId { set; get; }
        public DateTime AttendanceDay { get; set; }
        public int TotalMinutes { get; set; } // total effective minutes
        public bool IsHoliday { get; set; }
        public int PresentDayStatus { get; set; } = (int)DayStatus.Empty;
        public bool IsOnLeave { get; set; }
        public string LogOn { get; set; } // HH:MM
        public string LogOff { get; set; } // HH:MM
        public int SessionType { get; set; } // 1 = full day, 2 = first half and 3 = second half
        public string UserComments { get; set; }
        public bool IsOpen { get; set; }
        public bool IsWeekend { get; set; }
        public string Emails { get; set; }
        public long ApprovedBy { set; get; }
        public string ApprovedName { set; get; }
        public long AttendanceId { get; set; }
    }

    public class AttendanceWithClientDetail
    {
        public List<AttendanceDetailJson> AttendacneDetails { set; get; }
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
}
