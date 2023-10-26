using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using OnlineDataBuilder.ContextHandler;
using ServiceLayer.Code.PayrollCycle.Interface;
using System;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadPayrollDataController : BaseController
    {
        private readonly HttpContext _httpContext;
        private readonly IUploadPayrollDataService _uploadPayrollDataService;
        public UploadPayrollDataController(IHttpContextAccessor httpContext, IUploadPayrollDataService uploadPayrollDataService)
        {
            _httpContext = httpContext.HttpContext;
            _uploadPayrollDataService = uploadPayrollDataService;
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("UploadPayrollExcel")]
        public async Task<ApiResponse> UploadPayrollExcel()
        {
            try
            {
                IFormFileCollection file = _httpContext.Request.Form.Files;
                await _uploadPayrollDataService.ReadPayrollDataService(file);
                return BuildResponse("file found");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
