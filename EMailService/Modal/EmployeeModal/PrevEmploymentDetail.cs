using System;

namespace EMailService.Modal.EmployeeModal
{
    public class PrevEmploymentDetail
    {
        public long EmployeeUid { get; set; }
        public string LastCompanyDesignation { get; set; }
        public DateTime? WorkingFromDate { get; set; }
        public DateTime? WorkingToDate { get; set; }
        public string LastCompanyAddress { get; set; }
        public string LastCompanyNatureOfDuty { get; set; }
        public decimal LastDrawnSalary { get; set; }
        public int ExprienceInYear { get; set; }
        public string LastCompanyName { get; set; }
        public string ProfileStatusCode { get; set; }
    }
}
