using System;
using System.Collections.Generic;
using System.Text;

namespace ModalLayer.Modal
{
    public class EmployeeNoticePeriod
    {
        public long EmployeeNoticePeriodId {set; get;}
        public long EmployeeId {set; get;}
        public DateTime ApprovedOn {set; get;}
        public DateTime ApplicableFrom {set; get;}
        public int ApproverManagerId {set; get;}
        public string ManagerDescription {set; get;}
        public string AttachmentPath {set; get;}
        public string EmailTitle {set; get;}
        public string OtherApproverManagerIds {set; get;}
        public int ITClearanceStatus {set; get;}
        public int ReportingManagerClearanceStatus {set; get;}
        public int CanteenClearanceStatus {set; get;}
        public int ClientClearanceStatus {set; get;}
        public int HRClearanceStatus {set; get;}
        public DateTime OfficialLastWorkingDay {set; get;}
        public int PeriodDuration {set; get;}
        public int EarlyLeaveStatus {set; get;}
        public string Reason { set; get; }
    }
}
