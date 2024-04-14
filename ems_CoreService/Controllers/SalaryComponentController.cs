using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalaryComponentController : BaseController
    {
        private readonly ISalaryComponentService _salaryComponentService;
        private readonly HttpContext _httpContext;

        public SalaryComponentController(ISalaryComponentService salaryComponentService, IHttpContextAccessor httpContext)
        {
            _salaryComponentService = salaryComponentService;
            _httpContext = httpContext.HttpContext;
        }


        [HttpGet("GetSalaryComponentsDetail")]
        public IResponse<ApiResponse> GetSalaryComponentsDetail()
        {
            try
            {
                var result = _salaryComponentService.GetSalaryComponentsDetailService();
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [HttpGet("GetCustomSalryPageData/{CompanyId}")]
        public IResponse<ApiResponse> GetCustomSalryPageData(int CompanyId)
        {
            try
            {
                var result = _salaryComponentService.GetCustomSalryPageDataService(CompanyId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, CompanyId);
            }
        }

        [HttpGet("GetSalaryGroupsById/{SalaryGroupId}")]
        public IResponse<ApiResponse> GetSalaryGroupsById(int SalaryGroupId)
        {
            try
            {
                var result = _salaryComponentService.GetSalaryGroupsByIdService(SalaryGroupId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, SalaryGroupId);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("UpdateSalaryComponents")]
        public async Task<ApiResponse> UpdateSalaryComponents(List<SalaryComponents> salaryComponents)
        {
            try
            {
                var result = await _salaryComponentService.UpdateSalaryComponentService(salaryComponents);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, salaryComponents);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("InsertUpdateSalaryComponentsByExcel")]
        public async Task<ApiResponse> InsertUpdateSalaryComponentsByExcel()
        {
            try
            {
                IFormFileCollection file = _httpContext.Request.Form.Files;
                var result = await _salaryComponentService.InsertUpdateSalaryComponentsByExcelService(file);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("AddSalaryGroup")]
        public IResponse<ApiResponse> AddSalaryGroup(SalaryGroup salaryGroup)
        {
            try
            {
                var result = _salaryComponentService.AddSalaryGroup(salaryGroup);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, salaryGroup);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("UpdateSalaryGroup")]
        public IResponse<ApiResponse> UpdateSalaryGroup(SalaryGroup salaryGroup)
        {
            try
            {
                var result = _salaryComponentService.UpdateSalaryGroup(salaryGroup);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, salaryGroup);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpDelete("RemoveAndUpdateSalaryGroup/{componentId}/{groupId}")]
        public IResponse<ApiResponse> RemoveAndUpdateSalaryGroup(string componentId, int groupId)
        {
            try
            {
                var result = _salaryComponentService.RemoveAndUpdateSalaryGroupService(componentId, groupId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { ComponentId = componentId, GroupId = groupId });
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("UpdateSalaryGroupComponents")]
        public IResponse<ApiResponse> UpdateSalaryGroupComponents(SalaryGroup salaryGroup)
        {
            try
            {
                var result = _salaryComponentService.UpdateSalaryGroupComponentService(salaryGroup);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, salaryGroup);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("AddUpdateRecurringComponents")]
        public async Task<ApiResponse> AddUpdateRecurringComponents(SalaryStructure salaryStructure)
        {
            try
            {
                var result = await _salaryComponentService.AddUpdateRecurringComponents(salaryStructure);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, salaryStructure);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("AddAdhocComponents")]
        public IResponse<ApiResponse> AddAdhocComponents(SalaryStructure salaryStructure)
        {
            try
            {
                var result = _salaryComponentService.AddAdhocComponents(salaryStructure);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, salaryStructure);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("AddDeductionComponents")]
        public IResponse<ApiResponse> AddDeductionComponents(SalaryStructure salaryStructure)
        {
            try
            {
                var result = _salaryComponentService.AddDeductionComponents(salaryStructure);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, salaryStructure);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("AddBonusComponents")]
        public IResponse<ApiResponse> AddBonusComponents(SalaryComponents salaryStructure)
        {
            try
            {
                var result = _salaryComponentService.AddBonusComponents(salaryStructure);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, salaryStructure);
            }
        }

        [HttpGet("GetSalaryGroupComponents/{SalaryGroupId}/{CTC}")]
        public IResponse<ApiResponse> GetSalaryGroupComponents(int SalaryGroupId, decimal CTC)
        {
            try
            {
                var result = _salaryComponentService.GetSalaryGroupComponents(SalaryGroupId, CTC);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { SalaryGroupId = SalaryGroupId, CTC = CTC });
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("InsertUpdateSalaryBreakUp/{EmployeeId}/{PresentMonth}/{PresentYear}")]
        public IResponse<ApiResponse> SalaryDetail(long EmployeeId, int PresentMonth, int PresentYear)
        {
            List<CalculatedSalaryBreakupDetail> fullSalaryDetail = null;
            try
            {
                _httpContext.Request.Form.TryGetValue("completesalarydetail", out StringValues compSalaryDetail);
                if (compSalaryDetail.Count > 0)
                {
                    fullSalaryDetail = JsonConvert.DeserializeObject<List<CalculatedSalaryBreakupDetail>>(compSalaryDetail);
                    var result = _salaryComponentService.SalaryDetailService(EmployeeId, fullSalaryDetail, PresentMonth, PresentYear);
                    return BuildResponse(result);
                }
                return BuildResponse("No files found", HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, fullSalaryDetail);
            }
        }

        [HttpGet("ResetSalaryBreakup")]
        public async Task<ApiResponse> SalaryBreakupCalc()
        {
            try
            {
                var result = await _salaryComponentService.ResetSalaryBreakupService();
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [HttpGet("GetSalaryBreakupByEmpId/{EmployeeId}")]
        public IResponse<ApiResponse> GetSalaryBreakupByEmpId(long EmployeeId)
        {
            try
            {
                var result = _salaryComponentService.GetSalaryBreakupByEmpIdService(EmployeeId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, EmployeeId);
            }
        }

        [HttpGet("GetSalaryGroupByCTC/{CTC}/{EmployeeId}")]
        public IResponse<ApiResponse> GetSalaryGroupByCTC(decimal CTC, long EmployeeId)
        {
            try
            {
                var result = _salaryComponentService.GetSalaryGroupByCTC(CTC, EmployeeId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { CTC = CTC, EmployeeId = EmployeeId });
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpGet("GetBonusComponents")]
        public IResponse<ApiResponse> GetBonusComponents()
        {
            try
            {
                var result = _salaryComponentService.GetBonusComponentsService();
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("GetAllSalaryDetail")]
        public IResponse<ApiResponse> GetAllSalaryDetail([FromBody] FilterModel filterModel)
        {
            try
            {
                var result = _salaryComponentService.GetAllSalaryDetailService(filterModel);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("CloneSalaryGroup")]
        public IResponse<ApiResponse> CloneSalaryGroup(SalaryGroup salaryGroup)
        {
            try
            {
                var result = _salaryComponentService.CloneSalaryGroupService(salaryGroup);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, salaryGroup);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpGet("GetSalaryGroupAndComponent")]
        public async Task<ApiResponse> GetSalaryGroupAndComponent()
        {
            try
            {
                var result = await _salaryComponentService.GetSalaryGroupAndComponentService();
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }
    }
}