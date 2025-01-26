using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using Bt.Lib.PipelineConfig.Services;
using EMailService.Modal;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class PriceService : IPriceService
    {
        private readonly IDb _db;
        private readonly GitHubConnector _gitHubConnector;
        private readonly MicroserviceRegistry _microserviceRegistry;
        public PriceService(IDb db, MicroserviceRegistry microserviceRegistry, GitHubConnector gitHubConnector)
        {
            _db = db;
            _microserviceRegistry = microserviceRegistry;
            _gitHubConnector = gitHubConnector;
        }
        public async Task<List<PriceDetail>> GetPriceDetailService()
        {
            string jsonFilePath = "Model/PriceDetail.json";
            string json = File.ReadAllText(jsonFilePath);
            List<PriceDetail> priceDetail = JsonConvert.DeserializeObject<List<PriceDetail>>(json);
            return await Task.FromResult(priceDetail);
        }

        public async Task<string> AddContactusService(ContactUsDetail contactUsDetail)
        {
            validateContactUsDetail(contactUsDetail);

            var masterDatabse = await _gitHubConnector.FetchTypedConfiguraitonAsync<string>(_microserviceRegistry.DatabaseConfigurationUrl); ;
            _db.SetupConnectionString(masterDatabse);

            var result = _db.Execute<ContactUsDetail>(Procedures.CONTACT_US_INSUPD, new
            {
                ContactUsId = contactUsDetail.ContactUsId,
                FullName = contactUsDetail.FullName,
                Email = contactUsDetail.Email,
                CompanyName = contactUsDetail.CompanyName,
                PhoneNumber = contactUsDetail.PhoneNumber,
                Message = contactUsDetail.Message
            }, true);
            if (string.IsNullOrEmpty(result))
                throw HiringBellException.ThrowBadRequest("Faild to add contact details");

            return await Task.FromResult(result);
        }

        private void validateContactUsDetail(ContactUsDetail contactUsDetail)
        {
            if (string.IsNullOrEmpty(contactUsDetail.FullName))
                throw HiringBellException.ThrowBadRequest("Please enter a valid full name");

            if (string.IsNullOrEmpty(contactUsDetail.Email))
                throw HiringBellException.ThrowBadRequest("Email is null opr empty");

            if (string.IsNullOrEmpty(contactUsDetail.PhoneNumber))
                throw HiringBellException.ThrowBadRequest("Please enter a valid phone number");

            if (string.IsNullOrEmpty(contactUsDetail.CompanyName))
                throw HiringBellException.ThrowBadRequest("Please enter a valid company name");

            if (string.IsNullOrEmpty(contactUsDetail.Message))
                throw HiringBellException.ThrowBadRequest("Please enter a valid message");

            if (contactUsDetail.PhoneNumber.Length != 10)
                throw HiringBellException.ThrowBadRequest("Invalid mobile number");

            EmailAddressAttribute email = new EmailAddressAttribute();
            if (!email.IsValid(contactUsDetail.Email))
                throw HiringBellException.ThrowBadRequest("Please enter a valid email id");
        }

        public async Task<string> AddTrailRequestService(ContactUsDetail contactUsDetail)
        {
            var masterDatabse = await _gitHubConnector.FetchTypedConfiguraitonAsync<string>(_microserviceRegistry.DatabaseConfigurationUrl); ;
            _db.SetupConnectionString(masterDatabse);
            
            validateTrailRequestDetail(contactUsDetail);

            var result = _db.Execute<ContactUsDetail>(Procedures.TRAIL_REQUEST_INSUPD, new
            {
                contactUsDetail.TrailRequestId,
                contactUsDetail.FullName,
                contactUsDetail.Email,
                contactUsDetail.CompanyName,
                contactUsDetail.OrganizationName,
                contactUsDetail.PhoneNumber,
                contactUsDetail.HeadCount,
                contactUsDetail.FullAddress,
                contactUsDetail.Country,
                contactUsDetail.State,
                contactUsDetail.City,
                IsProcessed = false
            }, true);
            if (string.IsNullOrEmpty(result))
                throw HiringBellException.ThrowBadRequest("Faild to add contact details");

            return await Task.FromResult(result);
        }

        private void validateTrailRequestDetail(ContactUsDetail contactUsDetail)
        {
            if (string.IsNullOrEmpty(contactUsDetail.FullName))
                throw HiringBellException.ThrowBadRequest("Please enter a valid full name");

            if (string.IsNullOrEmpty(contactUsDetail.Email))
                throw HiringBellException.ThrowBadRequest("Email is null opr empty");

            if (string.IsNullOrEmpty(contactUsDetail.PhoneNumber))
                throw HiringBellException.ThrowBadRequest("Please enter a valid phone number");

            if (string.IsNullOrEmpty(contactUsDetail.CompanyName))
                throw HiringBellException.ThrowBadRequest("Please enter a valid company name");

            if (contactUsDetail.PhoneNumber.Length != 10)
                throw HiringBellException.ThrowBadRequest("Invalid mobile number");

            if (contactUsDetail.HeadCount <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid head count selected");

            EmailAddressAttribute email = new EmailAddressAttribute();
            if (!email.IsValid(contactUsDetail.Email))
                throw HiringBellException.ThrowBadRequest("Please enter a valid email id");

            var result = _db.GetList<ContactUsDetail>(Procedures.TRAIL_REQUEST_GETBY_EMAIL_PHONE, new
            {
                Email = contactUsDetail.Email,
                PhoneNumber = contactUsDetail.PhoneNumber
            });

            if (result.Count > 0)
                throw HiringBellException.ThrowBadRequest("Email or mobile already exist");
        }
    }
}