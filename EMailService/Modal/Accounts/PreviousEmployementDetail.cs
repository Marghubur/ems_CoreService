using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ModalLayer.Modal.Accounts
{
    [Table(name: "previous_employement_details")]
    public class PreviousEmployementDetail : CreationInfo
    {
        [Key]
        public long PreviousEmpDetailId { get; set; }
        public int EmployeeId { get; set; }
        public string Month { get; set; }
        public int MonthNumber { get; set; }
        public decimal Gross { get; set; }
        public decimal Basic { get; set; }
        public decimal HouseRent { get; set; }
        public decimal EmployeePR { get; set; }
        public decimal ESI { get; set; }
        public decimal LWF { get; set; }
        public decimal LWFEmp { get; set; }
        public decimal Professional { get; set; }
        public decimal IncomeTax { get; set; }
        public decimal OtherTax { get; set; }
        public decimal OtherTaxable { get; set; }
        public int Year { get; set; }
    }

}
