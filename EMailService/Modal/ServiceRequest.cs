using System;
using System.Collections.Generic;

namespace ModalLayer.Modal
{
    public class ServiceRequest: CreationInfo
    {
        public long ServiceRequestId { set; get; }
        public int CompanyId { set; get; }
        public string RequestTypeId { set; get; }
        public string RequestTitle { set; get; }
        public string RequestDescription { set; get; }
        public int Quantity { set; get; }
        public decimal Duration { set; get; }
        public DateTime FromDate { set; get; }
        public DateTime ToDate { set; get; }
        public long RequestedTo_1 { set; get; }
        public long RequestedTo_2 { set; get; }
        public long RequestedTo_3 { set; get; }
        public string Reference { set; get; }
        public int RequestStatus { set; get; }
        public long RequestedBy { set; get; }
        public DateTime RequestedOn { set; get; }
        public string AssignTo { get; set; }
        public int Total { get; set; }
    }
}
