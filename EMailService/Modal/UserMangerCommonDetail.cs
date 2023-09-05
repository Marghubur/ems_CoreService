using ModalLayer;

namespace CoreBottomHalf.Modal
{
    public class UserMangerCommonDetail : CreationInfo
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
    }
}
