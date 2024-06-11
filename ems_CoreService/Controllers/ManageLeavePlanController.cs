using Bot.CoreBottomHalf.CommonModal.API;
using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ModalLayer.Modal.Leaves;
using ServiceLayer.Interface;
using System;
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
            try
            {
                var result = _manageLeavePlanService.GetLeaveConfigurationDetail(leavePlanTypeId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, leavePlanTypeId);
            }
        }

        [HttpPut("UpdateLeaveDetail/{leavePlanTypeId}/{leavePlanId}")]
        public IResponse<ApiResponse> UpdateLeaveDetail([FromRoute] int leavePlanTypeId, [FromRoute] int leavePlanId, [FromBody] LeaveDetail leaveDetail)
        {
            try
            {
                var result = _manageLeavePlanService.UpdateLeaveDetail(leavePlanTypeId, leavePlanId, leaveDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { LeavePlanTypeId = leavePlanTypeId, LeavePlanId = leavePlanId, LeaveDetail = leaveDetail });
            }
        }

        [HttpPut("UpdateLeaveFromManagement/{leavePlanTypeId}/{leavePlanId}")]
        public IResponse<ApiResponse> UpdateLeaveFromManagement([FromRoute] int leavePlanTypeId, [FromRoute] int leavePlanId, [FromBody] ManagementLeave management)
        {
            try
            {
                var result = _manageLeavePlanService.UpdateLeaveFromManagement(leavePlanTypeId, leavePlanId, management);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { LeavePlanTypeId = leavePlanTypeId, LeavePlanId = leavePlanId, ManagementLeave = management });
            }
        }

        [HttpPut("UpdateLeaveAccrual/{leavePlanTypeId}/{leavePlanId}")]
        public IResponse<ApiResponse> UpdateLeaveAccrual([FromRoute] int leavePlanTypeId, [FromRoute] int leavePlanId, [FromBody] LeaveAccrual leaveAccrual)
        {
            try
            {
                var result = _manageLeavePlanService.UpdateLeaveAccrualService(leavePlanTypeId, leavePlanId, leaveAccrual);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { LeavePlanTypeId = leavePlanTypeId, LeavePlanId = leavePlanId, LeaveAccrual = leaveAccrual });
            }
        }

        [HttpPut("UpdateApplyForLeave/{leavePlanTypeId}/{leavePlanId}")]
        public IResponse<ApiResponse> UpdateApplyForLeave([FromRoute] int leavePlanTypeId, [FromRoute] int leavePlanId, [FromBody] LeaveApplyDetail leaveApplyDetail)
        {
            try
            {
                var result = _manageLeavePlanService.UpdateApplyForLeaveService(leavePlanTypeId, leavePlanId, leaveApplyDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { LeavePlanTypeId = leavePlanTypeId, LeavePlanId = leavePlanId, LeaveApplyDetail = leaveApplyDetail });
            }
        }

        [HttpPut("UpdateLeaveRestriction/{leavePlanTypeId}/{leavePlanId}")]
        public IResponse<ApiResponse> UpdateLeaveRestriction([FromRoute] int leavePlanTypeId, [FromRoute] int leavePlanId, [FromBody] LeavePlanRestriction leavePlanRestriction)
        {
            try
            {
                var result = _manageLeavePlanService.UpdateLeaveRestrictionService(leavePlanTypeId, leavePlanId, leavePlanRestriction);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { LeavePlanTypeId = leavePlanTypeId, LeavePlanId = leavePlanId, LeavePlanRestriction = leavePlanRestriction });
            }
        }

        [HttpPut("UpdateHolidayNWeekOffPlan/{leavePlanTypeId}/{leavePlanId}")]
        public IResponse<ApiResponse> UpdateHolidayNWeekOffPlan([FromRoute] int leavePlanTypeId, [FromRoute] int leavePlanId, [FromBody] LeaveHolidaysAndWeekoff leaveHolidaysAndWeekoff)
        {
            try
            {
                var result = _manageLeavePlanService.UpdateHolidayNWeekOffPlanService(leavePlanTypeId, leavePlanId, leaveHolidaysAndWeekoff);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { LeavePlanTypeId = leavePlanTypeId, LeavePlanId = leavePlanId, LeaveHolidaysAndWeekoff = leaveHolidaysAndWeekoff });
            }
        }

        [HttpPut("UpdateLeaveApproval/{leavePlanTypeId}/{leavePlanId}")]
        public IResponse<ApiResponse> UpdateLeaveApproval([FromRoute] int leavePlanTypeId, [FromRoute] int leavePlanId, [FromBody] LeaveApproval leaveApproval)
        {
            try
            {
                var result = _manageLeavePlanService.UpdateLeaveApprovalService(leavePlanTypeId, leavePlanId, leaveApproval);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { LeavePlanTypeId = leavePlanTypeId, LeavePlanId = leavePlanId, LeaveApproval = leaveApproval });
            }
        }

        [HttpPut("UpdateYearEndProcessing/{leavePlanTypeId}/{leavePlanId}")]
        public IResponse<ApiResponse> UpdateYearEndProcessing([FromRoute] int leavePlanTypeId, [FromRoute] int leavePlanId, [FromBody] LeaveEndYearProcessing leaveEndYearProcessing)
        {
            try
            {
                var result = _manageLeavePlanService.UpdateYearEndProcessingService(leavePlanTypeId, leavePlanId, leaveEndYearProcessing);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { LeavePlanTypeId = leavePlanTypeId, LeavePlanId = leavePlanId, LeaveEndYearProcessing = leaveEndYearProcessing });
            }
        }

        [HttpPut("AddUpdateEmpLeavePlan/{leavePlanId}")]
        public async Task<ApiResponse> AddUpdateEmpLeavePlan([FromRoute] int leavePlanId, [FromBody] List<Employee> employees)
        {
            try
            {
                var result = await _manageLeavePlanService.AddUpdateEmpLeavePlanService(leavePlanId, employees);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { LeavePlanId = leavePlanId, Employees = employees });
            }
        }

        [HttpGet("GetEmpMappingByLeavePlanId/{leavePlanId}")]
        public IResponse<ApiResponse> GetEmpMappingByLeavePlanId([FromRoute] int leavePlanId)
        {
            try
            {
                var result = _manageLeavePlanService.GetEmpMappingByLeavePlanIdService(leavePlanId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, leavePlanId);
            }
        }
    }
}