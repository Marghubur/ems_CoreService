using System;

namespace EMailService.Modal.Payroll
{
    public class PayrollMonthlyDetail
    {
        public int PayrollMonthlyDetailId { set; get; }
        public int EmployeeId { set; get; }
        public int ForYear { set; get; }
        public int ForMonth { set; get; }
        public decimal GrossTotal { set; get; }
        public decimal PayableToEmployee { set; get; }
        public decimal PFByEmployer { set; get; }
        public decimal PFByEmployee { set; get; }
        public decimal ProfessionalTax { set; get; }
        public decimal TotalDeduction { set; get; }
        public int PayrollStatus { set; get; }
        public DateTime PaymentRunDate { set; get; }
        public long ExecutedBy { set; get; }
        public DateTime ExecutedOn { set; get; }
        public int CompanyId { set; get; }
        public int TotalEmployees { set; get; }
    }
}
