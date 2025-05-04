using System;

namespace EMailService.Modal
{
    public class SalaryAdanceRepayment
    {
        public long RepaymentId { get; set; }
        public long SalaryAdvanceRequestId { get; set; }
        public long EmployeeId { get; set; }
        public int InstallmentNumber { get; set; }
        public decimal ScheduledAmount { get; set; }
        public DateTime ScheduledDate { get; set; }
        public decimal ActualAmount { get; set; }
        public DateTime ActualDate { get; set; }
        public int Status { get; set; }
        public string AdjustmentReason { get; set; }
    }
}