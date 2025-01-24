using System;

namespace EMailService.Modal.EmployeeModal
{
    public class EmployeeNomineeDetail
    {
        public int NomineeId { get; set; } = 0;
        public long EmployeeId { get; set; }
        public string NomineeName { get; set; } 
        public string NomineeRelationship { get; set; } 
        public string NomineeMobile { get; set; } 
        public string NomineeEmail { get; set; }
        public DateTime? NomineeDOB { get; set; }
        public string NomineeAddress { get; set; }
        public decimal PercentageShare { get; set; } = 0;
        public bool IsPrimaryNominee { get; set; } = false;
        public string ProfileStatusCode { get; set; }

    }
}
