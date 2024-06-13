using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;

namespace EMailService.Modal
{
    public class CalculateSalaryDetailModal
    {
        public long EmployeeId { get; set; }
        public EmployeeDeclaration employeeDeclaration { get; set; }
        public bool ReCalculateFlag { get; set; }
        public bool IsCTCChanged { get; set; }
    }
}
