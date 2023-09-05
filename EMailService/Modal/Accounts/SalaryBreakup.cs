using System;
using System.Collections.Generic;

namespace ModalLayer.Modal.Accounts
{
    public class CompleteSalaryBreakup
    {
        public decimal BasicAnnually { get; set; }
        public decimal ConveyanceAnnually { get; set; }
        public decimal HRAAnnually { get; set; }
        public decimal MedicalAnnually { get; set; }
        public decimal CarRunningAnnually { get; set; }
        public decimal InternetAnnually { get; set; }
        public decimal TravelAnnually { get; set; }
        public decimal ShiftAnnually { get; set; }
        public decimal SpecialAnnually { get; set; }
        public decimal GrossAnnually { get; set; }
        public decimal InsuranceAnnually { get; set; }
        public decimal PFAnnually { get; set; }
        public decimal GratuityAnnually { get; set; }
        public decimal CTCAnnually { get; set; }
        public decimal FoodAnnually { get; set; }
    }

    public class CalculatedSalaryBreakupDetail
    {
        public string ComponentId { set; get; }
        public string ComponentName { set; get; }
        public string Formula { set; get; }
        public decimal FinalAmount { set; get; }
        public decimal ComponentTypeId { set; get; }
        public bool IsIncludeInPayslip { get; set; }
    }

    public class AnnualSalaryBreakup
    {
        public string MonthName { set; get; }
        public bool IsPayrollExecutedForThisMonth { set; get; }
        public int MonthNumber { set; get; }
        public bool IsArrearMonth { set; get; }
        public DateTime PresentMonthDate { set; get; }

        // this flag indicate whether the candidate if eligible for the salary or not,
        // e.g. if joined, then current and onword month's he/she is eligible but not for previous month of current financial year.
        public bool IsActive { set; get; }
        public bool IsPreviouEmployer { get; set; } = false;
        public List<CalculatedSalaryBreakupDetail> SalaryBreakupDetails { get; set; }
    }
}
