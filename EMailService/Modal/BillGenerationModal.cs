using ModalLayer.Modal.Accounts;
using System.Collections.Generic;
using System.Data;

namespace ModalLayer.Modal
{
    public class BillGenerationModal
    {
        public string BillTemplatePath { set; get; }
        public string PdfTemplatePath { set; get; }
        public string HeaderLogoPath { set; get; }
        public string CompanyLogoPath { set; get; }
        public string SignatureWithOutStampPath { set; get; }
        public string SignatureWithStampPath { set; get; }
        public string Comment { set; get; }
        public Organization Sender { set; get; }
        public Organization Receiver { set; get; }
        public FileDetail FileDetail { set; get; }
        public DataSet ResultSet { set; get; }
        public Bills BillSequence { set; get; }
        public PdfModal PdfModal { set; get; }
        public List<DailyTimesheetDetail> FullTimeSheet { set; get; }
        public TimesheetDetail TimesheetDetail { set; get; }
        public BankDetail SenderBankDetail { set; get; }
    }
}
