using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using ModalLayer.Modal.Accounts;
using System;
using System.Collections.Generic;

namespace ModalLayer.Modal
{
    public class PayrollCalculationModal
    {
        public List<DailyAttendance> DailyAttendances { get; set; }
        public List<LeaveRequestNotification> UserLeaveRequests { set; get; }
        public EmployeePayrollData CurrentEmployee { get; set; }
        public TaxDetails PresentTaxDetail { set; get; }
        public List<TaxDetails> TaxDetails {  set; get; }
        public DateTime PayrollDate { get; set; }
        public DateTime Doj { get; set; }
        public DateTime LocalTimePresentDate { get; set; }
        public int PayrollRunDay { get; set; }
        public ShiftDetail ShiftDetail { get; set; }
        public decimal DaysInPresentMonth { get; set; }
        public decimal DaysInPreviousMonth { get; set; }
        public decimal MinutesInPresentMonth { set; get; }
        public decimal MinutesNeededInPresentMonth { set; get; }
        public decimal MinutesNeededInPreviousMonth { set; get; }
        public decimal PreviousMonthLOPMinutes { set; get; }
        public bool IsWeekendsExcluded { set; get; }
        public bool IsHolidaysExcluded { get; set; }
        public decimal ArrearAmount { get; set; }
        public decimal BonusAmount { get; set; }
    }
}
