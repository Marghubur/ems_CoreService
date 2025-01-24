using System;

namespace EMailService.Modal
{
    public class EmployeeOvertime
    {
        public int OvertimeId { get; set; }
        public long EmployeeId { get; set; }
        public string Comments { get; set; }
        public DateTime AppliedOn{ get; set; }
        public int LoggedMinutes { get; set; }
        public int ShiftId { get; set; }
        public int StatusId { get; set; }
        public int OvertimeConfigId { get; set; }
        public string ExecutionRecord { get; set;}
        public string StartOvertime { get; set; }
        public string EndOvertime { get; set; }
        public DateTime OvertimeDate { get; set; }
        public ExecutionRecords ExecutionRecords { get; set; }
    }

    public class ExecutionRecords
    {
        public long EmployeeId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime ExecutedOn { get; set; }
        public bool IsRequired { get; set; }
    }
}
