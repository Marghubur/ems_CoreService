using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using OnlineDataBuilder.ContextHandler;
using OnlineDataBuilder.Controllers;
using ServiceLayer.Code;
using ServiceLayer.Interface;
using System.Net;

namespace OnlineDataBuilder.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class ManageUserCommentsController : BaseController
    {
        private readonly IManageUserCommentService manageUserCommentService;
        public ManageUserCommentsController(IManageUserCommentService manageUserCommentService)
        {
            this.manageUserCommentService = manageUserCommentService;
        }

        [HttpPost]
        [Route("PostUserComments")]
        public IResponse<ApiResponse> PostUserComments(UserComments userComments)
        {
            string ResultSet = this.manageUserCommentService.PostUserCommentService(userComments);
            BuildResponse(ResultSet, HttpStatusCode.OK);
            return apiResponse;
        }

        [HttpGet("GetComments")]
        [AllowAnonymous]
        public IResponse<ApiResponse> GetComments(string EmailId)
        {
            var ResultSet = this.manageUserCommentService.GetCommentsService(EmailId);
            BuildResponse(ResultSet, HttpStatusCode.OK);
            return apiResponse;
        }
    }
}
