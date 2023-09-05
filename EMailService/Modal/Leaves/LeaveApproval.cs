namespace ModalLayer.Modal.Leaves
{
    public class LeaveApproval
    {
        public int LeaveApprovalId { get; set; }
        public int LeavePlanTypeId { get; set; }
        public bool IsLeaveRequiredApproval { get; set; }
        public int ApprovalLevels { get; set; }
        public bool IsRequiredAllLevelApproval { get; set; }
        public bool CanHigherRankPersonsIsAvailForAction { get; set; }
        public bool IsPauseForApprovalNotification { get; set; }
        public bool IsReportingManageIsDefaultForAction { get; set; }
        public int ApprovalWorkFlowId { get; set; }
        public int LeavePlanId { get; set; }
    }
}
