using System;

namespace ModalLayer.Modal.Accounts
{
    public class EmployeeSalaryDetail : ManagerDetail
    {
        public long EmployeeId { set; get; }
        public decimal CTC { set; get; }
        public decimal GrossIncome { set; get; }
        public decimal NetSalary { set; get; }
        public string CompleteSalaryDetail { set; get; }
        public string NewSalaryDetail { get; set; }
        public int GroupId { set; get; }
        public string TaxDetail { get; set; }
        public int FinancialStartYear { get; set; }
        public bool IsCTCChanged { set; get; } = false;
        public DateTime DateOfJoining { set; get; }
        public DateTime UpdatedOn { set; get; }
    }
}