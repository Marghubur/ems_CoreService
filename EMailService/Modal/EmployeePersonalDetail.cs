namespace ModalLayer.Modal
{
    public class EmployeePersonalDetail : AssignedClients
    {
        public long EmployeePersonalDetailId { set; get; }
        public string Mobile { set; get; }
        public string SecondaryMobile { set; get; }
        public string Email { set; get; }
        public bool Gender { set; get; }
        public string FatherName { set; get; }
        public string MotherName { set; get; }
        public string SpouseName { set; get; }
        public string State { set; get; }
        public string City { set; get; }
        public int Pincode { set; get; }
        public string Address { set; get; }
    }
}