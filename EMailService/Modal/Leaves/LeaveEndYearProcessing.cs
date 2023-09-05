using System.Collections.Generic;

namespace ModalLayer.Modal.Leaves
{
    public class LeaveEndYearProcessing
    {
        public int LeaveEndYearProcessingId { set; get; }
        public int LeavePlanTypeId { set; get; }
        public bool IsLeaveBalanceExpiredOnEndOfYear { set; get; }
        public bool AllConvertedToPaid { set; get; }
        public bool AllLeavesCarryForwardToNextYear { set; get; }
        public bool PayFirstNCarryForwordRemaning { set; get; }
        public bool CarryForwordFirstNPayRemaning { set; get; }
        public bool PayNCarryForwardForPercent { set; get; }
        public string PayNCarryForwardDefineType { set; get; }
        public bool DoestCarryForwardExpired { set; get; }
        public decimal ExpiredAfter { set; get; }
        public bool DoesExpiryLeaveRemainUnchange { set; get; }
        public bool DeductFromSalaryOnYearChange { set; get; }
        public bool ResetBalanceToZero { set; get; }
        public bool CarryForwardToNextYear { set; get; }
        public int LeavePlanId { get; set; }
        public List<FixedPayNCarryForward> FixedPayNCarryForward { set; get; }
        public List<PercentagePayNCarryForward> PercentagePayNCarryForward { get; set; }
    }

    public class FixedPayNCarryForward
    {
        public decimal PayNCarryForwardRuleInDays { set; get; }
        public decimal PaybleForDays { set; get; }
        public decimal CarryForwardForDays { set; get; }
    }

    public class PercentagePayNCarryForward
    {
        public decimal PayNCarryForwardRuleInPercent { set; get; }
        public decimal PayPercent { set; get; }
        public decimal CarryForwardPercent { set; get; }
        public bool IsMaximumPayableRequired { set; get; }
        public decimal MaximumPayableDays { set; get; }
        public bool IsMaximumCarryForwardRequired { set; get; }
        public decimal MaximumCarryForwardDays { set; get; }
    }
}
