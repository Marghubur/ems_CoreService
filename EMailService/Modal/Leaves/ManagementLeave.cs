namespace ModalLayer.Modal.Leaves
{
    public class ManagementLeave
    {
        public int LeaveManagementId { set; get; }
        public int LeavePlanTypeId { set; get; }
        public bool CanManagerAwardCausalLeave { set; get; }
        public int LeavePlanId { get; set; }
    }
}
