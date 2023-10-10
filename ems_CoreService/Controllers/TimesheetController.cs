using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using OnlineDataBuilder.ContextHandler;
using ServiceLayer.Interface;
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
            var result = _timesheetService.GetTimesheetByFilterService(timesheetDetail);
            return BuildResponse(result);
        }

        [HttpPost("GetWeekTimesheetData")]
        public async Task<ApiResponse> GetWeekTimesheetData(TimesheetDetail attendenceDetail)
        {
            var result = await _timesheetService.GetWeekTimesheetDataService(attendenceDetail);
            return BuildResponse(result, HttpStatusCode.OK);
        }

        [HttpPost("SaveTimesheet")]
        public async Task<ApiResponse> SaveTimesheet(TimesheetDetail timesheetDetail)
        {
            var result = await _timesheetService.SaveTimesheetService(timesheetDetail);
            return BuildResponse(result, HttpStatusCode.OK);
        }

        [HttpPost("SubmitTimesheet")]
        public async Task<ApiResponse> SubmitTimesheet(TimesheetDetail timesheetDetail)
        {
            var result = await _timesheetService.SubmitTimesheetService(timesheetDetail);
            return BuildResponse(result, HttpStatusCode.OK);
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("ExecuteActionOnTimesheet")]
        public async Task<ApiResponse> ExecuteActionOnTimesheet(TimesheetDetail timesheetDetail)
        {
            var result = await _timesheetService.ExecuteActionOnTimesheetService(timesheetDetail);
            return BuildResponse(result, HttpStatusCode.OK);
        }


        [HttpGet("GetPendingTimesheetById/{EmployeeId}/{clientId}")]
        public IResponse<ApiResponse> GetPendingTimesheetById(long employeeId, long clientId)
        {
            var result = _timesheetService.GetPendingTimesheetByIdService(employeeId, clientId);
            return BuildResponse(result, HttpStatusCode.OK);
        }

        [HttpPost("GetEmployeeTimeSheet")]
        public IResponse<ApiResponse> GetEmployeeTimeSheet(TimesheetDetail timesheetDetail)
        {
            var result = _timesheetService.GetEmployeeTimeSheetService(timesheetDetail);
            return BuildResponse(result, HttpStatusCode.OK);
        }

        [HttpPost]
        [Route("EditEmployeeBillDetail")]
        public IResponse<ApiResponse> EditEmployeeBillDetail([FromBody] GenerateBillFileDetail fileDetail)
        {
            var result = _timesheetService.EditEmployeeBillDetailService(fileDetail);
            return BuildResponse(result, HttpStatusCode.OK);
        }
    }
}
