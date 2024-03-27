using Bot.CoreBottomHalf.CommonModal.API;
using EMailService.Modal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineDataBuilder.Controllers;
using ServiceLayer.Interface;
using System;
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
            try
            {
                var result = await _priceService.GetPriceDetailService();
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [HttpPost("AddContactus")]
        public async Task<ApiResponse> AddContactus([FromBody] ContactUsDetail contactUsDetail)
        {
            try
            {
                var result = await _priceService.AddContactusService(contactUsDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [HttpPost("AddFreeTrial")]
        public async Task<ApiResponse> AddFreeTrial([FromBody] ContactUsDetail contactUsDetail)
        {
            try
            {
                var result = await _priceService.AddTrailRequestService(contactUsDetail);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }
    }
}