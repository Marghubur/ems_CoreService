using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using ModalLayer.Modal.Accounts;
using System.Collections.Generic;

namespace ModalLayer.Modal
{
    public class PayslipGenerationModal
    {
        public long EmployeeId { get; set; }
        public string PayslipTemplatePath { set; get; }
        public string PdfTemplatePath { set; get; }
        public string HeaderLogoPath { set; get; }
        public string CompanyLogoPath { set; get; }
        public Organization Company { set; get; }
        public FileDetail FileDetail { set; get; }
        public Employee Employee { set; get; }
        public AnnualSalaryBreakup SalaryDetail { get; set; }
        public List<DailyAttendance> dailyAttendances { get; set; }
        public TaxDetails TaxDetail { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal Gross { get; set; }
        public List<PTaxSlab> PTaxSlabs { get; set; }
        public List<EmployeeRole> EmployeeRoles { get; set; }
        public List<AnnualSalaryBreakup> AnnualSalaryBreakup { get; set; }
        public List<LeaveRequestNotification> leaveRequestNotifications { get; set; }
    }
}
