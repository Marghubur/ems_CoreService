using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System.Threading.Tasks;

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
            var result = _requestService.FetchPendingRequestService(employeeId, itemStatus);
            return BuildResponse(result);
        }

        [HttpGet("GetAllRequestedData/{employeeId}/{itemStatus}")]
        [Authorize(Roles = Role.Admin)]
        public IResponse<ApiResponse> GetAllRequestedData(int employeeId)
        {
            var result = _requestService.GetManagerAndUnAssignedRequestService(employeeId);
            return BuildResponse(result);
        }

        [HttpPut("ApprovalAction")]
        public async Task<ApiResponse> ApprovalAction(Attendance attendanceDetail)
        {
            var result = await _requestService.ApproveAttendanceService(attendanceDetail);
            return BuildResponse(result);
        }

        [HttpPut("RejectAction")]
        public async Task<ApiResponse> RejectAction([FromBody] Attendance attendanceDetail)
        {
            var result = await _requestService.RejectAttendanceService(attendanceDetail);
            return BuildResponse(result);
        }
        
        [HttpPut("ApproveAttendanceRequest/{filterId}")]
        public async Task<ApiResponse> ApproveAttendanceRequest([FromRoute] int filterId, [FromBody] Attendance attendanceDetail)
        {
            var result = await _requestService.ApproveAttendanceService(attendanceDetail, filterId);
            return BuildResponse(result);
        }

        [HttpPut("RejectAttendanceRequest/{filterId}")]
        public async Task<ApiResponse> RejectAttendanceRequest([FromRoute] int filterId, [FromBody] Attendance attendanceDetail)
        {
            var result = await _requestService.RejectAttendanceService(attendanceDetail, filterId);
            return BuildResponse(result);
        }

        [HttpPut("ReAssigneAttendanceRequest/{filterId}")]
        public IResponse<ApiResponse> ReAssigneToOtherManager(AttendenceDetail attendanceDetail)
        {
            var result = _requestService.ReAssigneAttendanceService(attendanceDetail);
            return BuildResponse(result);
        }

        [HttpPost("GetAttendenceRequestData")]
        public async Task<ApiResponse> GetAttendenceRequestData(Attendance attendance)
        {
            var result = await _requestService.GetAttendenceRequestDataServive(attendance);
            return BuildResponse(result);
        }
    }
}
