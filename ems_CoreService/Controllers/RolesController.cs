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
        public IResponse<ApiResponse> GetRoles()
        {
            try
            {
                var result = _rolesAndMenuService.GetRoles();
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [HttpPost("AddRole")]
        public IResponse<ApiResponse> AddRole(AddRole addRole)
        {
            try
            {
                var result = _rolesAndMenuService.AddRole(addRole);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, addRole);
            }
        }
    }
}