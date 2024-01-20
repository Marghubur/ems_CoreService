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
                        List<LeaveTypeBrief> completeLeaveTypeBrief = new List<LeaveTypeBrief>();
                        var existingLeave = await GetEmployeeLeaveRequestDetail(emp.EmployeeUid);
                        var liveProcessingData = leaveEndYearProcessingData.Where(x => x.LeavePlanId == emp.LeavePlanId).ToList();

                        liveProcessingData.ForEach(async i =>
                        {
                            if (i.IsLeaveBalanceExpiredOnEndOfYear)
                            {
                                await LeaveBalanceExpiredOnEndOfYear(existingLeave, i.LeavePlanTypeId);
                            }
                            else if (i.AllConvertedToPaid)
                            {
                                await AllLeaveConvertedToPaid();
                            }
                            else if (i.AllLeavesCarryForwardToNextYear)
                            {
                                await AllLeavesCarryForwardToNextYear(existingLeave, i.LeavePlanTypeId);
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

                        decimal totalLeaveQuota = completeLeaveTypeBrief.Sum(x => x.TotalLeaveQuota);
                        Leave newleaveDetail = new Leave
                        {
                            LeaveRequestId = 0,
                            EmployeeId = existingLeave.EmployeeId,
                            LeaveDetail = "[]",
                            AvailableLeaves = 0,
                            TotalLeaveApplied = 0,
                            TotalApprovedLeave = 0,
                            TotalLeaveQuota = totalLeaveQuota,
                            LeaveQuotaDetail = JsonConvert.SerializeObject(completeLeaveTypeBrief)
                        };
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

        private async Task<Leave> GetEmployeeLeaveRequestDetail(long employeeId)
        {
            var PresentDate = _timeZoneConverter.ToSpecificTimezoneDateTime(_currentSession.TimeZone);
            Leave existingLeave = _db.Get<Leave>(Procedures.Employee_Leave_Request_By_Empid, new
            {
                EmployeeId = employeeId,
                PresentDate.Year
            });
            return await Task.FromResult(existingLeave);
        }

        private async Task VerifyForNegetiveBalance(LeaveEndYearProcessing leaveEndYearProcessing)
        {
            if (leaveEndYearProcessing.DeductFromSalaryOnYearChange)
            {
                await DeductFromSalaryOnYearChange();
            }
            else if (leaveEndYearProcessing.ResetBalanceToZero)
            {
                await ResetLeaveBalanceToZero();
            }
            else if (leaveEndYearProcessing.CarryForwardToNextYear)
            {
                await CarryForwardNegativeLeave();
            }
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

        private async Task<LeaveTypeBrief> LeaveBalanceExpiredOnEndOfYear(Leave existingLeave, int leavePlanTypeId)
        {
            // reset all leave 0 i.e. initial setup
            if (string.IsNullOrEmpty(existingLeave.LeaveQuotaDetail) || existingLeave.LeaveQuotaDetail == "[]")
                throw new Exception("Leave quota not found. Please contact to admin");

            var leaveQuotaDetail = JsonConvert.DeserializeObject<List<LeaveTypeBrief>>(existingLeave.LeaveQuotaDetail);
            var leaveQuto = leaveQuotaDetail.FirstOrDefault(x => x.LeavePlanTypeId == leavePlanTypeId);
            leaveQuto.AvailableLeaves = leaveQuto.TotalLeaveQuota;

            return await Task.FromResult(leaveQuto);
        }

        private async Task AllLeaveConvertedToPaid()
        {
            // convert all leave as paid amount and reset to 0
            // 1. find number of days leave available
            // 2. find salary detail based on employeeid
            // 3. calculate basic * availabe leave
            // 4. add in HikeBonusSalsryAdhoc IsForSpecificPeriod, Start, End

            await Task.CompletedTask;
        }

        private async Task<LeaveTypeBrief> AllLeavesCarryForwardToNextYear(Leave existingLeave, int leavePlanTypeId)
        {
            // all leaves are carry forward to next year
            if (string.IsNullOrEmpty(existingLeave.LeaveQuotaDetail) || existingLeave.LeaveQuotaDetail == "[]")
                throw new Exception("Leave quota not found. Please contact to admin");

            var leaveQuotaDetail = JsonConvert.DeserializeObject<List<LeaveTypeBrief>>(existingLeave.LeaveQuotaDetail);
            var leaveQuto = leaveQuotaDetail.FirstOrDefault(x => x.LeavePlanTypeId == leavePlanTypeId);
            leaveQuto.AvailableLeaves = leaveQuto.TotalLeaveQuota + leaveQuto.AvailableLeaves;
            leaveQuto.TotalLeaveQuota = leaveQuto.TotalLeaveQuota + leaveQuto.AvailableLeaves;

            return await Task.FromResult(leaveQuto);
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

        private async Task CarryForwardNegativeLeave()
        {
            // carry forward -ve leave balance to next year.

            await Task.CompletedTask;
        }

        private async Task ResetLeaveBalanceToZero()
        {
            // reset the leave balance i.e 0.

            await Task.CompletedTask;
        }

        private async Task DeductFromSalaryOnYearChange()
        {
            //if leave balance is in -ve then amount deducted from salary.

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
    }
}
