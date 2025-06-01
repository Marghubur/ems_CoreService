using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.API;
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
    public class UserNotificationController : BaseController
    {
        private readonly IUserNotificationService _notificationService;
        private readonly HttpContext _httpContext;

        public UserNotificationController(IUserNotificationService notificationService, IHttpContextAccessor httpContext)
        {
            _notificationService = notificationService;
            _httpContext = httpContext.HttpContext;
        }

        [HttpPost("CreateEmployeeNotification")]
        public async Task<ApiResponse> CreateEmployeeNotification()
        {
            try
            {
                StringValues notification = default(string);
                _httpContext.Request.Form.TryGetValue("notification", out notification);
                _httpContext.Request.Form.TryGetValue("fileDetail", out StringValues FileData);
                if (notification.Count > 0)
                {
                    var notifications = JsonConvert.DeserializeObject<EmployeeNotification>(notification);
                    List<Files> files = JsonConvert.DeserializeObject<List<Files>>(FileData);
                    IFormFileCollection fileDetail = _httpContext.Request.Form.Files;
                    var result = await _notificationService.CreateEmployeeNotificationService(notifications, files, fileDetail);
                    return BuildResponse(result);
                }
                return BuildResponse("No files found", HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [HttpPost("GetNotifications")]
        public IResponse<ApiResponse> GetNotifications(FilterModel filterModel)
        {
            try
            {
                var result = _notificationService.GetEmployeeNotificationService(filterModel);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }
    }
}