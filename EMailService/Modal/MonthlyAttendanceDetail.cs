using System;
using System.Collections.Generic;

namespace EMailService.Modal
{
    public class MonthlyAttendanceDetail
    {
        public long EmployeeId { get; set; }
        public string Name { get; set; }
        public int Month { get; set; }
        public Dictionary<int, string> DailyData { get; set; } = new Dictionary<int, string>();
    }
}
