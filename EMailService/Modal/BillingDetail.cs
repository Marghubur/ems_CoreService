using System;
using System.Collections.Generic;
using System.Data;

namespace ModalLayer.Modal
{
    public class BillingDetail
    {
        public DataTable FileDetail { get; set; }
        public DataTable Employees { get; set; }
        public List<DailyTimesheetDetail> TimesheetDetails { get; set; }
        public DataTable Organizations { get; set; }
        public TimesheetDetail TimesheetDetail { get; set; }
    }
}
