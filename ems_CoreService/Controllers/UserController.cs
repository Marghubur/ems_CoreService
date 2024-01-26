using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ModalLayer.Modal.Profile;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : BaseController
    {
        private readonly HttpContext _httpContext;
        private readonly IUserService _userService;
        public UserController(IHttpContextAccessor httpContext, IUserService userService)
        {
            _httpContext = httpContext.HttpContext;
            _userService = userService;
        }

        [HttpPost("UpdateUserProfile/{UserTypeId}")]
        public IResponse<ApiResponse> UpdateUserProfile(ProfessionalUser professionalUser, int UserTypeId)
        {
            var result = _userService.UpdateProfile(professionalUser, UserTypeId);
            return BuildResponse(result);
        }


        [HttpGet("GetUserDetail/{EmployeeId}")]
        public IResponse<ApiResponse> GetUserDetail(long EmployeeId)
        {
            var result = _userService.GetUserDetail(EmployeeId);
            return BuildResponse(result);
        }

        [HttpPost("UploadProfileDetailFile/{userId}/{UserTypeId}")]
        public async Task<ApiResponse> UploadProfileDetailFile(string userId, int UserTypeId)
        {
            StringValues UserInfoData = default(string);
            _httpContext.Request.Form.TryGetValue("userInfo", out UserInfoData);
            if (UserInfoData.Count > 0)
            {
                var userInfo = JsonConvert.DeserializeObject<ProfessionalUser>(UserInfoData);
                IFormFileCollection files = _httpContext.Request.Form.Files;
                var Result = await _userService.UploadUserInfo(userId, userInfo, files, UserTypeId);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            return BuildResponse("No files found", HttpStatusCode.OK);
        }

        [HttpPost("UploadResume/{userId}/{UserTypeId}")]
        public async Task<ApiResponse> UploadResume(string userId, int UserTypeId)
        {
            StringValues UserInfoData = default(string);
            _httpContext.Request.Form.TryGetValue("userInfo", out UserInfoData);
            if (UserInfoData.Count > 0)
            {
                var userInfo = JsonConvert.DeserializeObject<ProfessionalUser>(UserInfoData);
                IFormFileCollection files = _httpContext.Request.Form.Files;
                var Result = await _userService.UploadResume(userId, userInfo, files, UserTypeId);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            return BuildResponse("No files found", HttpStatusCode.OK);
        }

        [HttpGet("GenerateResume/{userId}")]
        public IResponse<ApiResponse> GenerateResume(long userId)
        {
            var result = _userService.GenerateResume(userId);
            return BuildResponse(result);
        }

        [HttpPost("UploadDeclaration/{UserId}/{UserTypeId}")]
        public async Task<ApiResponse> UploadDeclaration(string UserId, int UserTypeId)
        {
            StringValues userDetail = default(string);
            _httpContext.Request.Form.TryGetValue("UserDetail", out userDetail);
            _httpContext.Request.Form.TryGetValue("fileDetail", out StringValues FileData);
            if (userDetail.Count > 0)
            {
                var UserInfo = JsonConvert.DeserializeObject<UserDetail>(userDetail);
                List<Files> files = JsonConvert.DeserializeObject<List<Files>>(FileData);
                IFormFileCollection fileDetail = _httpContext.Request.Form.Files;
                var result = await _userService.UploadDeclaration(UserId, UserTypeId, UserInfo, fileDetail, files);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            return BuildResponse("No files found", HttpStatusCode.OK);
        }

        [HttpGet("GetEmployeeAndChients")]
        public async Task<IResponse<ApiResponse>> GetEmployeeAndChients()
        {
            var result = await _userService.GetEmployeeAndChientListService();
            return BuildResponse(result);
        }
    }
}
