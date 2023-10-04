using ModalLayer.Modal.Accounts;
using System;
using System.Collections.Generic;

namespace ModalLayer.Modal
{
    public class PayrollCalculationModal
    {
        public int payCalculationId { get; set; }
        public bool isExcludeHolidays { get; set; }
        public long employeeId { get; set; }
        public List<PayrollEmployeeData> payrollEmployeeDatas { get; set; }
        public DateTime payrollDate { get; set; }
        public ShiftDetail shiftDetail { get; set; }
        public int totalDaysInMonth { get; set; }
    }
}
