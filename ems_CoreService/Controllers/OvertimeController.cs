using Bot.CoreBottomHalf.CommonModal.API;
using EMailService.Modal;
using Microsoft.AspNetCore.Mvc;
using OnlineDataBuilder.Controllers;
using ServiceLayer.Interface;
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
    }
}
