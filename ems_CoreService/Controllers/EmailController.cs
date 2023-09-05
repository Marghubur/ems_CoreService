using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.Modal;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ModalLayer.Modal;
using Newtonsoft.Json;
using OnlineDataBuilder.ContextHandler;
using ServiceLayer.Interface;
using System;
using System.Net;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : BaseController
    {
        private readonly HttpContext _httpContext;
        private readonly IEmailService _emailService;
        public EmailController(IEmailService emailService, IHttpContextAccessor httpContext)
        {
            _emailService = emailService;
            _httpContext = httpContext.HttpContext;
        }

        [HttpPost("SendEmailRequest")]
        public ApiResponse SendEmailRequest()
        {
            StringValues emailDetail = default(string);
            _httpContext.Request.Form.TryGetValue("mailDetail", out emailDetail);
            if (emailDetail.Count == 0)
                throw new HiringBellException("No detail found. Please pass all detail.");

            EmailSenderModal emailSenderModal = JsonConvert.DeserializeObject<EmailSenderModal>(emailDetail);
            IFormFileCollection files = _httpContext.Request.Form.Files;
            var Result = _emailService.SendEmailRequestService(emailSenderModal, files);
            return BuildResponse(Result, HttpStatusCode.OK);
        }

        [HttpGet("GetMyMails")]
        public ApiResponse GetMyMails()
        {
            var Result = _emailService.GetMyMailService();
            return BuildResponse(Result, HttpStatusCode.OK);
        }

        [HttpGet("GetEmailSettingByCompId/{CompanyId}")]
        public IResponse<ApiResponse> GetEmailSettingByCompId(int CompanyId)
        {
            var result = _emailService.GetEmailSettingByCompIdService(CompanyId);
            //Temporary hide the password
            result.Credentials = "************";
            return BuildResponse(result);
        }

        [HttpPost("InsertUpdateEmailSetting")]
        public IResponse<ApiResponse> InsertUpdateEmailSetting(EmailSettingDetail emailSettingDetail)
        {
            var result = _emailService.InsertUpdateEmailSettingService(emailSettingDetail);
            return BuildResponse(result);
        }

        [HttpPost("InsertUpdateEmailTemplate")]
        public IResponse<ApiResponse> InsertUpdateEmailTemplate()
        {
            try
            {
                _httpContext.Request.Form.TryGetValue("emailtemplate", out StringValues templateDetail);
                if (templateDetail.Count > 0)
                {
                    EmailTemplate emailTemplate = JsonConvert.DeserializeObject<EmailTemplate>(templateDetail);
                    IFormFileCollection file = _httpContext.Request.Form.Files;
                    var result = _emailService.InsertUpdateEmailTemplateService(emailTemplate, file)  ;
                    return BuildResponse(result);
                } else
                {
                    return BuildResponse(this.responseMessage, HttpStatusCode.BadRequest);
                }
            }
            catch (Exception ex)
            {

                throw ex; ;
            }
        }

        [HttpPost("GetEmailTemplate")]
        public IResponse<ApiResponse> GetEmailTemplate([FromBody] FilterModel filterModel)
        {
            var result = _emailService.GetEmailTemplateService(filterModel);
            return BuildResponse(result);
        }

        [HttpGet("GetEmailTemplateById/{EmailTemplateId}/{CompanyId}")]
        public async Task<ApiResponse> GetEmailTemplateByIdService(long EmailTemplateId, int CompanyId)
        {
            var result = await _emailService.GetEmailTemplateByIdService(EmailTemplateId, CompanyId);
            return BuildResponse(result);
        }

        [HttpPost("EmailTempMappingInsertUpdate")]
        public async Task<ApiResponse> EmailTempMappingInsertUpdate([FromBody] EmailMappedTemplate emailMappedTemplate)
        {
            var result = await _emailService.EmailTempMappingInsertUpdateService(emailMappedTemplate);
            return BuildResponse(result);
        }

        [HttpPost("GetEmailTempMapping")]
        public async Task<ApiResponse> GetEmailTempMapping([FromBody] FilterModel filterModel)
        {
            var result = await _emailService.GetEmailTempMappingService(filterModel);
            return BuildResponse(result);
        }
    }
}
