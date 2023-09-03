using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using OnlineDataBuilder.ContextHandler;
using ServiceLayer.Interface;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InitialRegistrationController : BaseController
    {
        private readonly IInitialRegistrationService _initialRegistrationService;

        public InitialRegistrationController(IInitialRegistrationService initialRegistrationService)
        {
            _initialRegistrationService = initialRegistrationService;
        }
        [AllowAnonymous]
        [HttpPost("InitialOrgRegistration")]
        public IResponse<ApiResponse> InitialOrgRegistration(RegistrationForm registrationForm)
        {
            var result = _initialRegistrationService.InitialOrgRegistrationService(registrationForm);
            return BuildResponse(result);
        }
    }
}
