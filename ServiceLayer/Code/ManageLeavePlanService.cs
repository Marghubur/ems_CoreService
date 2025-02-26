using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using EMailService.Modal;
using ModalLayer.Modal;
using ModalLayer.Modal.Leaves;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class ManageLeavePlanService : IManageLeavePlanService
    {
        private readonly IDb _db;
        private readonly CurrentSession _currentSession;
        public ManageLeavePlanService(IDb db, CurrentSession currentSession)
        {
            _db = db;
            _currentSession = currentSession;
        }

        public LeavePlanConfiguration UpdateLeaveAccrual(int leavePlanTypeId, LeaveAccrual leaveAccrual)
        {
            _db.Execute<LeaveAccrual>("", leaveAccrual, true);
            return this.GetLeaveConfigurationDetail(leavePlanTypeId);
        }

        public dynamic GetLeaveConfigurationDetail(int leavePlanTypeId)
        {
            LeavePlanConfiguration leavePlanConfiguration = new LeavePlanConfiguration();
            var resultSet = _db.FetchDataSet(Procedures.Leave_Plans_Type_And_Workflow_ById, new { LeavePlanTypeId = leavePlanTypeId });
            if (resultSet == null || resultSet.Tables.Count != 3)
                throw HiringBellException.ThrowBadRequest("Fail to get leave plan type details");

            LeavePlanType leavePlanType = Converter.ToType<LeavePlanType>(resultSet.Tables[0]);
            List<ApprovalWorkFlowChainFilter> approvalWorkFlowChain = Converter.ToList<ApprovalWorkFlowChainFilter>(resultSet.Tables[1]);
            if (leavePlanType != null && !string.IsNullOrEmpty(leavePlanType.PlanConfigurationDetail))
                leavePlanConfiguration = JsonConvert.DeserializeObject<LeavePlanConfiguration>(leavePlanType.PlanConfigurationDetail);

            List<EmployeeRole> employeeRole = Converter.ToList<EmployeeRole>(resultSet.Tables[2]);
            //employeeRole = employeeRole.FindAll(x => x.RoleId == 1 || x.RoleId == 2 || x.RoleId == 3 || x.RoleId == 5 || x.RoleId == 19);
            return new { leavePlanConfiguration, approvalWorkFlowChain, employeeRole };
        }

        public LeavePlanConfiguration UpdateLeaveDetail(int leavePlanTypeId, int leavePlanId, LeaveDetail leaveDetail)
        {
            if (leavePlanTypeId <= 0)
                throw new HiringBellException("Invalid plan selected");

            if (leavePlanId <= 0)
                throw new HiringBellException("Invalid plan selected");

            LeavePlanConfiguration leavePlanConfiguration = new LeavePlanConfiguration();
            LeavePlanType leavePlanType = _db.Get<LeavePlanType>(Procedures.Leave_Plans_Type_GetbyId, new { LeavePlanTypeId = leavePlanTypeId });

            if (leavePlanType == null)
                throw new HiringBellException("Invalid plan type id. No record found.");

            if (leaveDetail.IsLeaveDaysLimit == false)
                leaveDetail.LeaveLimit = 0;

            if (leaveDetail.IsNoLeaveAfterDate == false)
                leaveDetail.LeaveNotAllocatedIfJoinAfter = 0;

            if (leaveDetail.CanApplyExtraLeave == false)
                leaveDetail.ExtraLeaveLimit = 0;

            if (leavePlanType != null && !string.IsNullOrEmpty(leavePlanType.PlanConfigurationDetail))
            {
                leavePlanConfiguration = JsonConvert.DeserializeObject<LeavePlanConfiguration>(leavePlanType.PlanConfigurationDetail);
            }

            var result = _db.Execute<LeaveDetail>(Procedures.Leave_Detail_Insupd, new
            {
                leaveDetail.LeaveDetailId,
                leaveDetail.LeavePlanTypeId,
                leaveDetail.IsLeaveDaysLimit,
                leaveDetail.LeaveLimit,
                leaveDetail.CanApplyExtraLeave,
                leaveDetail.ExtraLeaveLimit,
                leaveDetail.IsNoLeaveAfterDate,
                leaveDetail.LeaveNotAllocatedIfJoinAfter,
                leaveDetail.CanCompoffAllocatedAutomatically,
                leaveDetail.CanCompoffCreditedByManager,
                leaveDetail.LeavePlanId
            }, true);

            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Fail to insert or update leave plan detail.");
            else
            {
                leaveDetail.LeaveDetailId = Convert.ToInt32(result);
                leavePlanConfiguration.leaveDetail = leaveDetail;
                this.UpdateLeavePlanConfigurationDetail(leavePlanTypeId, leavePlanId, leavePlanConfiguration);
            }

            return leavePlanConfiguration;
        }

        public LeavePlanConfiguration UpdateLeaveFromManagement(int leavePlanTypeId, int leavePlanId, ManagementLeave managementLeave)
        {
            if (leavePlanTypeId <= 0)
                throw new HiringBellException("Invalid plan selected");

            if (leavePlanId <= 0)
                throw new HiringBellException("Invalid plan selected");

            LeavePlanConfiguration leavePlanConfiguration = new LeavePlanConfiguration();
            LeavePlanType leavePlanType = _db.Get<LeavePlanType>(Procedures.Leave_Plans_Type_GetbyId, new { LeavePlanTypeId = leavePlanTypeId });

            if (leavePlanType == null)
                throw new HiringBellException("Invalid plan type id. No record found.");

            if (leavePlanType != null && !string.IsNullOrEmpty(leavePlanType.PlanConfigurationDetail))
            {
                leavePlanConfiguration = JsonConvert.DeserializeObject<LeavePlanConfiguration>(leavePlanType.PlanConfigurationDetail);
            }

            var result = _db.Execute<ManagementLeave>(Procedures.Leave_From_Management_Insupd, new
            {
                managementLeave.LeaveManagementId,
                leavePlanTypeId,
                managementLeave.CanManagerAwardCausalLeave,
                managementLeave.LeavePlanId
            }, true);

            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Fail to insert or update leave from management.");
            else
            {
                managementLeave.LeaveManagementId = Convert.ToInt32(result);
                leavePlanConfiguration.managementLeave = managementLeave;
                this.UpdateLeavePlanConfigurationDetail(leavePlanTypeId, leavePlanId, leavePlanConfiguration);
            }

            return leavePlanConfiguration;
        }

        public LeavePlanConfiguration UpdateLeaveAccrualService(int leavePlanTypeId, int leavePlanId, LeaveAccrual leaveAccrual)
        {
            if (leavePlanTypeId <= 0)
                throw new HiringBellException("Invalid plan selected");
            if (leavePlanId <= 0)
                throw new HiringBellException("Invalid plan selected");

            LeavePlanConfiguration leavePlanConfiguration = new LeavePlanConfiguration();
            LeavePlanType leavePlanType = _db.Get<LeavePlanType>(Procedures.Leave_Plans_Type_GetbyId, new { LeavePlanTypeId = leavePlanTypeId });

            if (leavePlanType == null)
                throw new HiringBellException("Invalid plan type id. No record found.");

            if (leavePlanType != null && !string.IsNullOrEmpty(leavePlanType.PlanConfigurationDetail))
                leavePlanConfiguration = JsonConvert.DeserializeObject<LeavePlanConfiguration>(leavePlanType.PlanConfigurationDetail);

            LeaveAccrualValidationCheck(leaveAccrual, leavePlanConfiguration.leaveDetail);
            ValidateAndPreFillValue(leaveAccrual);

            var result = _db.Execute<LeaveAccrual>(Procedures.Leave_Accrual_InsUpdate, new
            {
                leaveAccrual.LeaveAccrualId,
                leaveAccrual.LeavePlanTypeId,
                leaveAccrual.CanApplyEntireLeave,
                leaveAccrual.IsLeaveAccruedPatternAvail,
                JoiningMonthLeaveDistribution = JsonConvert.SerializeObject(leaveAccrual.JoiningMonthLeaveDistribution),
                ExitMonthLeaveDistribution = JsonConvert.SerializeObject(leaveAccrual.ExitMonthLeaveDistribution),
                LeaveDistributionSequence = leaveAccrual.LeaveDistributionSequence,
                leaveAccrual.LeaveDistributionAppliedFrom,
                leaveAccrual.IsLeavesProratedForJoinigMonth,
                leaveAccrual.IsLeavesProratedOnNotice,
                leaveAccrual.IsNotAllowProratedOnNotice,
                leaveAccrual.IsNoLeaveOnNoticePeriod,
                leaveAccrual.IsVaryOnProbationOrExprience,
                leaveAccrual.IsAccrualStartsAfterJoining,
                leaveAccrual.IsAccrualStartsAfterProbationEnds,
                leaveAccrual.AccrualDaysAfterJoining,
                leaveAccrual.AccrualDaysAfterProbationEnds,
                leaveAccrual.IsImpactedOnWorkDaysEveryMonth,
                leaveAccrual.WeekOffAsAbsentIfAttendaceLessThen,
                leaveAccrual.HolidayAsAbsentIfAttendaceLessThen,
                leaveAccrual.CanApplyForFutureDate,
                leaveAccrual.IsExtraLeaveBeyondAccruedBalance,
                leaveAccrual.IsNoExtraLeaveBeyondAccruedBalance,
                leaveAccrual.NoOfDaysForExtraLeave,
                leaveAccrual.IsAccrueIfHavingLeaveBalance,
                leaveAccrual.AllowOnlyIfAccrueBalanceIsAlleast,
                leaveAccrual.IsAccrueIfOnOtherLeave,
                leaveAccrual.NotAllowIfAlreadyOnLeaveMoreThan,
                leaveAccrual.RoundOffLeaveBalance,
                leaveAccrual.ToNearestHalfDay,
                leaveAccrual.ToNearestFullDay,
                leaveAccrual.ToNextAvailableHalfDay,
                leaveAccrual.ToNextAvailableFullDay,
                leaveAccrual.ToPreviousHalfDay,
                leaveAccrual.DoesLeaveExpireAfterSomeTime,
                leaveAccrual.AfterHowManyDays,
                leaveAccrual.LeavePlanId
            }, true);

            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Fail to insert or update leave plan detail.");
            else
            {
                leaveAccrual.LeaveAccrualId = Convert.ToInt32(result);
                leavePlanConfiguration.leaveAccrual = leaveAccrual;
                this.UpdateLeavePlanConfigurationDetail(leavePlanTypeId, leavePlanId, leavePlanConfiguration);
            }

            return leavePlanConfiguration;
        }

        private void ValidateAndPreFillValue(LeaveAccrual leaveAccrual)
        {
            if (!leaveAccrual.IsNotAllowProratedOnNotice)
                leaveAccrual.ExitMonthLeaveDistribution = new List<AllocateTimeBreakup>();

            if (leaveAccrual.IsLeavesProratedForJoinigMonth)
                leaveAccrual.JoiningMonthLeaveDistribution = new List<AllocateTimeBreakup>();

            if (leaveAccrual.IsLeaveAccruedPatternAvail == false)
            {
                leaveAccrual.LeaveDistributionSequence = "";
                leaveAccrual.LeaveDistributionAppliedFrom = 0;
            }

            if (leaveAccrual.IsVaryOnProbationOrExprience == false)
            {
                leaveAccrual.IsAccrualStartsAfterJoining = false;
                leaveAccrual.AccrualDaysAfterJoining = 0;
                leaveAccrual.IsAccrualStartsAfterProbationEnds = false;
                leaveAccrual.AccrualDaysAfterProbationEnds = 0;
            }

            if (leaveAccrual.IsAccrualStartsAfterJoining == true)
            {
                leaveAccrual.IsAccrualStartsAfterProbationEnds = false;
                leaveAccrual.AccrualDaysAfterProbationEnds = 0;
            }

            if (leaveAccrual.IsAccrualStartsAfterProbationEnds == true)
            {
                leaveAccrual.IsAccrualStartsAfterJoining = false;
                leaveAccrual.AccrualDaysAfterJoining = 0;
            }

            if (leaveAccrual.IsNoExtraLeaveBeyondAccruedBalance == true)
            {
                leaveAccrual.IsExtraLeaveBeyondAccruedBalance = false;
                leaveAccrual.NoOfDaysForExtraLeave = 0;
            }

            if (leaveAccrual.IsAccrueIfHavingLeaveBalance == false)
                leaveAccrual.AllowOnlyIfAccrueBalanceIsAlleast = 0;

            if (leaveAccrual.IsAccrueIfOnOtherLeave == false)
                leaveAccrual.NotAllowIfAlreadyOnLeaveMoreThan = 0;

            if (leaveAccrual.DoesLeaveExpireAfterSomeTime == false)
                leaveAccrual.AfterHowManyDays = 0;

            if (leaveAccrual.IsLeaveAccruedPatternAvail == true)
                leaveAccrual.CanApplyEntireLeave = false;
        }

        private void LeaveAccrualValidationCheck(LeaveAccrual leaveAccrual, LeaveDetail leaveDetail)
        {

            if (leaveAccrual.IsLeavesProratedForJoinigMonth == false && leaveAccrual.JoiningMonthLeaveDistribution.Count > 0)
                this.FromandTodateValidation(leaveAccrual.JoiningMonthLeaveDistribution);

            if (leaveAccrual.IsNotAllowProratedOnNotice && leaveAccrual.ExitMonthLeaveDistribution.Count > 0)
                this.FromandTodateValidation(leaveAccrual.ExitMonthLeaveDistribution);
        }

        private void FromandTodateValidation(List<AllocateTimeBreakup> data)
        {
            int i = 0;
            while (i < data.Count)
            {
                if (data[i].FromDate == 0)
                    throw new HiringBellException("If you submit 0 in from date then first month leave cann't be calculated.");

                if (data[i].ToDate == 0)
                    throw new HiringBellException("If you submit 0 in to date then last month leave cann't be calculated.");

                if (data[i].AllocatedLeave == 0)
                    throw new HiringBellException("If you submit 0 in allocate leave then leave cann't be calculated.");

                if (data[i].ToDate > 31)
                    throw new HiringBellException("Invalid to date enter in leave accrual quota");

                if (i > 0)
                {
                    var fromDate = data[i].FromDate;
                    var toDate = data[i].ToDate;
                    if (fromDate < data[i - 1].FromDate || fromDate < data[(i - 1)].ToDate)
                    {
                        throw new HiringBellException("From date must be greater than previous from date");
                    }
                    if (toDate <= data[(i - 1)].ToDate)
                    {
                        throw new HiringBellException("To date must be greater than previous to date");
                    }
                }
                i++;
            }
        }

        public void UpdateLeavePlanConfigurationDetail(int leavePlanTypeId, int leavePlanId, LeavePlanConfiguration leavePlanConfiguration)
        {
            LeavePlan leavePlan = _db.Get<LeavePlan>(Procedures.Leave_Plans_GetbyId, new { LeavePlanId = leavePlanId });
            if (leavePlan == null)
                throw new HiringBellException("Invalid plan used for setup");

            LeavePlanType leavePlantype = _db.Get<LeavePlanType>(Procedures.Leave_Plans_Type_GetbyId, new { LeavePlanTypeId = leavePlanTypeId });
            if (leavePlantype == null)
                throw new HiringBellException("Invalid plan type used for setup");

            leavePlantype.PlanConfigurationDetail = "";
            List<LeavePlanType> leavePlanTypes = JsonConvert.DeserializeObject<List<LeavePlanType>>(leavePlan.AssociatedPlanTypes);
            int workingPlanIndex = leavePlanTypes.FindIndex(x => x.LeavePlanTypeId == leavePlanTypeId);
            if (workingPlanIndex == -1)
                leavePlanTypes.Add(leavePlantype);
            else
                leavePlanTypes[workingPlanIndex] = leavePlantype;

            var result = _db.Execute<LeaveAccrual>(Procedures.Leave_Plan_Upd_Configuration, new
            {
                LeavePlanTypeId = leavePlanTypeId,
                LeavePlanId = leavePlanId,
                LeavePlanConfiguration = JsonConvert.SerializeObject(leavePlanConfiguration),
                AssociatedPlanTypes = JsonConvert.SerializeObject(leavePlanTypes)
            }, true);

            if (!ApplicationConstants.IsExecuted(result))
                throw new HiringBellException("Fail to insert or update leave plan detail.");
        }

        public LeavePlanConfiguration UpdateApplyForLeaveService(int leavePlanTypeId, int leavePlanId, LeaveApplyDetail leaveApplyDetail)
        {
            if (leavePlanTypeId <= 0)
                throw new HiringBellException("Invalid plan selected");
            if (leavePlanId <= 0)
                throw new HiringBellException("Invalid plan selected");

            LeavePlanConfiguration leavePlanConfiguration = new LeavePlanConfiguration();
            LeavePlanType leavePlanType = _db.Get<LeavePlanType>(Procedures.Leave_Plans_Type_GetbyId, new { LeavePlanTypeId = leavePlanTypeId });

            if (leavePlanType == null)
                throw new HiringBellException("Invalid plan type id. No record found.");

            if (leavePlanType != null && !string.IsNullOrEmpty(leavePlanType.PlanConfigurationDetail))
                leavePlanConfiguration = JsonConvert.DeserializeObject<LeavePlanConfiguration>(leavePlanType.PlanConfigurationDetail);

            if (leaveApplyDetail.EmployeeCanSeeAndApplyCurrentPlanLeave == true)
                leaveApplyDetail.RuleForLeaveInNotice = new List<LeaveRuleInNotice>();

            if (leaveApplyDetail.ProofRequiredIfDaysExceeds == false)
                leaveApplyDetail.NoOfDaysExceeded = 0;

            var result = _db.Execute<LeaveApplyDetail>(Procedures.Leave_Apply_Detail_InsUpdate, new
            {
                leaveApplyDetail.LeaveApplyDetailId,
                leaveApplyDetail.LeavePlanTypeId,
                leaveApplyDetail.IsAllowForHalfDay,
                leaveApplyDetail.EmployeeCanSeeAndApplyCurrentPlanLeave,
                leaveApplyDetail.ApplyPriorBeforeLeaveDate,
                leaveApplyDetail.BackDateLeaveApplyNotBeyondDays,
                leaveApplyDetail.RestrictBackDateLeaveApplyAfter,
                leaveApplyDetail.CurrentLeaveRequiredComments,
                leaveApplyDetail.ProofRequiredIfDaysExceeds,
                leaveApplyDetail.NoOfDaysExceeded,
                leaveApplyDetail.LeavePlanId,
                RuleForLeaveInNotice = JsonConvert.SerializeObject(leaveApplyDetail.RuleForLeaveInNotice)
            }, true);

            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Fail to insert or update apply for leave detail.");
            else
            {
                leaveApplyDetail.LeaveApplyDetailId = Convert.ToInt32(result);
                leavePlanConfiguration.leaveApplyDetail = leaveApplyDetail;
                this.UpdateLeavePlanConfigurationDetail(leavePlanTypeId, leavePlanId, leavePlanConfiguration);
            }

            return leavePlanConfiguration;
        }

        public LeavePlanConfiguration UpdateLeaveRestrictionService(int leavePlanTypeId, int leavePlanId, LeavePlanRestriction leavePlanRestriction)
        {
            if (leavePlanTypeId <= 0)
                throw new HiringBellException("Invalid plan selected");

            if (leavePlanId <= 0)
                throw new HiringBellException("Invalid plan selected");

            LeavePlanConfiguration leavePlanConfiguration = new LeavePlanConfiguration();
            LeavePlanType leavePlanType = _db.Get<LeavePlanType>(Procedures.Leave_Plans_Type_GetbyId, new { LeavePlanTypeId = leavePlanTypeId });

            if (leavePlanType == null)
                throw new HiringBellException("Invalid plan type id. No record found.");

            if (leavePlanType != null && !string.IsNullOrEmpty(leavePlanType.PlanConfigurationDetail))
                leavePlanConfiguration = JsonConvert.DeserializeObject<LeavePlanConfiguration>(leavePlanType.PlanConfigurationDetail);

            if (leavePlanRestriction.CanApplyAfterProbation == false)
                leavePlanRestriction.DaysAfterProbation = 0;

            if (leavePlanRestriction.CanApplyAfterJoining == false)
                leavePlanRestriction.DaysAfterJoining = 0;

            if (leavePlanRestriction.IsLeaveInNoticeExtendsNoticePeriod == false)
                leavePlanRestriction.NoOfTimesNoticePeriodExtended = 0;

            if (leavePlanRestriction.IsCurrentPlanDepnedsOnOtherPlan == false)
                leavePlanRestriction.AssociatedPlanTypeId = 0;

            if (leavePlanRestriction.IsCheckOtherPlanTypeBalance == false)
                leavePlanRestriction.DependentPlanTypeId = 0;

            var result = _db.Execute<LeavePlanConfiguration>(Procedures.Leave_Plan_Restriction_Insupd, leavePlanRestriction, true);

            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Fail to insert or update apply for leave detail.");
            else
            {
                leavePlanRestriction.LeavePlanRestrictionId = Convert.ToInt32(result);
                leavePlanConfiguration.leavePlanRestriction = leavePlanRestriction;
                this.UpdateLeavePlanConfigurationDetail(leavePlanTypeId, leavePlanId, leavePlanConfiguration);
            }

            return leavePlanConfiguration;
        }

        public LeavePlanConfiguration UpdateHolidayNWeekOffPlanService(int leavePlanTypeId, int leavePlanId, LeaveHolidaysAndWeekoff leaveHolidaysAndWeekoff)
        {
            if (leavePlanTypeId <= 0)
                throw new HiringBellException("Invalid plan selected");

            if (leavePlanId <= 0)
                throw new HiringBellException("Invalid plan selected");

            LeavePlanConfiguration leavePlanConfiguration = new LeavePlanConfiguration();
            LeavePlanType leavePlanType = _db.Get<LeavePlanType>(Procedures.Leave_Plans_Type_GetbyId, new { LeavePlanTypeId = leavePlanTypeId });

            if (leavePlanType == null)
                throw new HiringBellException("Invalid plan type id. No record found.");

            leaveHolidaysAndWeekoff.LeavePlanTypeId = leavePlanTypeId;
            if (leavePlanType != null && !string.IsNullOrEmpty(leavePlanType.PlanConfigurationDetail))
                leavePlanConfiguration = JsonConvert.DeserializeObject<LeavePlanConfiguration>(leavePlanType.PlanConfigurationDetail);

            if (leaveHolidaysAndWeekoff.AdJoiningHolidayIsConsiderAsLeave == false)
            {
                leaveHolidaysAndWeekoff.ConsiderLeaveIfNumOfDays = 0;
                leaveHolidaysAndWeekoff.IfLeaveLieBetweenTwoHolidays = false;
                leaveHolidaysAndWeekoff.IfHolidayIsRightBeforLeave = false;
                leaveHolidaysAndWeekoff.IfHolidayIsRightAfterLeave = false;
                leaveHolidaysAndWeekoff.IfHolidayIsRightBeforeAfterOrInBetween = false;
            }

            if (leaveHolidaysAndWeekoff.AdjoiningWeekOffIsConsiderAsLeave == false)
            {
                leaveHolidaysAndWeekoff.ConsiderLeaveIfIncludeDays = 0;
                leaveHolidaysAndWeekoff.IfLeaveLieBetweenWeekOff = false;
                leaveHolidaysAndWeekoff.IfWeekOffIsRightBeforLeave = false;
                leaveHolidaysAndWeekoff.IfWeekOffIsRightAfterLeave = false;
                leaveHolidaysAndWeekoff.IfWeekOffIsRightBeforeAfterOrInBetween = false;
            }

            var result = _db.Execute<LeaveHolidaysAndWeekoff>(Procedures.Leave_Holidays_And_Weekoff_Insupd, leaveHolidaysAndWeekoff, true);

            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Fail to insert or update apply for leave detail.");
            else
            {
                leaveHolidaysAndWeekoff.LeaveHolidaysAndWeekOffId = Convert.ToInt32(result);
                leavePlanConfiguration.leaveHolidaysAndWeekoff = leaveHolidaysAndWeekoff;
                this.UpdateLeavePlanConfigurationDetail(leavePlanTypeId, leavePlanId, leavePlanConfiguration);
            }

            return leavePlanConfiguration;
        }

        public LeavePlanConfiguration UpdateLeaveApprovalService(int leavePlanTypeId, int leavePlanId, LeaveApproval leaveApproval)
        {
            if (leavePlanTypeId <= 0)
                throw new HiringBellException("Invalid plan selected");

            if (leavePlanId <= 0)
                throw new HiringBellException("Invalid plan selected");

            LeavePlanConfiguration leavePlanConfiguration = new LeavePlanConfiguration();
            LeavePlanType leavePlanType = _db.Get<LeavePlanType>(Procedures.Leave_Plans_Type_GetbyId, new { LeavePlanTypeId = leavePlanTypeId });

            if (leavePlanType == null)
                throw new HiringBellException("Invalid plan type id. No record found.");

            leaveApproval.LeavePlanTypeId = leavePlanTypeId;
            if (leavePlanType != null && !string.IsNullOrEmpty(leavePlanType.PlanConfigurationDetail))
                leavePlanConfiguration = JsonConvert.DeserializeObject<LeavePlanConfiguration>(leavePlanType.PlanConfigurationDetail);

            var result = _db.Execute<LeaveApproval>(Procedures.Leave_Approval_Insupd, new
            {
                leaveApproval.LeaveApprovalId,
                leaveApproval.LeavePlanTypeId,
                leaveApproval.IsLeaveRequiredApproval,
                leaveApproval.ApprovalLevels,
                leaveApproval.ApprovalWorkFlowId,
                leaveApproval.IsRequiredAllLevelApproval,
                leaveApproval.CanHigherRankPersonsIsAvailForAction,
                leaveApproval.IsPauseForApprovalNotification,
                leaveApproval.IsReportingManageIsDefaultForAction,
                leaveApproval.LeavePlanId
            }, true);

            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Fail to insert or update apply for leave detail.");
            else
            {
                leaveApproval.LeaveApprovalId = Convert.ToInt32(result);
                leavePlanConfiguration.leaveApproval = leaveApproval;
                this.UpdateLeavePlanConfigurationDetail(leavePlanTypeId, leavePlanId, leavePlanConfiguration);
            }

            return leavePlanConfiguration;
        }

        public LeavePlanConfiguration UpdateYearEndProcessingService(int leavePlanTypeId, int leavePlanId, LeaveEndYearProcessing leaveEndYearProcessing)
        {
            if (leavePlanTypeId <= 0)
                throw new HiringBellException("Invalid plan selected");

            if (leavePlanId <= 0)
                throw new HiringBellException("Invalid plan selected");

            LeavePlanConfiguration leavePlanConfiguration = new LeavePlanConfiguration();
            LeavePlanType leavePlanType = _db.Get<LeavePlanType>(Procedures.Leave_Plans_Type_GetbyId, new { LeavePlanTypeId = leavePlanTypeId });

            if (leavePlanType == null)
                throw new HiringBellException("Invalid plan type id. No record found.");

            leaveEndYearProcessing.LeavePlanTypeId = leavePlanTypeId;
            if (leavePlanType != null && !string.IsNullOrEmpty(leavePlanType.PlanConfigurationDetail))
                leavePlanConfiguration = JsonConvert.DeserializeObject<LeavePlanConfiguration>(leavePlanType.PlanConfigurationDetail);

            if (leaveEndYearProcessing.DoestCarryForwardExpired == false)
                leaveEndYearProcessing.ExpiredAfter = 0;

            if (leaveEndYearProcessing.PayNCarryForwardDefineType == "fixed" || (leaveEndYearProcessing.PayFirstNCarryForwordRemaning == false && leaveEndYearProcessing.CarryForwordFirstNPayRemaning == false))
                leaveEndYearProcessing.PercentagePayNCarryForward = new List<PercentagePayNCarryForward>();

            if (leaveEndYearProcessing.PayNCarryForwardDefineType == "percentage" || (leaveEndYearProcessing.PayFirstNCarryForwordRemaning == false && leaveEndYearProcessing.CarryForwordFirstNPayRemaning == false))
                leaveEndYearProcessing.FixedPayNCarryForward = new List<FixedPayNCarryForward>();

            if (leaveEndYearProcessing.PayFirstNCarryForwordRemaning == false && leaveEndYearProcessing.CarryForwordFirstNPayRemaning == false)
                leaveEndYearProcessing.PayNCarryForwardDefineType = "";

            var result = _db.Execute<LeaveEndYearProcessing>(Procedures.Leave_Endyear_Processing_Insupd, new
            {
                leaveEndYearProcessing.LeaveEndYearProcessingId,
                leaveEndYearProcessing.LeavePlanTypeId,
                leaveEndYearProcessing.IsLeaveBalanceExpiredOnEndOfYear,
                leaveEndYearProcessing.AllConvertedToPaid,
                leaveEndYearProcessing.AllLeavesCarryForwardToNextYear,
                leaveEndYearProcessing.PayFirstNCarryForwordRemaning,
                leaveEndYearProcessing.CarryForwordFirstNPayRemaning,
                leaveEndYearProcessing.PayNCarryForwardForPercent,
                leaveEndYearProcessing.PayNCarryForwardDefineType,
                FixedPayNCarryForward = JsonConvert.SerializeObject(leaveEndYearProcessing.FixedPayNCarryForward),
                PercentagePayNCarryForward = JsonConvert.SerializeObject(leaveEndYearProcessing.PercentagePayNCarryForward),
                leaveEndYearProcessing.DoestCarryForwardExpired,
                leaveEndYearProcessing.ExpiredAfter,
                leaveEndYearProcessing.DoesExpiryLeaveRemainUnchange,
                leaveEndYearProcessing.DeductFromSalaryOnYearChange,
                leaveEndYearProcessing.ResetBalanceToZero,
                leaveEndYearProcessing.CarryForwardToNextYear,
                leaveEndYearProcessing.LeavePlanId
            }, true);

            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Fail to insert or update apply for leave detail.");
            else
            {
                leaveEndYearProcessing.LeaveEndYearProcessingId = Convert.ToInt32(result);
                leavePlanConfiguration.leaveEndYearProcessing = leaveEndYearProcessing;
                this.UpdateLeavePlanConfigurationDetail(leavePlanTypeId, leavePlanId, leavePlanConfiguration);
            }

            return leavePlanConfiguration;
        }

        public async Task<string> AddUpdateEmpLeavePlanService(int leavePlanId, List<Employee> employees)
        {
            string status = string.Empty;
            if (leavePlanId <= 0)
                throw new Exception("Invalid plan selected.");
            var employeeInfo = (from employee in employees
                                select new
                                {
                                    EmployeeId = employee.EmployeeUid,
                                    LeavePlanId = employee.LeavePlanId,
                                    AdminId = _currentSession.CurrentUserDetail.UserId,
                                }).ToList();

            var result = await _db.BulkExecuteAsync(Procedures.Employee_Leaveplan_Upd, employeeInfo, true);

            if (result <= 0)
                throw new HiringBellException("Fail to insert or update employee leave plan deatils.");
            else
                status = "updated";

            return status;
        }

        public List<EmpLeavePlanMapping> GetEmpMappingByLeavePlanIdService(int leavePlanId)
        {
            if (leavePlanId <= 0)
                throw new Exception("Invalid plan selected.");

            List<EmpLeavePlanMapping> empLeavePlanMappings = _db.GetList<EmpLeavePlanMapping>(Procedures.Employee_Leaveplan_Mapping_GetByPlanId, new { LeavePlanId = leavePlanId });
            Parallel.ForEach(empLeavePlanMappings, i =>
            {
                i.IsAdded = true;
            });

            return empLeavePlanMappings;
        }
    }
}
