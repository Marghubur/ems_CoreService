using System;
using System.Collections.Generic;

namespace ModalLayer.Modal.Accounts
{
    public class EmployeeDeclaration
    {
        public long EmployeeDeclarationId { set; get; }
        public long EmployeeId { set; get; }
        public string DocumentPath { set; get; }
        public string DeclarationDetail { set; get; }
        public string ComponentId { set; get; }
        public decimal DeclaredValue { set; get; }
        public int TaxRegimeDescId { get; set; }
        public string Email { set; get; }
        public decimal TotalAmount { set; get; }
        public decimal TotalAmountOnNewRegim { set; get; }
        public decimal TaxNeedToPay { set; get; }
        public decimal TaxNeedToPayOnNewRegim { set; get; }
        public decimal TaxPaid { set; get; }
        public List<SalaryComponents> SalaryComponentItems { set; get; }
        public List<SalaryComponents> ExemptionDeclaration { set; get; }
        public List<SalaryComponents> OtherDeclaration { set; get; }
        public List<SalaryComponents> TaxSavingAlloance { set; get; }
        public List<SalaryComponents> Section16TaxExemption { get; set; }
        public List<Files> FileDetails { set; get; }
        public EmployeeSalaryDetail SalaryDetail { set; get; }
        public Dictionary<string, List<string>> Sections { set; get; }
        public List<DeclarationReport> Declarations { set; get; } = new List<DeclarationReport>();
        public string HouseRentDetail { get; set; }
        public Dictionary<int, TaxSlabDetail> IncomeTaxSlab { get; set; }
        public Dictionary<int, TaxSlabDetail> NewRegimIncomeTaxSlab { get; set; }
        public decimal SurChargesAndCess { get; set; }
        public decimal SurChargesAndCessOnNewRegim { get; set; }
        public EmployeeHRA HRADeatils { get; set; }
        public decimal TotalDeclaredAmount { set; get; }
        public decimal TotalApprovedAmount { set; get; }
        public decimal TotalRejectedAmount { set; get; }
        public int EmployeeCurrentRegime { set; get; }
        public int DeclarationStartMonth { set; get; }
        public int DeclarationEndMonth { set; get; }
        public int DeclarationFromYear { set; get; }
        public int DeclarationToYear { set; get; }
        public string DefaultSlaryGroupMessage { get; set; }
        public int TotalMonths { get; set; }
        public string FullName { get; set; }
    }

    public class EmployeeHRA
    {
        public decimal HRA1 { set; get; }
        public decimal HRA2 { set; get; }
        public decimal HRA3 { set; get; }
        public decimal HRAAmount { set; get; }
    }

    public class DeclarationReport
    {
        public string DeclarationName { set; get; }
        public List<string> Declarations { set; get; }
        public decimal TotalAmountDeclared { set; get; }
        public int NumberOfProofSubmitted { set; get; }
        public decimal RejectedAmount { set; get; }
        public decimal AcceptedAmount { set; get; }
        public decimal MaxAmount { set; get; } = 0;
    }
}
