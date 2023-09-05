using System.Collections.Generic;

namespace ModalLayer.Modal.Leaves
{
    public class LeaveAccrual
    {
        public List<AllocateTimeBreakup> ExitMonthLeaveDistribution { set; get; }
        public List<AllocateTimeBreakup> JoiningMonthLeaveDistribution { set; get; }
        public int LeaveAccrualId { get; set; }
        public int LeavePlanTypeId { get; set; }
        public bool CanApplyEntireLeave { get; set; } = true;
        public bool IsLeaveAccruedPatternAvail { get; set; }
        public string LeaveDistributionSequence { get; set; }
        public decimal LeaveDistributionAppliedFrom { get; set; }
        public bool IsLeavesProratedForJoinigMonth { get; set; } = true;
        public bool IsLeavesProratedOnNotice { get; set; } = true;
        public bool IsNotAllowProratedOnNotice { get; set; }
        public bool IsNoLeaveOnNoticePeriod { get; set; }
        public bool IsVaryOnProbationOrExprience { get; set; }
        public bool IsAccrualStartsAfterJoining { get; set; }
        public bool IsAccrualStartsAfterProbationEnds { get; set; }
        public decimal AccrualDaysAfterJoining { get; set; }
        public decimal AccrualDaysAfterProbationEnds { get; set; }
        public bool IsImpactedOnWorkDaysEveryMonth { get; set; }
        public decimal WeekOffAsAbsentIfAttendaceLessThen { get; set; }
        public decimal HolidayAsAbsentIfAttendaceLessThen { get; set; }
        public bool CanApplyForFutureDate { get; set; }
        public bool IsExtraLeaveBeyondAccruedBalance { get; set; }
        public bool IsNoExtraLeaveBeyondAccruedBalance { get; set; }
        public decimal NoOfDaysForExtraLeave { get; set; }
        public decimal AllowOnlyIfAccrueBalanceIsAlleast { get; set; }
        public bool IsAccrueIfHavingLeaveBalance { get; set; }
        public bool IsAccrueIfOnOtherLeave { get; set; }
        public decimal NotAllowIfAlreadyOnLeaveMoreThan { get; set; }
        public bool RoundOffLeaveBalance { get; set; } = true;
        public bool ToNearestHalfDay { get; set; }
        public bool ToNearestFullDay { get; set; }
        public bool ToNextAvailableHalfDay { get; set; }
        public bool ToNextAvailableFullDay { get; set; }
        public bool ToPreviousHalfDay { get; set; }
        public bool DoesLeaveExpireAfterSomeTime { get; set; }
        public decimal AfterHowManyDays { get; set; }
        public int LeavePlanId { get; set; }
    }
}
