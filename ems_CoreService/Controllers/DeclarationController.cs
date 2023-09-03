using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using Newtonsoft.Json;
using OnlineDataBuilder.ContextHandler;
using ServiceLayer.Interface;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeclarationController : BaseController
    {
        private readonly IDeclarationService _declarationService;
        private readonly HttpContext _httpContext;
        public DeclarationController(IDeclarationService declarationService, IHttpContextAccessor httpContext)
        {
            _declarationService = declarationService;
            _httpContext = httpContext.HttpContext;
        }

        [HttpGet("GetEmployeeDeclarationDetailById/{EmployeeId}")]
        public async Task<ApiResponse> GetEmployeeDeclarationDetailById(long EmployeeId)
        {
            var result = await _declarationService.GetEmployeeDeclarationDetail(EmployeeId);
            return BuildResponse(result);
        }

        [HttpPost("UpdateDeclarationDetail/{EmployeeDeclarationId}")]
        public async Task<ApiResponse> UpdateDeclarationDetail([FromRoute] long EmployeeDeclarationId)
        {
            StringValues declaration = default(string);
            _httpContext.Request.Form.TryGetValue("declaration", out declaration);
            _httpContext.Request.Form.TryGetValue("fileDetail", out StringValues FileData);
            if (declaration.Count > 0)
            {
                var DeclarationDetail = JsonConvert.DeserializeObject<EmployeeDeclaration>(declaration);
                List<Files> files = JsonConvert.DeserializeObject<List<Files>>(FileData);
                IFormFileCollection fileDetail = _httpContext.Request.Form.Files;
                var result = await _declarationService.UpdateDeclarationDetail(EmployeeDeclarationId, DeclarationDetail, fileDetail, files);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            return BuildResponse("No files found", HttpStatusCode.OK);
        }

        [HttpPost("HouseRentDeclaration/{EmployeeDeclarationId}")]
        public async Task<ApiResponse> HousingPropertyDeclaration([FromRoute] long EmployeeDeclarationId)
        {
            StringValues declaration = default(string);
            _httpContext.Request.Form.TryGetValue("declaration", out declaration);
            _httpContext.Request.Form.TryGetValue("fileDetail", out StringValues FileData);
            if (declaration.Count > 0)
            {
                var DeclarationDetail = JsonConvert.DeserializeObject<HousingDeclartion>(declaration);
                List<Files> files = JsonConvert.DeserializeObject<List<Files>>(FileData);
                IFormFileCollection fileDetail = _httpContext.Request.Form.Files;
                var result = await _declarationService.HouseRentDeclarationService(EmployeeDeclarationId, DeclarationDetail, fileDetail, files);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            return BuildResponse("No files found", HttpStatusCode.OK);
        }

        [Authorize(Roles = Role.Admin)]
        [HttpGet("UpdateTaxDetail/{EmployeeId}/{PresentMonth}/{PresentYear}")]
        public async Task<ApiResponse> UpdateTaxDetail(long EmployeeId, int PresentMonth, int PresentYear)
        {
            var result = await _declarationService.UpdateTaxDetailsService(EmployeeId, PresentMonth, PresentYear);
            return BuildResponse(result);
        }

        [HttpPost("SwitchEmployeeTaxRegime")]
        public async Task<ApiResponse> SwitchEmployeeTaxRegime(EmployeeDeclaration employeeDeclaration)
        {
            var result = await _declarationService.SwitchEmployeeTaxRegimeService(employeeDeclaration);
            return BuildResponse(result);
        }

        [HttpDelete("DeleteDeclarationValue/{DeclarationId}/{ComponentId}")]
        public async Task<ApiResponse> DeleteDeclarationValue([FromRoute] long DeclarationId, [FromRoute] string ComponentId)
        {
            var result = await _declarationService.DeleteDeclarationValueService(DeclarationId, ComponentId);
            return BuildResponse(result);
        }

        [HttpDelete("DeleteDeclarationFile/{DeclarationId}/{FileId}/{ComponentId}")]
        public async Task<ApiResponse> DeleteDeclarationFile([FromRoute] long DeclarationId, [FromRoute] int FileId, [FromRoute] string ComponentId)
        {
            var result = await _declarationService.DeleteDeclarationFileService(DeclarationId, FileId, ComponentId);
            return BuildResponse(result);
        }

        [HttpDelete("DeleteDeclaredHRA/{DeclarationId}")]
        public async Task<ApiResponse> DeleteDeclaredHRA([FromRoute] long DeclarationId)
        {
            var result = await _declarationService.DeleteDeclaredHRAService(DeclarationId);
            return BuildResponse(result);
        }

        [HttpPost("PreviousEmployemnt/{EmployeeId}")]
        public async Task<ApiResponse> PreviousEmployemnt([FromRoute] int EmployeeId, [FromBody] List<PreviousEmployementDetail> previousEmployementDetail)
        {
            var result = await _declarationService.ManagePreviousEmployemntService(EmployeeId, previousEmployementDetail);
            return BuildResponse(result);
        }

        [HttpGet("GetPreviousEmployemntandEmp/{EmployeeId}")]
        public async Task<ApiResponse> GetPreviousEmployemntandEmp([FromRoute] int EmployeeId)
        {
            var result = await _declarationService.GetPreviousEmployemntandEmpService(EmployeeId);
            return BuildResponse(result);
        }

        [HttpGet("GetPreviousEmployemnt/{EmployeeId}")]
        public async Task<ApiResponse> GetPreviousEmployemnt([FromRoute] int EmployeeId)
        {
            var result = await _declarationService.GetPreviousEmployemntService(EmployeeId);
            return BuildResponse(result);
        }

        //[HttpGet("EmptyEmpDeclaration")]
        //public async Task<ApiResponse> EmptyEmpDeclaration()
        //{
        //    var result = await _declarationService.EmptyEmpDeclarationService();
        //    return BuildResponse(result);
        //}

        [HttpGet("GetEmployeeIncomeDetail")]
        public async Task<ApiResponse> GetEmployeeIncomeDetail([FromBody] FilterModel filterModel)
        {
            var result = await _declarationService.GetEmployeeIncomeDetailService(filterModel);
            return BuildResponse(result);
        }

        [HttpPost("ExportEmployeeDeclaration")]
        public async Task<ApiResponse> ExportEmployeeDeclaration([FromBody] List<int> EmployeeIds)
        {
            var result = await _declarationService.ExportEmployeeDeclarationService(EmployeeIds);
            return BuildResponse(result);
        }
    }
}
