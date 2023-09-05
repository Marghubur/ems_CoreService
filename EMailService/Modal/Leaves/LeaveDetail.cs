namespace ModalLayer.Modal.Leaves
{
    public class LeaveDetail
    {
        public int LeaveDetailId { set; get; }
        public int LeavePlanTypeId { set; get; }
        public bool IsLeaveDaysLimit { set; get; }
        public int LeaveLimit { set; get; }
        public bool CanApplyExtraLeave { set; get; }
        public int ExtraLeaveLimit { set; get; }
        public bool IsNoLeaveAfterDate { set; get; }
        public int LeaveNotAllocatedIfJoinAfter { set; get; }
        public bool CanCompoffAllocatedAutomatically { set; get; }
        public bool CanCompoffCreditedByManager { set; get; }
        public int LeavePlanId { get; set; }
    }
}
