using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ModalLayer.Modal;
using ModalLayer.Modal.Leaves;
using Newtonsoft.Json;
using OnlineDataBuilder.ContextHandler;
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
            var result = _leaveService.GetLeavePlansService(filterModel);
            return BuildResponse(result);
        }

        [HttpPost("AddLeavePlanType")]
        public IResponse<ApiResponse> AddLeavePlanType(LeavePlanType leavePlanType)
        {
            var result = _leaveService.AddLeavePlanTypeService(leavePlanType);
            return BuildResponse(result);
        }

        [HttpPost("AddLeavePlan")]
        public IResponse<ApiResponse> AddLeavePlans(LeavePlan leavePlan)
        {
            var result = _leaveService.AddLeavePlansService(leavePlan);
            return BuildResponse(result);
        }

        [HttpPost("LeavePlanUpdateTypes/{leavePlanId}")]
        public async Task<ApiResponse> LeavePlanUpdateTypes([FromRoute] int leavePlanId, [FromBody] List<LeavePlanType> leavePlanTypes)
        {
            var result = await _leaveService.LeavePlanUpdateTypes(leavePlanId, leavePlanTypes);
            return BuildResponse(result);
        }

        [HttpPut("UpdateLeavePlanType/{leavePlanTypeId}")]
        public IResponse<ApiResponse> UpdateLeavePlanType([FromRoute] int leavePlanTypeId, [FromBody] LeavePlanType leavePlanType)
        {
            var result = _leaveService.UpdateLeavePlanTypeService(leavePlanTypeId, leavePlanType);
            return BuildResponse(result);
        }

        [HttpPost("AddUpdateLeaveQuota")]
        public IResponse<ApiResponse> AddUpdateLeaveQuota([FromBody] LeaveDetail leaveDetail)
        {
            var result = _leaveService.AddUpdateLeaveQuotaService(leaveDetail);
            return BuildResponse(result);
        }

        [HttpGet("GetLeaveTypeDetailById/{leavePlanTypeId}")]
        public IResponse<ApiResponse> GetLeaveTypeDetailById(int leavePlanTypeId)
        {
            var result = _leaveService.GetLeaveTypeDetailByIdService(leavePlanTypeId);
            return BuildResponse(result);
        }

        [HttpGet("GetLeaveTypeFilter")]
        public IResponse<ApiResponse> GetLeaveTypeFilter()
        {
            var result = _leaveService.GetLeaveTypeFilterService();
            return BuildResponse(result);
        }

        [HttpPut("SetDefaultPlan/{leavePlanId}")]
        public IResponse<ApiResponse> SetDefaultPlan([FromRoute] int leavePlanId, [FromBody] LeavePlan leavePlan)
        {
            var result = _leaveService.SetDefaultPlanService(leavePlanId, leavePlan);
            return BuildResponse(result);
        }

        [HttpPut("LeaveRquestManagerAction/{RequestId}")]
        public IResponse<ApiResponse> LeaveRquestManagerAction([FromRoute] ItemStatus RequestId, LeaveRequestNotification approvalRequest)
        {
            var result = _leaveService.LeaveRquestManagerActionService(approvalRequest, RequestId);
            return BuildResponse(result);
        }

        [HttpPost("ApplyLeave")]
        public async Task<ApiResponse> ApplyLeave()
        {
            StringValues leave = default(string);
            _httpContext.Request.Form.TryGetValue("leave", out leave);
            _httpContext.Request.Form.TryGetValue("fileDetail", out StringValues FileData);
            if (leave.Count > 0)
            {
                var leaveRequestModal = JsonConvert.DeserializeObject<LeaveRequestModal>(leave);
                List<Files> files = JsonConvert.DeserializeObject<List<Files>>(FileData);
                IFormFileCollection fileDetail = _httpContext.Request.Form.Files;
                var result = await _leaveService.ApplyLeaveService(leaveRequestModal, fileDetail, files);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            return BuildResponse("No files found", HttpStatusCode.OK);
        }

        [HttpPost("GetAllLeavesByEmpId")]
        public async Task<ApiResponse> GetAllLeavesByEmpId(LeaveRequestModal leaveRequestModal)
        {
            var result = await _leaveService.GetEmployeeLeaveDetail(leaveRequestModal);
            return BuildResponse(result);
        }

        [HttpPut("UpdateAccrualForEmployee/{EmployeeId}")]
        public async Task<ApiResponse> RunAccrualByEmployee(long EmployeeId)
        {
            await _leaveService.RunAccrualByEmployeeService(EmployeeId);
            return BuildResponse(ApplicationConstants.Successfull);
        }

        [HttpGet("GetLeaveAttachment/{FileIds}")]
        public IResponse<ApiResponse> GetLeaveAttachment([FromRoute] string FileIds)
        {
            var result = _leaveService.GetLeaveAttachmentService(FileIds);
            return BuildResponse(result);
        }

        [HttpPost("GetLeaveAttachByManger")]
        public IResponse<ApiResponse> GetLeaveAttachByManger([FromBody] LeaveRequestNotification leaveRequestNotification)
        {
            var result = _leaveService.GetLeaveAttachByMangerService(leaveRequestNotification);
            return BuildResponse(result);
        }
    }
}
