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
        public List<LeaveRequestDetail> userLeaveRequests { set; get; }
        public DateTime payrollDate { get; set; }
        public ShiftDetail shiftDetail { get; set; }
        public int totalDaysInMonth { get; set; }
        public decimal actualMinutesWorked { set; get; }
        public decimal expectedMinutesToWorked { set; get; }
        public bool isExcludingWeekends { set; get; }
        public bool isExcludingHolidays { set; get; }
    }
}
