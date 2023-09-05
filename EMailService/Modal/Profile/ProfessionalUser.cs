using System;
using System.Collections.Generic;
using System.Text;

namespace ModalLayer.Modal.Profile
{
    public class ProfileDetail
    {
        public ProfessionalUser professionalUser { set; get; } = new ProfessionalUser();
        public List<FileDetail> profileDetail { set; get; }
        public Employee employee { set; get; }
    }

    public class ProfessionalUser
    {
        public long EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public long FileId { get; set; }
        public string ProfessionalDetailJson { get; set; }
        public OtherDetail OtherDetails { get; set; }
        public List<SkillDetail> Skills { get; set; } = new List<SkillDetail>();
        public List<Company> Companies { get; set; } = new List<Company>();
        public List<EducationalDetail> EducationalDetails { set; get; } = new List<EducationalDetail>();
        public List<ProjectDetail> Projects { get; set; } = new List<ProjectDetail>();
        public AccomplishmentsDetail Accomplishments { get; set; } = new AccomplishmentsDetail();
        public PersonalDetail PersonalDetail { get; set; } = new PersonalDetail();
        public List<EmploymentDetail> Employments { set; get; } = new List<EmploymentDetail>();
    }
}
