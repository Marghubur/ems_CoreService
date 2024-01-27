using EMailService.Modal;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface ICronJobSettingService
    {
        Task<CronJobSettingJson> ManageCronJobSettingService(CronJobSettingJson cronJobSetting);
        Task<CronJobSettingJson> GetCronJobSettingService();
    }
}
