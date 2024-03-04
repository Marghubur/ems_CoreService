using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using OnlineDataBuilder.ContextHandler;
using OnlineDataBuilder.Controllers;
using ServiceLayer.Interface;
using System;
using System.Threading.Tasks;

namespace ems_CoreService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AutoTriggerController : BaseController
    {
        private readonly IAutoTriggerService _autoTriggerService;

        public AutoTriggerController(IAutoTriggerService autoTriggerService)
        {
            _autoTriggerService = autoTriggerService;
        }

        [AllowAnonymous]
        [HttpGet("triggerLeaveAccrual")]
        // [Authorize(Roles = Role.Admin)]
        public async Task LeaveAccrualTriggerLeave()
        {
            await _autoTriggerService.ExecuteLeaveAccrualJobAsync(null, null);
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("triggerWeeklyTimesheet")]
        public async Task<ApiResponse> WeeklyTimesheetTrigger([FromBody] TimesheetDetail timesheetDetail)
        {
            await _autoTriggerService.RunTimesheetJobAsync(null, timesheetDetail.TimesheetStartDate, timesheetDetail.TimesheetEndDate, false);
            return BuildResponse("Timesheet generated successfully", System.Net.HttpStatusCode.OK);
        }

        [Authorize(Roles = Role.Admin)]
        [HttpGet("triggerMonthlyPayroll/{forYear}/{forMonth}/{dom}")]
        public async Task MonthlyPayrollTrigger(int forYear, int forMonth, int dom)
        {
            await _autoTriggerService.RunPayrollJobAsync(new DateTime(forYear, forMonth, dom));
        }
    }
}