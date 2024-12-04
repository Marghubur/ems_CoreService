using ModalLayer.Modal;
using System;

namespace EMailService.Modal
{
    public class EmployeePayrollConfig: ManagerDetail
    {
        public long EmployeePfDetailId { get; set; }
        public long EmployeeId { get; set; }
        public string PFNumber { get; set; }
        public string UAN { get; set; }
        public DateTime? PFAccountCreationDate { get; set; }
        public bool IsPFEnable { get; set; }
    }
}
