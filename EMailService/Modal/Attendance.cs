using Bot.CoreBottomHalf.CommonModal;
using EMailService.Modal;
using ModalLayer.Modal.Accounts;
using System;
using System.Collections.Generic;

namespace ModalLayer.Modal
{
    public class Attendance : AttendanceDetailJson
    {
        public int UserTypeId { set; get; }
        public string AttendanceDetail { set; get; }
        public int TotalDays { set; get; }
        public int TotalWeekDays { set; get; }
        public int DaysPending { set; get; }
        public float TotalHoursBurend { set; get; }
        public float ExpectedHours { set; get; }
        public int ForYear { set; get; }
        public int ForMonth { set; get; }
        public DateTime? SubmittedOn { set; get; }
        // public DateTime DOJ { set; get; }
        public int PendingRequestCount { set; get; }
        public long SubmittedBy { set; get; }
    }






    public class DailyAttendance
    {
        public long AttendanceId { set; get; }
        public long EmployeeId { set; get; }
        public string EmployeeName { set; get; }
        public string EmployeeEmail { set; get; }
        public long ReviewerId { set; get; }
        public string ReviewerName { set; get; }
        public string ReviewerEmail { set; get; }
        public int ProjectId { set; get; }
        public int TaskId { set; get; }
        public int TaskType { set; get; }
        public string LogOn { set; get; }
        public string LogOff { set; get; }
        public int TotalMinutes { set; get; }
        public string Comments { set; get; }
        public int AttendanceStatus { set; get; }
        public int WeekOfYear { set; get; }
        public DateTime AttendanceDate { set; get; }
        public WorkType WorkTypeId { set; get; }
        public bool IsHoliday { set; get; }
        public int HolidayId { set; get; }
        public bool IsOnLeave { set; get; }
        public bool IsWeekend { set; get; }
        public int LeaveId { set; get; }
        public long CreatedBy { set; get; }
        public DateTime CreatedOn { set; get; }
        public long UpdatedBy { set; get; }
        public DateTime UpdatedOn { set; get; }
        public string ManagerName { set; get; }
        public string ManagerMobile { set; get; }
        public string ManagerEmail { set; get; }
        public string FilePath { set; get; }
        public string FileExtension { set; get; }
        public string FileName { set; get; }
        public int Total { get; set; }
        public int PageIndex { get; set; }
        public int TotalDays { get; set; }
    }












    public class PayrollEmployeeData : Attendance
    {
        public string LeaveDetail { set; get; }
        public int DeclarationStartMonth { set; get; }
        public int DeclarationEndMonth { set; get; }
        public int FinancialYear { set; get; }
        public EmployeeDeclaration employeeDeclaration { set; get; }
        public DateTime Doj { get; set; }
        public string PreviuosMonthAttendance { get; set; }
        public int CompanyId { set; get; }
        public string CompleteSalaryDetail { set; get; }
        public decimal CTC { set; get; }
        public int GroupId { set; get; }
        public string TaxDetail { set; get; }
        public int WorkShiftId { get; set; }
    }

    public class EmployeePayrollData
    {
        public long EmployeeId { set; get; }
        public string EmployeeName { set; get; }
        public string Email { set; get; }
        public string CompleteSalaryDetail { set; get; }
        public decimal CTC { set; get; }
        public int GroupId { set; get; }
        public string TaxDetail { set; get; }
        public int CompanyId { set; get; }
        public int WorkShiftId { get; set; }
        public DateTime Doj { get; set; }
        public EmployeeDeclaration employeeDeclaration { set; get; }
    }

    public class PayrollEmployeePageData
    {
        public List<LeaveRequestNotification> leaveRequestDetails { set; get; }
        public List<EmployeePayrollData> employeeData { set; get; }
        public List<HikeBonusSalaryAdhoc> hikeBonusSalaryAdhoc { set; get; }
        public List<DailyAttendance> dailyAttendances { set; get; }
        public List<JoinedAfterPayrollEmployees> joinedAfterPayrollEmployees { set; get; } = [];
    }

    public class JoinedAfterPayrollEmployees
    {
        public long EmployeeUid { get; set; }
    }
}
