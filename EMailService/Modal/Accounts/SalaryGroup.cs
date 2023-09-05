using ModalLayer.MarkerInterface;
using System.Collections.Generic;

namespace ModalLayer.Modal.Accounts
{
    [Table(name: "salary_group")]
    public class SalaryGroup : CreationInfo
    {
        [Primary("SalaryGroupId")]
        public int SalaryGroupId { get; set; }
        public string SalaryComponents { get; set; }
        public List<SalaryComponents> GroupComponents { get; set; }
        public string GroupName { get; set; }
        public string GroupDescription { get; set; }
        public decimal MinAmount { set; get; }
        public decimal MaxAmount { set; get; }
        public decimal? CTC { get; set; }
        public int CompanyId { get; set; }
    }
}
