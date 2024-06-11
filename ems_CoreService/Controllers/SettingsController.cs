using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.API;
using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : BaseController
    {
        private readonly ISettingService _settingService;
        public SettingsController(ISettingService settingService)
        {
            _settingService = settingService;
        }

        [Authorize(Roles = Role.Admin)]
        [HttpGet("GetSalaryComponents/{CompanyId}")]
        public IResponse<ApiResponse> GetSalaryComponents(int CompanyId)
        {
            try
            {
                var result = _settingService.GetSalaryComponentService(CompanyId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, CompanyId);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpGet("GetOrganizationInfo")]
        public IResponse<ApiResponse> GetOrganizationInfo()
        {
            try
            {
                var result = _settingService.GetOrganizationInfo();
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpGet("GetOrganizationAccountsInfo/{OrganizationId}")]
        public IResponse<ApiResponse> GetOrganizationBankDetailInfo(int organizationId)
        {
            try
            {
                var result = _settingService.GetOrganizationBankDetailInfoService(organizationId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, organizationId);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPut("PfEsiSetting/{CompanyId}")]
        public IResponse<ApiResponse> PfEsiSetting([FromRoute] int CompanyId, [FromBody] PfEsiSetting pfesiSetting)
        {
            try
            {
                var result = _settingService.PfEsiSetting(CompanyId, pfesiSetting);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { CompanyId = CompanyId, PfEsiSetting = pfesiSetting });
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("InsertUpdatePayrollSetting")]
        public IResponse<ApiResponse> InsertUpdatePayrollSetting(Payroll payroll)
        {
            try
            {
                var result = _settingService.InsertUpdatePayrollSetting(payroll);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, payroll);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpGet("GetPayrollSetting/{companyId}")]
        public IResponse<ApiResponse> GetPayrollSetting(int companyId)
        {
            try
            {
                var result = _settingService.GetPayrollSetting(companyId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, companyId);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("InsertUpdateSalaryStructure")]
        public IResponse<ApiResponse> InsertUpdateSalaryStructure(List<SalaryStructure> salaryStructure)
        {
            try
            {
                var result = _settingService.InsertUpdateSalaryStructure(salaryStructure);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, salaryStructure);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("ActivateCurrentComponent")]
        public async Task<ApiResponse> ActivateCurrentComponent(List<SalaryComponents> components)
        {
            try
            {
                var result = await _settingService.ActivateCurrentComponentService(components);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, components);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPut("UpdateGroupSalaryComponentDetail/{componentId}/{groupId}")]
        public async Task<ApiResponse> UpdateSalaryComponentDetail([FromRoute] string componentId, [FromRoute] int groupId, [FromBody] SalaryComponents component)
        {
            try
            {
                var result = await _settingService.UpdateGroupSalaryComponentDetailService(componentId, groupId, component);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { ComponentId = componentId, GroupId = groupId, Component = component });
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPut("UpdateSalaryComponentDetail/{componentId}")]
        public IResponse<ApiResponse> UpdateSalaryComponentDetail([FromRoute] string componentId, [FromBody] SalaryComponents component)
        {
            try
            {
                var result = _settingService.UpdateSalaryComponentDetailService(componentId, component);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { ComponentId = componentId, Component = component });
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpGet("FetchComponentDetailById/{componentId}")]
        public IResponse<ApiResponse> FetchComponentDetailById(int componentTypeId)
        {
            try
            {
                var result = _settingService.FetchComponentDetailByIdService(componentTypeId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, componentTypeId);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpGet("FetchActiveComponents")]
        public IResponse<ApiResponse> FetchActiveComponents()
        {
            try
            {
                var result = _settingService.FetchActiveComponentService();
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [AllowAnonymous]
        [HttpPost("LayoutConfigurationSetting")]
        public async Task<ApiResponse> LayoutConfigurationSetting([FromBody] UserLayoutConfigurationJSON userLayoutConfiguration)
        {
            try
            {
                var result = await _settingService.LayoutConfigurationSettingService(userLayoutConfiguration);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, userLayoutConfiguration);
            }
        }
    }
}