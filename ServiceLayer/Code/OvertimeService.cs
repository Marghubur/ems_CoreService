using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using EMailService.Modal;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class OvertimeService(IDb _db,
                                CurrentSession _currentSession,
                                ITimezoneConverter _timezoneConverter) : IOvertimeService
    {
        public async Task<DataSet> GetOvertimeTypeAndConfigService()
        {
            var result = _db.FetchDataSet(Procedures.OVERTIME_TYPE_CONFIGURATION_GETALL);

            if (result.Tables.Count != 3)
                throw HiringBellException.ThrowBadRequest("Fail to get overtime configuration");

            return await Task.FromResult(result);
        }

        public async Task<List<OvertimeConfiguration>> ManageOvertimeConfigService(OvertimeConfiguration overtimeDetail)
        {
            await ValidateOvertimeDetail(overtimeDetail);
            var existingOvertime = new OvertimeConfiguration();

            if (overtimeDetail.OvertimeConfigId > 0)
            {
                existingOvertime = await GetOvertimeById(overtimeDetail.OvertimeConfigId);
                if (existingOvertime == null)
                    throw HiringBellException.ThrowBadRequest("Existing overtime is not found");

                await updateOvertimeDetail(overtimeDetail, existingOvertime);
            }
            else
            {
                existingOvertime = overtimeDetail;
            }

            var result = await _db.ExecuteAsync(Procedures.OVERTIMETABLE_CONFIGURATION_INSUPD, new
            {
                overtimeDetail.OvertimeConfigId,
                existingOvertime.ConvertInCash,
                existingOvertime.ConvertInLeave,
                existingOvertime.RateMultiplier,
                existingOvertime.MinOvertimeMin,
                existingOvertime.MaxOvertimeMin,
                existingOvertime.IsWeekend,
                existingOvertime.IsHoliday,
                existingOvertime.LeavePerHour,
                existingOvertime.PartialHours,
                existingOvertime.PartialLeave,
                existingOvertime.MaxLeave,
                existingOvertime.FullDayHours,
                existingOvertime.BonusShift,
                existingOvertime.BonusLeave,
                existingOvertime.ExpiryMonths,
                existingOvertime.ConfigName,
                existingOvertime.WorkflowId,
                existingOvertime.OvertimeTypeId,
                existingOvertime.OTCalculatedOn,
                AdminId = _currentSession.CurrentUserDetail.UserId
            }, true);

            if (string.IsNullOrEmpty(result.statusMessage))
                throw HiringBellException.ThrowBadRequest("Unable to inert/update overtime detail");

            return await GetAllOvertimeConfiguration();
        }

        private async Task updateOvertimeDetail(OvertimeConfiguration overtimeDetail, OvertimeConfiguration existingOvertime)
        {
            existingOvertime.ConvertInCash = overtimeDetail.ConvertInCash;
            existingOvertime.ConvertInLeave = overtimeDetail.ConvertInLeave;
            existingOvertime.RateMultiplier = overtimeDetail.RateMultiplier;
            existingOvertime.MinOvertimeMin = overtimeDetail.MinOvertimeMin;
            existingOvertime.MaxOvertimeMin = overtimeDetail.MaxOvertimeMin;
            existingOvertime.IsWeekend = overtimeDetail.IsWeekend;
            existingOvertime.IsHoliday = overtimeDetail.IsHoliday;
            existingOvertime.LeavePerHour = overtimeDetail.LeavePerHour;
            existingOvertime.PartialHours = overtimeDetail.PartialHours;
            existingOvertime.PartialLeave = overtimeDetail.PartialLeave;
            existingOvertime.MaxLeave = overtimeDetail.MaxLeave;
            existingOvertime.FullDayHours = overtimeDetail.FullDayHours;
            existingOvertime.BonusShift = overtimeDetail.BonusShift;
            existingOvertime.BonusLeave = overtimeDetail.BonusLeave;
            existingOvertime.ExpiryMonths = overtimeDetail.ExpiryMonths;
            existingOvertime.ConfigName = overtimeDetail.ConfigName;
            existingOvertime.WorkflowId = overtimeDetail.WorkflowId;
            existingOvertime.OvertimeTypeId = overtimeDetail.OvertimeTypeId;
            existingOvertime.OTCalculatedOn = overtimeDetail.OTCalculatedOn;

            await Task.CompletedTask;
        }

        private async Task<List<OvertimeConfiguration>> GetAllOvertimeConfiguration()
        {
            var overtimeConfiguration = _db.GetList<OvertimeConfiguration>(Procedures.OVERTIMETABLE_CONFIGURATION_GET_ALL);
            return await Task.FromResult(overtimeConfiguration);
        }

        private async Task<OvertimeConfiguration> GetOvertimeById(int overtimeConfigId)
        {
            var overtimeDetail = _db.Get<OvertimeConfiguration>(Procedures.OVERTIMETABLE_CONFIGURATION_GET_BYID, new
            {
                OvertimeConfigId = overtimeConfigId
            });

            return await Task.FromResult(overtimeDetail);
        }

        private async Task ValidateOvertimeDetail(OvertimeConfiguration overtimeDetail)
        {
            if (string.IsNullOrEmpty(overtimeDetail.ConfigName))
                throw HiringBellException.ThrowBadRequest("Invalid config name");

            if (overtimeDetail.MinOvertimeMin <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid minimum overtime");

            if (overtimeDetail.MaxOvertimeMin <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid maximum overtime");

            if (overtimeDetail.WorkflowId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid workflow selected");

            if (overtimeDetail.OvertimeTypeId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid overtime type selected");

            if (overtimeDetail.ConvertInCash)
            {
                if (overtimeDetail.RateMultiplier <= 0)
                    throw HiringBellException.ThrowBadRequest("Invalid rate limiter");
            }
            else
            {
                if (overtimeDetail.LeavePerHour <= 0)
                    throw HiringBellException.ThrowBadRequest("Invalid leave per hour");

                if (overtimeDetail.PartialHours < 0)
                    throw HiringBellException.ThrowBadRequest("Invalid partial hours");

                if (overtimeDetail.PartialLeave < 0)
                    throw HiringBellException.ThrowBadRequest("Invalid partial leave");

                if (overtimeDetail.MaxLeave <= 0)
                    throw HiringBellException.ThrowBadRequest("Invalid maximum leave");

                if (overtimeDetail.FullDayHours <= 0)
                    throw HiringBellException.ThrowBadRequest("Invalid full day hours");

                if (overtimeDetail.BonusShift < 0)
                    throw HiringBellException.ThrowBadRequest("Invalid bonus shift");

                if (overtimeDetail.BonusLeave < 0)
                    throw HiringBellException.ThrowBadRequest("Invalid bonus leave");

                if (overtimeDetail.ExpiryMonths < 0)
                    throw HiringBellException.ThrowBadRequest("Invalid expiray month");
            }

            await Task.CompletedTask;
        }

        public async Task<(List<EmployeeOvertime> EmployeeOvertimes, List<OvertimeConfiguration> OvertimeConfigurations)> GetEmployeeOvertimeService()
        {
            return await GetEmployeeOvertimeByEmpId(_currentSession.CurrentUserDetail.UserId);
        }

        private async Task<(List<EmployeeOvertime> employeeOvertimes, List<OvertimeConfiguration> overtimeConfigurations)> GetEmployeeOvertimeByEmpId(long empId)
        {
            (var employeeOvertime, var overtimeConfiguration) = _db.GetList<EmployeeOvertime, OvertimeConfiguration>(Procedures.EMPLOYEE_OVERTIMETABLE_GET_BY_EMPID, new
            {
                EmployeeId = empId
            });

            return await Task.FromResult((employeeOvertime, overtimeConfiguration));
        }

        public async Task<(List<EmployeeOvertime> EmployeeOvertimes, List<OvertimeConfiguration> OvertimeConfigurations)> ApplyOvertimeService(EmployeeOvertime employeeOvertime)
        {
            ValidateEmployeeOvertime(employeeOvertime);

            var overTimeConfigId = await GetOvertimeConfigurationId(employeeOvertime.OvertimeDate);

            var result = await _db.ExecuteAsync(Procedures.EMPLOYEE_OVERTIMETABLE_INSUPD, new
            {
                employeeOvertime.OvertimeId,
                EmployeeId = _currentSession.CurrentUserDetail.UserId,
                employeeOvertime.Comments,
                AppliedOn = DateTime.UtcNow,
                employeeOvertime.LoggedMinutes,
                StatusId = (int)ItemStatus.Pending,
                OvertimeConfigId = overTimeConfigId,
                ExecutionRecord = "[]",
                employeeOvertime.StartOvertime,
                employeeOvertime.EndOvertime,
                employeeOvertime.OvertimeDate
            }, true);

            if (string.IsNullOrEmpty(result.statusMessage))
                throw HiringBellException.ThrowBadRequest("Unable to inert/update overtime detail");

            return await GetEmployeeOvertimeByEmpId(_currentSession.CurrentUserDetail.UserId);
        }

        private async Task<int> GetOvertimeConfigurationId(DateTime overtimeDate)
        {
            var dataset = await GetWorkShiftAndCompanyCalenderByEmpId();
            var shiftDetail = Converter.ToType<ShiftDetail>(dataset.Tables[0]);
            var companyCalendar = Converter.ToList<CompanyCalendarDetail>(dataset.Tables[1]);
            var overtimeConfiguration = Converter.ToList<OvertimeConfiguration>(dataset.Tables[2]);

            if (companyCalendar.Any())
            {
                var isHoliday = companyCalendar.Any(x => _timezoneConverter.ToTimeZoneDateTime(x.CalendarDate, _currentSession.TimeZone).Date
                                                            .Subtract(_timezoneConverter.ToTimeZoneDateTime(overtimeDate, _currentSession.TimeZone).Date).TotalDays == 0);
                if (isHoliday)
                    return overtimeConfiguration.Find(x => x.IsHoliday).OvertimeConfigId;
            }

            var isweekend = await IsWeekend(overtimeDate, shiftDetail);
            if (isweekend)
                return overtimeConfiguration.Find(x => x.IsWeekend).OvertimeConfigId;
            
            return overtimeConfiguration.Find(x => !x.IsHoliday && !x.IsWeekend).OvertimeConfigId;
        }

        private async Task<bool> IsWeekend(DateTime date, ShiftDetail shiftDetail)
        {
            var zoneDate = _timezoneConverter.ToTimeZoneDateTime(date, _currentSession.TimeZone);

            var flag = zoneDate.DayOfWeek switch
            {
                DayOfWeek.Sunday => !shiftDetail.IsSun,
                DayOfWeek.Monday => !shiftDetail.IsMon,
                DayOfWeek.Tuesday => !shiftDetail.IsTue,
                DayOfWeek.Wednesday => !shiftDetail.IsWed,
                DayOfWeek.Thursday => !shiftDetail.IsThu,
                DayOfWeek.Friday => !shiftDetail.IsFri,
                DayOfWeek.Saturday => !shiftDetail.IsSat,
                _ => false
            };

            return await Task.FromResult(flag);
        }

        private void ValidateEmployeeOvertime(EmployeeOvertime employeeOvertime)
        {
            if (employeeOvertime.LoggedMinutes <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid logged minutes");

            if (string.IsNullOrEmpty(employeeOvertime.Comments))
                throw HiringBellException.ThrowBadRequest("Invalid comments");

            if (employeeOvertime.OvertimeDate == null)
                throw HiringBellException.ThrowBadRequest("Invalid overtime date");

            if (string.IsNullOrEmpty(employeeOvertime.StartOvertime))
                throw HiringBellException.ThrowBadRequest("Invalid start over time");

            if (string.IsNullOrEmpty(employeeOvertime.EndOvertime))
                throw HiringBellException.ThrowBadRequest("Invalid end over time");
        }

        private async Task<DataSet> GetWorkShiftAndCompanyCalenderByEmpId()
        {
            var dataset = _db.FetchDataSet(Procedures.WORK_SHIFTS_COMAPNY_CALENDAR_GETBY_EMPID, new
            {
                EmployeeId = _currentSession.CurrentUserDetail.UserId,
                CompanyId = _currentSession.CurrentUserDetail.CompanyId
            });

            if (dataset.Tables.Count != 3)
                throw HiringBellException.ThrowBadRequest("Fail to get shift, calendar and overtime configuration");

            if (dataset.Tables[0].Rows.Count != 1)
                throw HiringBellException.ThrowBadRequest("Fail to get employee work shift");

            if (dataset.Tables[0].Rows.Count == 0)
                throw HiringBellException.ThrowBadRequest("Fail to get over time configuration");

            return await Task.FromResult(dataset);
        }
    }
}