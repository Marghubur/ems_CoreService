using Bot.CoreBottomHalf.CommonModal.API;
using Bot.CoreBottomHalf.CommonModal.Leave;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ModalLayer.Modal.Leaves;
using ServiceLayer.Interface;
using System;
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
            try
            {
                var result = await _requestService.ApprovalLeaveService(leaveRequestDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, leaveRequestDetail);
            }
        }

        [HttpPut("RejectAction")]
        public async Task<ApiResponse> RejectAction(LeaveRequestDetail leaveRequestDetail)
        {
            try
            {
                var result = await _requestService.RejectLeaveService(leaveRequestDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, leaveRequestDetail);
            }
        }

        [HttpPut("ReAssigneToOtherManager")]
        public IResponse<ApiResponse> ReAssigneToOtherManager(LeaveRequestNotification approvalRequest)
        {
            try
            {
                var result = _requestService.ReAssigneToOtherManagerService(approvalRequest);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, approvalRequest);
            }
        }

        [HttpPut("ApproveLeaveRequest/{filterId}")]
        public async Task<ApiResponse> ApproveLeaveRequest([FromRoute] int filterId, [FromBody] LeaveRequestDetail leaveRequestDetail)
        {
            try
            {
                var result = await _requestService.ApprovalLeaveService(leaveRequestDetail, filterId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { FilterId = filterId, LeaveRequestDetail = leaveRequestDetail });
            }
        }

        [HttpPut("RejectLeaveRequest/{filterId}")]
        public async Task<ApiResponse> RejectLeaveRequest([FromRoute] int filterId, [FromBody] LeaveRequestDetail leaveRequestDetail)
        {
            try
            {
                var result = await _requestService.RejectLeaveService(leaveRequestDetail, filterId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { FilterId = filterId, LeaveRequestDetail = leaveRequestDetail });
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPut("ReAssigneLeaveRequest/{filterId}")]
        public IResponse<ApiResponse> ReAssigneLeaveRequest([FromRoute] int filterId, [FromBody] LeaveRequestNotification approvalRequest)
        {
            try
            {
                var result = _requestService.ReAssigneToOtherManagerService(approvalRequest, filterId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { FilterId = filterId, ApprovalRequest = approvalRequest });
            }
        }

        [HttpPost("GetLeaveRequestNotification")]
        public async Task<ApiResponse> GetLeaveRequestNotification(LeaveRequestNotification leaveRequestNotification)
        {
            try
            {
                var result = await _requestService.GetLeaveRequestNotificationService(leaveRequestNotification);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, leaveRequestNotification);
            }
        }

        [HttpPost("ConfigPayrollApproveAppliedLeave")]
        public async Task<ApiResponse> ApproveAppliedLeave(LeaveRequestDetail leaveRequestDetail)
        {
            try
            {
                var result = await _requestService.ConfigPayrollApproveAppliedLeaveService(leaveRequestDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, leaveRequestDetail);
            }
        }

        [HttpPost("ConfigPayrollCancelAppliedLeave")]
        public async Task<ApiResponse> CancelAppliedLeave(LeaveRequestDetail leaveRequestDetail)
        {
            try
            {
                var result = await _requestService.ConfigPayrollCancelAppliedLeaveService(leaveRequestDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, leaveRequestDetail);
            }
        }
    }
}