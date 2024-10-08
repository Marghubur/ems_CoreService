﻿using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
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
        public DateTime PayrollLocalTimeStartDate { set; get; }

        public DateTime financialYearStartDateTime { set; get; }
        public DateTime financialYearEndDateTime { set; get; }
        public DateTime presentDate { set; get; }
        public Employee employee { set; get; }
        public decimal expectedAnnualGrossIncome { set; get; }
        public EmployeeDeclaration employeeDeclaration { set; get; }
        public EmployeeSalaryDetail employeeSalaryDetail { set; get; }
        public EmployeeEmailMobileCheck emailMobileCheck { set; get; }
        public List<SalaryComponents> salaryComponents { set; get; }
        public CompanySetting companySetting { set; get; }
        public List<SurChargeSlab> surchargeSlabs { set; get; }
        public List<PTaxSlab> ptaxSlab { set; get; }
        public PreviousEmployerDetail previousEmployerDetail { set; get; }
        public ShiftDetail shiftDetail { set; get; }
        public PfEsiSetting pfEsiSetting { get; set; }
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
        // public List<SalaryGroup> salaryGroups { set; get; }
        public Payroll payroll { set; get; }
        public List<SurChargeSlab> surchargeSlabs { set; get; }
        public List<PTaxSlab> ptaxSlab { set; get; }
        public List<ShiftDetail> shiftDetail { get; set; }
        public TimeZoneInfo timeZone { set; get; }
        public Company company { set; get; }
        public DateTime localTimePresentDate { set; get; }
        public DateTime utcTimePresentDate { set; get; }
    }
}
