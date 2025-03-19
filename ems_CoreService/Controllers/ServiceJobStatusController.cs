using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Mvc;
using OnlineDataBuilder.Controllers;
using ServiceLayer.Interface;
using System.Threading.Tasks;

namespace ems_CoreService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceJobStatusController(IServiceJobStatusService _serviceJobStatusService) : BaseController
    {
        [HttpGet("GetServiceJobStatus/{serviceJobStatusId}")]
        public async Task<ApiResponse> GetServiceJobStatus([FromRoute] int serviceJobStatusId)
        {
            var result = await _serviceJobStatusService.GetServiceJobStatusService(serviceJobStatusId);
            return BuildResponse(result);
        }
    }
}
