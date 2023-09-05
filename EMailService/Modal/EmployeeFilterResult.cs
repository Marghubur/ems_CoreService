using System;
using System.Collections.Generic;
using System.Text;

namespace ModalLayer.Modal
{
    public class EmployeeFilterResult
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public int LeavePlanId { get; set; }
        public bool IsActive { get; set; }
        public string AadharNo { get; set; }
        public string PANNo { get; set; }
        public string AccountNumber { get; set; }
        public string BankName { get; set; }
        public string IFSCCode { get; set; }
        public string Domain { get; set; }
        public string Specification { get; set; }
        public decimal ExprienceInYear { get; set; }
        public float ActualPackage { get; set; }
        public float FinalPackage { get; set; }
        public float TakeHomeByCandidate { get; set; }
        public string ClientJson { get; set; }
        public DateTime UpdatedOn { get; set; }
        public DateTime CreatedOn { get; set; }

    }
}
