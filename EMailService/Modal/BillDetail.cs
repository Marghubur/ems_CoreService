using System;
using System.Collections.Generic;
using System.Text;

namespace ModalLayer.Modal
{
    public class BillDetail
    {
        public string DeveloperName { set; get; }
        public long BillDetailUid { get; set; }
        public decimal PaidAmount { get; set; }
        public int BillForMonth { get; set; }
        public int BillYear { get; set; }
        public int NoOfDays { get; set; }
        public decimal NoOfDaysAbsent { get; set; }
        public int IGST { get; set; }
        public int SGST { get; set; }
        public int CGST { get; set; }
        public int TDS { get; set; }
        public long BillStatusId { get; set; }
        public DateTime PaidOn { get; set; }
        public long FileDetailId { get; set; }
        public long EmployeeUid { get; set; }
        public long ClientId { get; set; }
        public string BillNo { get; set; }
        public int UpdateSeqNo { get; set; }
        public long CreatedBy { get; set; }
        public long UpdatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public DateTime BillUpdatedOn { get; set; }
    }
}
