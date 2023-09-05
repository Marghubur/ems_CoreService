using ModalLayer.MarkerInterface;
using System;
using System.Collections.Generic;

namespace ModalLayer.Modal
{
    public class Project : ProjectMemberDetail
    {
        public string ProjectName { set; get; }
        public string ProjectDescription { set; get; }
        public DateTime? ProjectStartedOn { set; get; }
        public DateTime? ProjectEndedOn { set; get; }
        public bool IsClientProject { set; get; }
        public long ClientId { set; get; }
        public string HomePageUrl { set; get; }
        public string PageIndexDetail { set; get; }
        public string KeywordDetail { set; get; }
        public string DocumentationDetail { set; get; }
        public int CompanyId { set; get; }
        public long AdminId { get; set; }
        public int Total { get; set; }
        public string DocumentPath { get; set; }
        public List<ProjectMemberDetail> TeamMembers { get; set; }
    }

    [Table(name: "project_members_detail")]
    public class ProjectMemberDetail
    {
        [Primary("ProjectMemberDetailId")]
        public int ProjectMemberDetailId { set; get; }
        public int ProjectId { set; get; }
        public long EmployeeId { set; get; }
        public int DesignationId { set; get; }
        public string FullName { set; get; }
        public string Email { set; get; }
        public bool IsActive { set; get; }
        public string Grade { get; set; }
        public int MemberType { get; set; }
        public DateTime AssignedOn { set; get; }
        public DateTime? LastDateOnProject { set; get; }
    }

    public class WikiDetail
    {
        public long ProjectId { get; set; }
        public string Title { get; set; }
        public string ProjectName { get; set; }
        public string SectionName { get; set; }
        public string SectionDescription { get; set; }
    }
}
