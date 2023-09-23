using System;

namespace EMailService.Modal.Payroll
{
    public class PayrollMonthlyDetail
    {
        public int PayrollMonthlyDetailId { set; get; }
        public int ForYear { set; get; }
        public int ForMonth { set; get; }
        public decimal TotalPayableToEmployees { set; get; }
        public decimal TotalPFByEmployer { set; get; }
        public decimal TotalProfessionalTax { set; get; }
        public decimal TotalDeduction { set; get; }
        public int PayrollStatus { set; get; }
        public String Reason { set; get; }
        public DateTime PaymentRunDate { set; get; }
        public String ProofOfDocumentPath { set; get; }
        public long ExecutedBy { set; get; }
        public DateTime ExecutedOn { set; get; }
        public int CompanyId { set; get; }
        public int TotalEmployees { set; get; }
    }
}
