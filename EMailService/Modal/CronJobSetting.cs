namespace EMailService.Modal
{
    public class CronJobSetting
    {
        public int CronJobSettingId { get; set; }
        public int OrganizationId { get; set; }
        public int CompanyId { get; set; }
        public string CronJobDetail { get; set; }
    }
}
