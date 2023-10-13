using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using EMailService.Modal.Leaves;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ModalLayer;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using ModalLayer.Modal.Leaves;
using Newtonsoft.Json;
using ServiceLayer.Code.Leaves;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace ServiceLayer.Code
{
    public class LeaveCalculation : ILeaveCalculation
    {
        private readonly IDb _db;
        private LeavePlanConfiguration _leavePlanConfiguration;
        private readonly DateTime now = DateTime.UtcNow;
        private LeavePlanType _leavePlanType;
        private readonly ITimezoneConverter _timezoneConverter;
        private readonly CurrentSession _currentSession;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private readonly ICompanyCalendar _companyCalendar;
        private readonly Quota _quota;
        private readonly Accrual _accrual;
        private readonly Apply _apply;
        private readonly Restriction _restriction;
        private readonly IHolidaysAndWeekoffs _holidaysAndWeekoffs;
        private readonly Approval _approval;
        private readonly IFileService _fileService;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly ILogger<LeaveCalculation> _logger;
        public LeaveCalculation(IDb db,
            ITimezoneConverter timezoneConverter,
            CurrentSession currentSession,
            Quota quota,
            Accrual accrual,
            Apply apply,
            IHolidaysAndWeekoffs holidaysAndWeekoffs,
            Restriction restriction,
            Approval approval,
            ICompanyCalendar companyCalendar,
            IFileService fileService,
            FileLocationDetail fileLocationDetail,
            ILogger<LeaveCalculation> logger)
        {
            _db = db;
            _timezoneConverter = timezoneConverter;
            _currentSession = currentSession;
            _quota = quota;
            _accrual = accrual;
            _apply = apply;
            _restriction = restriction;
            _holidaysAndWeekoffs = holidaysAndWeekoffs;
            _approval = approval;
            _companyCalendar = companyCalendar;
            _fileService = fileService;
            _fileLocationDetail = fileLocationDetail;
            _logger = logger;
        }

        private async Task<List<LeaveTypeBrief>> PrepareLeaveType(List<LeaveTypeBrief> leaveTypeBrief, List<LeavePlanType> leavePlanTypes)
        {
            List<LeaveTypeBrief> leaveTypeBriefs = new List<LeaveTypeBrief>();
            leaveTypeBrief.ForEach(x =>
            {
                var planType = leavePlanTypes.Find(i => i.LeavePlanTypeId == x.LeavePlanTypeId);
                if (planType != null)
                {
                    var config = JsonConvert.DeserializeObject<LeavePlanConfiguration>(planType.PlanConfigurationDetail);
                    if (config == null)
                        throw HiringBellException.ThrowBadRequest($"[{nameof(PrepareLeaveType)}]: Fail to get leave configuration detail.");


                    if (config.leaveApplyDetail.EmployeeCanSeeAndApplyCurrentPlanLeave)
                    {
                        if (config.leaveApplyDetail.CurrentLeaveRequiredComments)
                            x.IsCommentsRequired = true;

                        x.IsHalfDay = config.leaveApplyDetail.IsAllowForHalfDay;
                        if (x.AvailableLeaves % 1.0m > 0)
                            x.AvailableLeaves = _accrual.RoundUpTheLeaves(x.AvailableLeaves, config);

                        x.IsFutureDateAllowed = config.leaveAccrual.CanApplyForFutureDate;
                        leaveTypeBriefs.Add(x);
                    }
                }
            });

            return await Task.FromResult(leaveTypeBriefs);
        }

        public async Task<LeaveCalculationModal> GetLeaveDetailService(long EmployeeId)
        {
            var result = _db.FetchDataSet("sp_leave_type_detail_get_by_employeeId", new { EmployeeId });
            if (!ApplicationConstants.IsValidDataSet(result, 4))
                throw HiringBellException.ThrowBadRequest($"Leave detail not found for employee id: {EmployeeId}");

            if (!ApplicationConstants.IsSingleRow(result.Tables[0]))
                throw HiringBellException.ThrowBadRequest($"Leave detail not found for employee id: {EmployeeId}");

            var leaveRequestDetail = Converter.ToType<LeaveRequestDetail>(result.Tables[0]);
            if (leaveRequestDetail == null || string.IsNullOrEmpty(leaveRequestDetail.LeaveQuotaDetail))
                throw HiringBellException.ThrowBadRequest($"Leave quota detail not found for employee id: {EmployeeId}");

            var leaveTypeBrief = JsonConvert.DeserializeObject<List<LeaveTypeBrief>>(leaveRequestDetail.LeaveQuotaDetail);

            var leavePlanTypes = Converter.ToList<LeavePlanType>(result.Tables[1]);
            if (leavePlanTypes.Count == 0)
                throw HiringBellException.ThrowBadRequest($"Leave detail not found for employee id: {EmployeeId}");

            // LeavePlanConfiguration leavePlanConfiguration = JsonConvert.DeserializeObject<LeavePlanConfiguration>(leavePlanType.PlanConfigurationDetail);

            List<LeaveTypeBrief> leaveTypeBriefs = await PrepareLeaveType(leaveTypeBrief, leavePlanTypes);

            var shiftDetail = Converter.ToType<ShiftDetail>(result.Tables[2]);
            if (shiftDetail == null)
                throw HiringBellException.ThrowBadRequest($"Shift detail not found for employee id: {EmployeeId}");

            List<LeaveRequestNotification> leaveRequestNotification = Converter.ToList<LeaveRequestNotification>(result.Tables[3]);

            LeaveCalculationModal leaveCalculationModal = new LeaveCalculationModal
            {
                shiftDetail = shiftDetail,
                leaveTypeBriefs = leaveTypeBriefs,
                lastAppliedLeave = leaveRequestNotification
            };

            return await Task.FromResult(leaveCalculationModal);
        }

        public async Task<List<CompanySetting>> StartAccrualCycle(RunAccrualModel runAccrualModel)
        {
            _logger.LogInformation("Start Accrual Cycle");
            var CompanySettings = _db.GetList<CompanySetting>("sp_company_setting_get_all");
            foreach (var setting in CompanySettings)
            {
                if (setting.LeaveAccrualRunCronDayOfMonth == DateTime.Now.Day)
                {
                    _currentSession.CurrentUserDetail.CompanyId = setting.CompanyId;
                    _currentSession.TimeZone = TZConvert.GetTimeZoneInfo(setting.TimezoneName);
                    await RunAccrualCycle(runAccrualModel);
                }
            }
            _logger.LogInformation("End Accrual Cycle");
            return CompanySettings;
        }

        public async Task RunAccrualCycle(RunAccrualModel runAccrualModel)
        {
            LeavePlan leavePlan = default;
            List<LeavePlanType> leavePlanTypes = default;
            LeaveCalculationModal leaveCalculationModal = await LoadLeaveMasterData();
            leaveCalculationModal.runTillMonthOfPresnetYear = runAccrualModel.RunTillMonthOfPresnetYear;

            var offsetindex = 0;
            while (true)
            {
                try
                {
                    _logger.LogInformation("Calling: sp_leave_accrual_cycle_data_by_employee");
                    List<EmployeeAccrualData> employeeAccrualData = _db.GetList<EmployeeAccrualData>("sp_leave_accrual_cycle_data_by_employee", new
                    {
                        EmployeeId = runAccrualModel.EmployeeId,
                        OffsetIndex = offsetindex,
                        PageSize = 500
                    }, false);

                    if (runAccrualModel.IsSingleRun && employeeAccrualData.Count > 1)
                        throw HiringBellException.ThrowBadRequest("While running employee accural getting multiple employee detail.");

                    if (employeeAccrualData == null || employeeAccrualData.Count == 0)
                    {
                        _logger.LogInformation("employeeAccrualData is null or count is 0");
                        break;
                    }

                    foreach (EmployeeAccrualData emp in employeeAccrualData)
                    {
                        leaveCalculationModal.employee = new Employee { CreatedOn = emp.CreatedOn };
                        leavePlan = leaveCalculationModal.leavePlans
                            .FirstOrDefault(x => emp.LeavePlanId > 0 ? x.LeavePlanId == emp.LeavePlanId : x.IsDefaultPlan == true);

                        if (runAccrualModel.RunTillMonthOfPresnetYear)
                        {
                            emp.LeaveTypeBrief = new List<LeaveTypeBrief>();
                        }
                        else
                        {
                            emp.LeaveTypeBrief = JsonConvert.DeserializeObject<List<LeaveTypeBrief>>(emp.LeaveQuotaDetail);
                            if (emp.LeaveTypeBrief == null)
                                emp.LeaveTypeBrief = new List<LeaveTypeBrief>();
                        }

                        if (leavePlan != null)
                        {
                            leavePlanTypes = JsonConvert.DeserializeObject<List<LeavePlanType>>(leavePlan.AssociatedPlanTypes);

                            int i = 0;
                            while (i < leavePlanTypes.Count)
                            {
                                var type = leaveCalculationModal.leavePlanTypes
                                    .FirstOrDefault(x => x.LeavePlanTypeId == leavePlanTypes[i].LeavePlanTypeId);
                                if (type != null)
                                {
                                    var availableLeaves = await RunAccrualCycleAsync(leaveCalculationModal, type);
                                    if (runAccrualModel.RunTillMonthOfPresnetYear)
                                        replaceLeaveTypeBriefCompletely(availableLeaves, emp.LeaveTypeBrief, type);
                                    else
                                        updateLeaveTypeBrief(availableLeaves, emp.LeaveTypeBrief, type, leaveCalculationModal);
                                }
                                else
                                {
                                    _logger.LogInformation("Leave plan type is null");
                                }

                                i++;
                            }
                        }
                        else
                        {
                            _logger.LogInformation("leavePlan is null");
                        }
                    }

                    await UpdateEmployeesRecord(employeeAccrualData);
                    offsetindex += 500;
                }
                catch (Exception)
                {
                    break;
                }
            }

            await Task.CompletedTask;
        }

        private void replaceLeaveTypeBriefCompletely(decimal availableLeaves, List<LeaveTypeBrief> brief, LeavePlanType planType)
        {
            brief.Add(new LeaveTypeBrief
            {
                LeavePlanTypeId = planType.LeavePlanTypeId,
                AvailableLeaves = availableLeaves,
                AccruedSoFar = availableLeaves,
                IsCommentsRequired = false,
                IsHalfDay = false,
                LeavePlanTypeName = planType.PlanName,
                TotalLeaveQuota = planType.MaxLeaveLimit
            });
        }

        public void updateLeaveTypeBrief(decimal availableLeaves, List<LeaveTypeBrief> brief, LeavePlanType planType, LeaveCalculationModal leaveCalculationModal)
        {
            var planBrief = brief.Find(x => x.LeavePlanTypeId == planType.LeavePlanTypeId);

            if (planBrief == null)
            {
                brief.Add(new LeaveTypeBrief
                {
                    LeavePlanTypeId = planType.LeavePlanTypeId,
                    AvailableLeaves = availableLeaves,
                    AccruedSoFar = availableLeaves,
                    IsCommentsRequired = false,
                    IsHalfDay = false,
                    LeavePlanTypeName = planType.PlanName,
                    TotalLeaveQuota = planType.MaxLeaveLimit
                });
            }
            else
            {
                if (leaveCalculationModal.IsAllLeaveAvailable)
                    planBrief.AccruedSoFar = availableLeaves;
                else
                    planBrief.AvailableLeaves += availableLeaves;
            }
        }

        public async Task RunAccrualCycleByEmployee(long EmployeeId)
        {
            LeavePlan leavePlan = default;
            List<LeavePlanType> leavePlanTypes = default;
            var leaveCalculationModal = await LoadLeaveMasterData();
            int runDay = leaveCalculationModal.companySetting.PayrollCycleMonthlyRunDay;
            try
            {
                EmployeeAccrualData employeeAccrual = _db.Get<EmployeeAccrualData>("SP_Employees_ById", new { EmployeeId = EmployeeId, IsActive = true });

                if (employeeAccrual == null)
                    throw HiringBellException.ThrowBadRequest("Employee detail not found. Please contact to admin.");

                employeeAccrual.LeaveTypeBrief = new List<LeaveTypeBrief>();
                if (leaveCalculationModal.employee == null)
                    leaveCalculationModal.employee = new Employee { CreatedOn = employeeAccrual.CreatedOn };
                leavePlan = leaveCalculationModal.leavePlans
                                .FirstOrDefault(x => employeeAccrual.LeavePlanId > 0 ? x.LeavePlanId == employeeAccrual.LeavePlanId : x.IsDefaultPlan == true);
                if (leavePlan != null)
                {
                    leavePlanTypes = JsonConvert.DeserializeObject<List<LeavePlanType>>(leavePlan.AssociatedPlanTypes);

                    int i = 0;
                    while (i < leavePlanTypes.Count)
                    {
                        var type = leaveCalculationModal.leavePlanTypes
                           .FirstOrDefault(x => x.LeavePlanTypeId == leavePlanTypes[i].LeavePlanTypeId);
                        if (type != null)
                        {
                            var availableLeaves = await RunAccrualCycleAsync(leaveCalculationModal, type);
                            employeeAccrual.LeaveTypeBrief.Add(BuildLeaveTypeBrief(type, availableLeaves));
                        }

                        i++;
                    }
                }
                await UpdateEmployeesRecord(new List<EmployeeAccrualData> { employeeAccrual });
            }
            catch (Exception)
            {
                throw;
            }

            await Task.CompletedTask;
        }

        private LeaveTypeBrief BuildLeaveTypeBrief(LeavePlanType type, decimal availableLeaves)
        {
            var leaveBriefs = new LeaveTypeBrief
            {
                LeavePlanTypeId = type.LeavePlanTypeId,
                AvailableLeaves = availableLeaves,
                AccruedSoFar = availableLeaves,
                IsCommentsRequired = false,
                IsHalfDay = false,
                LeavePlanTypeName = type.PlanName,
                TotalLeaveQuota = type.MaxLeaveLimit
            };

            return leaveBriefs;
        }

        private async Task UpdateEmployeesRecord(List<EmployeeAccrualData> employeeAccrualData)
        {
            _logger.LogInformation("Method: UpdateEmployeesRecord strated");
            var tableJsonData = (from r in employeeAccrualData
                                 select new
                                 {
                                     EmployeeId = r.EmployeeUid,
                                     Year = _timezoneConverter.ToTimeZoneDateTime(DateTime.UtcNow, _currentSession.TimeZone).Year,
                                     LeaveTypeBriefJson = JsonConvert.SerializeObject(r.LeaveTypeBrief)
                                 }).ToList();
            _logger.LogInformation("Calling: sp_employee_leave_request_update_accrual_detail");

            var rowsAffected = await _db.BulkExecuteAsync("sp_employee_leave_request_update_accrual_detail", tableJsonData, false);
            if (rowsAffected != employeeAccrualData.Count)
            {
                _logger.LogError("Fail to update leave deatil. Please contact to admin");
                throw new HiringBellException("Fail to update leave deatil. Please contact to admin");
            }
            _logger.LogInformation("Method: UpdateEmployeesRecord end");

        }

        private async Task<LeaveCalculationModal> LoadLeaveMasterData()
        {
            var leaveCalculationModal = new LeaveCalculationModal();
            leaveCalculationModal.timeZonePresentDate = DateTime.UtcNow;
            _logger.LogInformation("Calling : sp_leave_accrual_cycle_master_data");
            var ds = _db.GetDataSet("sp_leave_accrual_cycle_master_data", new { _currentSession.CurrentUserDetail.CompanyId }, false);

            if (ds != null && ds.Tables.Count == 3)
            {
                //if (ds.Tables[0].Rows.Count == 0 || ds.Tables[1].Rows.Count == 0 || ds.Tables[3].Rows.Count == 0)
                if (ds.Tables[0].Rows.Count == 0 || ds.Tables[1].Rows.Count == 0)
                {
                    _logger.LogError("Fail to get employee related details. Please contact to admin.");
                    throw new HiringBellException("Fail to get employee related details. Please contact to admin.");
                }

                // leaveCalculationModal.employee = Converter.ToType<Employee>(ds.Tables[0]);

                // load all leave plan type data
                leaveCalculationModal.leavePlans = Converter.ToList<LeavePlan>(ds.Tables[0]);
                leaveCalculationModal.leaveRequestDetail = new LeaveRequestDetail();
                leaveCalculationModal.leaveRequestDetail.EmployeeLeaveQuotaDetail = new List<EmployeeLeaveQuota>();

                // load all leave plan data
                leaveCalculationModal.leavePlanTypes = Converter.ToList<LeavePlanType>(ds.Tables[1]);

                leaveCalculationModal.companySetting = Converter.ToType<CompanySetting>(ds.Tables[2]);
            }
            else
            {
                _logger.LogError("Employee does not exist. Please contact to admin.");
                throw new HiringBellException("Employee does not exist. Please contact to admin.");
            }

            return await Task.FromResult(leaveCalculationModal);
        }

        public async Task<LeaveCalculationModal> GetBalancedLeave(long EmployeeId, DateTime FromDate, DateTime ToDate)
        {
            var leaveCalculationModal = await GetCalculationModal(EmployeeId, FromDate, ToDate);

            int i = 0;
            while (i < leaveCalculationModal.leavePlanTypes.Count)
            {
                await ProcessLeaveSections(leaveCalculationModal, leaveCalculationModal.leavePlanTypes[i]);
                i++;
            }

            return leaveCalculationModal;
        }

        private async Task<LeaveCalculationModal> LoadPrepareRequiredData(LeaveRequestModal leaveRequestModal)
        {
            var leaveCalculationModal = await GetCalculationModal(
                leaveRequestModal.EmployeeId,
                leaveRequestModal.LeaveFromDay,
                leaveRequestModal.LeaveToDay);

            leaveCalculationModal.LeaveTypeId = leaveRequestModal.LeaveTypeId;
            leaveCalculationModal.leaveRequestDetail.Reason = leaveRequestModal.Reason;
            if (leaveRequestModal.Session == (int)CommonFlags.HalfDay)
            {
                leaveCalculationModal.isApplyingForHalfDay = true;
                leaveCalculationModal.numberOfLeaveApplyring = 0.5m;
            }

            if (leaveRequestModal.DocumentProffAttached)
                leaveCalculationModal.DocumentProffAttached = true;

            _leavePlanType = leaveCalculationModal.leavePlanTypes.Find(x => x.LeavePlanTypeId == leaveRequestModal.LeaveTypeId);

            if (_leavePlanType == null)
                throw HiringBellException.ThrowBadRequest("Leave plan type not found.");

            // get current leave plan configuration and check if its valid one.
            ValidateAndGetLeavePlanConfiguration(_leavePlanType);
            leaveCalculationModal.leavePlanConfiguration = _leavePlanConfiguration;

            return await Task.FromResult(leaveCalculationModal);
        }

        private void CheckProjectedFutureLeaves(LeaveRequestModal leaveRequestModal, LeaveCalculationModal leaveCalculationModal)
        {
            // check future proejcted date
            if (leaveRequestModal.IsProjectedFutureDateAllowed)
            {
                decimal leavePerMonth = 0;
                var planType = leaveCalculationModal.leaveTypeBriefs.Find(x => x.LeavePlanTypeId == leaveRequestModal.LeaveTypeId);
                string seq = leaveCalculationModal.leavePlanConfiguration.leaveAccrual.LeaveDistributionSequence;
                switch (seq)
                {
                    case "1":
                        leavePerMonth = planType.TotalLeaveQuota / 12m;
                        break;
                    case "2":
                        leavePerMonth = planType.TotalLeaveQuota / 4m;
                        break;
                    case "3":
                        leavePerMonth = planType.TotalLeaveQuota / 6m;
                        break;
                }


                leavePerMonth = _accrual.ProjectedFutureLeaveAccrualedBalance(
                    leaveRequestModal.LeaveFromDay,
                    leavePerMonth,
                    leaveCalculationModal.leavePlanConfiguration);

                leaveCalculationModal.ProjectedFutureLeave = leavePerMonth;
                if ((planType.AvailableLeaves + leavePerMonth) < leaveCalculationModal.numberOfLeaveApplyring)
                    throw HiringBellException.ThrowBadRequest("Total leave applying is exceeding from available (with projected) leaves");
            }
        }

        public async Task<LeaveCalculationModal> PrepareCheckLeaveCriteria(LeaveRequestModal leaveRequestModal)
        {

            //LeavePlanType leavePlanType = default;
            var leaveCalculationModal = await LoadPrepareRequiredData(leaveRequestModal);

            // check future proejcted date
            CheckProjectedFutureLeaves(leaveRequestModal, leaveCalculationModal);

            // check is applying for conflicting day or already applied on the same day
            await SameDayRequestValidationCheck(leaveCalculationModal);

            // call apply leave
            await _apply.CheckLeaveApplyRules(leaveCalculationModal, _leavePlanType);

            // check and remove holiday and weekoffs
            await _holidaysAndWeekoffs.CheckHolidayWeekOffRules(leaveCalculationModal);

            // call leave quote
            // await _quota.CalculateFinalLeaveQuota(leaveCalculationModal, leavePlanType);


            // call leave restriction
            _restriction.CheckRestrictionForLeave(leaveCalculationModal, _leavePlanType);

            // call leave approval
            await _approval.CheckLeaveApproval(leaveCalculationModal);
            return leaveCalculationModal;
        }

        private async Task<decimal> RunAccrualCycleAsync(LeaveCalculationModal leaveCalculationModal, LeavePlanType leavePlanType)
        {
            _logger.LogInformation("Method: RunAccrualCycleAsync started");

            decimal availableLeaves = 0;

            // get current leave plan configuration and check if its valid one.
            ValidateAndGetLeavePlanConfiguration(leavePlanType);
            leaveCalculationModal.leavePlanConfiguration = _leavePlanConfiguration;

            // check if your is in probation period
            CheckForProbationPeriod(leaveCalculationModal);

            // check if your is in probation period
            CheckForNoticePeriod(leaveCalculationModal);

            // call leave accrual
            if (leaveCalculationModal.runTillMonthOfPresnetYear)
                availableLeaves = await _accrual.CalculateLeaveAccrualTillMonth(leaveCalculationModal, leavePlanType);
            else
                availableLeaves = await _accrual.CalculateLeaveAccrual(leaveCalculationModal, leavePlanType);
            _logger.LogInformation("Method: RunAccrualCycleAsync end");

            return await Task.FromResult(availableLeaves);
        }

        private async Task ProcessLeaveSections(LeaveCalculationModal leaveCalculationModal, LeavePlanType leavePlanType)
        {
            // get current leave plan configuration and check if its valid one.
            ValidateAndGetLeavePlanConfiguration(leavePlanType);
            leaveCalculationModal.leavePlanConfiguration = _leavePlanConfiguration;

            await ComputeApplyingLeaveDays(leaveCalculationModal);

            // call leave quote
            await _quota.CalculateFinalLeaveQuota(leaveCalculationModal, leavePlanType);

            // call leave by management

            // call leave accrual
            await _accrual.CalculateLeaveAccrual(leaveCalculationModal, leavePlanType);

            // call apply leave
            await _apply.CheckLeaveApplyRules(leaveCalculationModal, leavePlanType);

            // call leave restriction
            _restriction.CheckRestrictionForLeave(leaveCalculationModal, leavePlanType);

            // call holiday and weekoff
            // call leave approval
            // call year end processing


            // await RunEmployeeLeaveAccrualCycle(leaveCalculationModal, leavePlanType);

            await Task.CompletedTask;
        }

        private async Task ComputeApplyingLeaveDays(LeaveCalculationModal leaveCalculationModal)
        {
            leaveCalculationModal.numberOfLeaveApplyring = 0;
            if (_leavePlanConfiguration.leaveHolidaysAndWeekoff.AdJoiningHolidayIsConsiderAsLeave)
            {

            }

            if (!_leavePlanConfiguration.leaveHolidaysAndWeekoff.AdjoiningWeekOffIsConsiderAsLeave)
            {
                var fromDate = _timezoneConverter.ToTimeZoneDateTime(
                    leaveCalculationModal.fromDate.ToUniversalTime(),
                    _currentSession.TimeZone
                    );

                var toDate = _timezoneConverter.ToTimeZoneDateTime(
                    leaveCalculationModal.toDate.ToUniversalTime(),
                    _currentSession.TimeZone
                    );

                while (toDate.Subtract(fromDate).TotalDays >= 0)
                {
                    if (fromDate.DayOfWeek != DayOfWeek.Saturday && fromDate.DayOfWeek != DayOfWeek.Sunday)
                        leaveCalculationModal.numberOfLeaveApplyring++;

                    fromDate = fromDate.AddDays(1);
                }
            }

            await Task.CompletedTask;
        }


        private void CheckSameDateAlreadyApplied(List<LeaveRequestNotification> completeLeaveDetails, LeaveCalculationModal leaveCalculationModal)
        {
            try
            {
                if (completeLeaveDetails.Count > 0)
                {
                    decimal backDayLimit = _leavePlanConfiguration.leaveApplyDetail.BackDateLeaveApplyNotBeyondDays;
                    DateTime initFilterDate = now.AddDays(Convert.ToDouble(-backDayLimit));

                    var empLeave = completeLeaveDetails
                                    .Where(x => x.FromDate.Subtract(initFilterDate).TotalDays >= 0);
                    if (empLeave.Any())
                    {
                        var startDate = leaveCalculationModal.fromDate;
                        var endDate = leaveCalculationModal.toDate;
                        Parallel.ForEach(empLeave, i =>
                        {
                            if (i.FromDate.Month == startDate.Month)
                            {
                                if (startDate.Date.Subtract(i.FromDate.Date).TotalDays >= 0 &&
                                    startDate.Date.Subtract(i.ToDate.Date).TotalDays <= 0)
                                    throw new HiringBellException($"From date: " +
                                        $"{_timezoneConverter.ToTimeZoneDateTime(startDate, _currentSession.TimeZone)} " +
                                        $"already exist in another leave request");
                            }

                            if (i.ToDate.Month == endDate.Month)
                            {
                                if (endDate.Date.Subtract(i.FromDate.Date).TotalDays >= 0 &&
                                    endDate.Date.Subtract(i.ToDate.Date).TotalDays <= 0)
                                    throw new HiringBellException($"To date: " +
                                        $"{_timezoneConverter.ToTimeZoneDateTime(endDate, _currentSession.TimeZone)} " +
                                        $"already exist in another leave request");
                            }
                        });
                    }
                }
            }
            catch (AggregateException ax)
            {
                if (ax.Flatten().InnerExceptions.Count > 0)
                {
                    var hex = ax.Flatten().InnerExceptions.ElementAt(0) as HiringBellException;
                    throw hex;
                }

                throw;
            }
        }

        private async Task SameDayRequestValidationCheck(LeaveCalculationModal leaveCalculationModal)
        {
            if (!string.IsNullOrEmpty(leaveCalculationModal.leaveRequestDetail.LeaveDetail))
            {
                List<LeaveRequestNotification> completeLeaveDetails = leaveCalculationModal.lastAppliedLeave;
                //List<CompleteLeaveDetail> completeLeaveDetails = JsonConvert.DeserializeObject<List<CompleteLeaveDetail>>(leaveCalculationModal.leaveRequestDetail.LeaveDetail);
                //completeLeaveDetails = completeLeaveDetails.Where(x => x.LeaveStatus != (int)ItemStatus.Rejected).ToList();
                if (completeLeaveDetails.Count > 0)
                {
                    CheckSameDateAlreadyApplied(completeLeaveDetails, leaveCalculationModal);
                }
            }

            await Task.CompletedTask;
        }

        private void ValidateAndGetLeavePlanConfiguration(LeavePlanType leavePlanType)
        {
            // fetching data from database using leaveplantypeId
            _leavePlanConfiguration = JsonConvert.DeserializeObject<LeavePlanConfiguration>(leavePlanType.PlanConfigurationDetail);
            if (_leavePlanConfiguration == null)
            {
                _logger.LogInformation("Leave setup/configuration is not defined. Please complete the setup/configuration first.");
                throw new HiringBellException("Leave setup/configuration is not defined. Please complete the setup/configuration first.");
            }
        }

        private void LoadCalculationData(long EmployeeId, LeaveCalculationModal leaveCalculationModal)
        {
            var ds = _db.FetchDataSet("sp_leave_plan_calculation_get", new
            {
                EmployeeId,
                _currentSession.CurrentUserDetail.ReportingManagerId,
                IsActive = 1,
                Year = now.Year
            }, false);

            if (ds != null && ds.Tables.Count == 8)
            {
                //if (ds.Tables[0].Rows.Count == 0 || ds.Tables[1].Rows.Count == 0 || ds.Tables[3].Rows.Count == 0)
                if (ds.Tables[0].Rows.Count == 0 || ds.Tables[1].Rows.Count == 0)
                    throw new HiringBellException("Fail to get employee related details. Please contact to admin.");

                leaveCalculationModal.employee = Converter.ToType<Employee>(ds.Tables[0]);
                if (string.IsNullOrEmpty(leaveCalculationModal.employee.LeaveTypeBriefJson))
                    throw HiringBellException.ThrowBadRequest("Unable to get employee leave detail. Please contact to admin.");

                leaveCalculationModal.leaveTypeBriefs = JsonConvert.DeserializeObject<List<LeaveTypeBrief>>(leaveCalculationModal.employee.LeaveTypeBriefJson);
                if (leaveCalculationModal.leaveTypeBriefs.Count == 0)
                    throw HiringBellException.ThrowBadRequest("Unable to get employee leave detail. Please contact to admin.");

                leaveCalculationModal.shiftDetail = Converter.ToType<ShiftDetail>(ds.Tables[5]);
                leaveCalculationModal.leavePlanTypes = Converter.ToList<LeavePlanType>(ds.Tables[1]);
                leaveCalculationModal.leaveRequestDetail = Converter.ToType<LeaveRequestDetail>(ds.Tables[2]);
                leaveCalculationModal.lastAppliedLeave = Converter.ToList<LeaveRequestNotification>(ds.Tables[6]);
                if (leaveCalculationModal.lastAppliedLeave.Count > 0)
                    leaveCalculationModal.lastAppliedLeave = leaveCalculationModal.lastAppliedLeave.Where(x => x.RequestStatusId != (int)ItemStatus.Rejected).ToList();

                if (!string.IsNullOrEmpty(leaveCalculationModal.leaveRequestDetail.LeaveQuotaDetail))
                    leaveCalculationModal.leaveRequestDetail.EmployeeLeaveQuotaDetail = JsonConvert
                        .DeserializeObject<List<EmployeeLeaveQuota>>(leaveCalculationModal.leaveRequestDetail.LeaveQuotaDetail);
                else
                    leaveCalculationModal.leaveRequestDetail.EmployeeLeaveQuotaDetail = new List<EmployeeLeaveQuota>();

                leaveCalculationModal.companySetting = Converter.ToType<CompanySetting>(ds.Tables[3]);
                leaveCalculationModal.leavePlan = Converter.ToType<LeavePlan>(ds.Tables[4]);
                leaveCalculationModal.projectMemberDetail = Converter.ToList<ProjectMemberDetail>(ds.Tables[7]);
            }
            else
                throw new HiringBellException("Employee does not exist. Please contact to admin.");
        }

        private void CheckForProbationPeriod(LeaveCalculationModal leaveCalculationModal)
        {
            _logger.LogInformation("Method: CheckForProbationPeriod started");
            leaveCalculationModal.employeeType = ApplicationConstants.Regular;
            if ((leaveCalculationModal.employee.CreatedOn.AddDays(leaveCalculationModal.companySetting.ProbationPeriodInDays))
                .Subtract(now).TotalDays > 0)
            {
                leaveCalculationModal.employeeType = ApplicationConstants.InProbationPeriod;
                leaveCalculationModal.probationEndDate = leaveCalculationModal.employee
                    .CreatedOn.AddDays(leaveCalculationModal.companySetting.ProbationPeriodInDays);
            }
            _logger.LogInformation("Method: CheckForProbationPeriod end");
        }

        private void CheckForNoticePeriod(LeaveCalculationModal leaveCalculationModal)
        {
            if (leaveCalculationModal.employee.NoticePeriodId != 0 && leaveCalculationModal.employee.NoticePeriodAppliedOn != null)
                leaveCalculationModal.employeeType = ApplicationConstants.InNoticePeriod;
        }

        private async Task<LeaveCalculationModal> GetCalculationModal(long EmployeeId, DateTime FromDate, DateTime ToDate)
        {
            var leaveCalculationModal = new LeaveCalculationModal();
            leaveCalculationModal.fromDate = FromDate;
            leaveCalculationModal.toDate = ToDate;
            leaveCalculationModal.timeZoneFromDate = _timezoneConverter.ToTimeZoneDateTime(FromDate, _currentSession.TimeZone);
            leaveCalculationModal.timeZoneToDate = _timezoneConverter.ToTimeZoneDateTime(ToDate, _currentSession.TimeZone);
            leaveCalculationModal.timeZonePresentDate = _timezoneConverter.ToTimeZoneDateTime(DateTime.UtcNow, _currentSession.TimeZone);
            leaveCalculationModal.utcPresentDate = DateTime.UtcNow;
            leaveCalculationModal.numberOfLeaveApplyring = Convert.ToDecimal(ToDate.Date.Subtract(FromDate.Date).TotalDays + 1);
            var holidays = await _companyCalendar.GetHolidayBetweenTwoDates(FromDate, ToDate);
            // get employee detail and store it in class level variable
            LoadCalculationData(EmployeeId, leaveCalculationModal);

            var weekoff = _holidaysAndWeekoffs.WeekOffCountIfBetweenLeaveDates(leaveCalculationModal);
            leaveCalculationModal.numberOfLeaveApplyring = leaveCalculationModal.numberOfLeaveApplyring - (weekoff + holidays);

            // Check employee is in probation period
            CheckForProbationPeriod(leaveCalculationModal);

            // Check employee is in notice period
            CheckForNoticePeriod(leaveCalculationModal);


            return await Task.FromResult(leaveCalculationModal);
        }

        #region APPLY FOR LEAVE

        public async Task<LeaveCalculationModal> CheckAndApplyForLeave(LeaveRequestModal leaveRequestModal, IFormFileCollection fileCollection, List<Files> fileDetail)
        {
            try
            {
                if (fileDetail != null && fileDetail.Count > 0)
                    leaveRequestModal.DocumentProffAttached = true;

                var leaveCalculationModal = await PrepareCheckLeaveCriteria(leaveRequestModal);
                LeavePlanType leavePlanType = leaveCalculationModal.leavePlanTypes.Find(x => x.LeavePlanTypeId == leaveRequestModal.LeaveTypeId);

                List<string> reporterEmail = await ApplyAndSaveChanges(leaveCalculationModal, leaveRequestModal, fileCollection, fileDetail);
                leaveCalculationModal.ReporterEmail = reporterEmail;
                return leaveCalculationModal;
            }
            catch
            {
                throw;
            }
        }

        private async Task<List<string>> ApplyAndSaveChanges(LeaveCalculationModal leaveCalculationModal, LeaveRequestModal leaveRequestModal, IFormFileCollection fileCollection, List<Files> fileDetail)
        {
            var leavePlanType = leaveCalculationModal.leavePlanTypes.Find(x => x.LeavePlanTypeId == leaveRequestModal.LeaveTypeId);
            if (leavePlanType == null)
                throw HiringBellException.ThrowBadRequest("Fail to get leave plan type detai. Please contact to admin.");

            ValidateAndGetLeavePlanConfiguration(leavePlanType);
            decimal totalAllocatedLeave = leaveCalculationModal.leavePlanTypes.Sum(x => x.MaxLeaveLimit);

            List<int> leaveDetails = new List<int>();
            var fileIds = await SaveLeaveAttachment(fileCollection, fileDetail, leaveCalculationModal.employee);
            List<string> emails = new List<string>();
            LeaveRequestNotification requestChainDetail = GetApprovalChainDetail(leaveRequestModal.EmployeeId, out emails);

            string result = _db.Execute<LeaveRequestNotification>("sp_leave_request_notification_InsUpdate", new
            {
                LeaveRequestNotificationId = 0,
                LeaveRequestId = leaveCalculationModal.leaveRequestDetail.LeaveRequestId,
                UserMessage = leaveRequestModal.Reason,
                leaveRequestModal.EmployeeId,
                ReportingManagerId = leaveCalculationModal.employee.ReportingManagerId,
                ProjectId = 0,
                ProjectName = string.Empty,
                FromDate = leaveRequestModal.LeaveFromDay,
                ToDate = leaveRequestModal.LeaveToDay,
                NumOfDays = Convert.ToDecimal(leaveCalculationModal.numberOfLeaveApplyring),
                RequestStatusId = (int)ItemStatus.Pending,
                NoOfApprovalsRequired = requestChainDetail.NoOfApprovalsRequired,
                ReporterDetail = requestChainDetail.ReporterDetail,
                FileIds = fileIds,
                FeedBack = ApplicationConstants.EmptyJsonArray,
                LeaveTypeName = leaveRequestModal.LeavePlanName,
                AutoActionAfterDays = requestChainDetail.AutoActionAfterDays,
                IsAutoApprovedEnabled = requestChainDetail.IsAutoApprovedEnabled,
                leaveCalculationModal.LeaveTypeId,
                AdminId = _currentSession.CurrentUserDetail.UserId
            }, true);

            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("fail to insert or update leave notification detail");

            leaveCalculationModal.leaveRequestDetail.LeaveRequestNotificationId = int.Parse(result);
            if (leaveCalculationModal.leaveRequestDetail.LeaveDetail != null && leaveCalculationModal.leaveRequestDetail.LeaveDetail != "[]")
                leaveDetails = JsonConvert.DeserializeObject<List<int>>(leaveCalculationModal.leaveRequestDetail.LeaveDetail);

            leaveDetails.Add(leaveCalculationModal.leaveRequestDetail.LeaveRequestNotificationId);
            var leaveTypeBriefs = JsonConvert.DeserializeObject<List<LeaveTypeBrief>>(leaveCalculationModal.leaveRequestDetail.LeaveQuotaDetail);
            var availableLeave = leaveTypeBriefs.Find(x => x.LeavePlanTypeId == leaveRequestModal.LeaveTypeId);
            availableLeave.AvailableLeaves = availableLeave.AvailableLeaves - leaveCalculationModal.numberOfLeaveApplyring;
            leaveCalculationModal.leaveTypeBriefs = leaveTypeBriefs;

            result = _db.Execute<LeaveRequestDetail>("sp_employee_leave_request_InsUpdate", new
            {
                leaveCalculationModal.leaveRequestDetail.LeaveRequestId,
                leaveRequestModal.EmployeeId,
                LeaveDetail = JsonConvert.SerializeObject(leaveDetails),
                Year = leaveRequestModal.LeaveToDay.Year,
                IsPending = true,
                AvailableLeaves = 0,
                TotalLeaveApplied = 0,
                TotalApprovedLeave = 0,
                TotalLeaveQuota = totalAllocatedLeave,
                LeaveQuotaDetail = JsonConvert.SerializeObject(leaveTypeBriefs)
            }, true);

            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("fail to insert or update leave detail");

            leaveCalculationModal.lastAppliedLeave.Add(new LeaveRequestNotification
            {
                LeaveRequestNotificationId = leaveCalculationModal.leaveRequestDetail.LeaveRequestNotificationId,
                LeaveRequestId = leaveCalculationModal.leaveRequestDetail.LeaveRequestId,
                UserMessage = leaveRequestModal.Reason,
                EmployeeId = leaveRequestModal.EmployeeId,
                ReportingManagerId = leaveCalculationModal.employee.ReportingManagerId,
                ProjectId = 0,
                ProjectName = string.Empty,
                FromDate = leaveRequestModal.LeaveFromDay,
                ToDate = leaveRequestModal.LeaveToDay,
                NumOfDays = Convert.ToDecimal(leaveCalculationModal.numberOfLeaveApplyring),
                RequestStatusId = (int)ItemStatus.Pending,
                NoOfApprovalsRequired = requestChainDetail.NoOfApprovalsRequired,
                ReporterDetail = requestChainDetail.ReporterDetail,
                FileIds = fileIds,
                FeedBack = ApplicationConstants.EmptyJsonArray,
                LeaveTypeName = leaveRequestModal.LeavePlanName,
                AutoActionAfterDays = requestChainDetail.AutoActionAfterDays,
                IsAutoApprovedEnabled = requestChainDetail.IsAutoApprovedEnabled,
                LeaveTypeId = leaveCalculationModal.LeaveTypeId,
                AdminId = _currentSession.CurrentUserDetail.UserId,
                CreatedOn = DateTime.UtcNow
            });
            leaveCalculationModal.lastAppliedLeave = leaveCalculationModal.lastAppliedLeave.OrderByDescending(x => x.CreatedOn).ToList();
            return await Task.FromResult(emails);
        }

        private async Task<string> SaveLeaveAttachment(IFormFileCollection fileCollection, List<Files> fileDetail, Employee employee)
        {
            DbResult Result = null;
            List<int> fileIds = new List<int>();
            if (fileCollection != null && fileCollection.Count > 0)
            {
                var documentPath = Path.Combine(
                    _fileLocationDetail.UserFolder,
                    employee.Email,
                ApplicationConstants.LeaveAttachmentPath
                );
                // save file to server filesystem
                _fileService.SaveFileToLocation(documentPath, fileDetail, fileCollection);

                foreach (var n in fileDetail)
                {
                    Result = await _db.ExecuteAsync("sp_userfiledetail_Upload", new
                    {
                        FileId = n.FileUid,
                        FileOwnerId = employee.EmployeeUid,
                        FilePath = documentPath,
                        FileName = n.FileName,
                        FileExtension = n.FileExtension,
                        UserTypeId = (int)UserType.Employee,
                        AdminId = _currentSession.CurrentUserDetail.UserId
                    }, true);

                    if (!BotConstant.IsSuccess(Result))
                        throw new HiringBellException("Fail to update housing property document detail. Please contact to admin.");

                    fileIds.Add(Convert.ToInt32(Result.statusMessage));
                }
            }
            return JsonConvert.SerializeObject(fileIds);
        }

        public LeaveRequestNotification GetApprovalChainDetail(long employeeId, out List<string> emails)
        {
            string designationId = JsonConvert.SerializeObject(new List<int> { (int)Roles.Admin, (int)Roles.Manager });
            var resultSet = _db.GetDataSet("sp_workflow_chain_by_ids", new
            {
                Ids = $"{_leavePlanConfiguration.leaveApproval.ApprovalWorkFlowId}",
                EmployeeId = employeeId,
                DesignationIds = designationId
            });

            List<ApprovalChainDetail> approvalChainDetail = Converter.ToList<ApprovalChainDetail>(resultSet.Tables[0]);
            List<EmployeeWithRoles> employeeWithRoles = Converter.ToList<EmployeeWithRoles>(resultSet.Tables[1]);
            if (approvalChainDetail.Count == 0)
                throw HiringBellException.ThrowBadRequest("Approval chain deatails not found. Please contact to admin");

            if (employeeWithRoles.Count == 0)
                throw HiringBellException.ThrowBadRequest("Reportee details not found. Please contact to admin");

            ApprovalChainDetail ApprovalChain = approvalChainDetail.First();
            LeaveRequestNotification notification = new LeaveRequestNotification
            {
                NoOfApprovalsRequired = ApprovalChain.NoOfApprovalLevel,
                AutoActionAfterDays = ApprovalChain.AutoExpireAfterDays,
                IsAutoApprovedEnabled = ApprovalChain.IsRequired,
                ReporterDetail = JsonConvert.SerializeObject(employeeWithRoles)
            };
            emails = employeeWithRoles.Select(x => x.Email).ToList();
            return notification;
        }

        private bool GetExecuterId(LeaveCalculationModal leaveCalculationModal, ApprovalChainDetail chain, List<EmployeeWithRoles> employeeWithRoles)
        {
            var record = employeeWithRoles.Find(x => x.DesignationId == chain.AssignieId);
            if (record != null)
            {
                chain.AssignieId = record.EmployeeUid;
                chain.AssignieeEmail = record.Email;
                return true;
            }

            var memberDetail = leaveCalculationModal.projectMemberDetail.Find(x => x.DesignationId == chain.AssignieId && x.ProjectId == leaveCalculationModal.employee.ProjectId);
            if (memberDetail != null)
            {
                chain.AssignieId = memberDetail.EmployeeId;
                chain.AssignieeEmail = memberDetail.Email;
                return true;
            }

            return false;
        }

        private List<ProjectMemberDetail> LoadOtherProjects(int nextOffset)
        {
            return _db.GetList<ProjectMemberDetail>("sp_project_basic_detail_page_by_offset", new
            {
                PageSize = 100,
                OffsetSize = nextOffset
            });
        }

        #endregion
    }
}