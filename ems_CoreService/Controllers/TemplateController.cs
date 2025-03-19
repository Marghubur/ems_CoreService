using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TemplateController : BaseController
    {
        private readonly ITemplateService _templateService;
        public TemplateController(ITemplateService templateService)
        {
            _templateService = templateService;
        }

        [HttpGet("GetBillingTemplateDetail")]
        public IResponse<ApiResponse> GetBillingTemplateDetail()
        {
            try
            {
                var result = _templateService.GetBillingTemplateDetailService();
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [HttpPost("AnnexureOfferLetterInsertUpdate/{LetterType}")]
        public async Task<ApiResponse> AnnexureOfferLetterInsertUpdate(AnnexureOfferLetter annexureOfferLetter, [FromRoute] int LetterType)
        {
            try
            {
                var result = await _templateService.AnnexureOfferLetterInsertUpdateService(annexureOfferLetter, LetterType);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { AnnexureOfferLetter = annexureOfferLetter, LetterType = LetterType });
            }
        }

        [HttpGet("GetOfficiaLetter/{CompanyId}/{LetterType}")]
        public async Task<ApiResponse> GetOfficiaLetter([FromRoute] int CompanyId, [FromRoute] int LetterType)
        {
            try
            {
                var result = await _templateService.GetOfficiaLetterService(CompanyId, LetterType);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { CompanyId = CompanyId, LetterType = LetterType });
            }
        }

        [HttpPost("EmailLinkConfigInsUpdate")]
        public IResponse<ApiResponse> EmailLinkConfigInsUpdate(EmailLinkConfig emailLinkConfig)
        {
            try
            {
                var result = _templateService.EmailLinkConfigInsUpdateService(emailLinkConfig);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, emailLinkConfig);
            }
        }

        [HttpGet("GetEmailLinkConfigByPageName/{PageName}/{CompanyId}")]
        public IResponse<ApiResponse> EmailLinkConfigGetByPageName([FromRoute] string PageName, [FromRoute] int CompanyId)
        {
            try
            {
                var result = _templateService.EmailLinkConfigGetByPageNameService(PageName, CompanyId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { PageName = PageName, CompanyId = CompanyId });
            }
        }

        [HttpPost("GenerateUpdatedPageMail")]
        public async Task<ApiResponse> GenerateUpdatedPageMail(EmailLinkConfig emailLinkConfig)
        {
            try
            {
                var result = await _templateService.GenerateUpdatedPageMailService(emailLinkConfig);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, emailLinkConfig);
            }
        }

        [HttpGet("GenerateOfferLetterPDF")]
        public async Task<ApiResponse> GenerateOfferLetterPDF()
        {
            try
            {
                var result = await _templateService.GenerateOfferLetterPDFService(); 
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [HttpPost("GenerateOfferLetterByteArray")]
        public async Task<IActionResult> GenerateOfferLetterByteArray([FromBody] AnnexureOfferLetter annexureOfferLetter)
        {
            try
            {
                var result = await _templateService.GenerateOfferLetterByteArrayService();
                return File(result, "application/pdf", "document.pdf");
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }
    }
}
