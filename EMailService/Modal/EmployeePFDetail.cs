using ModalLayer.Modal;
using System;

namespace EMailService.Modal
{
    public class EmployeePFDetail: ManagerDetail
    {
        public long EmployeePfDetailId { get; set; }
        public long EmployeeId { get; set; }
        public string PFNumber { get; set; }
        public string UniversalAccountNumber { get; set; }
        public DateTime? PFJoinDate { get; set; }
        public bool IsPFEnable { get; set; }
    }
}
