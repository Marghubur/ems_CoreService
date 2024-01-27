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

        public async Task<CronJobSettingJson> GetCronJobSettingService()
        {
            CronJobSettingJson cronJobSettingJson = new CronJobSettingJson();
            int companyId = _currentSession.CurrentUserDetail.CompanyId;
            var result = await GetCronJobSeetinByCompId(companyId);
            if (result != null)
                cronJobSettingJson = JsonConvert.DeserializeObject<CronJobSettingJson>(result.CronJobDetail);

            return cronJobSettingJson;
        }

        private async Task<CronJobSetting> GetCronJobSeetinByCompId(int CompanyId)
        {
            var result = _db.Get<CronJobSetting>(Procedures.CRONJOB_SETTING_GET_BY_COMPID, new
            {
                CompanyId
            });
            return await Task.FromResult(result);
        }

        public async Task<CronJobSettingJson> ManageCronJobSettingService(CronJobSettingJson cronJobSetting)
        {
            await ValidateCronJobSeting(cronJobSetting);
            int companyId = _currentSession.CurrentUserDetail.CompanyId;
            var existingCronJobSetting = await GetCronJobSeetinByCompId(companyId);
            if (existingCronJobSetting != null)
            {
                var cronJobDetail = JsonConvert.DeserializeObject<CronJobSettingJson>(existingCronJobSetting.CronJobDetail);
                cronJobDetail.TimesheetCronType = cronJobSetting.TimesheetCronType;
                cronJobDetail.TimesheetCronDay = cronJobSetting.TimesheetCronDay;
                cronJobDetail.TimesheetCronTime = cronJobSetting.TimesheetCronTime;
                cronJobDetail.LeaveAccrualCronTime = cronJobSetting.LeaveAccrualCronTime;
                cronJobDetail.LeaveAccrualCronDay = cronJobSetting.LeaveAccrualCronDay;
                cronJobDetail.LeaveAccrualCronType = cronJobSetting.LeaveAccrualCronType;
                cronJobDetail.LeaveYearEndCronDay = cronJobSetting.LeaveYearEndCronDay;
                cronJobDetail.LeaveYearEndCronTime = cronJobSetting.LeaveYearEndCronTime;
                cronJobDetail.LeaveYearEndCronType = cronJobSetting.LeaveYearEndCronType;
                existingCronJobSetting.CronJobDetail = JsonConvert.SerializeObject(cronJobDetail);
            }
            else
            {
                existingCronJobSetting = new CronJobSetting
                {
                    CompanyId = companyId,
                    OrganizationId = _currentSession.CurrentUserDetail.OrganizationId,
                    CronJobDetail = JsonConvert.SerializeObject(cronJobSetting)
                };
            }

            var result = _db.Execute<CronJobSetting>(Procedures.CRONJOB_SETTING_INSUPD, existingCronJobSetting, true);
            if (string.IsNullOrEmpty(result))
                throw HiringBellException.ThrowBadRequest("Fail to insert or update cron job detail");

            return cronJobSetting;
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
