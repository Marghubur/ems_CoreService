using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ModalLayer.Modal.Leaves;
using OnlineDataBuilder.ContextHandler;
using ServiceLayer.Interface;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class LeaveRequestController : BaseController
    {
        private readonly ILeaveRequestService _requestService;
        public LeaveRequestController(ILeaveRequestService requestService)
        {
            _requestService = requestService;
        }

        [HttpPut("ApprovalAction")]
        public async Task<ApiResponse> ApprovalAction(LeaveRequestDetail leaveRequestDetail)
        {
            var result = await _requestService.ApprovalLeaveService(leaveRequestDetail);
            return BuildResponse(result);
        }

        [HttpPut("RejectAction")]
        public async Task<ApiResponse> RejectAction(LeaveRequestDetail leaveRequestDetail)
        {
            var result = await _requestService.RejectLeaveService(leaveRequestDetail);
            return BuildResponse(result);
        }

        [HttpPut("ReAssigneToOtherManager")]
        public IResponse<ApiResponse> ReAssigneToOtherManager(LeaveRequestNotification approvalRequest)
        {
            var result = _requestService.ReAssigneToOtherManagerService(approvalRequest);
            return BuildResponse(result);
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPut("ApproveLeaveRequest/{filterId}")]
        public async Task<ApiResponse> ApproveLeaveRequest([FromRoute]int filterId, [FromBody]LeaveRequestDetail leaveRequestDetail)
        {
            var result = await _requestService.ApprovalLeaveService(leaveRequestDetail, filterId);
            return BuildResponse(result);
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPut("RejectLeaveRequest/{filterId}")]
        public async Task<ApiResponse> RejectLeaveRequest([FromRoute] int filterId, [FromBody]LeaveRequestDetail leaveRequestDetail)
        {
            var result = await _requestService.RejectLeaveService(leaveRequestDetail, filterId);
            return BuildResponse(result);
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPut("ReAssigneLeaveRequest/{filterId}")]
        public IResponse<ApiResponse> ReAssigneLeaveRequest([FromRoute] int filterId, [FromBody]LeaveRequestNotification approvalRequest)
        {
            var result = _requestService.ReAssigneToOtherManagerService(approvalRequest, filterId);
            return BuildResponse(result);
        }

        [HttpPost("GetLeaveRequestNotification")]
        public async Task<ApiResponse> GetLeaveRequestNotification(LeaveRequestNotification leaveRequestNotification)
        {
            var result = await _requestService.GetLeaveRequestNotificationService(leaveRequestNotification);
            return BuildResponse(result);
        }
    }
}
