using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ObjectiveController : BaseController
    {
        private readonly IObjectiveService _objectiveService;

        public ObjectiveController(IObjectiveService objectiveService)
        {
            _objectiveService = objectiveService;
        }

        [HttpPost("ObjectiveInsertUpdate")]
        public IResponse<ApiResponse> ObjectiveInsertUpdate([FromBody] ObjectiveDetail objectiveDetail)
        {
            try
            {
                var result = _objectiveService.ObjectiveInsertUpdateService(objectiveDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, objectiveDetail);
            }
        }

        [HttpPost("GetPerformanceObjective")]
        public IResponse<ApiResponse> GetPerformanceObjective([FromBody] FilterModel filterModel)
        {
            try
            {
                var result = _objectiveService.GetPerformanceObjectiveService(filterModel);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }

        [HttpGet("GetEmployeeObjective/{DesignationId}/{CompanyId}/{EmployeeId}")]
        public IResponse<ApiResponse> GetEmployeeObjective([FromRoute] int DesignationId, [FromRoute] int CompanyId, [FromRoute] long EmployeeId)
        {
            try
            {
                var result = _objectiveService.GetEmployeeObjectiveService(DesignationId, CompanyId, EmployeeId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { DesignationId = DesignationId, CompanyId = CompanyId, EmployeeId = EmployeeId });
            }
        }

        [HttpPost("UpdateEmployeeObjective")]
        public IResponse<ApiResponse> UpdateEmployeeObjective([FromBody] EmployeePerformance employeePerformance)
        {
            try
            {
                var result = _objectiveService.UpdateEmployeeObjectiveService(employeePerformance);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, employeePerformance);
            }
        }
    }
}
