namespace ModalLayer.Modal.Accounts
{
    public class PfEsiSetting: CreationInfo
	{
		public int PfEsi_setting_Id { set; get; }
        public bool PFEnable { set; get; }
        public bool IsPfAmountLimitStatutory { set; get; }
        public bool IsPfCalculateInPercentage { set; get; }
        public bool IsAllowOverridingPf { set; get; }
        public bool IsPfEmployerContribution { set; get; }
        public bool IsHidePfEmployer { set; get; }
        public bool IsPayOtherCharges { set; get; }
        public bool IsAllowVPF { set; get; }
        public bool EsiEnable { set; get; }
        public bool IsAllowOverridingEsi { set; get; }
        public bool IsHideEsiEmployer { set; get; }
        public bool IsEsiExcludeEmployerShare { set; get; }
        public bool IsEsiExcludeEmployeeGratuity { set; get; }
        public bool IsEsiEmployerContributionOutside { set; get; }
        public bool IsRestrictEsi { set; get; }
        public bool IsIncludeBonusEsiEligibility { set; get; }
        public bool IsIncludeBonusEsiContribution { set; get; }
        public bool IsEmployerPFLimitContribution { set; get; }
        public decimal EmployerPFLimit { set; get; }
        public decimal MaximumGrossForESI { set; get; }
        public decimal EsiEmployeeContribution { set; get; }
        public decimal EsiEmployerContribution { set; get; }
        public int CompanyId { set; get; }
        public long Admin { get; set; }
    }
}