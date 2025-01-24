namespace EMailService.Modal.EmployeeModal
{
    public class EmployeeAddressDetail
    {
        public long EmployeeUid { get; set; }
        public string PermanentState { get; set; }
        public string PermanentCity { get; set; }
        public int PermanentPincode { get; set; } = 0;
        public string PermanentAddress { get; set; }
        public string PermanentCountry { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public int Pincode { get; set; } = 0;
        public string Address { get; set; }
        public string EmployeeProfileStatus { get; set; }
    }
}
