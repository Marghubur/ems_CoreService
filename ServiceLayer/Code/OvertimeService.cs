using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using EMailService.Modal;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class OvertimeService(IDb _db,
                                CurrentSession _currentSession) : IOvertimeService
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
                existingOvertime.ExcludedShifts,
                existingOvertime.IsWeekend,
                existingOvertime.IsHoliday,
                existingOvertime.IsNightShift,
                existingOvertime.TaxRate,
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
            existingOvertime.ExcludedShifts = overtimeDetail.ExcludedShifts;
            existingOvertime.IsWeekend = overtimeDetail.IsWeekend;
            existingOvertime.IsHoliday = overtimeDetail.IsHoliday;
            existingOvertime.IsNightShift = overtimeDetail.IsNightShift;
            existingOvertime.TaxRate = overtimeDetail.TaxRate;
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

                if (overtimeDetail.ExcludedShifts <= 0)
                    throw HiringBellException.ThrowBadRequest("Invalid shift selected");
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

            var result = await _db.ExecuteAsync(Procedures.EMPLOYEE_OVERTIMETABLE_INSUPD, new
            {
                employeeOvertime.OvertimeId,
                EmployeeId = _currentSession.CurrentUserDetail.UserId,
                employeeOvertime.Comments,
                AppliedOn = DateTime.UtcNow,
                employeeOvertime.LoggedMinutes,
                ShiftId = 1,
                StatusId = (int)ItemStatus.Pending,
                employeeOvertime.OvertimeConfigId,
                ExecutionRecord = "[]",
                employeeOvertime.StartOvertime,
                employeeOvertime.EndOvertime,
                employeeOvertime.OvertimeDate
            }, true);

            if (string.IsNullOrEmpty(result.statusMessage))
                throw HiringBellException.ThrowBadRequest("Unable to inert/update overtime detail");

            return await GetEmployeeOvertimeByEmpId(_currentSession.CurrentUserDetail.UserId);
        }

        private static void ValidateEmployeeOvertime(EmployeeOvertime employeeOvertime)
        {
            if (employeeOvertime.LoggedMinutes <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid logged minutes");

            if (string.IsNullOrEmpty(employeeOvertime.Comments))
                throw HiringBellException.ThrowBadRequest("Invalid comments");

            if (employeeOvertime.OvertimeDate == null)
                throw HiringBellException.ThrowBadRequest("Invalid overtime date");

            if (employeeOvertime.OvertimeConfigId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid Overtime selected");
        }
    }
}
