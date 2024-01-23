using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using EMailService.Modal;
using EMailService.Modal.CronJobs;
using Microsoft.Extensions.Logging;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using ModalLayer.Modal.Leaves;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Code.Leaves
{
    public class YearEndCalculation
    {
        private readonly ILogger<YearEndCalculation> _logger;
        private readonly IDb _db;
        private readonly CurrentSession _currentSession;
        private readonly ITimezoneConverter _timeZoneConverter;
        public YearEndCalculation(ILogger<YearEndCalculation> logger, IDb db, CurrentSession currentSession, ITimezoneConverter timeZoneConverter)
        {
            _logger = logger;
            _db = db;
            _currentSession = currentSession;
            _timeZoneConverter = timeZoneConverter;
        }

        public async Task RunLeaveYearEndCycle(LeaveYearEnd leaveYearEnd)
        {
            if (string.IsNullOrEmpty(leaveYearEnd.ConnectionString))
                throw new Exception("Connection string is null");

            if (leaveYearEnd.Timezone == null)
                throw new Exception("Timezone is invalid");

            _db.SetupConnectionString(leaveYearEnd.ConnectionString);
            List<LeaveEndYearModal> leaveEndYearProcessingData = await LoadLeaveYearEndProcessingData();
            var offsetindex = 0;
            int index = 0;
            while (true)
            {
                try
                {
                    _logger.LogInformation("Calling: sp_leave_accrual_cycle_data_by_employee");
                    List<EmployeeAccrualData> employeeAccrualData = _db.GetList<EmployeeAccrualData>(Procedures.Leave_Accrual_Cycle_Data_By_Employee, new
                    {
                        EmployeeId = 0,
                        OffsetIndex = offsetindex,
                        PageSize = 500,
                        leaveYearEnd.ProcessingDateTime.Year
                    }, false);

                    if (employeeAccrualData == null || employeeAccrualData.Count == 0)
                    {
                        _logger.LogInformation("EmployeeAccrualData is null or count is 0");
                        break;
                    }
                    
                    foreach (EmployeeAccrualData emp in employeeAccrualData)
                    {
                        leaveYearEnd.EmployeeId = emp.EmployeeUid;
                        leaveYearEnd.CompanyId = emp.CompanyId;
                        var completeLeaveTypeBrief = await GetEmployeeLeaveQuotaDetail(leaveYearEnd);
                        var liveProcessingData = leaveEndYearProcessingData.Where(x => x.LeavePlanId == emp.LeavePlanId).ToList();
                        decimal totalConvertedAmtToPaid = 0;
                        liveProcessingData.ForEach(async i =>
                        {
                            if (i.IsLeaveBalanceExpiredOnEndOfYear)
                            {
                                await LeaveBalanceExpiredOnEndOfYear(completeLeaveTypeBrief, i.LeavePlanTypeId);
                            }
                            else if (i.AllConvertedToPaid)
                            {
                                totalConvertedAmtToPaid += await AllLeaveConvertedToPaid(completeLeaveTypeBrief, leaveYearEnd, i);
                            }
                            else if (i.AllLeavesCarryForwardToNextYear)
                            {
                                await AllLeavesCarryForwardToNextYear(completeLeaveTypeBrief, i);
                            }
                            else
                            {
                                if (i.PayFirstNCarryForwordRemaning)
                                {
                                    totalConvertedAmtToPaid += await PayFirstThenCarrayForward(i, completeLeaveTypeBrief, leaveYearEnd);
                                }
                                else if (i.CarryForwordFirstNPayRemaning)
                                {
                                    totalConvertedAmtToPaid += await CarrayForwardThenPayFirst(i, completeLeaveTypeBrief, leaveYearEnd);
                                }
                            }
                        });

                        if (totalConvertedAmtToPaid != 0)
                            await addConvertedAmountAsBonus(totalConvertedAmtToPaid, leaveYearEnd.EmployeeId, leaveYearEnd.CompanyId);

                        await addEmployeeLeaveDetail(completeLeaveTypeBrief, leaveYearEnd);
                        index++;
                        Console.WriteLine($"Success: {index}");
                    }

                    offsetindex += 500;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    break;
                }
            }

            await Task.CompletedTask;
        }

        private async Task<List<LeaveTypeBrief>> GetEmployeeLeaveQuotaDetail(LeaveYearEnd leaveYearEnd)
        {
            Leave existingLeave = _db.Get<Leave>(Procedures.Employee_Leave_Request_By_Empid, new
            {
                EmployeeId = leaveYearEnd.EmployeeId,
                Year = leaveYearEnd.ProcessingDateTime.Year
            });

            if (string.IsNullOrEmpty(existingLeave.LeaveQuotaDetail) || existingLeave.LeaveQuotaDetail == "[]")
                throw new Exception("Leave quota not found. Please contact to admin");

            var employeeLeaveQuto = JsonConvert.DeserializeObject<List<LeaveTypeBrief>>(existingLeave.LeaveQuotaDetail);
            return await Task.FromResult(employeeLeaveQuto);
        }

        private async Task<decimal> VerifyForNegetiveBalance(LeaveEndYearModal leaveEndYearProcessing, LeaveTypeBrief leaveQuota)
        {
            decimal balance = 0;
            if (leaveEndYearProcessing.DeductFromSalaryOnYearChange)
            {
                balance = leaveQuota.AvailableLeaves;
                leaveQuota.AvailableLeaves = 0;
                leaveQuota.AccruedSoFar = 0;
            }
            else if (leaveEndYearProcessing.ResetBalanceToZero)
            {
                balance = 0;
                leaveQuota.AvailableLeaves = 0;
                leaveQuota.AccruedSoFar = 0;
            }
            else if (leaveEndYearProcessing.CarryForwardToNextYear)
            {
                balance = 0;
                leaveQuota.AccruedSoFar = 0;
            }

            return await Task.FromResult(balance);
        }

        private async Task<decimal> PayFirstThenCarrayForward(LeaveEndYearModal leaveEndYearProcessing, List<LeaveTypeBrief> leaveTypeBriefs, LeaveYearEnd leaveYearEnd)
        {
            decimal payableAmount = 0;
            if (string.IsNullOrEmpty(leaveEndYearProcessing.PayNCarryForwardDefineType))
                throw new Exception("Invalid pay and carry forward type selected");

            var currentLeaveType = leaveTypeBriefs.Find(x => x.LeavePlanTypeId == leaveEndYearProcessing.LeavePlanTypeId);
            if (currentLeaveType == null)
                return payableAmount;

            if (currentLeaveType.AvailableLeaves > 0)
            {
                if (leaveEndYearProcessing.PayNCarryForwardDefineType.ToString() == "fixed")
                {
                    if (string.IsNullOrEmpty(leaveEndYearProcessing.FixedPayNCarryForward) || leaveEndYearProcessing.FixedPayNCarryForward == "[]")
                        throw new Exception("Fixed pay and carry forward detail not found");
                    else
                        leaveEndYearProcessing.AllFixedPayNCarryForward = JsonConvert.DeserializeObject<List<FixedPayNCarryForward>>(leaveEndYearProcessing.FixedPayNCarryForward);
                    
                    payableAmount += await FixedPayNCarryForward(leaveEndYearProcessing.AllFixedPayNCarryForward, currentLeaveType, leaveYearEnd);
                }
                else
                {
                    if (string.IsNullOrEmpty(leaveEndYearProcessing.PercentagePayNCarryForward) || leaveEndYearProcessing.PercentagePayNCarryForward == "[]")
                        throw new Exception("Percentage pay and carry forward detail not found");
                    else
                        leaveEndYearProcessing.AllPercentagePayNCarryForward = JsonConvert.DeserializeObject<List<PercentagePayNCarryForward>>(leaveEndYearProcessing.PercentagePayNCarryForward);

                    payableAmount += await PercentagePayNCarryForward(leaveEndYearProcessing.AllPercentagePayNCarryForward, currentLeaveType, leaveYearEnd);
                }
            }
            else
            {
                payableAmount += await VerifyForNegetiveBalance(leaveEndYearProcessing, currentLeaveType);
            }

            if (leaveEndYearProcessing.DoestCarryForwardExpired)
            {
                await CarryForwardLeaveExpired();
            }
            else if (leaveEndYearProcessing.DoesExpiryLeaveRemainUnchange)
            {

            }
            return payableAmount;
        }

        private async Task<decimal> CarrayForwardThenPayFirst(LeaveEndYearModal leaveEndYearProcessing, List<LeaveTypeBrief> leaveTypeBriefs, LeaveYearEnd leaveYearEnd)
        {
            decimal payableAmount = 0;
            if (string.IsNullOrEmpty(leaveEndYearProcessing.PayNCarryForwardDefineType))
                throw new Exception("Invalid pay and carry forward type selected");

            var currentLeaveType = leaveTypeBriefs.Find(x => x.LeavePlanTypeId == leaveEndYearProcessing.LeavePlanTypeId);
            if (currentLeaveType == null)
                return payableAmount;

            if (currentLeaveType.AvailableLeaves > 0)
            {
                if (leaveEndYearProcessing.PayNCarryForwardDefineType.ToString() == "fixed")
                {
                    if (string.IsNullOrEmpty(leaveEndYearProcessing.FixedPayNCarryForward) || leaveEndYearProcessing.FixedPayNCarryForward == "[]")
                        throw new Exception("Fixed pay and carry forward detail not found");
                    else
                        leaveEndYearProcessing.AllFixedPayNCarryForward = JsonConvert.DeserializeObject<List<FixedPayNCarryForward>>(leaveEndYearProcessing.FixedPayNCarryForward);

                    payableAmount += await FixedPayNCarryForward(leaveEndYearProcessing.AllFixedPayNCarryForward, currentLeaveType, leaveYearEnd);
                }
                else
                {
                    if (string.IsNullOrEmpty(leaveEndYearProcessing.PercentagePayNCarryForward) || leaveEndYearProcessing.PercentagePayNCarryForward == "[]")
                        throw new Exception("Percentage pay and carry forward detail not found");
                    else
                        leaveEndYearProcessing.AllPercentagePayNCarryForward = JsonConvert.DeserializeObject<List<PercentagePayNCarryForward>>(leaveEndYearProcessing.PercentagePayNCarryForward);

                    payableAmount += await PercentagePayNCarryForward(leaveEndYearProcessing.AllPercentagePayNCarryForward, currentLeaveType, leaveYearEnd);
                }
            }
            else
            {
                payableAmount += await VerifyForNegetiveBalance(leaveEndYearProcessing, currentLeaveType);
            }

            if (leaveEndYearProcessing.DoestCarryForwardExpired)
            {
                await CarryForwardLeaveExpired();
            }
            else if (leaveEndYearProcessing.DoesExpiryLeaveRemainUnchange)
            {

            }
            return payableAmount;
        }

        private async Task LeaveBalanceExpiredOnEndOfYear(List<LeaveTypeBrief> leaveQuotaDetail, int leavePlanTypeId)
        {
            // reset all leave 0 i.e. initial setup
            var leaveQuota = leaveQuotaDetail.FirstOrDefault(x => x.LeavePlanTypeId == leavePlanTypeId);
            if (leaveQuota != null)
            {
                leaveQuota.AvailableLeaves = 0;
                leaveQuota.AccruedSoFar = 0;
            }

            await Task.CompletedTask;
        }

        private async Task<decimal> AllLeaveConvertedToPaid(List<LeaveTypeBrief> leaveQuotaDetail,
                                                            LeaveYearEnd leaveYearEnd, LeaveEndYearModal leaveEndYearProcessing)
        {
            // convert all leave as paid amount and reset to 0
            int leavePlanTypeId = leaveEndYearProcessing.LeavePlanTypeId;
            var leaveQuota = leaveQuotaDetail.FirstOrDefault(x => x.LeavePlanTypeId == leavePlanTypeId);
            if (leaveQuota == null)
            {
                return 0;
            }

            decimal availableLeaveBalance = leaveQuota.AvailableLeaves;
            if (leaveQuota.AvailableLeaves < 0)
                availableLeaveBalance = await VerifyForNegetiveBalance(leaveEndYearProcessing, leaveQuota);
            else
                leaveQuota.AvailableLeaves = 0;

            leaveQuota.AccruedSoFar = 0;
            var basicSalary = await GetEmployeeBasicSalary(leaveYearEnd);
            var totalAmountToPaid = (basicSalary * availableLeaveBalance);

            return await Task.FromResult(totalAmountToPaid);
        }

        private async Task AllLeavesCarryForwardToNextYear(List<LeaveTypeBrief> leaveQuotaDetail, LeaveEndYearModal leaveEndYearProcessing)
        {
            // all leaves are carry forward to next year
            var leaveQuota = leaveQuotaDetail.FirstOrDefault(x => x.LeavePlanTypeId == leaveEndYearProcessing.LeavePlanTypeId);
            if (leaveQuota != null)
            {
                if (leaveEndYearProcessing.DoestCarryForwardExpired)
                {
                    await CarryForwardLeaveExpired();
                }
                else if (leaveEndYearProcessing.DoesExpiryLeaveRemainUnchange)
                {

                }
            }

            await Task.CompletedTask;
        }

        private async Task<decimal> FixedPayNCarryForward(List<FixedPayNCarryForward> fixedPayNCarryForwards, LeaveTypeBrief currentLeaveType, LeaveYearEnd leaveYearEnd)
        {
            // according to fixed number of days.
            decimal payableAmount = 0;
            fixedPayNCarryForwards = fixedPayNCarryForwards.OrderByDescending(x => x.PayNCarryForwardRuleInDays).ToList();
            int i = 0;
            while (i < fixedPayNCarryForwards.Count)
            {
                if (currentLeaveType.AvailableLeaves > fixedPayNCarryForwards[i].PayNCarryForwardRuleInDays)
                {
                    decimal basicAmount = await GetEmployeeBasicSalary(leaveYearEnd);
                    payableAmount = (fixedPayNCarryForwards[i].PaybleForDays * basicAmount);
                    var remianingLeave = currentLeaveType.AvailableLeaves - fixedPayNCarryForwards[i].PaybleForDays;
                    if (remianingLeave > fixedPayNCarryForwards[i].CarryForwardForDays)
                        currentLeaveType.AvailableLeaves = fixedPayNCarryForwards[i].CarryForwardForDays;
                    else
                        currentLeaveType.AvailableLeaves = remianingLeave;

                    currentLeaveType.AccruedSoFar = 0;
                    break;
                }
                i++;
            }
            return await Task.FromResult(payableAmount);
        }

        private async Task<decimal> PercentagePayNCarryForward(List<PercentagePayNCarryForward> percentagePayNCarryForwards, LeaveTypeBrief currentLeaveType, LeaveYearEnd leaveYearEnd)
        {
            // according to percentage of remianing leave.
            decimal payableAmount = 0;
            percentagePayNCarryForwards = percentagePayNCarryForwards.OrderByDescending(x => x.PayNCarryForwardRuleInPercent).ToList();
            int i = 0;
            while (i < percentagePayNCarryForwards.Count)
            {
                if (currentLeaveType.AvailableLeaves > percentagePayNCarryForwards[i].PayNCarryForwardRuleInPercent)
                {
                    var payableLeaveCount = (percentagePayNCarryForwards[i].PayPercent * currentLeaveType.AvailableLeaves) / 100;
                    decimal basicAmount = await GetEmployeeBasicSalary(leaveYearEnd);
                    payableAmount = (payableLeaveCount * basicAmount);
                    var carryForwardLeaveCount = (percentagePayNCarryForwards[i].CarryForwardPercent * currentLeaveType.AvailableLeaves) / 100;
                    currentLeaveType.AvailableLeaves = carryForwardLeaveCount;
                    currentLeaveType.AccruedSoFar = 0;
                    break;
                }
                i++;
            }
            return await Task.FromResult(payableAmount);
        }

        private async Task CarryForwardLeaveExpired()
        {
            // carry forward leave expired after certain time.

            await Task.CompletedTask;
        }

        private async Task<List<LeaveEndYearModal>> LoadLeaveYearEndProcessingData()
        {
            _logger.LogInformation("Calling : SP_LEAVE_YEAREND_PROCESSING_ALL");
            var leaveEndYearProcessing = _db.GetList<LeaveEndYearModal>(Procedures.SP_LEAVE_YEAREND_PROCESSING_ALL);
            if (leaveEndYearProcessing == null || leaveEndYearProcessing.Count == 0)
            {
                _logger.LogError("Employee does not exist. Please contact to admin.");
                throw new HiringBellException("Employee does not exist. Please contact to admin.");
            }

            return await Task.FromResult(leaveEndYearProcessing);
        }

        private async Task<decimal> GetEmployeeBasicSalary(LeaveYearEnd leaveYearEnd)
        {
            // Get month day to be consider for calculation e.g. Weekdays = 22/23 or Month days = 30/31
            var ds = _db.GetDataSet(Procedures.EMPLOYEE_SALARY_DETAIL_BY_EMPID_YEAR, new
            {
                EmployeeId = leaveYearEnd.EmployeeId,
                Year = leaveYearEnd.ProcessingDateTime.Year,
                CompanyId = leaveYearEnd.CompanyId
            });
            if (ds == null || ds.Tables.Count != 3)
                throw new Exception("Fail to get employee salary, payroll and workshift detial");

            if (ds.Tables[1].Rows.Count == 0)
                throw new HiringBellException("Fail to get employee salary details. Please contact to admin.");

            if (ds.Tables[1].Rows.Count == 0)
                throw new HiringBellException("Fail to get payroll details. Please contact to admin.");

            if (ds.Tables[1].Rows.Count == 0)
                throw new HiringBellException("Fail to get workshift details. Please contact to admin.");

            EmployeeSalaryDetail employeeSalaryDetail = Converter.ToType<EmployeeSalaryDetail>(ds.Tables[0]);
            Payroll payroll = Converter.ToType<Payroll>(ds.Tables[1]);
            ShiftDetail shiftDetail = Converter.ToType<ShiftDetail>(ds.Tables[2]);
            int calculationDaysInCurrentMonth = 0;
            if (payroll.PayCalculationId == 1)  //PayCalculationId = 1 => Actual days in month, PayCalculationId = 2 => Weekday only
            {
                calculationDaysInCurrentMonth = DateTime.DaysInMonth(leaveYearEnd.ProcessingDateTime.Year, leaveYearEnd.ProcessingDateTime.Month);
            } else
            {
                var firstDayOfMonth = new DateTime(leaveYearEnd.ProcessingDateTime.Year, leaveYearEnd.ProcessingDateTime.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                calculationDaysInCurrentMonth = await GetWeekDaysBetweenTwoDate(firstDayOfMonth, lastDayOfMonth, shiftDetail);
            }

            List<AnnualSalaryBreakup> annualSalaryBreakup = null;
            if (!string.IsNullOrEmpty(employeeSalaryDetail.CompleteSalaryDetail) && employeeSalaryDetail.CompleteSalaryDetail != "[]")
                annualSalaryBreakup = JsonConvert.DeserializeObject<List<AnnualSalaryBreakup>>(employeeSalaryDetail.CompleteSalaryDetail);
            else
                annualSalaryBreakup = JsonConvert.DeserializeObject<List<AnnualSalaryBreakup>>(employeeSalaryDetail.NewSalaryDetail);

            var curretMonthSalary = annualSalaryBreakup.OrderByDescending(x => x.MonthNumber).ToList().FirstOrDefault();
            var basicSalary = curretMonthSalary.SalaryBreakupDetails.Find(x => x.ComponentId == ComponentNames.Basic);
            var perDayBasicSalary = (basicSalary.FinalAmount / calculationDaysInCurrentMonth);
            return await Task.FromResult(perDayBasicSalary);
        }

        private async Task addConvertedAmountAsBonus(decimal totalConvertedAmtToPaid, long employeeId, int companyId)
        {
            var presentDate = _timeZoneConverter.ToSpecificTimezoneDateTime(_currentSession.TimeZone);
            HikeBonusSalaryAdhoc hikeBonusSalaryAdhoc = new HikeBonusSalaryAdhoc
            {
                SalaryAdhocId = 0,
                EmployeeId = employeeId,
                OrganizationId = 1,
                CompanyId = companyId,
                IsPaidByCompany = true,
                IsFine = totalConvertedAmtToPaid > 0 ? false : true,
                IsHikeInSalary = false,
                IsBonus = totalConvertedAmtToPaid > 0 ? true : false,
                Amount = totalConvertedAmtToPaid,
                ApprovedBy = 0,
                IsRepeatJob = false,
                IsForSpecificPeriod = false,
                SequencePeriodOrder = 0,
                IsActive = true,
                Description = "Leave converted into " + (totalConvertedAmtToPaid > 0 ? "bonus" : "fine"),
                StartDate = presentDate.AddDays(1),
                EndDate = presentDate.AddDays(1),
            };
            var result = _db.Execute<HikeBonusSalaryAdhoc>(Procedures.HIKE_BONUS_SALARY_ADHOC_INS_UPDATE, hikeBonusSalaryAdhoc, true);
            if (string.IsNullOrEmpty(result))
                throw new Exception("Fail to insert leave paid amount");

            await Task.CompletedTask;
        }

        private async Task addEmployeeLeaveDetail(List<LeaveTypeBrief> completeLeaveTypeBrief, LeaveYearEnd leaveYearEnd)
        {
            decimal totalLeaveQuota = completeLeaveTypeBrief.Sum(x => x.TotalLeaveQuota);
            var result = _db.Execute<LeaveRequestDetail>(Procedures.Employee_Leave_Request_InsUpdate, new
            {
                LeaveRequestId = 0,
                EmployeeId = leaveYearEnd.EmployeeId,
                LeaveDetail = "[]",
                AvailableLeaves = 0,
                TotalLeaveApplied = 0,
                TotalApprovedLeave = 0,
                Year = leaveYearEnd.ProcessingDateTime.Year + 1,
                IsPending = false,
                TotalLeaveQuota = totalLeaveQuota,
                LeaveQuotaDetail = JsonConvert.SerializeObject(completeLeaveTypeBrief)
            }, true);
            if (string.IsNullOrEmpty(result))
                throw new Exception("Fail to insert employee leave detail");

            await Task.CompletedTask;
        }

        private async Task<int> GetWeekDaysBetweenTwoDate(DateTime fromDate, DateTime toDate, ShiftDetail shiftDetail)
        {
            int count = 0;
            while (fromDate.Date <= toDate.Date)
            {
                switch (fromDate.DayOfWeek)
                {
                    case DayOfWeek.Sunday:
                        if (shiftDetail.IsSun)
                            count++;
                        break;
                    case DayOfWeek.Monday:
                        if (shiftDetail.IsMon)
                            count++;
                        break;
                    case DayOfWeek.Tuesday:
                        if (shiftDetail.IsTue)
                            count++;
                        break;
                    case DayOfWeek.Wednesday:
                        if (shiftDetail.IsWed)
                            count++;
                        break;
                    case DayOfWeek.Thursday:
                        if (shiftDetail.IsThu)
                            count++;
                        break;
                    case DayOfWeek.Friday:
                        if (shiftDetail.IsFri)
                            count++;
                        break;
                    case DayOfWeek.Saturday:
                        if (shiftDetail.IsSat)
                            count++;
                        break;
                }
                fromDate = fromDate.AddDays(1);
            }
            return await Task.FromResult(count);
        }
    }
}