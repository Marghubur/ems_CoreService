using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Interface;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrganizationController : ControllerBase
    {
        private readonly ICompanyService _companyService;

        public OrganizationController(ICompanyService companyService)
        {
            _companyService = companyService;
        }
    }
}
