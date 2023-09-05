using System;

namespace ModalLayer.Modal
{
    public class EmployeePerformance : CreationInfo
    {
        public long EmployeePerformanceId { get; set; }
        public long EmployeeId { get; set; }
        public int CompanyId { get; set; }
        public decimal CurrentValue { get; set; }
        public int Status { get; set; }
        public string Comments { get; set; }
        public string PerformanceDetail { get; set; }
        public long Admin { get; set; }
        public long ObjectiveId { get; set; }
    }
    public class PerformanceDetail
    {
        public int Index { get; set; }
        public decimal CurrentValue { get; set; }
        public int Status { get; set; }
        public string Comments { get; set; }
        public DateTime UpdatedOn { set; get; }
    }
}
