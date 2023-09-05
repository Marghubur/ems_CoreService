using System;
using System.Collections.Generic;
using System.Text;

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
