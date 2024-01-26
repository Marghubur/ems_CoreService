using EMailService.Modal;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface ICronJobSettingService
    {
        Task<string> ManageCronJobSettingService(CronJobSettingJson cronJobSetting);
        Task<CronJobSettingJson> GetCronJobSettingService();
    }
}
