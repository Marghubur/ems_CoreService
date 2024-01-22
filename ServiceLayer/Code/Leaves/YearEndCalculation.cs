using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using EMailService.Modal;
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

        public async Task RunAccrualCycle(CompanySetting companySetting)
        {
            _db.SetupConnectionString("server=tracker.io;port=3308;database=bottomhalf;User Id=root;password=live@Bottomhalf_001;Connection Timeout=30;Connection Lifetime=0;Min Pool Size=0;Max Pool Size=100;Pooling=true;");
            LeavePlan leavePlan = default;
            List<LeavePlanType> leavePlanTypes = default;
            _currentSession.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            List<LeaveEndYearProcessing> leaveEndYearProcessingData = await LoadLeaveYearEndProcessingData();

            var offsetindex = 0;
            while (true)
            {
                try
                {
                    _logger.LogInformation("Calling: sp_leave_accrual_cycle_data_by_employee");
                    List<EmployeeAccrualData> employeeAccrualData = _db.GetList<EmployeeAccrualData>(Procedures.Leave_Accrual_Cycle_Data_By_Employee, new
                    {
                        EmployeeId = 0,
                        OffsetIndex = offsetindex,
                        PageSize = 500
                    }, false);

                    if (employeeAccrualData == null || employeeAccrualData.Count == 0)
                    {
                        _logger.LogInformation("EmployeeAccrualData is null or count is 0");
                        break;
                    }

                    foreach (EmployeeAccrualData emp in employeeAccrualData)
                    {
                        var completeLeaveTypeBrief = await GetEmployeeLeaveQuotaDetail(emp.EmployeeUid);
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
                                totalConvertedAmtToPaid += await AllLeaveConvertedToPaid(completeLeaveTypeBrief, i.LeavePlanTypeId, emp.EmployeeUid, i);
                            }
                            else if (i.AllLeavesCarryForwardToNextYear)
                            {
                                await AllLeavesCarryForwardToNextYear(completeLeaveTypeBrief, i.LeavePlanTypeId);
                            }
                            else
                            {
                                if (i.PayFirstNCarryForwordRemaning)
                                {
                                    await PayFirstThenCarrayForward(i);
                                }
                                else if (i.CarryForwordFirstNPayRemaning)
                                {
                                    await CarrayForwardThenPayFirst(i);
                                }
                            }
                        });

                        if (totalConvertedAmtToPaid != 0)
                            await addConvertedAmountAsBonus(totalConvertedAmtToPaid, emp.EmployeeUid);

                        await addEmployeeLeaveDetail(completeLeaveTypeBrief, emp.EmployeeUid);
                    }

                    offsetindex += 500;
                }
                catch (Exception)
                {
                    break;
                }
            }

            await Task.CompletedTask;
        }

        private async Task<List<LeaveTypeBrief>> GetEmployeeLeaveQuotaDetail(long employeeId)
        {
            var PresentDate = _timeZoneConverter.ToSpecificTimezoneDateTime(_currentSession.TimeZone);
            Leave existingLeave = _db.Get<Leave>(Procedures.Employee_Leave_Request_By_Empid, new
            {
                EmployeeId = employeeId,
                Year = 2023
                //PresentDate.Year
            });

            if (string.IsNullOrEmpty(existingLeave.LeaveQuotaDetail) || existingLeave.LeaveQuotaDetail == "[]")
                throw new Exception("Leave quota not found. Please contact to admin");

            var employeeLeaveQuto = JsonConvert.DeserializeObject<List<LeaveTypeBrief>>(existingLeave.LeaveQuotaDetail);
            return await Task.FromResult(employeeLeaveQuto);
        }

        private async Task<decimal> VerifyForNegetiveBalance(LeaveEndYearProcessing leaveEndYearProcessing, LeaveTypeBrief leaveQuota)
        {
            decimal balance = 0;
            if (leaveEndYearProcessing.DeductFromSalaryOnYearChange)
            {
                balance = leaveQuota.AvailableLeaves;
                leaveQuota.AvailableLeaves = 0;
            }
            else if (leaveEndYearProcessing.ResetBalanceToZero)
            {
                balance = 0;
                leaveQuota.AvailableLeaves = 0;
            }
            else if (leaveEndYearProcessing.CarryForwardToNextYear)
            {
                balance = 0;
                leaveQuota.TotalLeaveQuota = leaveQuota.AvailableLeaves;
            }
            return await Task.FromResult(balance);
        }

        private async Task PayFirstThenCarrayForward(LeaveEndYearProcessing leaveEndYearProcessing)
        {
            if (!string.IsNullOrEmpty(leaveEndYearProcessing.FixedPayNCarryForward) && leaveEndYearProcessing.FixedPayNCarryForward != "[]")
            {
                leaveEndYearProcessing.AllFixedPayNCarryForward =
                    JsonConvert.DeserializeObject<List<FixedPayNCarryForward>>(leaveEndYearProcessing.FixedPayNCarryForward);
            }

            if (!string.IsNullOrEmpty(leaveEndYearProcessing.PercentagePayNCarryForward) && leaveEndYearProcessing.PercentagePayNCarryForward != "[]")
            {
                leaveEndYearProcessing.AllPercentagePayNCarryForward =
                    JsonConvert.DeserializeObject<List<PercentagePayNCarryForward>>(leaveEndYearProcessing.PercentagePayNCarryForward);
            }

            if (string.IsNullOrEmpty(leaveEndYearProcessing.PayNCarryForwardDefineType))
                throw new Exception("Invalid pay and carry forward type selected");

            if (leaveEndYearProcessing.PayNCarryForwardDefineType.ToString() == "fixed")
                await FixedPayNCarryForward(leaveEndYearProcessing.AllFixedPayNCarryForward);
            else
                await PercentagePayNCarryForward(leaveEndYearProcessing.AllPercentagePayNCarryForward);

            if (leaveEndYearProcessing.DoestCarryForwardExpired)
            {
                await CarryForwardLeaveExpired();
            }
            else if (leaveEndYearProcessing.DoesExpiryLeaveRemainUnchange)
            {

            }
        }

        private async Task CarrayForwardThenPayFirst(LeaveEndYearProcessing leaveEndYearProcessing)
        {
            if (!string.IsNullOrEmpty(leaveEndYearProcessing.FixedPayNCarryForward) && leaveEndYearProcessing.FixedPayNCarryForward != "[]")
            {
                leaveEndYearProcessing.AllFixedPayNCarryForward =
                    JsonConvert.DeserializeObject<List<FixedPayNCarryForward>>(leaveEndYearProcessing.FixedPayNCarryForward);
            }

            if (!string.IsNullOrEmpty(leaveEndYearProcessing.PercentagePayNCarryForward) && leaveEndYearProcessing.PercentagePayNCarryForward != "[]")
            {
                leaveEndYearProcessing.AllPercentagePayNCarryForward =
                    JsonConvert.DeserializeObject<List<PercentagePayNCarryForward>>(leaveEndYearProcessing.PercentagePayNCarryForward);
            }

            if (string.IsNullOrEmpty(leaveEndYearProcessing.PayNCarryForwardDefineType))
                throw new Exception("Invalid pay and carry forward type selected");

            if (leaveEndYearProcessing.PayNCarryForwardDefineType.ToString() == "fixed")
                await FixedPayNCarryForward(leaveEndYearProcessing.AllFixedPayNCarryForward);
            else
                await PercentagePayNCarryForward(leaveEndYearProcessing.AllPercentagePayNCarryForward);

            if (leaveEndYearProcessing.DoestCarryForwardExpired)
            {
                await CarryForwardLeaveExpired();
            }
            else if (leaveEndYearProcessing.DoesExpiryLeaveRemainUnchange)
            {

            }
        }

        private async Task LeaveBalanceExpiredOnEndOfYear(List<LeaveTypeBrief> leaveQuotaDetail, int leavePlanTypeId)
        {
            // reset all leave 0 i.e. initial setup
            var leaveQuto = leaveQuotaDetail.FirstOrDefault(x => x.LeavePlanTypeId == leavePlanTypeId);
            leaveQuto.AvailableLeaves = 0;
            await Task.CompletedTask;
        }

        private async Task<decimal> AllLeaveConvertedToPaid(List<LeaveTypeBrief> leaveQuotaDetail, int leavePlanTypeId,
                                                            long employeeId, LeaveEndYearProcessing leaveEndYearProcessing)
        {
            // convert all leave as paid amount and reset to 0
            // 1. find number of days leave available
            // 2. find salary detail based on employeeid
            // 3. calculate basic * availabe leave
            // 4. add in HikeBonusSalsryAdhoc IsForSpecificPeriod, Start, End

            var leaveQuto = leaveQuotaDetail.FirstOrDefault(x => x.LeavePlanTypeId == leavePlanTypeId);
            decimal availableLeaveBalance = leaveQuto.AvailableLeaves;
            if (leaveQuto.AvailableLeaves < 0)
                availableLeaveBalance = await VerifyForNegetiveBalance(leaveEndYearProcessing, leaveQuto);
            else
                leaveQuto.AvailableLeaves = 0;

            var basicSalary = await GetEmployeeBasicSalary(employeeId);
            var totalAmountToPaid = (basicSalary * availableLeaveBalance);
            return await Task.FromResult(totalAmountToPaid);
        }

        private async Task AllLeavesCarryForwardToNextYear(List<LeaveTypeBrief> leaveQuotaDetail, int leavePlanTypeId)
        {
            // all leaves are carry forward to next year
            var leaveQuto = leaveQuotaDetail.FirstOrDefault(x => x.LeavePlanTypeId == leavePlanTypeId);
            leaveQuto.TotalLeaveQuota = leaveQuto.TotalLeaveQuota + leaveQuto.AvailableLeaves;

            await Task.CompletedTask;
        }

        private async Task FixedPayNCarryForward(List<FixedPayNCarryForward> fixedPayNCarryForwards)
        {
            // according to fixed number of days.

            await Task.CompletedTask;
        }

        private async Task PercentagePayNCarryForward(List<PercentagePayNCarryForward> percentagePayNCarryForwards)
        {
            // according to percentage of remianing leave.

            await Task.CompletedTask;
        }

        private async Task CarryForwardLeaveExpired()
        {
            // carry forward leave expired after certain time.

            await Task.CompletedTask;
        }

        private async Task<List<LeaveEndYearProcessing>> LoadLeaveYearEndProcessingData()
        {
            _logger.LogInformation("Calling : SP_LEAVE_YEAREND_PROCESSING_ALL");
            var leaveEndYearProcessing = _db.GetList<LeaveEndYearProcessing>(Procedures.SP_LEAVE_YEAREND_PROCESSING_ALL);
            if (leaveEndYearProcessing == null || leaveEndYearProcessing.Count == 0)
            {
                _logger.LogError("Employee does not exist. Please contact to admin.");
                throw new HiringBellException("Employee does not exist. Please contact to admin.");
            }

            return await Task.FromResult(leaveEndYearProcessing);
        }

        private async Task<decimal> GetEmployeeBasicSalary(long employeeId)
        {
            var presentDate = _timeZoneConverter.ToSpecificTimezoneDateTime(_currentSession.TimeZone);
            EmployeeSalaryDetail employeeSalaryDetail = _db.Get<EmployeeSalaryDetail>(Procedures.EMPLOYEE_SALARY_DETAIL_BY_EMPID_YEAR, new
            {
                EmployeeId = employeeId,
                Year = 2023
                //Year = presentDate.Year
            });
            if (employeeSalaryDetail == null)
                throw new Exception("Employee salary detail not found");

            List<AnnualSalaryBreakup> annualSalaryBreakup = null;
            if (!string.IsNullOrEmpty(employeeSalaryDetail.CompleteSalaryDetail) && employeeSalaryDetail.CompleteSalaryDetail != "[]")
                annualSalaryBreakup = JsonConvert.DeserializeObject<List<AnnualSalaryBreakup>>(employeeSalaryDetail.CompleteSalaryDetail);
            else
                annualSalaryBreakup = JsonConvert.DeserializeObject<List<AnnualSalaryBreakup>>(employeeSalaryDetail.NewSalaryDetail);

            var curretMonthSalary = annualSalaryBreakup.OrderByDescending(x => x.MonthNumber).ToList().FirstOrDefault();
            var basicSalary = curretMonthSalary.SalaryBreakupDetails.Find(x => x.ComponentId == ComponentNames.Basic);
            var perDayBasicSalary = (basicSalary.FinalAmount / presentDate.Day);
            return await Task.FromResult(perDayBasicSalary);
        }

        private async Task addConvertedAmountAsBonus(decimal totalConvertedAmtToPaid, long employeeId)
        {
            var presentDate = _timeZoneConverter.ToSpecificTimezoneDateTime(_currentSession.TimeZone);
            HikeBonusSalaryAdhoc hikeBonusSalaryAdhoc = new HikeBonusSalaryAdhoc
            {
                SalaryAdhocId = 0,
                EmployeeId = employeeId,
                OrganizationId = 1,
                CompanyId = 1,
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
                Description = "Leave converted into" + (totalConvertedAmtToPaid > 0 ? "bonus" : "fine"),
                StartDate = presentDate.AddDays(1),
                EndDate = presentDate.AddDays(1),
            };
            var result = _db.Execute<HikeBonusSalaryAdhoc>(Procedures.HIKE_BONUS_SALARY_ADHOC_INS_UPDATE, hikeBonusSalaryAdhoc, true);
            if (string.IsNullOrEmpty(result))
                throw new Exception("Fail to insert leave paid amount");

            await Task.CompletedTask;
        }

        private async Task addEmployeeLeaveDetail(List<LeaveTypeBrief> completeLeaveTypeBrief, long employeeId)
        {
            decimal totalLeaveQuota = completeLeaveTypeBrief.Sum(x => x.TotalLeaveQuota);
            var presentDate = _timeZoneConverter.ToSpecificTimezoneDateTime(_currentSession.TimeZone);
            var result = _db.Execute<LeaveRequestDetail>(Procedures.Employee_Leave_Request_InsUpdate, new
            {
                LeaveRequestId = 0,
                EmployeeId = employeeId,
                LeaveDetail = "[]",
                AvailableLeaves = 0,
                TotalLeaveApplied = 0,
                TotalApprovedLeave = 0,
                Year = presentDate.Year + 1,
                IsPending = false,
                TotalLeaveQuota = totalLeaveQuota,
                LeaveQuotaDetail = JsonConvert.SerializeObject(completeLeaveTypeBrief)
            }, true);
            if (string.IsNullOrEmpty(result))
                throw new Exception("Fail to insert employee leave detail");

            await Task.CompletedTask;
        }
    }
}