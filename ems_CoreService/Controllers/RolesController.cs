using Bot.CoreBottomHalf.CommonModal.API;
using EMailService.Modal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Net;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Authorize(Role.Admin)]
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : BaseController
    {
        private readonly IRolesAndMenuService _rolesAndMenuService;

        public RolesController(IRolesAndMenuService rolesAndMenuService)
        {
            _rolesAndMenuService = rolesAndMenuService;
        }

        [HttpPost("AddUpdatePermission")]
        public async Task<ApiResponse> AddUpdatePermission(RolesAndMenu rolesAndMenus)
        {
            try
            {
                var result = await _rolesAndMenuService.AddUpdatePermission(rolesAndMenus);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, rolesAndMenus);
            }
        }

        [HttpGet("GetMenu/{AccessLevelId}")]
        public IResponse<ApiResponse> GetsRolesandMenu(int AccessLevelId)
        {
            try
            {
                var result = _rolesAndMenuService.GetsRolesandMenu(AccessLevelId);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, AccessLevelId);
            }
        }

        [HttpGet("GetRoles")]
        public async Task<ApiResponse> GetRoles()
        {
            try
            {
                var result = await  _rolesAndMenuService.GetRoles();
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [HttpPost("AddRole")]
        public async Task<ApiResponse> AddRole(AddRole addRole)
        {
            try
            {
                var result = await _rolesAndMenuService.AddRole(addRole);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, addRole);
            }
        }

        [HttpPost("ManageDefaultReportingManager")]
        public async Task<ApiResponse> ManageDefaultReportingManager([FromBody] DefaultReportingManager defaultReportingManager)
        {
            var result = await _rolesAndMenuService.ManageDefaultReportingManagerService(defaultReportingManager);
            return BuildResponse(result);
        }

        [HttpGet("GetDefaultReportingManager")]
        public async Task<ApiResponse> GetDefaultReportingManager()
        {
            var result = await _rolesAndMenuService.GetDefaultReportingManagerService();
            return BuildResponse(result);
        }
    }
}