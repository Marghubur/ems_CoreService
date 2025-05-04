using System;

namespace EMailService.Modal
{
    public class SalaryAdvanceRequest
    {
        public long SalaryAdvanceRequestId { get; set; }
        public long EmployeeId { get; set; }
        public decimal RequestAmount { get; set; }
        public DateTime RequestDate { get; set; }
        public string RequestReason { get; set; }
        public int InstallmentCount { get; set; }
        public decimal MonthlyDeductionAmount { get; set; }
        public int Status { get; set; }
        public long ApproverId { get; set; }
        public decimal ApprovedAmount { get; set; }
        public DateTime ApprovedDate { get; set; }
        public DateTime DisbursementDate { get; set; }
        public string Comments { get; set; }
        public bool IsActive { get; set; }
    }
}
