using ModalLayer.Modal.Accounts;
using System;
using System.Collections.Generic;

namespace ModalLayer.Modal.Leaves
{
    public class LeaveCalculationModal
    {
        public DateTime fromDate { set; get; }
        public DateTime toDate { set; get; }
        public DateTime timeZoneFromDate { set; get; }
        public DateTime timeZoneToDate { set; get; }
        public DateTime utcPresentDate { set; get; }
        public DateTime timeZonePresentDate { set; get; }
        public DateTime probationEndDate { set; get; }
        public DateTime noticePeriodEndDate { set; get; }
        public Employee employee { set; get; }
        public ShiftDetail shiftDetail { set; get; }
        public LeavePlan leavePlan { set; get; }
        public List<LeavePlan> leavePlans { set; get; }
        public LeavePlanConfiguration leavePlanConfiguration { set; get; }
        public LeaveRequestDetail leaveRequestDetail { set; get; }
        public CompanySetting companySetting { set; get; }
        public int LeaveTypeId { set; get; }
        public List<LeavePlanType> leavePlanTypes { set; get; }
        public List<LeaveTypeBrief> leaveTypeBriefs { set; get; }
        public int employeeType { set; get; }
        public bool isApplyingForHalfDay { set; get; }
        public long AssigneId { get; set; }
        public string AssigneeEmail { get; set; }
        public bool IsEmailNotificationPasued { get; set; }
        public bool IsLeaveAutoApproval { get; set; }
        public bool DocumentProffAttached { get; set; }
        public decimal numberOfLeaveApplyring { set; get; }
        public List<LeaveRequestNotification> lastAppliedLeave { set; get; }
        public bool runTillMonthOfPresnetYear { set; get; }
        public decimal ProjectedFutureLeave { get; set; }
        public List<ProjectMemberDetail> projectMemberDetail { set; get; }
        public bool IsAllLeaveAvailable { set; get; }
        public int ProjectOffset { set; get; } = 0;
        public int GetNextOffset()
        {
            return ProjectOffset += 100;
        }
    }
}
