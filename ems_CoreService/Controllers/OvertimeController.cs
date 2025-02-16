using Bot.CoreBottomHalf.CommonModal.API;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using EMailService.Modal;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using OnlineDataBuilder.Controllers;
using ServiceLayer.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ems_CoreService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OvertimeController(IOvertimeService _overtimeService) : BaseController
    {
        [HttpPost("ManageOvertimeConfig")]
        public async Task<ApiResponse> ManageOvertimeConfig([FromBody] OvertimeConfiguration overtimeDetail)
        {
            var result = await _overtimeService.ManageOvertimeConfigService(overtimeDetail);
            return BuildResponse(result);
        }

        [HttpGet("GetOvertimeTypeAndConfig")]
        public async Task<ApiResponse> GetOvertimeTypeAndConfig()
        {
            var result = await _overtimeService.GetOvertimeTypeAndConfigService();
            return BuildResponse(result);
        }

        [HttpGet("GetEmployeeOvertime")]
        public async Task<ApiResponse> GetEmployeeOvertime()
        {
            var result = await _overtimeService.GetEmployeeOvertimeService();
            return BuildResponse(result);
        }

        [HttpPost("ApplyOvertime")]
        public async Task<ApiResponse> ApplyOvertime([FromBody] EmployeeOvertime employeeOvertime)
        {
            var result = await _overtimeService.ApplyOvertimeService(employeeOvertime);
            return BuildResponse(result);
        }

        [HttpPost("GetEmployeeOTByManger")]
        public async Task<ApiResponse> GetEmployeeOTByManger([FromBody] FilterModel filterModel)
        {
            var result = await _overtimeService.GetEmployeeOTByMangerService(filterModel);
            return BuildResponse(result);
        }

        [HttpPost("ApproveEmployeeOvertime")]
        public async Task<ApiResponse> ApproveEmployeeOvertime([FromBody] List<EmployeeOvertime> employeeOvertimes)
        {
            var result = await _overtimeService.ApproveEmployeeOvertimeService(employeeOvertimes);
            return BuildResponse(result);
        }

        [HttpPost("RejectEmployeeOvertime")]
        public async Task<ApiResponse> RejectEmployeeOvertime([FromBody] List<EmployeeOvertime> employeeOvertimes)
        {
            var result = await _overtimeService.RejectEmployeeOvertimeService(employeeOvertimes);
            return BuildResponse(result);
        }
    }
}
