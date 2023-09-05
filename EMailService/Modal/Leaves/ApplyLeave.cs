using System;

namespace ModalLayer.Modal.Leaves
{
    public class ApplyLeave
    {
        public long EmployeeId { set; get; }
        public DateTime FromDate { set; get; }
        public DateTime ToDate { set; get; }
        public decimal NumOfDays { set; get; }
    }
}
