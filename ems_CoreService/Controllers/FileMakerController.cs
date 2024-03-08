using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.API;
using DocMaker.HtmlToDocx;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileMakerController : BaseController
    {
        private readonly IOnlineDocumentService _onlineDocumentService;
        private readonly IFileService _fileService;
        private readonly IBillService _billService;
        private readonly IDOCXToHTMLConverter _iDOCXToHTMLConverter;
        private readonly HttpContext _httpContext;
        private readonly IHTMLConverter iHTMLConverter;
        public FileMakerController(IConfiguration configuration,
            IOnlineDocumentService onlineDocumentService,
            IFileService fileService, IBillService billService,
            IHttpContextAccessor httpContext,
            IDOCXToHTMLConverter iDOCXToHTMLConverter)
        {
            _onlineDocumentService = onlineDocumentService;
            _fileService = fileService;
            _billService = billService;
            _httpContext = httpContext.HttpContext;
            _iDOCXToHTMLConverter = iDOCXToHTMLConverter;
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost]
        [Route("GenerateBill")]
        public async Task<ApiResponse> GenerateBill()
        {
            try
            {
                _httpContext.Request.Form.TryGetValue("Comment", out StringValues commentJson);
                _httpContext.Request.Form.TryGetValue("DailyTimesheetDetail", out StringValues timeSheetDetailJson);
                _httpContext.Request.Form.TryGetValue("TimesheetDetail", out StringValues timesheetJson);
                _httpContext.Request.Form.TryGetValue("BillRequestData", out StringValues pdfModalJson);

                BillGenerationModal billModal = new BillGenerationModal();
                billModal.Comment = JsonConvert.DeserializeObject<string>(commentJson);
                billModal.FullTimeSheet = JsonConvert.DeserializeObject<List<DailyTimesheetDetail>>(timeSheetDetailJson);
                billModal.TimesheetDetail = JsonConvert.DeserializeObject<TimesheetDetail>(timesheetJson);
                billModal.PdfModal = JsonConvert.DeserializeObject<PdfModal>(pdfModalJson);

                // var fileDetail = _billService.GenerateDocument(pdfModal, dailyTimesheetDetails, timesheetDetail, Comment);
                var fileDetail = await _billService.GenerateBillService(billModal);
                return BuildResponse(fileDetail, System.Net.HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost]
        [Route("UpdateGeneratedBill")]
        public async Task<ApiResponse> UpdateGeneratedBill()
        {
            try
            {
                _httpContext.Request.Form.TryGetValue("Comment", out StringValues commentJson);
                _httpContext.Request.Form.TryGetValue("DailyTimesheetDetail", out StringValues timeSheetDetailJson);
                _httpContext.Request.Form.TryGetValue("TimesheetDetail", out StringValues timesheetJson);
                _httpContext.Request.Form.TryGetValue("BillRequestData", out StringValues pdfModalJson);

                BillGenerationModal billModal = new BillGenerationModal();
                billModal.Comment = JsonConvert.DeserializeObject<string>(commentJson);
                billModal.FullTimeSheet = JsonConvert.DeserializeObject<List<DailyTimesheetDetail>>(timeSheetDetailJson);
                billModal.TimesheetDetail = JsonConvert.DeserializeObject<TimesheetDetail>(timesheetJson);
                billModal.PdfModal = JsonConvert.DeserializeObject<PdfModal>(pdfModalJson);

                var fileDetail = await _billService.UpdateGeneratedBillService(billModal);
                return BuildResponse(fileDetail, System.Net.HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost]
        [Route("ReGenerateBill")]
        public IResponse<ApiResponse> ReGenerateBill([FromBody] GenerateBillFileDetail fileDetail)
        {
            try
            {
                var Result = _onlineDocumentService.ReGenerateService(fileDetail);
                return BuildResponse(Result, System.Net.HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [HttpPost]
        [Route("CreateFolder")]
        public IResponse<ApiResponse> CreateFolder(Files file)
        {
            try
            {
                var result = _fileService.CreateFolder(file);
                return BuildResponse(result, System.Net.HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, file);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpDelete]
        [Route("DeleteFile/{userId}/{UserTypeId}")]
        public IResponse<ApiResponse> DeleteFiles(long userId, int userTypeId, List<string> fileIds)
        {
            try
            {
                var result = _fileService.DeleteFiles(userId, fileIds, userTypeId);
                return BuildResponse(result, System.Net.HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { UserId = userId, FileIds = fileIds, UserTypeId = userTypeId });
            }
        }

        [HttpPost]
        [Route("GetDocxHtml")]
        public IResponse<ApiResponse> GetDocxHtml(FileDetail fileDetail)
        {
            try
            {
                var result = _iDOCXToHTMLConverter.ToHtml(fileDetail);
                return BuildResponse(result, System.Net.HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, fileDetail);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpGet]
        [Route("GetBillDetailWithTemplate/{BillNo}/{EmployeeId}")]
        public async Task<ApiResponse> GetBillDetailWithTemplate(string BillNo, long EmployeeId)
        {
            try
            {
                var fileDetail = await _billService.GetBillDetailWithTemplateService(BillNo, EmployeeId);
                return BuildResponse(fileDetail, System.Net.HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { BillNo = BillNo, EmployeeId = EmployeeId });
            }
        }

        [HttpPost]
        [Route("GeneratePayslip")]
        public async Task<ApiResponse> GeneratePayslip(PayslipGenerationModal payslipGenerationModal)
        {
            try
            {
                var fileDetail = await _billService.GeneratePayslipService(payslipGenerationModal);
                return BuildResponse(fileDetail, System.Net.HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, payslipGenerationModal);
            }
        }
    }
}