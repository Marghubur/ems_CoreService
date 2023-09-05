using System.Collections.Generic;

namespace ModalLayer.Modal.Leaves
{
    public class LeaveApplyDetail
    {
        public int LeaveApplyDetailId { get; set; }
        public int LeavePlanTypeId { get; set; }
        public bool IsAllowForHalfDay { get; set; }
        public bool EmployeeCanSeeAndApplyCurrentPlanLeave { get; set; } = true;
        public List<LeaveRuleInNotice> RuleForLeaveInNotice { get; set; }
        public int ApplyPriorBeforeLeaveDate { get; set; }
        public int BackDateLeaveApplyNotBeyondDays { get; set; }
        public int RestrictBackDateLeaveApplyAfter { get; set; }
        public bool CurrentLeaveRequiredComments { get; set; }
        public bool ProofRequiredIfDaysExceeds { get; set; }
        public int NoOfDaysExceeded { get; set; }
        public int LeavePlanId { get; set; }
    }

    public class LeaveRuleInNotice
    {
        public int RemaningCalendarDayInNotice { set; get; }
        public int RequiredCalendarDaysForLeaveApply { set; get; }
        public int RemaningWorkingDaysInNotice { set; get; }
    }
}
