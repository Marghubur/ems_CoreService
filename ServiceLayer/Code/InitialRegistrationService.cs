using BottomhalfCore.DatabaseLayer.Common.Code;
using Microsoft.Extensions.Configuration;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System.Net.Mail;

namespace ServiceLayer.Code
{
    public class InitialRegistrationService : IInitialRegistrationService
    {
        private readonly IDb _db;
        private readonly IAuthenticationService _authenticationService;
        private readonly IConfiguration _configuration;
        public InitialRegistrationService(IDb db, IAuthenticationService authenticationService, IConfiguration configuration)
        {
            _db = db;
            _authenticationService = authenticationService;
            _configuration = configuration;
        }
        public string InitialOrgRegistrationService(RegistrationForm companyDetail)
        {
            string newEncryptedPassword = _authenticationService.Encrypt(_configuration.GetSection("DefaultNewEmployeePassword").Value, _configuration.GetSection("EncryptSecret").Value);
            CompanyDetailValidation(companyDetail);
            var result = _db.Execute<RegistrationForm>("sp_new_registration", new
            {
                companyDetail.OrganizationName,
                companyDetail.CompanyName,
                companyDetail.FirstName,
                companyDetail.FirstAddress,
                companyDetail.SecondAddress,
                companyDetail.ThirdAddress,
                companyDetail.ForthAddress,
                companyDetail.State,
                companyDetail.City,
                companyDetail.Country,
                companyDetail.LastName,
                companyDetail.EmailName,
                companyDetail.EmailId,
                companyDetail.EmailHost,
                companyDetail.Credentials,
                companyDetail.EnableSsl,
                companyDetail.UserDefaultCredentials,
                companyDetail.PortNo,
                companyDetail.GSTNo,
                companyDetail.AccountNo,
                companyDetail.BankName,
                companyDetail.Branch,
                companyDetail.IFSC,
                companyDetail.BranchCode,
                companyDetail.IsPrimaryAccount,
                companyDetail.DeliveryMethod,
                companyDetail.IsPrimary,
                companyDetail.Mobile,
                companyDetail.DeclarationStartMonth,
                companyDetail.DeclarationEndMonth,
                companyDetail.FinancialYear,
                companyDetail.AttendanceSubmissionLimit,
                Password = newEncryptedPassword
            }, true);
            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Fail to insert/update");
            return result;
        }
        private void CompanyDetailValidation(RegistrationForm companyDetail)
        {
            if (string.IsNullOrEmpty(companyDetail.OrganizationName))
                throw new HiringBellException("Organization Name is null or empty");

            if (string.IsNullOrEmpty(companyDetail.Mobile))
                throw new HiringBellException("Mobile No is null or empty");

            if (companyDetail.Mobile.Length < 10 || companyDetail.Mobile.Length > 10)
                throw new HiringBellException("Mobile no must be 10 digit only");

            if (string.IsNullOrEmpty(companyDetail.CompanyName))
                throw new HiringBellException("Company Name is null or empty");

            if (string.IsNullOrEmpty(companyDetail.FirstName))
                throw new HiringBellException("First Name is null or empty");

            if (string.IsNullOrEmpty(companyDetail.LastName))
                throw new HiringBellException("Last Name is null or empty");

            if (string.IsNullOrEmpty(companyDetail.EmailName))
                throw new HiringBellException("Email Name is null or empty");

            if (string.IsNullOrEmpty(companyDetail.EmailId))
                throw new HiringBellException("Email Id is null or empty");

            if (string.IsNullOrEmpty(companyDetail.EmailHost))
                throw new HiringBellException("Email Host is null or empty");

            if (string.IsNullOrEmpty(companyDetail.Credentials))
                throw new HiringBellException("Email Credential is null or empty");

            if (companyDetail.EnableSsl == null)
                throw new HiringBellException("Invalid SSL select");

            if (companyDetail.UserDefaultCredentials == null)
                throw new HiringBellException("Invalid User Default Credentials option");

            if (companyDetail.PortNo <= 0)
                throw new HiringBellException("Invalid port number");

            if (companyDetail.FinancialYear <= 0)
                throw new HiringBellException("Please enter a valid financial year");

            if (string.IsNullOrEmpty(companyDetail.GSTNo))
                throw new HiringBellException("GST No is null or empty");

            if (string.IsNullOrEmpty(companyDetail.AccountNo))
                throw new HiringBellException("Account No is null or empty");

            if (string.IsNullOrEmpty(companyDetail.BankName))
                throw new HiringBellException("Bank name is null or empty");

            if (string.IsNullOrEmpty(companyDetail.Branch))
                throw new HiringBellException("Branch name is null or empty");

            if (string.IsNullOrEmpty(companyDetail.IFSC))
                throw new HiringBellException("IFSC code is null or empty");

            if (string.IsNullOrEmpty(companyDetail.FirstAddress))
                throw new HiringBellException("IFSC code is null or empty");

            if (string.IsNullOrEmpty(companyDetail.SecondAddress))
                throw new HiringBellException("IFSC code is null or empty");

            if (string.IsNullOrEmpty(companyDetail.State))
                throw new HiringBellException("State is null or empty");

            var mail = new MailAddress(companyDetail.EmailId);
            bool isValidEmail = mail.Host.Contains(".");
            if (!isValidEmail)
                throw new HiringBellException ("Invalid email id");
        }
    }
}
