namespace ModalLayer.Modal.Accounts
{
    public class Payroll : CreationInfo
    {
        public int CompanyId { set; get; }
        public int PayrollCycleSettingId { set; get; }
        public long OrganizationId { set; get; }
        public string PayFrequency { get; set; }
        public int PayCycleMonth { get; set; }
        public int PayCycleDayOfMonth { get; set; }
        public int PayCalculationId { get; set; }
        public bool IsExcludeWeeklyOffs { get; set; }
        public bool IsExcludeHolidays { get; set; }
        public string TimezoneName { set; get; }
        public string State { set; get; }
        public int FinancialYear { set; get; }
        public int DeclarationStartMonth { get; set; }
        public int DeclarationEndMonth { get; set; }
    }
}
