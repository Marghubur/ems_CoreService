namespace EMailService.Modal
{
    public class AttendanceSetting
    {
        public int AttendanceSettingId { get; set; }
        public int CompanyId { get; set; }
        public bool IsWeeklyAttendanceEnabled { get; set; }
        public int BackDateLimitToApply { get; set; }
        public int BackWeekLimitToApply { get; set; }
        public bool IsAutoApprovalEnable { get; set; }
        public int AutoApproveAfterDays { get; set; }
        public int MinWorkDaysRequired { get; set; }
    }
}
