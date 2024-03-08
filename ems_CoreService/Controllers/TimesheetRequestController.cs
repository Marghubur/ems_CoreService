using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class TimesheetRequestController : BaseController
    {
        private readonly ITimesheetRequestService _requestService;
        public TimesheetRequestController(ITimesheetRequestService requestService)
        {
            _requestService = requestService;
        }

        [HttpPut("ApproveTimesheet/{TimesheetId}")]
        public async Task<ApiResponse> ApproveTimesheet(int timesheetId)
        {
            try
            {
                var result = await _requestService.ApprovalTimesheetService(timesheetId, null);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, timesheetId);
            }
        }

        [HttpPut("RejectAction/{TimesheetId}")]
        public async Task<ApiResponse> RejectAction(int timesheetId)
        {
            try
            {
                var result = await _requestService.RejectTimesheetService(timesheetId, null);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, timesheetId);
            }
        }

        [HttpPut("ReAssigneTimesheet")]
        public IResponse<ApiResponse> ReAssigneToOtherManager(List<DailyTimesheetDetail> dailyTimesheetDetails)
        {
            try
            {
                var result = _requestService.ReAssigneTimesheetService(dailyTimesheetDetails);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, dailyTimesheetDetails);
            }
        }

        [HttpPut("ApproveTimesheetRequest/{TimesheetId}/{filterId}")]
        public async Task<ApiResponse> ApproveTimesheetRequest([FromRoute] int timesheetId, [FromRoute] int filterId, [FromBody] TimesheetDetail timesheetDetail)
        {
            try
            {
                var result = await _requestService.ApprovalTimesheetService(timesheetId, timesheetDetail, filterId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { TimesheetId = timesheetId, FilterId = filterId, TimesheetDetail = timesheetDetail });
            }
        }

        [HttpPut("RejectTimesheetRequest/{TimesheetId}/{filterId}")]
        public async Task<ApiResponse> RejectTimesheetRequest([FromRoute] int timesheetId, [FromRoute] int filterId, [FromBody] TimesheetDetail timesheetDetail)
        {
            try
            {
                var result = await _requestService.RejectTimesheetService(timesheetId, timesheetDetail, filterId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { TimesheetId = timesheetId, FilterId = filterId, TimesheetDetail = timesheetDetail });
            }
        }

        [HttpPut("ReOpenTimesheetRequest/{TimesheetId}/{filterId}")]
        public async Task<ApiResponse> ReOpenTimesheetRequest([FromRoute] int timesheetId, [FromRoute] int filterId, [FromBody] TimesheetDetail timesheetDetail)
        {
            try
            {
                var result = await _requestService.ReOpenTimesheetRequestService(timesheetId, timesheetDetail, filterId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { TimesheetId = timesheetId, FilterId = filterId, TimesheetDetail = timesheetDetail });
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPut("ReAssigneTimesheetRequest/{filterId}")]
        public IResponse<ApiResponse> ReAssigneTimesheetRequest([FromRoute] int filterId, [FromBody] List<DailyTimesheetDetail> dailyTimesheetDetails)
        {
            try
            {
                var result = _requestService.ReAssigneTimesheetService(dailyTimesheetDetails, filterId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { FilterId = filterId, DailyTimesheetDetails = dailyTimesheetDetails });
            }
        }

        [HttpPost("GetTimesheetRequestData")]
        public async Task<ApiResponse> GetTimesheetRequestData([FromBody] TimesheetDetail timesheetDetail)
        {
            try
            {
                var result = await _requestService.GetTimesheetRequestDataService(timesheetDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, timesheetDetail);
            }
        }
    }
}