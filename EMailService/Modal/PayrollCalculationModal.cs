using ModalLayer.Modal.Leaves;
using System;
using System.Collections.Generic;

namespace ModalLayer.Modal
{
    public class PayrollCalculationModal
    {
        public int payCalculationId { get; set; }
        public bool isExcludeHolidays { get; set; }
        public long employeeId { get; set; }
        public List<PayrollEmployeeData> payrollEmployeeData { get; set; }
        public List<LeaveRequestNotification> userLeaveRequests { set; get; }
        public DateTime payrollDate { get; set; }
        public ShiftDetail shiftDetail { get; set; }
        public decimal totalDaysInPresentMonth { get; set; }
        public decimal daysInPreviousMonth { get; set; }
        public decimal presentActualMins { set; get; }
        public decimal presentMinsNeeded { set; get; }
        public decimal prevLOPMins { set; get; }
        public decimal prevMinsNeeded { set; get; }
        public bool isExcludingWeekends { set; get; }
        public decimal ArrearAmount { get; set; }
        public decimal BonusAmount { get; set; }
    }
}
