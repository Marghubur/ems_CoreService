using EMailService.Modal;
using ModalLayer.Modal.Accounts;
using ModalLayer.Modal.Leaves;
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
        public DateTime DOJ { set; get; }
        public int PendingRequestCount { set; get; }
        public long SubmittedBy { set; get; }
        public WorkType WorkTypeId { set; get; }
    }

    public class PayrollEmployeeData : Attendance
    {
        public string LeaveDetail { set; get; }
        public int CompanyId { set; get; }
        public string CompleteSalaryDetail { set; get; }
        public decimal CTC { set; get; }
        public int GroupId { set; get; }
        public string TaxDetail { set; get; }
        public int DeclarationStartMonth { set; get; }
        public int DeclarationEndMonth { set; get; }
        public int FinancialYear { set; get; }
        public EmployeeDeclaration employeeDeclaration { set; get; }
        public DateTime Doj { get; set; }
        public int WorkShiftId { get; set; }
    }

    public class PayrollEmployeePageData
    {
        public List<LeaveRequestDetail> leaveRequestDetails { set; get; }
        public List<PayrollEmployeeData> payrollEmployeeData { set; get; }
        public List<HikeBonusSalaryAdhoc> hikeBonusSalaryAdhoc { set; get; }

    }

    public enum WorkType
    {
        EMPTY = 0,
        WORKFROMHOME = 1,
        WORKFROMOFFICE = 2,
        LEAVE = 3,
        FIRSTHALFDAY = 4,
        SECONDHALFDAY = 5
    }
}
