using System;

namespace ModalLayer.Modal.Leaves
{
    public class Leave
    {
        public long LeaveRequestId { set; get; }
        public long EmployeeId { set; get; }
        public string LeaveDetail { get; set; }
        public int Year { get; set; }
        public decimal NumOfDays { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal AvailableLeaves { get; set; }
        public decimal TotalLeaveApplied {get; set;}
        public decimal TotalApprovedLeave {get; set;}
        public decimal TotalLeaveQuota {get; set;}
        public string LeaveQuotaDetail {get; set;}
    }
}
