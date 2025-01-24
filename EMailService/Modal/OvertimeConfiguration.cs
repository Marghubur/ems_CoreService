namespace EMailService.Modal
{
    public class OvertimeConfiguration
    {
        public int OvertimeConfigId { get; set; }               // Unique identifier for the configuration
        public string ConfigName { get; set; }         // Name of the configuration (e.g., "Weekday Rate", "Weekend Rate")
        public bool ConvertInCash { get; set; }
        public bool ConvertInLeave { get; set; }
        public decimal? RateMultiplier { get; set; }   // Multiplier for overtime conversion (e.g., 1.5x, 2x)
        public decimal MinOvertimeMin { get; set; } // Minimum hours required to qualify for overtime conversion
        public decimal MaxOvertimeMin { get; set; } // Maximum overtime hours allowed for cash conversion in a period
        public int? ExcludedShifts { get; set; }     // Shifts excluded from the rule (e.g., "Night", "Weekend")
        public bool IsWeekend { get; set; }           // Indicates if the rule applies to weekends
        public bool IsHoliday { get; set; }           // Indicates if the rule applies to holidays
        public bool IsNightShift { get; set; }        // Indicates if the rule applies to night shifts
        public decimal? TaxRate { get; set; }         // Tax or deduction rate applied to cash payouts
        public decimal? LeavePerHour { get; set; }    // Leave days earned per hour
        public decimal? PartialHours { get; set; }   // Hours required for partial leave
        public decimal? PartialLeave { get; set; }   // Leave days for partial hours
        public decimal? MaxLeave { get; set; }       // Maximum leave per month
        public decimal? FullDayHours { get; set; }   // Hours in a single day for full leave
        public int? BonusShift { get; set; }        // Consecutive shifts for bonus leave
        public int? BonusLeave { get; set; }         // Bonus leave days for consecutive shifts
        public int? ExpiryMonths { get; set; }       // Leave expiry in months
        public int WorkflowId { get; set; }
        public int OvertimeTypeId { get; set; }
    }
}
