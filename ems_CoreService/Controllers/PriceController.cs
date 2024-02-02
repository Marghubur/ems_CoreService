using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineDataBuilder.Controllers;
using ServiceLayer.Interface;
using System.Threading.Tasks;

namespace ems_CoreService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class PriceController : BaseController
    {
        private readonly IPriceService _priceService;

        public PriceController(IPriceService priceService)
        {
            _priceService = priceService;
        }
        [HttpGet("GetPriceDetail")]
        public async Task<ApiResponse> GetPriceDetail()
        {
            var result = await _priceService.GetPriceDetailService();
            return BuildResponse(result);
        }

    }
}
