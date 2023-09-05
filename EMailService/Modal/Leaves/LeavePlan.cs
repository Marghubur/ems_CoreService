using System;

namespace ModalLayer.Modal.Leaves
{
    public class LeavePlan
    {
        public int LeavePlanId { set; get; }
        public int CompanyId { set; get; }
        public string PlanName { set; get; }
        public string PlanDescription { set; get; }
        public DateTime PlanStartCalendarDate { set; get; }
        public string AssociatedPlanTypes { set; get; }
        public bool IsShowLeavePolicy { set; get; }
        public bool IsUploadedCustomLeavePolicy { set; get; }
        public bool IsDefaultPlan { set; get; }
        public bool CanApplyEntireLeave { set; get; }
    }
}
