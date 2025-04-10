using System;

namespace EMailService.Modal
{
    public class EmployeeExitClearance
    {
        public long EmployeeExitClearanceId { get; set; }
        public long EmployeeId { get; set; }
        public string ClearanceByName { get; set; }
        public bool IsDepartment { get; set; }
        public long HandledBy { get; set; }
        public int ApprovalStatusId { get; set; }
        public string Comments { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
}
