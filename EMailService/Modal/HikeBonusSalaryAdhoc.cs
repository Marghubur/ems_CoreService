using System;

namespace EMailService.Modal
{
    public class HikeBonusSalaryAdhoc
    {
        public long SalaryAdhocId {set; get;}
        public long EmployeeId {set; get;}
        public int ProcessStepId { set; get; }
        public int FinancialYear { set; get; }
        public int OrganizationId {set; get;}
        public int CompanyId {set; get;}
        public bool IsPaidByCompany {set; get;}
        public bool IsPaidByEmployee { get; set; }
        public bool IsFine {set; get;}
        public bool IsHikeInSalary {set; get;}
        public bool IsBonus {set; get;}
        public bool IsReimbursment { get; set; }
        public bool IsSalaryOnHold { set; get;}
        public bool IsArrear { get; set; }
        public bool IsOvertime { get; set; }
        public bool IsCompOff { get; set; }
        public string OTCalculatedOn { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountInPercentage { get; set; }
        public bool IsActive { set; get; }
        public string PaymentActionType { get; set; }
        public string Comments { get; set; }
        public int Status { get; set; }
        public int ForYear { get; set; }
        public int ForMonth { get; set; }
        public int ProgressState { get; set; }
    }
}
