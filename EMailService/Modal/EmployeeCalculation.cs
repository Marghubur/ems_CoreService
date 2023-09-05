using ModalLayer.Modal.Accounts;
using System;
using System.Collections.Generic;

namespace ModalLayer.Modal
{
    public class EmployeeCalculation
    {
        public decimal CTC { set; get; }
        public decimal TaxableCTC { set; get; } // this is equals to the Gross i.e. CTC - (Tax Exampted components)
        public long EmployeeId { set; get; }
        public DateTime Doj { set; get; }
        public bool IsFirstYearDeclaration { set; get; }

        // Till date always be 1st of the financial year and month
        // e.g. let financial year and monthe is April, 2023 then date = 1st of April, 2023        
        public DateTime PayrollStartDate { set; get; }

        public DateTime financialYearDateTime { set; get; }
        public DateTime presentDate { set; get; }
        public Employee employee { set; get; }
        public decimal expectedAnnualGrossIncome { set; get; }
        public EmployeeDeclaration employeeDeclaration { set; get; }
        public EmployeeSalaryDetail employeeSalaryDetail { set; get; }
        public EmployeeEmailMobileCheck emailMobileCheck { set; get; }
        public List<SalaryComponents> salaryComponents { set; get; }
        public SalaryGroup salaryGroup { set; get; }
        public CompanySetting companySetting { set; get; }
        public List<SurChargeSlab> surchargeSlabs { set; get; }
        public List<PTaxSlab> ptaxSlab { set; get; }
        public PreviousEmployerDetail previousEmployerDetail { set; get; }
        public ShiftDetail shiftDetail { set; get; }
    }

    public class PreviousEmployerDetail
    {
        public decimal ProfessionalTax { set; get; }
        public decimal PF_with_80C { set; get; }
        public decimal TDS { set; get; }
        public decimal TotalIncome { set; get; }
    }

    public class PayrollCommonData
    {
        public List<SalaryComponents> salaryComponents { set; get; }
        public List<SalaryGroup> salaryGroups { set; get; }
        public List<Payroll> payrolls { set; get; }
        public List<SurChargeSlab> surchargeSlabs { set; get; }
        public List<PTaxSlab> ptaxSlab { set; get; }
        public TimeZoneInfo timeZone { set; get; }
        public DateTime presentDate { set; get; }
        public DateTime utcPresentDate { set; get; }
    }
}
