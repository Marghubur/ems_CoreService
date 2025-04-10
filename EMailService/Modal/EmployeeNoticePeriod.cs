using System;

namespace ModalLayer.Modal
{
    public class EmployeeNoticePeriod
    {
        public long EmployeeNoticePeriodId { set; get; }
        public long EmployeeId { set; get; }
        public string ResignType { get; set; }
        public DateTime ApprovedOn { set; get; }
        public string AttachmentPath { set; get; }
        public DateTime OfficialLastWorkingDay { set; get; }
        public string EmployeeComment { get; set; }
        public int CompanyNoticePeriodInDays { get; set; }
        public int ResignationStatus { get; set; }
    }
}
