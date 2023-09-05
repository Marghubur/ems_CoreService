using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ModalLayer.Modal.Accounts
{
    [Table(name: "salary_components")]
    public class SalaryComponents : SalaryCommon
    {
        [Key]
        public string ComponentId { set; get; }
        public int ComponentTypeId { get; set; }
        public decimal PercentageValue { set; get; }
        public decimal EmployeeContribution { set; get; }
        public decimal EmployerContribution { set; get; }
        public bool IncludeInPayslip { set; get; }
        public bool IsOpted { set; get; }
        public bool IsActive { set; get; }
    }

    public class SalaryCommon : CreationInfo
    {
        public bool CalculateInPercentage { set; get; }
        public string ComponentDescription { set; get; }
        public string ComponentFullName { set; get; }
        public bool IsComponentEnabled { set; get; }
        public decimal MaxLimit { set; get; }
        public decimal DeclaredValue { set; get; }
        public decimal AcceptedAmount { set; get; }
        public decimal RejectedAmount { set; get; }
        public string UploadedFileIds { set; get; }
        public string Formula { set; get; }
        public string Section { get; set; }
        public decimal SectionMaxLimit { get; set; }
        public bool IsAffectInGross { get; set; }
        public bool RequireDocs { get; set; }
        public bool TaxExempt { get; set; }
        public int AdHocId { get; set; }
        public bool IsAdHoc { get; set; }
        public int ComponentCatagoryId { get; set; }
    }
}
