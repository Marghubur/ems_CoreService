using System;
using System.Collections.Generic;

namespace ModalLayer.Modal
{
    public class EmployeeLeavePayrollAndOtherDetail
    {
        public long EmployeeId { set; get; }
        public string LeaveTypeBriefJson { set; get; }
        public List<LeaveTypeBrief> LeaveTypeBrief { set; get; }
        public int AccrualRunDay { set; get; }
        public DateTime NextAccrualRunDate { set; get; }
    }

    public class LeaveTypeBrief
    {
        public int LeavePlanTypeId { set; get; }
        public string LeavePlanTypeName { set; get; }
        public decimal AvailableLeaves { set; get; }
        public decimal TotalLeaveQuota { set; get; }
        public bool IsCommentsRequired { set; get; } = false;
        public bool IsFutureDateAllowed { set; get; } = false;
        public bool IsHalfDay { get; set; } = false;
        public decimal AccruedSoFar { get; set; }
    }
}
