using CoreBottomHalf.Modal;
using System;

namespace ModalLayer.Modal
{
    public class LeaveRequestNotification : UserMangerCommonDetail
    {
        public long LeaveRequestNotificationId { set; get; }
        public long LeaveRequestId { set; get; }
        public string UserMessage { set; get; }
        public long AssigneeId { set; get; }
        public long ProjectId { set; get; }
        public string ProjectName { set; get; }
        public DateTime FromDate { set; get; }
        public DateTime ToDate { set; get; }
        public decimal NumOfDays { set; get; }
        public int RequestStatusId { set; get; }
        public int LeaveTypeId { set; get; }
        public string FeedBackMessage { set; get; }
        public DateTime? LastReactedOn { set; get; }
        public string LeaveDetail { set; get; }
        public string RecordIdStr { set; get; }
        public string RecordId { set; get; }
        public int NoOfApprovalsRequired { get; set; }
        public string ReporterDetail { get; set; }
        public string FileIds { get; set; }
        public string FeedBack { get; set; }
        public string LeaveTypeName { get; set; }
        public int AutoActionAfterDays { get; set; }
        public bool IsAutoApprovedEnabled { get; set; }
        public bool IsPaidLeave { get; set; }
    }
}
