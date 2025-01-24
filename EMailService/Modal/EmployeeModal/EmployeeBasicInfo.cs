using System;

namespace EMailService.Modal.EmployeeModal
{
    public class EmployeeBasicInfo
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int CompanyId { get; set; } = 0;
        public string Location { get; set; }
        public int DepartmentId { get; set; }
        public long EmployeeUid { get; set; }
        public string Mobile { get; set; }
        public string SecondaryMobile { get; set; }
        public string Email { get; set; }
        public int Gender { get; set; }
        public int DesignationId { get; set; }
        public int AccessLevelId { get; set; }
        public int UserTypeId { get; set; } = 2;
        public int WorkShiftId { get; set; }
        public string OldFileName { get; set; }
        public bool IsPayrollOnCTC { get; set; } = false;
        public int LeavePlanId { get; set; }
        public int SalaryGroupId { get; set; } = 0;
        public DateTime DOB { get; set; }
        public decimal CTC { get; set; }
        public long ReportingManagerId { get; set; }
        public int OrganizationId { get; set; } = 0;
        public int FileId { get; set; } = 0;
        public int EmployeeId { get; set; } = 0;
        public DateTime DateOfJoining { get; set; }
        public string Password { get; set; }
        public int PayrollGroupId { get; set; } = 0;
        public string ProfileStatusCode { get; set; }
    }
}
