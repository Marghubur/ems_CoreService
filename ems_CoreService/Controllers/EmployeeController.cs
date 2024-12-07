using Bot.CoreBottomHalf.CommonModal.API;
using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Net;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeController : BaseController
    {
        private readonly IEmployeeService _employeeService;
        private readonly HttpContext _httpContext;
        private ILogger<EmployeeController> _logger;

        public EmployeeController(ILogger<EmployeeController> logger, IEmployeeService employeeService, IHttpContextAccessor httpContext)
        {
            _logger = logger;
            _employeeService = employeeService;
            _httpContext = httpContext.HttpContext;
        }

        #region Get Active and De-Active and Get All employees and Manage employee mapped clients

        [HttpPost]
        [Route("GetEmployees")]
        public ApiResponse GetEmployees([FromBody] FilterModel filterModel)
        {
            try
            {
                var Result = _employeeService.GetEmployees(filterModel);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }

        [HttpGet]
        [Route("GetAllManageEmployeeDetail/{EmployeeId}")]
        public ApiResponse GetAllManageEmployeeDetail(long EmployeeId)
        {
            try
            {
                var Result = _employeeService.GetManageEmployeeDetailService(EmployeeId);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, EmployeeId);
            }
        }

        [HttpGet]
        [Route("GetManageClient/{EmployeeId}")]
        public ApiResponse GetManageClient(long EmployeeId)
        {
            try
            {
                var Result = _employeeService.GetManageClientService(EmployeeId);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, EmployeeId);
            }
        }

        [HttpPost]
        [Authorize(Roles = Role.Admin)]
        [Route("UpdateEmployeeMappedClientDetail/{IsUpdating}")]
        public ApiResponse UpdateEmployeeMappedClientDetail([FromBody] EmployeeMappedClient employeeMappedClient, bool IsUpdating)
        {
            try
            {
                var Result = _employeeService.UpdateEmployeeMappedClientDetailService(employeeMappedClient, IsUpdating);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { EmployeeMappedClient = employeeMappedClient, IsUpdating = IsUpdating });
            }
        }

        [HttpGet("GetEmployeeById/{EmployeeId}/{IsActive}")]
        public ApiResponse GetEmployeeById(int EmployeeId, int IsActive)
        {
            try
            {
                var Result = _employeeService.GetEmployeeByIdService(EmployeeId, IsActive);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { EmployeeId = EmployeeId, IsActive = IsActive });
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpDelete("ActivateOrDeActiveEmployee/{EmployeeId}/{IsActive}")]
        public ApiResponse DeleteEmployeeById(int EmployeeId, bool IsActive)
        {
            try
            {
                var Result = _employeeService.ActivateOrDeActiveEmployeeService(EmployeeId, IsActive);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { EmployeeId, IsActive });
            }
        }

        #endregion

        #region Employee insert or update

        [Authorize(Roles = Role.Admin)]
        [HttpPost("employeeregistration")]
        public async Task<ApiResponse> EmployeeRegistration()
        {
            try
            {
                StringValues UserInfoData = default(string);
                _httpContext.Request.Form.TryGetValue("employeeDetail", out UserInfoData);
                if (UserInfoData.Count > 0)
                {
                    _logger.LogInformation("Starting method: employeeregistration controller");
                    Employee employee = JsonConvert.DeserializeObject<Employee>(UserInfoData);
                    _logger.LogInformation("Employee converted");
                    IFormFileCollection files = _httpContext.Request.Form.Files;
                    _logger.LogInformation("Employee file converted");
                    var resetSet = await _employeeService.RegisterEmployeeService(employee, files);
                    return BuildResponse(resetSet);
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

        [Authorize(Roles = Role.Admin)]
        [HttpPost("updateemployeedetail")]
        public async Task<ApiResponse> UpdateEmployeeDetail()
        {
            try
            {
                StringValues UserInfoData = default(string);
                _httpContext.Request.Form.TryGetValue("employeeDetail", out UserInfoData);
                if (UserInfoData.Count > 0)
                {
                    Employee employee = JsonConvert.DeserializeObject<Employee>(UserInfoData);
                    IFormFileCollection files = _httpContext.Request.Form.Files;
                    var resetSet = await _employeeService.UpdateEmployeeService(employee, files);
                    return BuildResponse(resetSet);
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

        #endregion

        #region Generate offer letter

        [Authorize(Roles = Role.Admin)]
        [HttpPost("GenerateOfferLetter")]
        public async Task<ApiResponse> GenerateOfferLetter(EmployeeOfferLetter employeeOfferLetter)
        {
            try
            {
                var result = await _employeeService.GenerateOfferLetterService(employeeOfferLetter);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, employeeOfferLetter);
            }
        }

        #endregion

        #region Emport employee detail in excel

        [Authorize(Roles = Role.Admin)]
        [HttpGet("ExportEmployee/{CompanyId}/{FileType}")]
        public async Task<ApiResponse> ExportEmployee([FromRoute] int CompanyId, [FromRoute] int FileType)
        {
            try
            {
                var result = await _employeeService.ExportEmployeeService(CompanyId, FileType);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { CompanyId = CompanyId, FileType = FileType });
            }
        }

        #endregion

        #region Insert employee record by using excel

        [Authorize(Roles = Role.Admin)]
        [HttpPost("UploadEmployeeExcel")]
        public async Task<ApiResponse> UploadEmployeeExcel()
        {
            try
            {
                IFormFileCollection file = _httpContext.Request.Form.Files;
                await _employeeService.ReadEmployeeDataService(file);
                return BuildResponse("file found");
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        #endregion

        #region Employee resignation

        [HttpGet("GetEmployeeResignationById/{EmployeeId}")]
        public async Task<ApiResponse> GetEmployeeResignationById(long EmployeeId)
        {
            try
            {
                var Result = await _employeeService.GetEmployeeResignationByIdService(EmployeeId);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, EmployeeId);
            }
        }

        [HttpPost("SubmitResignation")]
        public async Task<ApiResponse> SubmitResignation([FromBody] EmployeeNoticePeriod employeeNoticePeriod)
        {
            try
            {
                var Result = await _employeeService.SubmitResignationService(employeeNoticePeriod);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, employeeNoticePeriod);
            }
        }

        [HttpPost("ManageInitiateExist")]
        public async Task<ApiResponse> ManageInitiateExist([FromBody] EmployeeNoticePeriod employeeNoticePeriod)
        {
            try
            {
                var Result = await _employeeService.ManageInitiateExistService(employeeNoticePeriod);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, employeeNoticePeriod);
            }
        }

        #endregion

        #region Un-used code

        //[HttpPost]
        //[Route("EmployeesListData")]
        //public ApiResponse EmployeesListData([FromRoute] FilterModel filterModel)
        //{
        //    try
        //    {
        //        var Result = _employeeService.EmployeesListDataService(filterModel);
        //        return BuildResponse(Result, HttpStatusCode.OK);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw Throw(ex, filterModel);
        //    }
        //}

        //[HttpGet]
        //[Route("GetManageEmployeeDetail/{EmployeeId}")]
        //public ApiResponse GetManageEmployeeDetail(long EmployeeId)
        //{
        //    try
        //    {
        //        var Result = _employeeService.GetEmployeeLeaveDetailService(EmployeeId);
        //        return BuildResponse(Result, HttpStatusCode.OK);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw Throw(ex, EmployeeId);
        //    }
        //}

        //[HttpGet]
        //[Route("LoadMappedClients/{EmployeeId}")]
        //public ApiResponse LoadMappedClients(long EmployeeId)
        //{
        //    try
        //    {
        //        var Result = _employeeService.LoadMappedClientService(EmployeeId);
        //        return BuildResponse(Result, HttpStatusCode.OK);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw Throw(ex, EmployeeId);
        //    }
        //}

        #endregion
    }
}