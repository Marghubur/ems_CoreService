using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using EMailService.Modal;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using Newtonsoft.Json;
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

        public async Task<List<EmployeeOvertime>> GetEmployeeOTByMangerService(FilterModel filterModel)
        {
            var result = _db.GetList<EmployeeOvertime>(Procedures.EMPLOYEE_OVERTIME_FILTER_BY_MANAGER, new
            {
                filterModel.SearchString,
                filterModel.SortBy,
                filterModel.PageIndex,
                filterModel.PageSize,
                EmployeeId = _currentSession.CurrentUserDetail.UserId
            });

            return await Task.FromResult(result);
        }

        public async Task<List<EmployeeOvertime>> RejectEmployeeOvertimeService(List<EmployeeOvertime> employeeOvertimes)
        {
            if (!employeeOvertimes.Any())
                throw HiringBellException.ThrowBadRequest("Invalid request selected");

            foreach (var employeeOvertime in employeeOvertimes)
            {
                if (employeeOvertime.OvertimeId <= 0)
                    throw HiringBellException.ThrowBadRequest("Invalid overtime selected");
            }

            var existingOvertime = _db.Get<EmployeeOvertime>(Procedures.EMPLOYEE_OVERTIMETABLE_GET_BYID, new
            {
                OvertimeId = employeeOvertimes[0].OvertimeId
            });
            if (existingOvertime == null)
                throw HiringBellException.ThrowBadRequest("Overtime detail not found");

            List<EmployeeWithRoles> existingExecutionRecord = null;
            if (string.IsNullOrEmpty(existingOvertime.ExecutionRecord) || existingOvertime.ExecutionRecord == "[]")
            {
                existingExecutionRecord = new List<EmployeeWithRoles>();
            }
            else
            {
                existingExecutionRecord = JsonConvert.DeserializeObject<List<EmployeeWithRoles>>(existingOvertime.ExecutionRecord);
                if (IsActionAlreadyTaken(existingExecutionRecord, (int)ItemStatus.Rejected))
                    throw HiringBellException.ThrowBadRequest("You have already rejected this overtime.");
            }

            existingExecutionRecord.Add(new EmployeeWithRoles
            {
                DesignationId = _currentSession.CurrentUserDetail.DesignationId,
                Name = _currentSession.CurrentUserDetail.FullName,
                Status = (int)ItemStatus.Rejected,
                Email = _currentSession.CurrentUserDetail.EmailId,
                EmployeeId = _currentSession.CurrentUserDetail.UserId
            });

            existingOvertime.ExecutionRecord = JsonConvert.SerializeObject(existingExecutionRecord);
            existingOvertime.StatusId = (int)ItemStatus.Rejected;

            await updateOvertimeDetail(existingOvertime);
            return await GetEmployeeOTByMangerService(new FilterModel());
        }

        public async Task<List<EmployeeOvertime>> ApproveEmployeeOvertimeService(List<EmployeeOvertime> employeeOvertimes)
        {
            if (!employeeOvertimes.Any())
                throw HiringBellException.ThrowBadRequest("Invalid request selected");

            foreach (var employeeOvertime in employeeOvertimes)
            {
                if (employeeOvertime.OvertimeId <= 0)
                    throw HiringBellException.ThrowBadRequest("Invalid overtime selected");
            }

            var result = GetEmployeeOvertimeWithConnfiguration(employeeOvertimes[0].OvertimeId, out int shiftDuration);

            List<EmployeeWithRoles> existingExecutionRecord = null;
            if (string.IsNullOrEmpty(result.selectedOvertime.ExecutionRecord) || result.selectedOvertime.ExecutionRecord == "[]")
            {
                existingExecutionRecord = new List<EmployeeWithRoles>();
            }
            else
            {
                existingExecutionRecord = JsonConvert.DeserializeObject<List<EmployeeWithRoles>>(result.selectedOvertime.ExecutionRecord);
                if (IsActionAlreadyTaken(existingExecutionRecord, (int)ItemStatus.Approved))
                    throw HiringBellException.ThrowBadRequest("You have already approved this overtime.");
            }

            existingExecutionRecord.Add(new EmployeeWithRoles
            {
                DesignationId = _currentSession.CurrentUserDetail.DesignationId,
                Name = _currentSession.CurrentUserDetail.FullName,
                Status = (int)ItemStatus.Approved,
                Email = _currentSession.CurrentUserDetail.EmailId,
                EmployeeId = _currentSession.CurrentUserDetail.UserId
            });

            bool isNextApprovalRequired = await IsNextApprovalRequired(result.approvalChainDetail);
            result.selectedOvertime.ExecutionRecord = JsonConvert.SerializeObject(existingExecutionRecord);

            if (!isNextApprovalRequired)
            {
                result.selectedOvertime.StatusId = (int)ItemStatus.Approved;
                if (result.overtimeConfiguation.ConvertInCash)
                {
                    decimal overtimeAmount = await OvertimeConvertedIntoCashCalculation(result.selectedOvertime, result.overtimeConfiguation, shiftDuration);
                    await insertOvertimeConvertedToCashAmount(result.selectedOvertime, overtimeAmount);
                }
                else
                {
                    // Overtime converted into Leave
                }
            }

            // Update overtime record now.
            await updateOvertimeDetail(result.selectedOvertime);
            return await GetEmployeeOTByMangerService(new FilterModel());
        }

        private bool IsActionAlreadyTaken(List<EmployeeWithRoles> existingExecutionRecord, int status)
        {
            return existingExecutionRecord.Any(x => x.EmployeeId == _currentSession.CurrentUserDetail.UserId && x.Status == status);
        }

        private async Task insertOvertimeConvertedToCashAmount(EmployeeOvertime employeeOvertime, decimal amount)
        {
            var result = await _db.ExecuteAsync(Procedures.HIKE_BONUS_SALARY_ADHOC_INS_UPDATE, new
            {
                SalaryAdhocId = 0,
                SalaryRunConfigProcessingId = 0,
                EmployeeId = employeeOvertime.EmployeeId,
                ProcessStepId = 0,
                FinancialYear = _currentSession.FinancialStartYear,
                OrganizationId = _currentSession.CurrentUserDetail.OrganizationId,
                CompanyId = _currentSession.CurrentUserDetail.CompanyId,
                IsPaidByCompany = true,
                IsPaidByEmployee = false,
                IsFine = false,
                IsHikeInSalary = false,
                IsBonus = false,
                IsReimbursment = false,
                IsSalaryOnHold = false,
                IsArrear = false,
                IsOvertime = true,
                IsCompOff = false,
                OTCalculatedOn = "",
                AmountInPercentage = 0,
                Amount = amount,
                IsActive = true,
                PaymentActionType = "",
                Comments = "Overtime converted into cash",
                Status = (int)ItemStatus.Approved,
                ForYear = DateTime.UtcNow.Year,
                ForMonth = DateTime.UtcNow.Month,
                ProgressState = 0,
            }, true);

            if (string.IsNullOrEmpty(result.statusMessage))
                throw HiringBellException.ThrowBadRequest("Unable to inert overtime to cash record");
        }

        private async Task updateOvertimeDetail(EmployeeOvertime employeeOvertime)
        {
            var result = await _db.ExecuteAsync(Procedures.EMPLOYEE_OVERTIMETABLE_INSUPD, new
            {
                employeeOvertime.OvertimeId,
                employeeOvertime.EmployeeId,
                employeeOvertime.Comments,
                employeeOvertime.AppliedOn,
                employeeOvertime.LoggedMinutes,
                employeeOvertime.StatusId,
                employeeOvertime.OvertimeConfigId,
                employeeOvertime.ExecutionRecord,
                employeeOvertime.StartOvertime,
                employeeOvertime.EndOvertime,
                employeeOvertime.OvertimeDate
            }, true);

            if (string.IsNullOrEmpty(result.statusMessage))
                throw HiringBellException.ThrowBadRequest("Unable to update overtime detail");
        }

        private (EmployeeOvertime selectedOvertime, OvertimeConfiguration overtimeConfiguation, List<ApprovalChainDetail> approvalChainDetail) GetEmployeeOvertimeWithConnfiguration(int overtimeId, out int shiftDuration)
        {
            var result = _db.FetchDataSet(Procedures.EMPLOYEE_OVERTIMETABLE_CONFIG_CHAIN_GET_BYID, new
            {
                OvertimeId = overtimeId
            });

            if (result.Tables.Count != 4)
                throw HiringBellException.ThrowBadRequest("Fail to get employee overtime detail");

            if (result.Tables[0].Rows.Count != 1)
                throw HiringBellException.ThrowBadRequest("Overtime record not found");

            if (result.Tables[1].Rows.Count != 1)
                throw HiringBellException.ThrowBadRequest("Overtime configuration not found");

            if (result.Tables[2].Rows.Count == 0)
                throw HiringBellException.ThrowBadRequest("Fail to get approval chain detail");

            if (result.Tables[3].Rows.Count != 1)
                throw HiringBellException.ThrowBadRequest("Fail to get shift detail");

            var selectedOvertime = Converter.ToType<EmployeeOvertime>(result.Tables[0]);
            var overtimeConfiguation = Converter.ToType<OvertimeConfiguration>(result.Tables[1]);
            var approvalChainDetail = Converter.ToList<ApprovalChainDetail>(result.Tables[2]);
            var shiftDetail = Converter.ToType<ShiftDetail>(result.Tables[3]);

            shiftDuration = shiftDetail.Duration;

            return (selectedOvertime, overtimeConfiguation, approvalChainDetail);
        }

        private async Task<bool> IsNextApprovalRequired(List<ApprovalChainDetail> approvalChainDetails)
        {
            int index = approvalChainDetails.FindIndex(x => x.AssignieId == _currentSession.CurrentUserDetail.DesignationId);
            if (index == 0)
                return await Task.FromResult(false);

            return await Task.FromResult(true);
        }

        //Overtime converted into Cash
        private async Task<decimal> OvertimeConvertedIntoCashCalculation(EmployeeOvertime employeeOvertime, OvertimeConfiguration overtimeConfiguration, int shiftDuration)
        {
            int overtimeWorkedMin = ConvertOTDurationIntoMin(employeeOvertime.EndOvertime) - ConvertOTDurationIntoMin(employeeOvertime.StartOvertime);
            if (overtimeWorkedMin < overtimeConfiguration.MinOvertimeMin)
                return 0;

            if (overtimeWorkedMin > overtimeConfiguration.MaxOvertimeMin)
                overtimeWorkedMin = (int)overtimeConfiguration.MaxOvertimeMin;

            DateTime overtimeDate = _timezoneConverter.ToTimeZoneDateTime(employeeOvertime.OvertimeDate, _currentSession.TimeZone);
            int daysInOvertimeMonth = DateTime.DaysInMonth(overtimeDate.Year, overtimeDate.Month);

            decimal overtimeCalcutedOn = await OvertimeCalculatedOnValue(overtimeConfiguration.OTCalculatedOn, employeeOvertime.EmployeeId, overtimeDate.Month);
            decimal perMinutesAmount = overtimeCalcutedOn / (daysInOvertimeMonth * shiftDuration);
            decimal overtimeConvrtedAmount = (decimal)(overtimeWorkedMin * perMinutesAmount * overtimeConfiguration.RateMultiplier);

            return overtimeConvrtedAmount;
        }

        private async Task<decimal> OvertimeCalculatedOnValue(string oTCalculatedOn, long employeeId, int overtimeMonth)
        {
            var salaryDetail = _db.Get<EmployeeSalaryDetail>(Procedures.EMPLOYEE_SALARY_DETAIL_GET_BY_EMPID, new
            {
                _currentSession.FinancialStartYear,
                EmployeeId = employeeId
            });

            var annualSalaryBreakup = JsonConvert.DeserializeObject<List<AnnualSalaryBreakup>>(salaryDetail.CompleteSalaryDetail);
            var employeeSalaryDetail = annualSalaryBreakup.Find(x => x.MonthNumber == overtimeMonth);
            if (employeeSalaryDetail == null)
                throw HiringBellException.ThrowBadRequest("Employee salary detail not found");

            if (oTCalculatedOn.Equals("Gross", StringComparison.OrdinalIgnoreCase))
            {
                var grossComponent = employeeSalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId.ToLower() == ComponentNames.GrossId.ToLower());
                if (grossComponent == null)
                    throw HiringBellException.ThrowBadRequest("Gross component not found");

                return await Task.FromResult(grossComponent.ActualAmount);
            }
            else
            {
                var basicComponent = employeeSalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId.ToLower() == ComponentNames.Basic.ToLower()); ;
                if (basicComponent == null)
                    throw HiringBellException.ThrowBadRequest("Gross component not found");

                return await Task.FromResult(basicComponent.ActualAmount);
            }
        }

        private int ConvertOTDurationIntoMin(string time)
        {
            var splittedTime = time.Split(':');
            if (splittedTime.Length != 2)
                throw HiringBellException.ThrowBadRequest("Invalid time passed");

            int.TryParse(splittedTime[0], out int hrs);
            int.TryParse(splittedTime[1], out int min);

            return hrs * 60 + min;
        }
    }
}