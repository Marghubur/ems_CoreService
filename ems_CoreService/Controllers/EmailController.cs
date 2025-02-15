using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.API;
using Bot.CoreBottomHalf.Modal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ModalLayer.Modal;
using Newtonsoft.Json;
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
            try
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
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [HttpGet("GetMyMails")]
        public ApiResponse GetMyMails()
        {
            try
            {
                var Result = _emailService.GetMyMailService();
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [HttpGet("GetEmailSettingByCompId/{CompanyId}")]
        public async Task<ApiResponse> GetEmailSettingByCompId(int CompanyId)
        {
            try
            {
                var result = await _emailService.GetEmailSettingByCompIdService(CompanyId);
                //Temporary hide the password
                result.Credentials = "************";
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, CompanyId);
            }
        }

        [HttpPost("InsertUpdateEmailSetting")]
        public IResponse<ApiResponse> InsertUpdateEmailSetting(EmailSettingDetail emailSettingDetail)
        {
            try
            {
                var result = _emailService.InsertUpdateEmailSettingService(emailSettingDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, emailSettingDetail);
            }
        }

        [HttpPost("InsertUpdateEmailTemplate")]
        public async Task<ApiResponse> InsertUpdateEmailTemplate()
        {
            try
            {
                _httpContext.Request.Form.TryGetValue("emailtemplate", out StringValues templateDetail);
                if (templateDetail.Count > 0)
                {
                    EmailTemplate emailTemplate = JsonConvert.DeserializeObject<EmailTemplate>(templateDetail);
                    IFormFileCollection file = _httpContext.Request.Form.Files;
                    var result = await _emailService.InsertUpdateEmailTemplateService(emailTemplate, file);
                    return BuildResponse(result);
                }
                else
                {
                    return BuildResponse(this.responseMessage, HttpStatusCode.BadRequest);
                }
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [HttpPost("GetEmailTemplate")]
        public async Task<ApiResponse> GetEmailTemplate([FromBody] FilterModel filterModel)
        {
            try
            {
                var result = await _emailService.GetEmailTemplateService(filterModel);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }

        [HttpGet("GetEmailTemplateById/{EmailTemplateId}/{CompanyId}")]
        public async Task<ApiResponse> GetEmailTemplateByIdService(long EmailTemplateId, int CompanyId)
        {
            try
            {
                var result = await _emailService.GetEmailTemplateByIdService(EmailTemplateId, CompanyId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { EmailTemplateId = EmailTemplateId, CompanyId = CompanyId });
            }
        }

        [HttpPost("EmailTempMappingInsertUpdate")]
        public async Task<ApiResponse> EmailTempMappingInsertUpdate([FromBody] EmailMappedTemplate emailMappedTemplate)
        {
            try
            {
                var result = await _emailService.EmailTempMappingInsertUpdateService(emailMappedTemplate);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, emailMappedTemplate);
            }
        }

        [HttpPost("GetEmailTempMapping")]
        public async Task<ApiResponse> GetEmailTempMapping([FromBody] FilterModel filterModel)
        {
            try
            {
                var result = await _emailService.GetEmailTempMappingService(filterModel);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }
    }
}