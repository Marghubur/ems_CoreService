﻿using Bot.CoreBottomHalf.CommonModal.API;
using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using EMailService.Modal.EmployeeModal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
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

        public EmployeeController(IEmployeeService employeeService, IHttpContextAccessor httpContext)
        {
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
                    Employee employee = JsonConvert.DeserializeObject<Employee>(UserInfoData);
                    IFormFileCollection files = _httpContext.Request.Form.Files;
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

        [Authorize(Roles = Role.Admin)]
        [HttpPost("ManageEmployeeBasicInfo")]
        public async Task<ApiResponse> ManageEmployeeBasicInfo()
        {
            try
            {
                StringValues UserInfoData = default(string);
                _httpContext.Request.Form.TryGetValue("employeeDetail", out UserInfoData);
                if (UserInfoData.Count > 0)
                {
                    EmployeeBasicInfo employee = JsonConvert.DeserializeObject<EmployeeBasicInfo>(UserInfoData);
                    IFormFileCollection files = _httpContext.Request.Form.Files;
                    var resetSet = await _employeeService.ManageEmployeeBasicInfoService(employee, files);
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
        [HttpPost("ManageEmpPerosnalDetail")]
        public async Task<ApiResponse> ManageEmpPerosnalDetail([FromBody] EmpPersonalDetail empPersonalDetail)
        {
            try
            {
                var result = await _employeeService.ManageEmpPerosnalDetailService(empPersonalDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("ManageEmpAddressDetail")]
        public async Task<ApiResponse> ManageEmpAddressDetail([FromBody] EmployeeAddressDetail employeeAddressDetail)
        {
            try
            {
                var result = await _employeeService.ManageEmpAddressDetailService(employeeAddressDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("ManageEmpProfessionalDetail")]
        public async Task<ApiResponse> ManageEmpProfessionalDetail([FromBody] EmployeeProfessionalDetail employeeProfessionalDetail)
        {
            try
            {
                var result = await _employeeService.ManageEmpProfessionalDetailService(employeeProfessionalDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("ManageEmpPrevEmploymentDetail")]
        public async Task<ApiResponse> ManageEmpPrevEmploymentDetail([FromBody] PrevEmploymentDetail prevEmploymentDetail)
        {
            try
            {
                var result = await _employeeService.ManageEmpPrevEmploymentDetailService(prevEmploymentDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("ManageEmpBackgroundVerificationDetail")]
        public async Task<ApiResponse> ManageEmpBackgroundVerificationDetail([FromBody] EmployeeBackgroundVerification employeeBackgroundVerification)
        {
            try
            {
                var result = await _employeeService.ManageEmpBackgroundVerificationDetailService(employeeBackgroundVerification);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("ManageEmpNomineeDetail")]
        public async Task<ApiResponse> ManageEmpNomineeDetail([FromBody] EmployeeNomineeDetail employeeNomineeDetail)
        {
            try
            {
                var result = await _employeeService.ManageEmpNomineeDetailService(employeeNomineeDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpGet("GetEmployeesRecordHealthStatus")]
        public async Task<ApiResponse> GetEmployeesRecordHealthStatus()
        {
            try
            {
                var result = await _employeeService.GetEmployeesRecordHealthStatusService();
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("FixEmployeesRecordHealthStatus")]
        public async Task<ApiResponse> FixEmployeesRecordHealthStatus([FromBody] List<long> employeeIds)
        {
            try
            {
                var result = await _employeeService.FixEmployeesRecordHealthStatusService(employeeIds);
                return BuildResponse(result);
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
        [HttpPost("ExportEmployee/{CompanyId}")]
        public async Task<IActionResult> ExportEmployee([FromRoute] int CompanyId, [FromBody] int FileType)
        {
            try
            {
                var result = await _employeeService.ExportEmployeeService(CompanyId, FileType);
                return File(result, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "EmployeeDatq.xlsx");
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { CompanyId = CompanyId, FileType = FileType });
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("ExportEmployeeWithData")]
        public async Task<IActionResult> ExportEmployeeWithData([FromBody] int sheetName)
        {
            try
            {
                var result = await _employeeService.ExportEmployeeWithDataService();
                return File(result, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "EmployeeWithData.xlsx");
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("ExportEmployeeSkeletonExcel")]
        public async Task<IActionResult> ExportEmployeeSkeletonExcel([FromBody] int sheetName)
        {
            try
            {
                var result = await _employeeService.ExportEmployeeSkeletonExcelService();
                return File(result, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "EmployeeWithData.xlsx");
            }
            catch (Exception ex)
            {
                throw Throw(ex);
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
                var result = await _employeeService.ReadEmployeeDataService(file);
                return BuildResponse(result);
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

        [Authorize(Roles = Role.Admin)]
        [HttpGet("GetEmployeeUploadErrorLogs")]
        public async Task<ApiResponse> GetEmployeeUploadErrorLogs()
        {
            try
            {
                var result = await _employeeService.GetEmployeeUploadErrorLogsService();
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [HttpGet]
        [Route("DeActiveEmployee/{employeeId}")]
        public async Task<ApiResponse> DeActiveEmployee([FromRoute] long employeeId)
        {
            try
            {
                var Result = await _employeeService.DeActiveEmployeeService(employeeId);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, employeeId);
            }
        }

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