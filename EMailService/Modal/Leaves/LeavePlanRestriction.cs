namespace ModalLayer.Modal.Leaves
{
    public class LeavePlanRestriction
    {
        public decimal LeavePlanRestrictionId { set; get; }
        public decimal LeavePlanId { set; get; }
        public bool CanApplyAfterProbation { set; get; } = true;
        public bool CanApplyAfterJoining { set; get; }
        public decimal DaysAfterProbation { set; get; }
        public decimal DaysAfterJoining { set; get; }
        public bool IsAvailRestrictedLeavesInProbation { set; get; }
        public decimal LeaveLimitInProbation { set; get; }
        public bool IsLeaveInNoticeExtendsNoticePeriod { set; get; }
        public decimal NoOfTimesNoticePeriodExtended { set; get; }
        public bool CanManageOverrideLeaveRestriction { set; get; }
        public decimal GapBetweenTwoConsicutiveLeaveDates { set; get; }
        public decimal LimitOfMaximumLeavesInCalendarYear { set; get; }
        public decimal LimitOfMaximumLeavesInCalendarMonth { set; get; }
        public decimal LimitOfMaximumLeavesInEntireTenure { set; get; }
        public decimal MinLeaveToApplyDependsOnAvailable { set; get; }
        public decimal AvailableLeaves { set; get; }
        public decimal RestrictFromDayOfEveryMonth { set; get; }
        public bool IsCurrentPlanDepnedsOnOtherPlan { set; get; }
        public decimal AssociatedPlanTypeId { set; get; }
        public bool IsCheckOtherPlanTypeBalance { set; get; }
        public decimal DependentPlanTypeId { set; get; }
        public int LeavePlanTypeId { get; set; }
    }
}
