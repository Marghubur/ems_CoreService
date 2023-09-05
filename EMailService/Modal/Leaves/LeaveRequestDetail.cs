using System;
using System.Collections.Generic;

namespace ModalLayer.Modal.Leaves
{
    public class LeaveRequestDetail : EmployeeCommonFields
    {
        public int UserTypeId { get; set; }
        public string RecordId { set; get; }
        public long LeaveId { set; get; }
        public DateTime LeaveFromDay { get; set; }
        public DateTime LeaveToDay { get; set; }
        public string Session { get; set; }
        public string Reason { get; set; }
        public string Notify { get; set; }
        public long AssigneeId { get; set; }
        public int LeaveTypeId { get; set; }
        public int LeavePlanId { get; set; }
        public int LeaveType { get; set; }
        public string LeavePlanName { get; set; }
        public int RequestType { get; set; }
        public int Year { get; set; }
        public string LeaveDetail { get; set; }
        public long LeaveRequestId { set; get; }
        public decimal PresentMonthLeaveAccrualed { set; get; }
        public decimal AvailableLeaves { set; get; }
        public decimal TotalLeaveApplied { set; get; }
        public decimal TotalApprovedLeave { set; get; }
        public string LeaveQuotaDetail { set; get; } // class type: LeaveTypeBrief
        public decimal TotalLeaveQuota { set; get; }
        public int RequestStatusId { get; set; }
        public int LeaveRequestNotificationId { get; set; }
        public DateTime UpdatedOn { get; set; }
        public List<EmployeeLeaveQuota> EmployeeLeaveQuotaDetail { set; get; }
        public bool IsPending { set; get; }
    }
}
