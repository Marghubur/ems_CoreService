namespace ModalLayer.Modal.Leaves
{
    public class LeavePlanConfiguration
    {
        public LeaveDetail leaveDetail { get; set; }
        public LeaveAccrual leaveAccrual { get; set; }
        public LeaveApplyDetail leaveApplyDetail { get; set; }
        public LeaveEndYearProcessing leaveEndYearProcessing { get; set; }
        public LeaveHolidaysAndWeekoff leaveHolidaysAndWeekoff { get; set; }
        public LeavePlanRestriction leavePlanRestriction { get; set; }
        public LeaveApproval leaveApproval { get; set; }
        public ManagementLeave managementLeave { get; set; }
    }
}
