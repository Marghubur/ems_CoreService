using Bot.CoreBottomHalf.CommonModal.API;
using EMailService.Modal;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using OnlineDataBuilder.Controllers;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace ems_CoreService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResignationController(IResignationService _resignationService) : BaseController
    {
        [HttpGet("GetEmployeeResignationById/{EmployeeId}")]
        public async Task<ApiResponse> GetEmployeeResignationById(long EmployeeId)
        {
            try
            {
                var Result = await _resignationService.GetEmployeeResignationByIdService(EmployeeId);
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
                var Result = await _resignationService.SubmitResignationService(employeeNoticePeriod);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, employeeNoticePeriod);
            }
        }

        [HttpPost("GetAllEmployeeResignation")]
        public async Task<ApiResponse> GetAllEmployeeResignation([FromBody] FilterModel filterModel)
        {
            try
            {
                var Result = await _resignationService.GetAllEmployeeResignationService(filterModel);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [HttpGet("GetEmployeeAsetsAllocationByEmpId/{employeeId}")]
        public async Task<ApiResponse> GetEmployeeAsetsAllocationByEmpId([FromRoute]long employeeId)
        {
            try
            {
                var Result = await _resignationService.GetEmployeeAssetsAllocationByEmpIdService(employeeId);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, employeeId);
            }
        }

        [HttpPut("ApproveEmployeeAssetsAllocation/{employeeId}")]
        public async Task<ApiResponse> ApproveEmployeeAssetsAllocation([FromRoute] long employeeId, [FromBody] List<EmployeeAssetsAllocation> employeeAssetsAllocations)
        {
            try
            {
                var Result = await _resignationService.ApproveEmployeeAssetsAllocationService(employeeAssetsAllocations, employeeId);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, employeeId);
            }
        }

        [HttpPost("ApproveEmployeeResignation/{employeeId}")]
        public async Task<ApiResponse> ApproveEmployeeResignation([FromRoute] long employeeId, [FromBody] StringRequest stringRequest)
        {
            try
            {
                var Result = await _resignationService.ApproveEmployeeResignationService(employeeId, stringRequest.Content);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, employeeId);
            }
        }

        [HttpPost("RejectEmployeeResignation/{employeeId}")]
        public async Task<ApiResponse> RejectEmployeeResignation([FromRoute] long employeeId, [FromBody] StringRequest stringRequest)
        {
            try
            {
                var Result = await _resignationService.RejectEmployeeResignationService(employeeId, stringRequest.Content);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, employeeId);
            }
        }

        [HttpGet("EmployeeResignAssignToMe/{employeeId}")]
        public async Task<ApiResponse> EmployeeResignAssignToMe([FromRoute] long employeeId)
        {
            try
            {
                var Result = await _resignationService.EmployeeResignAssignToMeService(employeeId);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, employeeId);
            }
        }

        [HttpPost("ManageEmployeeExitConfiguration")]
        public async Task<ApiResponse> ManageEmployeeExitConfiguration([FromBody] List<EmployeeExitConfiguration> employeeExitConfigurations)
        {
            try
            {
                var Result = await _resignationService.ManageEmployeeExitConfigurationService(employeeExitConfigurations);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, employeeExitConfigurations);
            }
        }
    }
}