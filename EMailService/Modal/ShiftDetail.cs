using System;

namespace ModalLayer.Modal
{
    public class ShiftDetail : WeekDays
    {
        public int WorkShiftId { get; set; }
        public int CompanyId { get; set; }
        public int Department { get; set; }
        public string WorkFlowCode { get; set; }
        public string ShiftTitle { get; set; }
        public string Description { get; set; }
        public int TotalWorkingDays { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string OfficeTime { get; set; }
        public int Duration { get; set; }
        public int LunchDuration { get; set; }
        public int Status { get; set; }
        public DateTime LastUpdatedOn { get; set; }
        public int Total { get; set; }
        public int Index { get; set; }
        public long CreatedBy { set; get; }
        public long? UpdatedBy { set; get; }
        public DateTime CreatedOn { set; get; }
        public DateTime? UpdatedOn { set; get; }
        public long AdminId { get; set; }
    }

    public class WeekDays
    {
        public Boolean IsMon { get; set; }
        public Boolean IsTue { get; set; }
        public Boolean IsThu { get; set; }
        public Boolean IsWed { get; set; }
        public Boolean IsFri { get; set; }
        public Boolean IsSat { get; set; }
        public Boolean IsSun { get; set; }
    }
}
