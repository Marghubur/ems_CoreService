namespace EMailService.Modal
{
    public class TDSSetting
    {
        public int TDSSettingId { get; set; }
        public int FinancialYear { get; set; }
        public bool EnableTDSCalculation { get; set; }
        public bool AutoDeductTDSPending { get; set; }
        public int? DeductFromMonth { get; set; }
    }
}
