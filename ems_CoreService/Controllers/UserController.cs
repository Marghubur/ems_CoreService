using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ModalLayer.Modal.Profile;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
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
            try
            {
                var result = _userService.UpdateProfile(professionalUser, UserTypeId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { ProfessionalUser = professionalUser, UserTypeId = UserTypeId });
            }
        }

        [HttpGet("GetUserDetail/{EmployeeId}")]
        public IResponse<ApiResponse> GetUserDetail(long EmployeeId)
        {
            try
            {
                var result = _userService.GetUserDetail(EmployeeId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, EmployeeId);
            }
        }

        [HttpPost("UploadProfileDetailFile/{userId}/{UserTypeId}")]
        public async Task<ApiResponse> UploadProfileDetailFile(string userId, int UserTypeId)
        {
            ProfessionalUser userInfo = null;
            try
            {
                StringValues UserInfoData = default(string);
                _httpContext.Request.Form.TryGetValue("userInfo", out UserInfoData);
                if (UserInfoData.Count > 0)
                {
                    userInfo = JsonConvert.DeserializeObject<ProfessionalUser>(UserInfoData);
                    IFormFileCollection files = _httpContext.Request.Form.Files;
                    var Result = await _userService.UploadUserInfo(userId, userInfo, files, UserTypeId);
                    return BuildResponse(Result, HttpStatusCode.OK);
                }
                return BuildResponse("No files found", HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, userInfo);
            }
        }

        [HttpPost("UploadResume/{userId}/{UserTypeId}")]
        public async Task<ApiResponse> UploadResume(string userId, int UserTypeId)
        {
            ProfessionalUser userInfo = null;
            try
            {
                StringValues UserInfoData = default(string);
                _httpContext.Request.Form.TryGetValue("userInfo", out UserInfoData);
                if (UserInfoData.Count > 0)
                {
                    userInfo = JsonConvert.DeserializeObject<ProfessionalUser>(UserInfoData);
                    IFormFileCollection files = _httpContext.Request.Form.Files;
                    var Result = await _userService.UploadResume(userId, userInfo, files, UserTypeId);
                    return BuildResponse(Result, HttpStatusCode.OK);
                }
                return BuildResponse("No files found", HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, userInfo);
            }
        }

        [HttpGet("GenerateResume/{userId}")]
        public IResponse<ApiResponse> GenerateResume(long userId)
        {
            try
            {
                var result = _userService.GenerateResume(userId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, userId);
            }
        }

        //[HttpPost("UploadDeclaration/{UserId}/{UserTypeId}")]
        //public async Task<ApiResponse> UploadDeclaration(string UserId, int UserTypeId)
        //{
        //    UserDetail UserInfo = null;
        //    try
        //    {
        //        StringValues userDetail = default(string);
        //        _httpContext.Request.Form.TryGetValue("UserDetail", out userDetail);
        //        _httpContext.Request.Form.TryGetValue("fileDetail", out StringValues FileData);
        //        if (userDetail.Count > 0)
        //        {
        //            UserInfo = JsonConvert.DeserializeObject<UserDetail>(userDetail);
        //            List<Files> files = JsonConvert.DeserializeObject<List<Files>>(FileData);
        //            IFormFileCollection fileDetail = _httpContext.Request.Form.Files;
        //            var result = await _userService.UploadDeclaration(UserId, UserTypeId, UserInfo, fileDetail, files);
        //            return BuildResponse(result, HttpStatusCode.OK);
        //        }
        //        return BuildResponse("No files found", HttpStatusCode.OK);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw Throw(ex, UserInfo);
        //    }
        //}

        [HttpGet("GetEmployeeAndChients")]
        public async Task<IResponse<ApiResponse>> GetEmployeeAndChients()
        {
            try
            {
                var result = await _userService.GetEmployeeAndChientListService();
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }
    }
}