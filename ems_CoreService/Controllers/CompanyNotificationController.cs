using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.API;
using EMailService.Modal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyNotificationController : BaseController
    {
        private readonly ICompanyNotificationService _companyNotificationService;
        private readonly HttpContext _httpContext;

        public CompanyNotificationController(ICompanyNotificationService companyNotificationService, IHttpContextAccessor httpContext)
        {
            _companyNotificationService = companyNotificationService;
            _httpContext = httpContext.HttpContext;
        }

        [HttpPost("InsertUpdateNotification")]
        public async Task<ApiResponse> InsertUpdateNotification()
        {
            try
            {
                StringValues notification = default(string);
                _httpContext.Request.Form.TryGetValue("notification", out notification);
                _httpContext.Request.Form.TryGetValue("fileDetail", out StringValues FileData);
                if (notification.Count > 0)
                {
                    var notifications = JsonConvert.DeserializeObject<CompanyNotification>(notification);
                    List<Files> files = JsonConvert.DeserializeObject<List<Files>>(FileData);
                    IFormFileCollection fileDetail = _httpContext.Request.Form.Files;
                    var result = await _companyNotificationService.InsertUpdateNotificationService(notifications, files, fileDetail);
                    return BuildResponse(result);
                }
                return BuildResponse("No files found", HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [HttpPost("GetNotificationRecord")]
        public IResponse<ApiResponse> GetNotificationRecord(FilterModel filterModel)
        {
            try
            {
                var result = _companyNotificationService.GetNotificationRecordService(filterModel);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }

        [HttpGet("GetDepartmentsAndRoles")]
        public IResponse<ApiResponse> GetDepartmentsAndRoles()
        {
            try
            {
                var result = _companyNotificationService.GetDepartmentsAndRolesService();
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [HttpPost("GetCompanyNotification")]
        public async Task<ApiResponse> GetCompanyNotification([FromBody] FilterModel filterModel)
        {
            try
            {
                var result = await _companyNotificationService.GetCompanyNotificationFilterService(filterModel);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }
    }
}