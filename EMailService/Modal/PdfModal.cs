using System;

namespace ModalLayer.Modal
{
    public class PdfModal : FileDetail
    {
        public string header { get; set; }
        public DateTime billingMonth { get; set; }
        public int billYear { get; set; }
        public string billNo { get; set; }
        public long billId { get; set; }
        public DateTime dateOfBilling { get; set; }
        public decimal cGST { get; set; }
        public decimal sGST { get; set; }
        public decimal iGST { get; set; }
        public decimal cGstAmount { get; set; }
        public decimal sGstAmount { get; set; }
        public decimal iGstAmount { get; set; }
        public int workingDay { get; set; }
        public decimal packageAmount { get; set; }
        public decimal grandTotalAmount { get; set; }
        public string senderCompanyName { get; set; }
        public string receiverFirstAddress { get; set; }
        public long receiverCompanyId { get; set; }
        public string receiverCompanyName { get; set; }
        public long senderId { get; set; }
        public string developerName { set; get; }
        public string receiverSecondAddress { get; set; }
        public string receiverThirdAddress { set; get; }
        public string senderFirstAddress { get; set; }
        public decimal daysAbsent { set; get; }
        public string senderSecondAddress { get; set; }
        public string senderPrimaryContactNo { get; set; }
        public string senderEmail { get; set; }
        public string senderGSTNo { get; set; }
        public string receiverGSTNo { get; set; }
        public string receiverPrimaryContactNo { get; set; }
        public string receiverEmail { get; set; }
        public int UpdateSeqNo { set; get; }
        public bool IsCustomBill { set; get; } = false;
    }
}
