namespace EMailService.Modal
{
    public class RecordHealthStatus
    {
        public long EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public bool DeclarationTableStatus { get; set; }
        public string DeclarationTableProblemDetail { get; set; }
        public bool SalaryDetailTableStatus { get; set; }
        public string SalaryDetailTableProblemDetail { get; set; }
        public bool LeaveTableStatus { get; set; }
        public string LeaveTableProblemDetail { get; set; }
        public bool AttendanceTableStatus { get; set; }
        public string AttendanceTableProblemDetail { get; set; }
        public bool EmployeeRecordCompletenessStatus { get; set; }
        public string EmployeeRecordProblemDetail { get; set; }
        public int FinancialYear { get; set; }
    }
}
