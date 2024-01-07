using ModalLayer.Modal;
using System;

namespace CoreBottomHalf.Modal
{
    public class UserMangerCommonDetail : AttendanceJson
    {
        public long EmployeeId { set; get; }
        public long ReportingManagerId { set; get; }
        public string EmployeeName { set; get; }
        public string FirstName { set; get; }
        public string LastName { set; get; }
        public string Email { set; get; }
        public string Mobile { set; get; }
        public string ManagerName { set; get; }
        public string ManagerMobile { set; get; }
        public string ManagerEmail { set; get; }
        public int Total { set; get; }
        public int Index { set; get; }
        public int PageIndex { get; set; }
        public long CreatedBy { get; set; }
        public long? UpdatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public long AdminId { get; set; }
    }
}
