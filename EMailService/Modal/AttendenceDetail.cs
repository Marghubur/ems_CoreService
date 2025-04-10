﻿using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using Bot.CoreBottomHalf.CommonModal.Leave;
using EMailService.Modal;
using ModalLayer.Modal.Leaves;
using System;
using System.Collections.Generic;

namespace ModalLayer.Modal
{
    public class AttendenceDetail : AttendanceDetailJson
    {
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
        public List<LeaveRequestNotification> LeaveRequestDetail { set; get; }
        public List<DailyAttendance> DailyAttendances { set; get; }
        public List<Project> Projects { get; set; }
        public ShiftDetail EmployeeShift { get; set; }
    }

    public class AttendanceDetailBuildModal
    {
        public int attendanceSubmissionLimit { get; set; }
        public Attendance attendance { set; get; }
        public Employee employee { set; get; }
        public int SessionType { set; get; } = 1;
        public DateTime firstDate { set; get; }
        public DateTime presentDate { set; get; }
        public List<CompanyCalendarDetail> calendars { set; get; }
        public ShiftDetail shiftDetail { set; get; }
        public LeaveDetail leaveDetail { set; get; }
        public List<ComplaintOrRequest> compalintOrRequests { set; get; }
        public List<Project> projects { get; set; }
    }

    public class AttendanceDetailJson : UserMangerCommonDetail
    {
        public long AttendanceId { get; set; }
        public string FilePath { get; set; }
        public string FileExtension { get; set; }
        public string FileName { get; set; }
    }





    // -------------- new daily_attendance_model

    public class DailyAttendanceBuilder
    {
        public int attendanceSubmissionLimit { get; set; }
        public DailyAttendance attendance { set; get; }
        public Employee employee { set; get; }
        public int SessionType { set; get; } = 1;
        public DateTime firstDate { set; get; }
        public DateTime presentDate { set; get; }
        public List<CompanyCalendarDetail> calendars { set; get; }
        public ShiftDetail shiftDetail { set; get; }
        public List<LeaveRequestNotification> leaveDetails { set; get; }
        public List<ComplaintOrRequest> compalintOrRequests { set; get; }
        public List<Project> projects { get; set; }
        public DateTime LastRunPayrollDate { get; set; }
    }

    public class AttendanceConfig
    {
        public Employee EmployeeDetail { set; get; }
        public List<Project> Projects { get; set; }
        public List<WeekDates> Weeks { get; set; }
        public AttendanceWithClientDetail AttendanceWithClientDetail { get; set; }
    }

    public class WeekDates
    {
        public int WeekIndex { set; get; }
        public DateTime StartDate { set; get; }
        public DateTime EndDate { set; get; }
    }
}
