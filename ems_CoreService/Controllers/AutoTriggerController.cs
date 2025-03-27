using Bot.CoreBottomHalf.CommonModal.API;
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

        //[AllowAnonymous]
        [HttpGet("triggerLeaveAccrual")]
        [Authorize(Roles = Role.Admin)]
        public async Task LeaveAccrualTriggerLeave()
        {
            try
            {
                await _autoTriggerService.ExecuteLeaveAccrualJobAsync(null, null);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("triggerWeeklyTimesheet")]
        public async Task<ApiResponse> WeeklyTimesheetTrigger([FromBody] TimesheetDetail timesheetDetail)
        {
            try
            {
                await _autoTriggerService.RunTimesheetJobAsync(null, timesheetDetail.TimesheetStartDate, timesheetDetail.TimesheetEndDate, false);
                return BuildResponse("Timesheet generated successfully", System.Net.HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, timesheetDetail);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpGet("triggerMonthlyPayroll/{forYear}/{forMonth}/{dom}")]
        public async Task MonthlyPayrollTrigger(int forYear, int forMonth, int dom)
        {
            try
            {
                await _autoTriggerService.RunPayrollJobAsync(new DateTime(forYear, forMonth, dom));
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { ForYear = forYear, ForMonth = forMonth, DOM = dom });
            }
        }

        [HttpGet("RunAndBuilEmployeeSalaryAndDeclaration")]
        [AllowAnonymous]
        public async Task RunAndBuilEmployeeSalaryAndDeclaration()
        {
            try
            {
                await _autoTriggerService.RunAndBuilEmployeeSalaryAndDeclaration();
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        //[Authorize(Roles = Role.Admin)]
        //[HttpGet("MonthlyAttendanceTrigger")]
        //public async Task MonthlyAttendanceTrigger()
        //{
        //    try
        //    {
        //        await _autoTriggerService.RunGenerateAttendanceAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw Throw(ex);
        //    }
        //}
    }
}