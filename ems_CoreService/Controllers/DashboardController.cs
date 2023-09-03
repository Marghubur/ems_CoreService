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
        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("GetSystemDashboard")]
        public IResponse<ApiResponse> GetSystemDashboard(AttendenceDetail userDetail)
        {
            var result = _dashboardService.GetSystemDashboardService(userDetail);
            return BuildResponse(result, HttpStatusCode.OK);
        }
    }
}
