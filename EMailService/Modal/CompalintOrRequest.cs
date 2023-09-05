using System;
using System.Collections.Generic;

namespace ModalLayer.Modal
{
    public class ComplaintOrRequest
    {
        public int ComplaintOrRequestId { set; get; }
        public int RequestTypeId { set; get; }
        public int TargetId { set; get; }
        public int TargetOffset { set; get; }
        public long EmployeeId { set; get; }
        public string EmployeeName { set; get; }
        public string Email { set; get; }
        public string Mobile { set; get; }
        public long ManagerId { set; get; }
        public string ManagerName { set; get; }
        public string ManagerEmail { set; get; }
        public string ManagerMobile { set; get; }
        public string EmployeeMessage { set; get; }
        public string ManagerComments { set; get; }
        public int CurrentStatus { set; get; }
        public List<Notify> NotifyList { set; get; } = new List<Notify>();
        public string Notify { set; get; }
        public bool ExecutedByManager { set; get; }
        public long ExecuterId { set; get; }
        public string ExecuterName { set; get; }
        public string ExecuterEmail { set; get; }
        public bool IsFullDay { set; get; } = false;
        public DateTime RequestedOn { set; get; }
        public DateTime AttendanceDate { set; get; }
        public DateTime? LeaveFromDate { set; get; }
        public DateTime? LeaveToDate { set; get; }
        public DateTime UpdatedOn { set; get; }
        public int Total { get; set; }
    }

    public class Notify
    {
        public int Id { get; set; }
        public string Email { get; set; }
    }

    public class ComplaintOrRequestWithEmail
    {
        public string EmailBody { set; get; }
        public int AttendanceId { set; get; }
        public List<ComplaintOrRequest> CompalintOrRequestList { set; get; }
    }
}
