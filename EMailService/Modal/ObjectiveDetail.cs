using System;
using System.Collections.Generic;

namespace ModalLayer.Modal
{
    public class ObjectiveDetail: CreationInfo
    {
        public long ObjectiveId { get; set; }
        public string Objective {get; set;}
        public bool ObjSeeType {get; set;}  // true = manager can see, false = everyone see
        public bool IsIncludeReview {get; set;}
        public string Tag {get; set;}
        public int ProgressMeassureType {get; set;}
        public decimal StartValue {get; set;}
        public decimal TargetValue {get; set;}
        public string Description { get; set; }
        public DateTime TimeFrameStart {get; set;}
        public DateTime TimeFrmaeEnd { get; set; }
        public string ObjectiveType { get; set; }
        public int Total { get; set; }
        public int CompanyId { get; set; }
        public decimal CurrentValue { get; set; }
        public int FinancialYear { get; set; }
        public int DeclarationStartMonth { get; set; }
        public int DeclarationEndMonth { get; set; }
        public List<int> TagRole { get; set; }
        public int Status { get; set; }
        public long EmployeePerformanceId { get; set; }
        public List<PerformanceDetail> PerformanceDetail{ get; set; }
    }
}
