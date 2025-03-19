using System.Collections.Generic;

namespace EMailService.Modal
{
    public class ExcelDataWithDropdown
    {
        public List<dynamic> data { get; set; }
        public Dictionary<string, List<string>> dropdowndata { get; set; }
        public List<string>? mandatoryHeaderColumn { get; set; }
    }
}
