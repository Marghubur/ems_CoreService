using System;
using System.Collections.Generic;

namespace ModalLayer.Modal
{
    public class TemplateReplaceModal
    {
        public string BodyContent { set; get; }
        public string Subject { set; get; }
        public List<string> ToAddress { set; get; }
        public string DeveloperName { set; get; }
        public int DayCount { set; get; }
        public DateTime FromDate { set; get; }
        public DateTime ToDate { set; get; }
        public DateTime CurrentDate { set; get; }
        public DateTime CurrentDateTime { set; get; }
        public string ManagerName { set; get; }
        public string Message { set; get; }
        public string CompanyName { set; get; }
        public string RequestType { set; get; }
        public string ActionType { set; get; }
        public string LeaveType { set; get; }
        public string Title { set; get; }

    }
}
