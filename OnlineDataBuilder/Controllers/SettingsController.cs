using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using OnlineDataBuilder.ContextHandler;
using ServiceLayer.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Authorize(Roles = Role.Admin)]
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : BaseController
    {
        private readonly ISettingService _settingService;
        private readonly HttpContext _httpContext;
        public SettingsController(ISettingService settingService, IHttpContextAccessor httpContext)
        {
            _settingService = settingService;
            _httpContext = httpContext.HttpContext;
        }

        [HttpGet("GetSalaryComponents/{CompanyId}")]
        public IResponse<ApiResponse> GetSalaryComponents(int CompanyId)
        {
            var result = _settingService.GetSalaryComponentService(CompanyId);
            return BuildResponse(result);
        }

        [HttpGet("GetOrganizationInfo")]
        public IResponse<ApiResponse> GetOrganizationInfo()
        {
            var result = _settingService.GetOrganizationInfo();
            return BuildResponse(result);
        }

        [HttpGet("GetOrganizationAccountsInfo/{OrganizationId}")]
        public IResponse<ApiResponse> GetOrganizationBankDetailInfo(int organizationId)
        {
            var result = _settingService.GetOrganizationBankDetailInfoService(organizationId);
            return BuildResponse(result);
        }

        [HttpPut("PfEsiSetting/{CompanyId}")]
        public IResponse<ApiResponse> PfEsiSetting([FromRoute] int CompanyId, [FromBody] PfEsiSetting pfesiSetting)
        {
            var result = _settingService.PfEsiSetting(CompanyId, pfesiSetting);
            return BuildResponse(result);
        }

        [HttpPost("InsertUpdatePayrollSetting")]
        public IResponse<ApiResponse> InsertUpdatePayrollSetting(Payroll payroll)
        {
            var result = _settingService.InsertUpdatePayrollSetting(payroll);
            return BuildResponse(result);
        }

        [HttpGet("GetPayrollSetting/{companyId}")]
        public IResponse<ApiResponse> GetPayrollSetting(int companyId)
        {
            var result = _settingService.GetPayrollSetting(companyId);
            return BuildResponse(result);
        }

        [HttpPost("InsertUpdateSalaryStructure")]
        public IResponse<ApiResponse> InsertUpdateSalaryStructure(List<SalaryStructure> salaryStructure)
        {
            var result = _settingService.InsertUpdateSalaryStructure(salaryStructure);
            return BuildResponse(result);
        }

        [HttpPost("ActivateCurrentComponent")]
        public async Task<ApiResponse> ActivateCurrentComponent(List<SalaryComponents> components)
        {
            var result = await _settingService.ActivateCurrentComponentService(components);
            return BuildResponse(result);
        }

        [HttpPut("UpdateGroupSalaryComponentDetail/{componentId}/{groupId}")]
        public async Task<ApiResponse> UpdateSalaryComponentDetail([FromRoute] string componentId, [FromRoute] int groupId, [FromBody] SalaryComponents component)
        {
            var result = await _settingService.UpdateGroupSalaryComponentDetailService(componentId, groupId, component);
            return BuildResponse(result);
        }

        [HttpPut("UpdateSalaryComponentDetail/{componentId}")]
        public IResponse<ApiResponse> UpdateSalaryComponentDetail([FromRoute] string componentId, [FromBody] SalaryComponents component)
        {
            var result = _settingService.UpdateSalaryComponentDetailService(componentId, component);
            return BuildResponse(result);
        }

        [HttpGet("FetchComponentDetailById/{componentId}")]
        public IResponse<ApiResponse> FetchComponentDetailById(int componentTypeId)
        {
            var result = _settingService.FetchComponentDetailByIdService(componentTypeId);
            return BuildResponse(result);
        }

        [HttpGet("FetchActiveComponents")]
        public IResponse<ApiResponse> FetchActiveComponents()
        {
            var result = _settingService.FetchActiveComponentService();
            return BuildResponse(result);
        }
    }
}
