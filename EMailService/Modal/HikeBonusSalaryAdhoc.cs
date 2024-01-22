using System;

namespace EMailService.Modal
{
    public class HikeBonusSalaryAdhoc
    {
        public long SalaryAdhocId {set; get;}
        public long EmployeeId {set; get;}
        public int OrganizationId {set; get;}
        public int CompanyId {set; get;}
        public bool IsPaidByCompany {set; get;}
        public bool IsFine {set; get;}
        public bool IsHikeInSalary {set; get;}
        public bool IsBonus {set; get;}
        public string Description {set; get;}
        public decimal Amount {set; get;}
        public long ApprovedBy {set; get;}
        public bool IsRepeatJob {set; get;}
        public DateTime StartDate {set; get;}
        public DateTime EndDate {set; get;}
        public bool IsForSpecificPeriod {set; get;}
        public DateTime SequenceStartDate {set; get;}
        public int SequencePeriodOrder {set; get;}
        public DateTime SequenceEndDate {set; get;}
        public bool IsActive { set; get; }
    }
}
