using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
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

        [HttpGet("triggerLeaveAccrual")]
        [Authorize(Roles = Role.Admin)]
        public async Task LeaveAccrualTriggerLeave()
        {
            await _autoTriggerService.RunLeaveAccrualJobAsync();
        }

        [Authorize(Roles = Role.Admin)]
        [HttpGet("triggerWeeklyTimesheet/{weekFirstDate}")]
        public async Task WeeklyTimesheetTrigger(DateTime weekFirstDate)
        {
            await _autoTriggerService.RunTimesheetJobAsync();
        }

        [Authorize(Roles = Role.Admin)]
        [HttpGet("triggerMonthlyPayroll/{forYear}/{forMonth}")]
        public async Task MonthlyPayrollTrigger(int forYear, int forMonth)
        {
            await _autoTriggerService.RunPayrollJobAsync();
        }
    }
}
