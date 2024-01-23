using System.Collections.Generic;

namespace ModalLayer.Modal.Leaves
{
    public class LeaveEndYearModal
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
        public string FixedPayNCarryForward { set; get; }
        public string PercentagePayNCarryForward { set; get; }
        public List<FixedPayNCarryForward> AllFixedPayNCarryForward { set; get; }
        public List<PercentagePayNCarryForward> AllPercentagePayNCarryForward { set; get; }
    }
}
