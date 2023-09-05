using ModalLayer.Modal.Accounts;
using System;

namespace ModalLayer.Modal
{
    public class AssignedClients : EmployeeSalaryDetail
    {
        public long EmployeeUid { set; get; }
        public long ReportingManagerId { set; get; }
        public long EmployeeMappedClientsUid { get; set; }
        public long ClientUid { set; get; }
        public string ClientName { set; get; }
        public decimal ActualPackage { set; get; }
        public decimal FinalPackage { set; get; }
        public decimal TakeHomeByCandidate { set; get; }
        public bool IsPermanent { set; get; }
        public long FileId { set; get; }
        public int BillingHours { set; get; } = 0;
        public int WorkingDaysPerWeek { set; get; } = 0;
        public DateTime? DateOfLeaving { set; get; }        
    }
}
