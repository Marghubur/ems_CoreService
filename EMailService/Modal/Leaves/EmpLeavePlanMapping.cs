namespace ModalLayer.Modal.Leaves
{
    public class EmpLeavePlanMapping
    {
        public long EmployeeLeaveplanMappingId { get; set; }
        public long EmployeeId { get; set; }
        public int LeavePlanId { get; set; }
        public bool IsAdded { get; set; }
    }
}
