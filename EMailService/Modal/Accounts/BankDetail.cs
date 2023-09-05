using System;

namespace ModalLayer.Modal.Accounts
{
    public class BankDetail : CreationInfo
    {
        public long BankAccountId { set; get; }
        public int OrganizationId { set; get; }
        public int CompanyId { get; set; }
        public string BankName { set; get; }
        public string BranchCode { set; get; }
        public string Branch { set; get; }
        public string IFSC { set; get; }
        public string AccountNo { set; get; }
        public DateTime? OpeningDate { set; get; }
        public DateTime? ClosingDate { set; get; }
        public string PANNo { get; set; }
        public string GSTNo { get; set; }
        public string TradeLicenseNo { get; set; }
        public bool IsPrimaryAccount { set; get; }
        public int Total { set; get; }

    }
}
