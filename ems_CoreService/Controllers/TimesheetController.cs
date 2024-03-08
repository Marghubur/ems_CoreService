using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Net;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    public class TimesheetController : BaseController
    {
        private readonly ITimesheetService _timesheetService;
        public TimesheetController(ITimesheetService timesheetService)
        {
            _timesheetService = timesheetService;
        }

        [HttpPost("GetTimesheetByFilter")]
        public IResponse<ApiResponse> GetTimesheetByFilter(TimesheetDetail timesheetDetail)
        {
            try
            {
                var result = _timesheetService.GetTimesheetByFilterService(timesheetDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, timesheetDetail);
            }
        }

        [HttpGet("GetWeekTimesheetData/{TimesheetId}")]
        public async Task<ApiResponse> GetWeekTimesheetData(long TimesheetId)
        {
            try
            {
                var result = await _timesheetService.GetWeekTimesheetDataService(TimesheetId);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, TimesheetId);
            }
        }

        [HttpPost("SaveTimesheet")]
        public async Task<ApiResponse> SaveTimesheet(TimesheetDetail timesheetDetail)
        {
            try
            {
                var result = await _timesheetService.SaveTimesheetService(timesheetDetail);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, timesheetDetail);
            }
        }

        [HttpPost("SubmitTimesheet")]
        public async Task<ApiResponse> SubmitTimesheet(TimesheetDetail timesheetDetail)
        {
            try
            {
                var result = await _timesheetService.SubmitTimesheetService(timesheetDetail);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, timesheetDetail);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("ExecuteActionOnTimesheet")]
        public async Task<ApiResponse> ExecuteActionOnTimesheet(TimesheetDetail timesheetDetail)
        {
            try
            {
                var result = await _timesheetService.ExecuteActionOnTimesheetService(timesheetDetail);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, timesheetDetail);
            }
        }

        [HttpGet("GetPendingTimesheetById/{EmployeeId}/{clientId}")]
        public IResponse<ApiResponse> GetPendingTimesheetById(long employeeId, long clientId)
        {
            try
            {
                var result = _timesheetService.GetPendingTimesheetByIdService(employeeId, clientId);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { EmployeeId = employeeId, ClientId = clientId });
            }
        }

        [HttpPost("GetEmployeeTimeSheet")]
        public IResponse<ApiResponse> GetEmployeeTimeSheet(TimesheetDetail timesheetDetail)
        {
            try
            {
                var result = _timesheetService.GetEmployeeTimeSheetService(timesheetDetail);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, timesheetDetail);
            }
        }

        [HttpPost]
        [Route("EditEmployeeBillDetail")]
        public IResponse<ApiResponse> EditEmployeeBillDetail([FromBody] GenerateBillFileDetail fileDetail)
        {
            try
            {
                var result = _timesheetService.EditEmployeeBillDetailService(fileDetail);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, fileDetail);
            }
        }
    }
}