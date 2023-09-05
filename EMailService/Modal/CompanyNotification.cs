using System;
using System.Collections.Generic;

namespace ModalLayer.Modal
{
    public class CompanyNotification:CreationInfo
    {
        public long NotificationId {get; set;}
        public string Topic {get; set;}
        public int CompanyId {get; set;}
        public string BriefDetail {get; set;}
        public string Departments {get; set;}
        public string CompleteDetail {get; set;}
        public int Total {get; set;}
        public int Index { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsGeneralAnnouncement { get; set; }
        public int AnnouncementType { get; set; }
        public string FileIds { get; set; }
        public string AnnouncementId { get; set; }
        public bool IsExpired { get; set; }
        public List<Departments> DepartmentsList { get; set; }
    }
    public class Departments
    {
        public int Id { get; set; }
        public string Value { get; set; }
    }
}
