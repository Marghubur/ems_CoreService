using System.Collections.Generic;

namespace EMailService.Modal
{
    public class OvertimeConfiguration
    {
        public int OvertimeConfigId { get; set; }               // Unique identifier for the configuration
        public string ConfigName { get; set; }         // Name of the configuration (e.g., "Weekday Rate", "Weekend Rate")
        public bool ConvertInCash { get; set; }
        public bool ConvertInLeave { get; set; }
        public decimal? RateMultiplier { get; set; }   // Multiplier for overtime conversion (e.g., 1.5x, 2x)
        public decimal MinOvertimeHrs { get; set; } // Minimum hours required to qualify for overtime conversion
        public decimal MaxOvertimeHrs { get; set; } // Maximum overtime hours allowed for cash conversion in a period
        public bool IsWeekend { get; set; }           // Indicates if the rule applies to weekends
        public bool IsHoliday { get; set; }           // Indicates if the rule applies to holidays
        public int ExpiryMonths { get; set; }       // Leave expiry in months
        public int WorkflowId { get; set; }
        public int OvertimeTypeId { get; set; }
        public string OTCalculatedOn { get; set; }
        public bool IsRoundOffOtHrs { get; set; }
        public int IntervalForRoundOff { get; set; }
        public bool RoundOffOtHrsType { get; set; } // true -> Round up to, false -> Round down to
        public List<CompOffCriteria> CompOffCriteria { get; set; }
        public string CompOffCriterias { get; set; }
    }

    public class CompOffCriteria
    {
        public int? Index { get; set; }
        public int? StartHour { get; set; }
        public int? EndHour { get; set; }
        public decimal? TimeOfDay { get; set; }
    }
}
