namespace EMailService.Modal
{
    public class CronJobSettingJson
    {
        public int LeaveAccrualCronDay { get; set; }
        public int LeaveAccrualCronTime { get; set; }
        public int LeaveAccrualCronType { get; set; }
        public int TimesheetCronDay { get; set; }
        public int TimesheetCronTime { get; set; }
        public int TimesheetCronType { get; set; }
        public int LeaveYearEndCronDay { get; set; }
        public int LeaveYearEndCronTime { get; set; }
        public int LeaveYearEndCronType { get; set; }
    }
}
