using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;

namespace ModalLayer.Modal.Accounts
{
    public class SalaryStructure : SalaryCommon
    {
        public string ComponentName { get; set; }
        public int ComponentTypeId { get; set; }
        public bool IndividualOverride { get; set; }
        public bool IsComponentEnable { get; set; }
        public bool IsAllowtoOverride { get; set; }
        
    }
}
