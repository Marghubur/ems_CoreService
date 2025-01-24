using System;

namespace EMailService.Modal.EmployeeModal
{
    public class EmployeeProfessionalDetail
    {
        public long EmployeeUid { get; set; }
        public string PANNo { get; set; }
        public string AadharNo { get; set; }
        public string AccountNumber { get; set; }
        public string BankName { get; set; }
        public string BranchName { get; set; }
        public string IFSCCode { get; set; }
        public string PFNumber { get; set; }
        public int PFTypeId { get; set; }
        public string UAN { get; set; }
        public string ESISerialNumber { get; set; }
        public DateTime? PFAccountCreationDate { get; set; }
        public bool IsEmployeeEligibleForESI { get; set; }
        public bool IsEmployeeEligibleForPF { get; set; }
        public bool IsExistingMemberOfPF { get; set; }
        public string BankAccountType { get; set; }
        public string ProfileStatusCode { get; set; }
    }
}
