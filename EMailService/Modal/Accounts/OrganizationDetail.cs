using System;

namespace ModalLayer.Modal.Accounts
{
    public class OrganizationDetail : Company
    {
        public string OrganizationName { set; get; }
        public string OrgMobileNo { get; set; }
        public string OrgEmail { get; set; }
        public string OrgPrimaryPhoneNo { get; set; }
        public string OrgSecondaryPhoneNo { get; set; }
        public string OrgFax { get; set; }
        public Files Files { get; set; }
    }
}
