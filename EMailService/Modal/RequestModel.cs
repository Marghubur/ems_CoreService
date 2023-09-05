using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ModalLayer.Modal
{
    public class RequestModel
    {
        public DataTable ApprovalRequest { get; set; }
        public DataTable AttendaceTable { get; set; }
        public DataTable TimesheetTable { set; get; }
    }
}
