using ModalLayer.Modal.Accounts;

namespace ModalLayer.Modal
{
    public class RegistrationForm
    {
        public string EmailId { set; get; }
        public string Mobile { set; get; }
        public string OrganizationName { set; get; }
        public string CompanyName { set; get; }
        public string FirstName { set; get; }
        public string FirstAddress { set; get; }
        public string SecondAddress { set; get; }
        public string ThirdAddress { set; get; }
        public string ForthAddress { set; get; }
        public string Country { set; get; }
        public string State { set; get; }
        public string City { set; get; }
        public string LastName { set; get; }
        public int DesignationId { set; get; }
        public int UserTypeId { set; get; }
        public int AccessLevelId { set; get; }
        public string Password { set; get; }
        public string AuthenticationCode { set; get; }
        public string BankName { set; get; }
        public string BranchCode { set; get; }
        public string Branch { set; get; }
        public string IFSC { set; get; }
        public string AccountNo { set; get; }
        public string GSTNo { get; set; }
        public bool IsPrimaryAccount { set; get; }
        public string EmailName { set; get; }
        public string EmailHost { set; get; }
        public int PortNo { set; get; }
        public bool EnableSsl { set; get; }
        public string DeliveryMethod { set; get; }
        public bool UserDefaultCredentials { set; get; }
        public string Credentials { set; get; }
        public bool IsPrimary { set; get; }
        public int DeclarationStartMonth {set; get;}
        public int DeclarationEndMonth {set; get;}
        public int FinancialYear {set; get;}
        public int AttendanceSubmissionLimit { set; get; }
    }
}
