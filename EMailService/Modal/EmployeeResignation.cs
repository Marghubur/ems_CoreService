using System;

namespace EMailService.Modal
{
    public class EmployeeResignation
    {
        public string FullName { get; set; }
        public long EmployeeNoticePeriodId { get; set; }
        public long EmployeeId { get; set; }
        public string ResignType { get; set; }
        public int ResignationStatus { get; set; }
        public DateTime OfficialLastWorkingDay { get; set; }
        public string EmployeeComment { get; set; }
        public DateTime CreatedOn { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string Designation { get; set; }
        public string DepartmentName { get; set; }
        public int RowIndex { get; set; }
        public int Total { get; set; }
        public string ClearanceDetails { get; set; }
        public string CompanyMobileNo { get; set; }
        public string CompanyEmail { get; set; }
    }
}
