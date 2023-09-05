using System;

namespace ModalLayer.Modal.Accounts
{
    public class Company : BankDetail
    {
        public new int CompanyId { set; get; }
        public new int OrganizationId { set; get; }
        public string CompanyName { set; get; }
        public string CompanyDetail { get; set; }
        public int SectorType { set; get; }
        public string Country { set; get; }
        public string State { set; get; }
        public string City { set; get; }
        public string FirstAddress { get; set; }
        public string SecondAddress { get; set; }
        public string ThirdAddress { get; set; }
        public string ForthAddress { get; set; }
        public string FullAddress { set; get; }
        public string MobileNo { get; set; }
        public string Email { get; set; }
        public string FirstEmail { set; get; }
        public string SecondEmail { set; get; }
        public string ThirdEmail { set; get; }
        public string ForthEmail { set; get; }
        public string PrimaryPhoneNo { get; set; }
        public string SecondaryPhoneNo { get; set; }
        public string Fax { get; set; }
        public int Pincode { get; set; }
        public long FileId { set; get; }
        public string LegalDocumentPath { set; get; }
        public string LegalEntity { set; get; }
        public string TypeOfBusiness { set; get; }
        public DateTime InCorporationDate { set; get; }
        public bool IsPrimaryCompany { set; get; } = false;
        public string FixedComponentsId { set; get; } = "[]";
    }
}
