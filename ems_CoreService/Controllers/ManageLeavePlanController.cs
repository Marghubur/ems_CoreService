using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ModalLayer.Modal.Leaves;
using ServiceLayer.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManageLeavePlanController : BaseController
    {
        private readonly IManageLeavePlanService _manageLeavePlanService;

        public ManageLeavePlanController(IManageLeavePlanService manageLeavePlanService)
        {
            _manageLeavePlanService = manageLeavePlanService;
        }

        [HttpGet("GetLeavePlanTypeConfiguration/{leavePlanTypeId}")]
        public IResponse<ApiResponse> GetLeavePlanTypeConfigurationDetail(int leavePlanTypeId)
        {
            var result = _manageLeavePlanService.GetLeaveConfigurationDetail(leavePlanTypeId);
            return BuildResponse(result);
        }

        [HttpPut("UpdateLeaveDetail/{leavePlanTypeId}/{leavePlanId}")]
        public IResponse<ApiResponse> UpdateLeaveDetail([FromRoute] int leavePlanTypeId, [FromRoute] int leavePlanId, [FromBody] LeaveDetail leaveDetail)
        {
            var result = _manageLeavePlanService.UpdateLeaveDetail(leavePlanTypeId, leavePlanId, leaveDetail);
            return BuildResponse(result);
        }

        [HttpPut("UpdateLeaveFromManagement/{leavePlanTypeId}/{leavePlanId}")]
        public IResponse<ApiResponse> UpdateLeaveFromManagement([FromRoute] int leavePlanTypeId, [FromRoute] int leavePlanId, [FromBody] ManagementLeave management)
        {
            var result = _manageLeavePlanService.UpdateLeaveFromManagement(leavePlanTypeId, leavePlanId, management);
            return BuildResponse(result);
        }

        [HttpPut("UpdateLeaveAccrual/{leavePlanTypeId}/{leavePlanId}")]
        public IResponse<ApiResponse> UpdateLeaveAccrual([FromRoute] int leavePlanTypeId, [FromRoute] int leavePlanId, [FromBody] LeaveAccrual leaveAccrual)
        {
            var result = _manageLeavePlanService.UpdateLeaveAccrualService(leavePlanTypeId, leavePlanId, leaveAccrual);
            return BuildResponse(result);
        }

        [HttpPut("UpdateApplyForLeave/{leavePlanTypeId}/{leavePlanId}")]
        public IResponse<ApiResponse> UpdateApplyForLeave([FromRoute] int leavePlanTypeId, [FromRoute] int leavePlanId, [FromBody] LeaveApplyDetail leaveApplyDetail)
        {
            var result = _manageLeavePlanService.UpdateApplyForLeaveService(leavePlanTypeId, leavePlanId, leaveApplyDetail);
            return BuildResponse(result);
        }

        [HttpPut("UpdateLeaveRestriction/{leavePlanTypeId}/{leavePlanId}")]
        public IResponse<ApiResponse> UpdateLeaveRestriction([FromRoute] int leavePlanTypeId, [FromRoute] int leavePlanId, [FromBody] LeavePlanRestriction leavePlanRestriction)
        {
            var result = _manageLeavePlanService.UpdateLeaveRestrictionService(leavePlanTypeId, leavePlanId, leavePlanRestriction);
            return BuildResponse(result);
        }

        [HttpPut("UpdateHolidayNWeekOffPlan/{leavePlanTypeId}/{leavePlanId}")]
        public IResponse<ApiResponse> UpdateHolidayNWeekOffPlan([FromRoute] int leavePlanTypeId, [FromRoute] int leavePlanId, [FromBody] LeaveHolidaysAndWeekoff leaveHolidaysAndWeekoff)
        {
            var result = _manageLeavePlanService.UpdateHolidayNWeekOffPlanService(leavePlanTypeId, leavePlanId, leaveHolidaysAndWeekoff);
            return BuildResponse(result);
        }

        [HttpPut("UpdateLeaveApproval/{leavePlanTypeId}/{leavePlanId}")]
        public IResponse<ApiResponse> UpdateLeaveApproval([FromRoute] int leavePlanTypeId, [FromRoute] int leavePlanId, [FromBody] LeaveApproval leaveApproval)
        {
            var result = _manageLeavePlanService.UpdateLeaveApprovalService(leavePlanTypeId, leavePlanId, leaveApproval);
            return BuildResponse(result);
        }

        [HttpPut("UpdateYearEndProcessing/{leavePlanTypeId}/{leavePlanId}")]
        public IResponse<ApiResponse> UpdateYearEndProcessing([FromRoute] int leavePlanTypeId, [FromRoute] int leavePlanId, [FromBody] LeaveEndYearProcessing leaveEndYearProcessing)
        {
            var result = _manageLeavePlanService.UpdateYearEndProcessingService(leavePlanTypeId, leavePlanId, leaveEndYearProcessing);
            return BuildResponse(result);
        }

        [HttpPut("AddUpdateEmpLeavePlan/{leavePlanId}")]
        public async Task<ApiResponse> AddUpdateEmpLeavePlan([FromRoute] int leavePlanId, [FromBody] List<Employee> employees)
        {
            var result = _manageLeavePlanService.AddUpdateEmpLeavePlanService(leavePlanId, employees);
            return BuildResponse(result);
        }

        [HttpGet("GetEmpMappingByLeavePlanId/{leavePlanId}")]
        public IResponse<ApiResponse> GetEmpMappingByLeavePlanId([FromRoute] int leavePlanId)
        {
            var result = _manageLeavePlanService.GetEmpMappingByLeavePlanIdService(leavePlanId);
            return BuildResponse(result);
        }
    }
}
