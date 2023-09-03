using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using OnlineDataBuilder.ContextHandler;
using ServiceLayer.Interface;
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
            var result = await _serviceRequestService.GetServiceRequestService(filter);
            return BuildResponse(result);
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("AddUpdateServiceRequest")]
        public async Task<ApiResponse> AddUpdateServiceRequest(ServiceRequest serviceRequest)
        {
            var result = await _serviceRequestService.AddUpdateServiceRequestService(serviceRequest);
            return BuildResponse(result);
        }

        [HttpPost("GetServiceRequestByEmpId")]
        public async Task<ApiResponse> GetServiceRequestByEmpId(FilterModel filter)
        {
            var result = await _serviceRequestService.GetServiceRequestService(filter);
            return BuildResponse(result);
        }
    }
}
