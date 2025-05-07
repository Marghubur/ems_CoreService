using System;

namespace EMailService.Modal
{
    public class OtherDeductionAndReimbursementRepayment
    {
        public long RepaymentId { get; set; }
        public long OtherDeductionReimbursementId { get; set; }
        public long EmployeeId { get; set; }
        public int InstallmentNumber { get; set; }
        public decimal DeductionAmount { get; set; }
        public int DeductionMonth { get; set; }
        public int DeductionYear { get; set; }
        public decimal ActualDeductionAmount { get; set; }
        public DateTime ActualDeductionDate { get; set; }
        public int Status { get; set; }
        public string AdjustmentReason { get; set; }
    }
}
