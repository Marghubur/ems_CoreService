namespace EMailService.Modal
{
    public class EmployeeExitConfiguration
    {
        public int EmployeeExitConfigurationId { get; set; }
        public int DepartmentId { get; set; }
        public bool ClearanceByDepartment { get; set; }
        public string RoleName { get; set; }
        public bool ClearanceByRole { get; set; }
        public string ConfigDesc { get; set; }
    }
}
