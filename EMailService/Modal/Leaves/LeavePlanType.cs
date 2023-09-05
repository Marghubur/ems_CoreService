namespace ModalLayer.Modal.Leaves
{
    public class LeavePlanType : CreationInfo
    {
        public int LeavePlanTypeId { set; get; }
        public int LeavePlanId { set; get; }
        public string LeavePlanCode { set; get; }
        public string PlanName { set; get; }
        public string PlanDescription { set; get; }
        public decimal MaxLeaveLimit { set; get; }
        public decimal AvailableLeave { set; get; }
        public decimal ConsumedLeave { set; get; }
        public bool ShowDescription { set; get; }
        public bool IsPaidLeave { set; get; }
        public bool IsSickLeave { set; get; }
        public bool IsStatutoryLeave { set; get; }
        public bool IsRestrictOnGender { set; get; }
        public bool? IsMale { set; get; }
        public bool IsRestrictOnMaritalStatus { set; get; }
        public bool? IsMarried { set; get; }
        public string Reasons { set; get; }
        public string PlanConfigurationDetail { set; get; } // After json deserialize class type would be [LeavePlanConfiguration]
        public bool IsApplicable { set; get; }
        public string Reason { set; get; }
    }
}
