using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using OnlineDataBuilder.ContextHandler;
using ServiceLayer.Interface;
using System.Net;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : BaseController
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILeaveCalculation _leaveCalculation;
        public DashboardController(IDashboardService dashboardService, ILeaveCalculation leaveCalculation)
        {
            _dashboardService = dashboardService;
            _leaveCalculation = leaveCalculation;
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("GetSystemDashboard")]
        public IResponse<ApiResponse> GetSystemDashboard(AttendenceDetail userDetail)
        {
            var result = _dashboardService.GetSystemDashboardService(userDetail);
            return BuildResponse(result, HttpStatusCode.OK);
        }

        [AllowAnonymous]
        [HttpGet("RunAcrrualCycle")]
        public IResponse<ApiResponse> GetSystemDashboard()
        {
            var result = _leaveCalculation.StartAccrualCycle(true);
            return BuildResponse(result, HttpStatusCode.OK);
        }
    }
}
