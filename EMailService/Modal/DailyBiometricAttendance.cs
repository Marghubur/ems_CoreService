using System;
using System.Collections.Generic;

namespace EMailService.Modal
{
    public class DailyBiometricAttendance
    {
        public long EmployeeId { get; set; }
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public List<PunchTime> PunchTimes { get; set; } = new List<PunchTime>();
        public string Punch_In { get; set; }
        public string Punch_Out { get; set; }
        public int TotalWorkingMinutes { get; set; }
    }

    public class PunchTime
    {
        public string Punch_In { get; set; }
        public string Punch_Out { get; set; }
    }
}
