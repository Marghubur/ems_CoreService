namespace ModalLayer.Modal.Leaves
{
    public class LeaveHolidaysAndWeekoff
    {
        public int LeaveHolidaysAndWeekOffId { get; set; }
        public int LeavePlanTypeId { get; set; }
        public bool AdJoiningHolidayIsConsiderAsLeave { get; set; }
        public decimal ConsiderLeaveIfNumOfDays { get; set; }
        public bool IfLeaveLieBetweenTwoHolidays { get; set; }
        public bool IfHolidayIsRightBeforLeave { get; set; }
        public bool IfHolidayIsRightAfterLeave { get; set; }
        public bool IfHolidayIsRightBeforeAfterOrInBetween { get; set; }
        public bool AdjoiningWeekOffIsConsiderAsLeave { get; set; }
        public decimal ConsiderLeaveIfIncludeDays { get; set; }
        public bool IfLeaveLieBetweenWeekOff { get; set; }
        public bool IfWeekOffIsRightBeforLeave { get; set; }
        public bool IfWeekOffIsRightAfterLeave { get; set; }
        public bool IfWeekOffIsRightBeforeAfterOrInBetween { get; set; }
        public int LeavePlanId { get; set; }
    }
}
