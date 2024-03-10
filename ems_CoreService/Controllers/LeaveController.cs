using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.API;
using EMailService.Modal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ModalLayer.Modal;
using ModalLayer.Modal.Leaves;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaveController : BaseController
    {
        private readonly ILeaveService _leaveService;
        private readonly HttpContext _httpContext;
        public LeaveController(ILeaveService leaveService, IHttpContextAccessor httpContext)
        {
            _leaveService = leaveService;
            _httpContext = httpContext.HttpContext;
        }

        [AllowAnonymous]
        [HttpGet("test/income")]
        public async Task<IEnumerable<WeatherForecast>> GetTest()
        {
            string[] Summaries = new[]
            {
                "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
            };
            var rng = new Random();
            var result = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();

            return await Task.FromResult(result);
        }

        [HttpPost("GetLeavePlans")]
        public IResponse<ApiResponse> GetLeavePlans(FilterModel filterModel)
        {
            try
            {
                var result = _leaveService.GetLeavePlansService(filterModel);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }

        [HttpPost("AddLeavePlanType")]
        public IResponse<ApiResponse> AddLeavePlanType(LeavePlanType leavePlanType)
        {
            try
            {
                var result = _leaveService.AddLeavePlanTypeService(leavePlanType);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, leavePlanType);
            }
        }

        [HttpPost("AddLeavePlan")]
        public IResponse<ApiResponse> AddLeavePlans(LeavePlan leavePlan)
        {
            try
            {
                var result = _leaveService.AddLeavePlansService(leavePlan);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, leavePlan);
            }
        }

        [HttpPost("LeavePlanUpdateTypes/{leavePlanId}")]
        public async Task<ApiResponse> LeavePlanUpdateTypes([FromRoute] int leavePlanId, [FromBody] List<int> LeavePlanTypeId)
        {
            try
            {
                var result = await _leaveService.LeavePlanUpdateTypes(leavePlanId, LeavePlanTypeId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { LeavePlanId = leavePlanId, LeavePlanTypeId = LeavePlanTypeId });
            }
        }

        [HttpPut("UpdateLeavePlanType/{leavePlanTypeId}")]
        public IResponse<ApiResponse> UpdateLeavePlanType([FromRoute] int leavePlanTypeId, [FromBody] LeavePlanType leavePlanType)
        {
            try
            {
                var result = _leaveService.UpdateLeavePlanTypeService(leavePlanTypeId, leavePlanType);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, leavePlanType);
            }
        }

        [HttpPost("AddUpdateLeaveQuota")]
        public IResponse<ApiResponse> AddUpdateLeaveQuota([FromBody] LeaveDetail leaveDetail)
        {
            try
            {
                var result = _leaveService.AddUpdateLeaveQuotaService(leaveDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, leaveDetail);
            }
        }

        [HttpGet("GetLeaveTypeDetailById/{leavePlanTypeId}")]
        public IResponse<ApiResponse> GetLeaveTypeDetailById(int leavePlanTypeId)
        {
            try
            {
                var result = _leaveService.GetLeaveTypeDetailByIdService(leavePlanTypeId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, leavePlanTypeId);
            }
        }

        [HttpGet("GetLeaveTypeFilter")]
        public IResponse<ApiResponse> GetLeaveTypeFilter()
        {
            try
            {
                var result = _leaveService.GetLeaveTypeFilterService();
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [HttpPut("SetDefaultPlan/{leavePlanId}")]
        public IResponse<ApiResponse> SetDefaultPlan([FromRoute] int leavePlanId, [FromBody] LeavePlan leavePlan)
        {
            try
            {
                var result = _leaveService.SetDefaultPlanService(leavePlanId, leavePlan);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { LeavePlanId = leavePlanId, LeavePlan = leavePlan });
            }
        }

        [HttpPut("LeaveRquestManagerAction/{RequestId}")]
        public IResponse<ApiResponse> LeaveRquestManagerAction([FromRoute] ItemStatus RequestId, LeaveRequestNotification approvalRequest)
        {
            try
            {
                var result = _leaveService.LeaveRquestManagerActionService(approvalRequest, RequestId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { ItemStatus = RequestId, LeaveRequestNotification = approvalRequest });
            }
        }

        [HttpPost("ApplyLeave")]
        public async Task<ApiResponse> ApplyLeave()
        {
            LeaveRequestModal leaveRequestModal = null;

            try
            {
                StringValues leave = default(string);
                _httpContext.Request.Form.TryGetValue("leave", out leave);
                _httpContext.Request.Form.TryGetValue("fileDetail", out StringValues FileData);
                if (leave.Count > 0)
                {
                    leaveRequestModal = JsonConvert.DeserializeObject<LeaveRequestModal>(leave);
                    List<Files> files = JsonConvert.DeserializeObject<List<Files>>(FileData);

                    IFormFileCollection fileDetail = _httpContext.Request.Form.Files;
                    var result = await _leaveService.ApplyLeaveService(leaveRequestModal, fileDetail, files);
                    return BuildResponse(result, HttpStatusCode.OK);
                }

                return BuildResponse("No files found", HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, leaveRequestModal);
            }
        }

        [HttpPost("GetAllLeavesByEmpId")]
        public async Task<ApiResponse> GetAllLeavesByEmpId(LeaveRequestModal leaveRequestModal)
        {
            try
            {
                var result = await _leaveService.GetEmployeeLeaveDetail(leaveRequestModal);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, leaveRequestModal);
            }
        }

        [HttpPut("UpdateAccrualForEmployee/{EmployeeId}")]
        public async Task<ApiResponse> RunAccrualByEmployee(long EmployeeId)
        {
            try
            {
                await _leaveService.RunAccrualByEmployeeService(EmployeeId);
                return BuildResponse(ApplicationConstants.Successfull);
            }
            catch (Exception ex)
            {
                throw Throw(ex, EmployeeId);
            }
        }

        [HttpGet("GetLeaveAttachment/{FileIds}")]
        public IResponse<ApiResponse> GetLeaveAttachment([FromRoute] string FileIds)
        {
            try
            {
                var result = _leaveService.GetLeaveAttachmentService(FileIds);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, FileIds);
            }
        }

        [HttpPost("GetLeaveAttachByManger")]
        public IResponse<ApiResponse> GetLeaveAttachByManger([FromBody] LeaveRequestNotification leaveRequestNotification)
        {
            try
            {
                var result = _leaveService.GetLeaveAttachByMangerService(leaveRequestNotification);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, leaveRequestNotification);
            }
        }

        [HttpGet("GetLeaveDetailByEmpId/{EmployeeId}")]
        public IResponse<ApiResponse> GetLeaveDetailByEmpId([FromRoute] long EmployeeId)
        {
            try
            {
                var result = _leaveService.GetLeaveDetailByEmpIdService(EmployeeId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, EmployeeId);
            }
        }

        [HttpPost("AdjustLOPAsLeave")]
        public async Task<ApiResponse> AdjustLOPAsLeave(LOPAdjustmentDetail lOPAdjustmentDetail)
        {
            try
            {
                var result = await _leaveService.AdjustLOPAsLeaveService(lOPAdjustmentDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, lOPAdjustmentDetail);
            }
        }
    }
}