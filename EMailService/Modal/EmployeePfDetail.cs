using System;

namespace EMailService.Modal
{
    public class EmployeePfDetail
    {
        public long EmployeePfDetailId { get; set; }
        public long EmployeeId { get; set; }
        public bool IsEmployeeEligibleForPF { get; set; }
        public bool IsExistingMemberOfPF { get; set; }
        public string PFNumber { get; set; }
        public string UniversalAccountNumber { get; set; }
        public string ESISerialNumber { get; set; }
        public bool IsEmployeeEligibleForESI { get; set; }
        public DateTime PFJoinDate { get; set; }
    }
}
