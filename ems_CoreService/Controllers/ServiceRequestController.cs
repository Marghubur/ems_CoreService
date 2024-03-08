using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceRequestController : BaseController
    {
        private readonly IServiceRequestService _serviceRequestService;

        public ServiceRequestController(IServiceRequestService serviceRequestService)
        {
            _serviceRequestService = serviceRequestService;
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("GetServiceRequest")]
        public async Task<ApiResponse> GetServiceRequest(FilterModel filter)
        {
            try
            {
                var result = await _serviceRequestService.GetServiceRequestService(filter);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filter);
            }
        }

        [HttpPost("AddUpdateServiceRequest")]
        public async Task<ApiResponse> AddUpdateServiceRequest(ServiceRequest serviceRequest)
        {
            try
            {
                var result = await _serviceRequestService.AddUpdateServiceRequestService(serviceRequest);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, serviceRequest);
            }
        }

        [HttpPost("GetServiceRequestByEmpId")]
        public async Task<ApiResponse> GetServiceRequestByEmpId(FilterModel filter)
        {
            try
            {
                var result = await _serviceRequestService.GetServiceRequestService(filter);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filter);
            }
        }
    }
}