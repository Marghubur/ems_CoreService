using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Net;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InitialRegistrationController : BaseController
    {
        private readonly IInitialRegistrationService _initialRegistrationService;
        private readonly HttpContext _httpContext;
        public InitialRegistrationController(IInitialRegistrationService initialRegistrationService, IHttpContextAccessor httpContext)
        {
            _initialRegistrationService = initialRegistrationService;
            _httpContext = httpContext.HttpContext;
        }
        [AllowAnonymous]
        [HttpPost("InitialOrgRegistration")]
        public IResponse<ApiResponse> InitialOrgRegistration()
        {
            try
            {
                StringValues registrationInfoData = default(string);
                StringValues fileData = default(string);
                _httpContext.Request.Form.TryGetValue("FileDetail", out fileData);
                _httpContext.Request.Form.TryGetValue("RegistrationDetail", out registrationInfoData);
                if (registrationInfoData.Count > 0 && fileData.Count > 0)
                {
                    RegistrationForm registrationForm = JsonConvert.DeserializeObject<RegistrationForm>(registrationInfoData);
                    Files files = JsonConvert.DeserializeObject<Files>(fileData);
                    IFormFileCollection fileCollection = _httpContext.Request.Form.Files;
                    var resetSet = _initialRegistrationService.InitialOrgRegistrationService(registrationForm, files, fileCollection);
                    return BuildResponse(resetSet);
                }
                else
                {
                    return BuildResponse(this.responseMessage, HttpStatusCode.BadRequest);
                }
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }
    }
}