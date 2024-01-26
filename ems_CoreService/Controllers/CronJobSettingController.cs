using Bot.CoreBottomHalf.CommonModal.API;
using EMailService.Modal;
using Microsoft.AspNetCore.Mvc;
using OnlineDataBuilder.Controllers;
using ServiceLayer.Interface;
using System.Threading.Tasks;

namespace ems_CoreService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CronJobSettingController : BaseController
    {
        private readonly ICronJobSettingService _cronJobSetting;

        public CronJobSettingController(ICronJobSettingService cronJobSetting)
        {
            _cronJobSetting = cronJobSetting;
        }

        [HttpGet("GetCronJobSetting")]
        public async Task<ApiResponse> GetCronJobSetting()
        {
            var result = await _cronJobSetting.GetCronJobSettingService();
            return BuildResponse(result, System.Net.HttpStatusCode.OK);
        }

        [HttpPost("ManageCronJObSetting")]
        public async Task<ApiResponse> ManageCronJObSetting(CronJobSettingJson cronJobSetting)
        {
            var result = await _cronJobSetting.ManageCronJobSettingService(cronJobSetting);
            return BuildResponse(result, System.Net.HttpStatusCode.OK);
        }
    }
}
