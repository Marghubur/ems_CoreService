namespace EMailService.Modal
{
    public class AttendanceSetting
    {
        public int AttendanceSettingId { get; set; }
        public int CompanyId { get; set; }
        public int BackDateLimitToApply { get; set; }
        public bool IsAutoApproved { get; set; }
        public int LastDateOfAttendanceCheck { get; set; }
    }
}
