using Bot.CoreBottomHalf.CommonModal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
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
    public class CompanyController : BaseController
    {
        private readonly ICompanyService _companyService;
        private readonly IPayrollService _payrollService;
        private readonly HttpContext _httpContext;

        public CompanyController(ICompanyService companyService, IHttpContextAccessor httpContext, IPayrollService payrollService)
        {
            _companyService = companyService;
            _httpContext = httpContext.HttpContext;
            _payrollService = payrollService;
        }

        [HttpGet("GetAllCompany")]
        public IResponse<ApiResponse> GetAllCompany()
        {
            var result = _companyService.GetAllCompany();
            return BuildResponse(result);
        }

        [HttpPost("AddCompanyGroup")]
        public IResponse<ApiResponse> AddCompanyGroup(OrganizationDetail companyGroup)
        {
            var result = _companyService.AddCompanyGroup(companyGroup);
            return BuildResponse(result);
        }

        [HttpPut("UpdateCompanyGroup/{companyId}")]
        public IResponse<ApiResponse> UpdateCompanyGroup([FromRoute] int companyId, [FromBody] OrganizationDetail companyGroup)
        {
            var result = _companyService.UpdateCompanyGroup(companyGroup, companyId);
            return BuildResponse(result);
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("InsertUpdateOrganizationDetail")]
        public async Task<ApiResponse> InsertUpdateOrganizationDetail()
        {
            StringValues compnyinfo = default(string);
            OrganizationDetail org = null;
            _httpContext.Request.Form.TryGetValue("OrganizationInfo", out compnyinfo);
            if (compnyinfo.Count > 0)
            {
                OrganizationDetail organizationSettings = JsonConvert.DeserializeObject<OrganizationDetail>(compnyinfo);
                IFormFileCollection files = _httpContext.Request.Form.Files;
                org = await _companyService.InsertUpdateOrganizationDetailService(organizationSettings, files);
            }
            return BuildResponse(org);
        }

        [HttpPost("UpdateCompanyDetails")]
        public async Task<ApiResponse> UpdateCompanyDetails()
        {
            StringValues compnyinfo = default(string);
            OrganizationDetail org = null;
            _httpContext.Request.Form.TryGetValue("CompanyInfo", out compnyinfo);
            if (compnyinfo.Count > 0)
            {
                OrganizationDetail organizationSettings = JsonConvert.DeserializeObject<OrganizationDetail>(compnyinfo);
                IFormFileCollection files = _httpContext.Request.Form.Files;
                org = await _companyService.InsertUpdateCompanyDetailService(organizationSettings, files);
            }
            return BuildResponse(org);
        }

        [HttpGet("GetCompanyById/{companyId}")]
        public IResponse<ApiResponse> GetCompanyById(int companyId)
        {
            var result = _companyService.GetCompanyById(companyId);
            return BuildResponse(result);
        }

        [Authorize(Roles = Role.Admin)]
        [HttpGet("GetOrganizationDetail")]
        public IResponse<ApiResponse> GetOrganizationDetail()
        {
            var result = _companyService.GetOrganizationDetailService();
            return BuildResponse(result);
        }

        [HttpPost("InsertUpdateCompanyAccounts")]
        public IResponse<ApiResponse> InsertUpdateCompanyAccounts(BankDetail bankDetail)
        {
            var org = _companyService.InsertUpdateCompanyAccounts(bankDetail);
            return BuildResponse(org);
        }

        [HttpPost("GetCompanyBankDetail")]
        public IResponse<ApiResponse> GetCompanyBankDetail(FilterModel filterModel)
        {
            var bankDetail = _companyService.GetCompanyBankDetail(filterModel);
            return BuildResponse(bankDetail);
        }

        [HttpPut("UpdateSetting/{companyId}/{isRunLeaveAccrual}")]
        public async Task<ApiResponse> UpdateSetting([FromRoute] int companyId, [FromRoute] bool isRunLeaveAccrual, [FromBody] CompanySetting companySetting)
        {
            var settingDetail = await _companyService.UpdateSettingService(companyId, companySetting, isRunLeaveAccrual);
            return BuildResponse(settingDetail);
        }

        [HttpGet("RunPayroll/{MonthNumber}/{ReCalculateFlagId}")]
        public async Task<ApiResponse> RunPayroll(int MonthNumber, int ReCalculateFlagId)
        {
            await _payrollService.RunPayrollCycle(MonthNumber, ReCalculateFlagId == 1);
            return BuildResponse(ApplicationConstants.Successfull);
        }

        [HttpGet("getcompanysettingdetail/{companyId}")]
        public async Task<ApiResponse> GetCompanySetting(int companyId)
        {
            var settingDetail = await _companyService.GetCompanySettingService(companyId);
            return BuildResponse(settingDetail);
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("addcompanyfiles")]
        public async Task<ApiResponse> AddCompanyFiles()
        {
            try
            {
                StringValues OrganizationData = default(string);
                _httpContext.Request.Form.TryGetValue("FileDetail", out OrganizationData);
                if (OrganizationData.Count > 0)
                {
                    Files files = JsonConvert.DeserializeObject<Files>(OrganizationData);
                    IFormFileCollection fileCollection = _httpContext.Request.Form.Files;
                    var resetSet = await _companyService.UpdateCompanyFiles(files, fileCollection);
                    return BuildResponse(resetSet);
                }
                else
                {
                    return BuildResponse(this.responseMessage, HttpStatusCode.BadRequest);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        [HttpGet("getcompanyfiles/{CompanyId}")]
        public async Task<ApiResponse> GetCompanyFiles(int CompanyId)
        {
            var resetSet = await _companyService.GetCompanyFiles(CompanyId);
            return BuildResponse(resetSet);
        }
        [HttpPost("DeleteCompanyFile")]
        public async Task<ApiResponse> DeleteCompanyFiles(Files companyFile)
        {
            var result = await _companyService.DeleteCompanyFilesService(companyFile);
            return BuildResponse(result);
        }
    }
}