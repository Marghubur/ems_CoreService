using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System.Net;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Authorize(Role.Admin)]
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : BaseController
    {
        private readonly IAttendanceService _attendanceService;
        private readonly IRolesAndMenuService _rolesAndMenuService;

        public RolesController(IAttendanceService attendanceService, IRolesAndMenuService rolesAndMenuService)
        {
            _attendanceService = attendanceService;
            _rolesAndMenuService = rolesAndMenuService;
        }


        [HttpPost("AddUpdatePermission")]
        public async Task<ApiResponse> AddUpdatePermission(RolesAndMenu rolesAndMenus)
        {
            var result = await _rolesAndMenuService.AddUpdatePermission(rolesAndMenus);
            return BuildResponse(result, HttpStatusCode.OK);
        }

        [HttpGet("GetMenu/{AccessLevelId}")]
        public IResponse<ApiResponse> GetsRolesandMenu(int AccessLevelId)
        {
            var result = _rolesAndMenuService.GetsRolesandMenu(AccessLevelId);
            return BuildResponse(result, HttpStatusCode.OK);
        }

        [HttpGet("GetRoles")]
        public IResponse<ApiResponse> GetRoles()
        {
            var result = _rolesAndMenuService.GetRoles();
            return BuildResponse(result, HttpStatusCode.OK);
        }

        [HttpPost("AddRole")]
        public IResponse<ApiResponse> AddRole(AddRole addRole)
        {
            var result = _rolesAndMenuService.AddRole(addRole);
            return BuildResponse(result, HttpStatusCode.OK);
        }
    }
}