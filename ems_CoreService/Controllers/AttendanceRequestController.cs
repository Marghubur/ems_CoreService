using Bot.CoreBottomHalf.CommonModal.API;
using Bot.CoreBottomHalf.CommonModal.Leave;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DailyAttendance = ModalLayer.Modal.DailyAttendance;

namespace OnlineDataBuilder.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceRequestController : BaseController
    {
        private readonly IAttendanceRequestService _requestService;
        public AttendanceRequestController(IAttendanceRequestService requestService)
        {
            _requestService = requestService;
        }

        [HttpGet("GetManagerRequestedData/{employeeId}/{itemStatus}")]
        public IResponse<ApiResponse> FetchPendingRequests(int employeeId, ItemStatus itemStatus)
        {
            try
            {
                var result = _requestService.FetchPendingRequestService(employeeId, itemStatus);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { employeeId = employeeId, ItemStatus = itemStatus });
            }
        }

        [HttpGet("GetAllRequestedData/{employeeId}/{itemStatus}")]
        [Authorize(Roles = Role.Admin)]
        public IResponse<ApiResponse> GetAllRequestedData(int employeeId)
        {
            try
            {
                var result = _requestService.GetManagerAndUnAssignedRequestService(employeeId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, employeeId);
            }
        }

        [HttpPut("ApprovalAction")]
        public async Task<ApiResponse> ApprovalAction(List<DailyAttendance> dailyAttendances)
        {
            try
            {
                var result = await _requestService.ApproveAttendanceService(dailyAttendances);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, dailyAttendances);
            }
        }

        [HttpPut("RejectAction")]
        public async Task<ApiResponse> RejectAction([FromBody] List<DailyAttendance> dailyAttendances)
        {
            try
            {
                var result = await _requestService.RejectAttendanceService(dailyAttendances);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, dailyAttendances);
            }
        }

        [HttpPut("ApproveAttendanceRequest/{filterId}")]
        public async Task<ApiResponse> ApproveAttendanceRequest([FromRoute] int filterId, [FromBody] List<DailyAttendance> dailyAttendances)
        {
            try
            {
                var result = await _requestService.ApproveAttendanceService(dailyAttendances, filterId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { FilterId = filterId, Attendances = dailyAttendances });
            }
        }

        [HttpPut("RejectAttendanceRequest/{filterId}")]
        public async Task<ApiResponse> RejectAttendanceRequest([FromRoute] int filterId, [FromBody] List<DailyAttendance> dailyAttendances)
        {
            try
            {
                var result = await _requestService.RejectAttendanceService(dailyAttendances, filterId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { FilterId = filterId, Attendances = dailyAttendances });
            }
        }

        [HttpPut("ReAssigneAttendanceRequest/{filterId}")]
        public IResponse<ApiResponse> ReAssigneToOtherManager(AttendenceDetail attendanceDetail)
        {
            try
            {
                var result = _requestService.ReAssigneAttendanceService(attendanceDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, attendanceDetail);
            }
        }

        [HttpPost("GetAttendenceRequestData")]
        public async Task<ApiResponse> GetAttendenceRequestData(Attendance attendance)
        {
            try
            {
                var result = await _requestService.GetAttendanceRequestDataService(attendance);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, attendance);
            }
        }
    }
}