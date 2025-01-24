using System;

namespace EMailService.Modal.EmployeeModal
{
    public class EmpPersonalDetail
    {
        public long EmployeeUid { get; set; }
        public string FatherName { get; set; }
        public string MotherName { get; set; }
        public string SpouseName { get; set; }
        public string EmergencyContactName { get; set; }
        public string RelationShip { get; set; }
        public string EmergencyMobileNo { get; set; }
        public string EmergencyState { get; set; }
        public string EmergencyCity { get; set; }
        public int EmergencyPincode { get; set; }
        public string EmergencyAddress { get; set; }
        public string EmergencyCountry { get; set; }
        public string MaritalStatus { get; set; }
        public DateTime? MarriageDate { get; set; }
        public string CountryOfOrigin { get; set; }
        public string Religion { get; set; }
        public string BloodGroup { get; set; }
        public bool IsPhChallanged { get; set; }
        public bool IsInternationalEmployee { get; set; }
        public string Specification { get; set; }
        public string Domain { get; set; }
        public string ProfileStatusCode { get; set; }
    }
}
