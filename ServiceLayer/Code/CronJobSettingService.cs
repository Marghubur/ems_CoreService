using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using EMailService.Modal;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class CronJobSettingService : ICronJobSettingService
    {
        private readonly IDb _db;
        private readonly CurrentSession _currentSession;
        public CronJobSettingService(IDb db, CurrentSession currentSession)
        {
            _db = db;
            _currentSession = currentSession;
        }

        public Task<CronJobSettingJson> GetCronJobSettingService()
        {
            CronJobSettingJson cronJobSettingJson = null;
            var result = _db.Get<CronJobSetting>("", new
            {
                CompanyId = _currentSession.CurrentUserDetail.CompanyId
            });
            if (result != null)
                cronJobSettingJson = JsonConvert.DeserializeObject<CronJobSettingJson>(result.CronJobDetail);

            return Task.FromResult(cronJobSettingJson);
        }

        public async Task<string> ManageCronJobSettingService(CronJobSettingJson cronJobSetting)
        {
            await ValidateCronJobSeting(cronJobSetting);
            var cronJObSetting = new CronJobSetting
            {
                CompanyId = _currentSession.CurrentUserDetail.CompanyId,
                OrganizationId = _currentSession.CurrentUserDetail.OrganizationId,
                CronJobDetail = JsonConvert.SerializeObject(cronJobSetting)
            };

            var result = _db.Execute<CronJobSettingJson>("", cronJObSetting, true);
            if (string.IsNullOrEmpty(result))
                throw HiringBellException.ThrowBadRequest("Fail to insert or update cron job detail");

            return await Task.FromResult("");
        }

        private async Task ValidateCronJobSeting(CronJobSettingJson cronJobSetting)
        {
            if (cronJobSetting == null)
                throw HiringBellException.ThrowBadRequest("CronJobSetting detail is invalid");

            if (cronJobSetting.LeaveAccrualCronType == (int)CronJobType.Weekly || cronJobSetting.LeaveAccrualCronType == (int)CronJobType.Monthly || cronJobSetting.LeaveAccrualCronType == (int)CronJobType.Year)
            {
                if (cronJobSetting.LeaveAccrualCronDay == 0)
                    throw HiringBellException.ThrowBadRequest("Leave cron job day is invalid");
            }

            if (cronJobSetting.TimesheetCronDay == (int)CronJobType.Weekly || cronJobSetting.TimesheetCronDay == (int)CronJobType.Monthly || cronJobSetting.TimesheetCronDay == (int)CronJobType.Year)
            {
                if (cronJobSetting.TimesheetCronDay == 0)
                    throw HiringBellException.ThrowBadRequest("Timesheet cron job day is invalid");
            }

            if (cronJobSetting.LeaveYearEndCronType == (int)CronJobType.Weekly || cronJobSetting.LeaveYearEndCronType == (int)CronJobType.Monthly || cronJobSetting.LeaveYearEndCronType == (int)CronJobType.Year)
            {
                if (cronJobSetting.LeaveYearEndCronDay == 0)
                    throw HiringBellException.ThrowBadRequest("Leave accrual cron job day is invalid");
            }

            await Task.CompletedTask;
        }
    }
}
