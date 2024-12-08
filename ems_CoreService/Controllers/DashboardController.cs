using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Net;
using System.Threading.Tasks;

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
        public async Task<ApiResponse> GetSystemDashboard(AttendenceDetail userDetail)
        {
            try
            {                
                userDetail = null;
                userDetail.AdminId = 9;
                var result = await _dashboardService.GetSystemDashboardService(userDetail);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, userDetail);
            }
        }
    }
}
