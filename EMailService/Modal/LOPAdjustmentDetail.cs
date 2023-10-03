using System;
using System.Collections.Generic;

namespace EMailService.Modal
{
    public class LOPAdjustmentDetail
    {
        public long EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Email { get; set; }
        public int ActualLOP { get; set; }
        public int FinalLOP { get; set; }
        public int LOPAdjusment { get; set; }
        public string Comment { get; set; }
        public List<DateTime> BlockedDates { get; set; }
        public int LeaveTypeId { get; set; }
        public string LeavePlanName { get; set; }
    }
}
