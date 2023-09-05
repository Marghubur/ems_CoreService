using System;
using System.Collections.Generic;
using System.Text;

namespace ModalLayer.Modal.Profile
{
    public class PersonalDetail
    {
        public DateTime? DOB { get; set; }
        public string Gender { get; set; }
        public string Address { get; set; }
        public string HomeTown { get; set; }
        public int PinCode { get; set; }
        public string MaritalStatus { get; set; }
        public string Category { get; set; }
        public string DifferentlyAbled { get; set; }
        public string PermitUSA { get; set; }
        public string PermitOtherCountry { get; set; }
        public List<LanguageDetail> LanguageDetails { get; set; }
    }
    public class LanguageDetail
    {
        public string Language { get; set; }
        public bool LanguageRead { get; set; }
        public bool LanguageWrite { get; set; }
        public string ProficiencyLanguage { get; set; }
        public bool LanguageSpeak { get; set; }
    }
}
