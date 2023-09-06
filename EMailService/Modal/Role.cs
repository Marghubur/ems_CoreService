namespace ModalLayer.Modal
{
    public struct Role
    {
        public const string Admin = "Admin";
        public const string Employee = "Employee";
        public const string Manager = "Manager";
        public const string ManagerWithAdmin = Admin + "," + Manager;
        public const string EmployeeWithAdmin = Admin + "," + Employee;
        public const string AllRole = Admin + "," + Employee + "," + Manager;
    }
}
