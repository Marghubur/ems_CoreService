using BottomhalfCore.Services.Interface;
using ModalLayer.Modal;
using ModalLayer.Modal.Leaves;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Code.Leaves
{
    public class Restriction
    {
        private readonly ITimezoneConverter _timezoneConverter;
        private readonly CurrentSession _currentSession;
        private LeavePlanConfiguration _leavePlanConfiguration;
        private LeavePlanType _leavePlanType;

        public Restriction(ITimezoneConverter timezoneConverter, CurrentSession currentSession)
        {
            _timezoneConverter = timezoneConverter;
            _currentSession = currentSession;
        }

        public void CheckRestrictionForLeave(LeaveCalculationModal leaveCalculationModal, LeavePlanType leavePlanType)
        {
            _leavePlanType = leavePlanType;
            _leavePlanConfiguration = leaveCalculationModal.leavePlanConfiguration;

            // step - 1
            // check employee type and apply restriction 
            NewEmployeeWhenCanAvailThisLeave(leaveCalculationModal);

            // step 2
            CheckAvailAllBalanceLeaveInAMonth(leaveCalculationModal);

            // step 3
            // call if manage tries of overrite and apply leave for you.
            // await ManagerOverrideAndApplyLeave(leaveCalculationModal);

            // step - 4
            LeaveGapRestriction(leaveCalculationModal);
        }

        public async Task<bool> ManagerOverrideAndApplyLeave(LeaveCalculationModal leaveCalculationModal)
        {
            bool flag = false;
            if (_leavePlanConfiguration.leavePlanRestriction.CanManageOverrideLeaveRestriction)
                flag = true;
            return await Task.FromResult(flag);
        }

        private void CheckAvailAllBalanceLeaveInAMonth(LeaveCalculationModal leaveCalculationModal)
        {
            if (!_leavePlanConfiguration.leavePlanRestriction.IsLeaveInNoticeExtendsNoticePeriod)
            {
                if (leaveCalculationModal.numberOfLeaveApplyring == _leavePlanType.AvailableLeave)
                    throw HiringBellException.ThrowBadRequest("You can't apply your entire available leave in a month.");
            }
            else
            {
                if (leaveCalculationModal.employeeType == ApplicationConstants.InNoticePeriod)
                {
                    leaveCalculationModal.noticePeriodEndDate = leaveCalculationModal.noticePeriodEndDate
                        .AddDays((double)_leavePlanConfiguration.leavePlanRestriction.NoOfTimesNoticePeriodExtended);
                }
            }
        }

        private void CheckForExistingLeave(LeaveCalculationModal leaveCalculationModal, DateTime fromDate, DateTime toDate)
        {
            var item = leaveCalculationModal.lastAppliedLeave
                        .Find(x => leaveCalculationModal.fromDate >= x.FromDate && leaveCalculationModal.toDate <= x.ToDate);
            if (item != null)
                throw HiringBellException.ThrowBadRequest($"Minimumn " +
                      $"{_leavePlanConfiguration.leavePlanRestriction.GapBetweenTwoConsicutiveLeaveDates} days gap required between any two leaves.");
        }

        private void LeaveGapRestriction(LeaveCalculationModal leaveCalculationModal)
        {

            var currentPlanType = leaveCalculationModal.leaveTypeBriefs.Find(x => x.LeavePlanTypeId == _leavePlanType.LeavePlanTypeId);
            if (currentPlanType == null)
                throw HiringBellException.ThrowBadRequest("Leave plan type not found");

            decimal availableLeaveLimit = currentPlanType.AvailableLeaves;

            // check leave gap between two consucutive leaves
            if (leaveCalculationModal.lastAppliedLeave != null && leaveCalculationModal.lastAppliedLeave.Count > 0)
            {
                var lastApplied = leaveCalculationModal.lastAppliedLeave.OrderByDescending(i => i.FromDate).First();

                // date after last applied todate.
                var dayDiff = leaveCalculationModal.fromDate.Date.Subtract(lastApplied.ToDate.Date).TotalDays;

                var toDate = leaveCalculationModal.toDate;
                var fromDate = leaveCalculationModal.fromDate
                                .AddDays(-1 * (double)_leavePlanConfiguration.leavePlanRestriction.GapBetweenTwoConsicutiveLeaveDates);
                CheckForExistingLeave(leaveCalculationModal, fromDate, toDate);

                fromDate = leaveCalculationModal.fromDate;
                toDate = leaveCalculationModal.toDate
                                .AddDays((double)_leavePlanConfiguration.leavePlanRestriction.GapBetweenTwoConsicutiveLeaveDates);
                CheckForExistingLeave(leaveCalculationModal, fromDate, toDate);
            }

            List<CompleteLeaveDetail> completeLeaveDetail = new List<CompleteLeaveDetail>();
            if (leaveCalculationModal.leaveRequestDetail.LeaveDetail != null)
                completeLeaveDetail = JsonConvert.DeserializeObject<List<CompleteLeaveDetail>>(leaveCalculationModal.leaveRequestDetail.LeaveDetail);

            // check total leave applied and restrict for current year
            if ((completeLeaveDetail.Count + leaveCalculationModal.numberOfLeaveApplyring) >
                _leavePlanConfiguration.leavePlanRestriction.LimitOfMaximumLeavesInCalendarYear)
                throw HiringBellException.ThrowBadRequest($"Calendar year leave limit is only {_leavePlanConfiguration.leavePlanRestriction.LimitOfMaximumLeavesInCalendarYear} days.");

            // check total leave applied and restrict for current month
            decimal count = completeLeaveDetail
                .Where(x => _timezoneConverter.ToTimeZoneDateTime(x.LeaveFromDay, _currentSession.TimeZone).Date.Month ==
                leaveCalculationModal.timeZoneFromDate.Date.Month)
                .Sum(i => i.NumOfDays);
            if ((count + leaveCalculationModal.numberOfLeaveApplyring) > _leavePlanConfiguration.leavePlanRestriction.LimitOfMaximumLeavesInCalendarMonth)
                throw HiringBellException.ThrowBadRequest($"Calendar month leave limit is only {_leavePlanConfiguration.leavePlanRestriction.LimitOfMaximumLeavesInCalendarMonth} days.");

            //// check total leave applied and restrict for entire tenure
            //if (leaveCalculationModal.numberOfLeaveApplyring > _leavePlanConfiguration.leavePlanRestriction.LimitOfMaximumLeavesInEntireTenure)
            //    throw HiringBellException.ThrowBadRequest($"Entire tenure leave limit is only {_leavePlanConfiguration.leavePlanRestriction.LimitOfMaximumLeavesInEntireTenure} days.");

            // check available if any restrict minimun leave to be appllied
            if (availableLeaveLimit >= _leavePlanConfiguration.leavePlanRestriction.AvailableLeaves &&
                leaveCalculationModal.numberOfLeaveApplyring < _leavePlanConfiguration.leavePlanRestriction.MinLeaveToApplyDependsOnAvailable)
                throw HiringBellException.ThrowBadRequest($"Minimun {_leavePlanConfiguration.leavePlanRestriction.AvailableLeaves} days of leave only allowed for this type.");

            // restrict leave date every month
            if (leaveCalculationModal.timeZoneFromDate.Day <= _leavePlanConfiguration.leavePlanRestriction.RestrictFromDayOfEveryMonth)
                throw new HiringBellException($"Apply this leave after {_leavePlanConfiguration.leavePlanRestriction.RestrictFromDayOfEveryMonth} day of any month.");
        }

        private void NewEmployeeWhenCanAvailThisLeave(LeaveCalculationModal leaveCalculationModal)
        {
            if (_leavePlanConfiguration.leavePlanRestriction.CanApplyAfterProbation)
            {
                var dateFromApplyLeave = leaveCalculationModal.employee.CreatedOn.AddDays(
                    leaveCalculationModal.companySetting.ProbationPeriodInDays +
                    Convert.ToDouble(_leavePlanConfiguration.leavePlanRestriction.DaysAfterProbation));

                if (dateFromApplyLeave.Date.Subtract(leaveCalculationModal.fromDate.Date).TotalDays > 0)
                    throw new HiringBellException("Days restriction after Probation period is not completed to apply this leave.");
            }
            else if (leaveCalculationModal.employeeType == ApplicationConstants.InProbationPeriod &&
                _leavePlanConfiguration.leavePlanRestriction.CanApplyAfterJoining)
            {
                var dateAfterProbation = leaveCalculationModal.employee.CreatedOn.AddDays(
                    Convert.ToDouble(_leavePlanConfiguration.leavePlanRestriction.DaysAfterJoining));
                if (leaveCalculationModal.fromDate.Date.Subtract(dateAfterProbation.Date).TotalDays < 0)
                    throw new HiringBellException("Days restriction after Joining period is not completed to apply this leave.");

                var probationEndDate = leaveCalculationModal.employee.CreatedOn.AddDays(
                    leaveCalculationModal.companySetting.ProbationPeriodInDays );

                if (_leavePlanConfiguration.leavePlanRestriction.IsAvailRestrictedLeavesInProbation && 
                    probationEndDate.Date.Subtract(leaveCalculationModal.fromDate.Date).TotalDays > 0)
                {
                    if (leaveCalculationModal.numberOfLeaveApplyring > _leavePlanConfiguration.leavePlanRestriction.LeaveLimitInProbation)
                        throw new HiringBellException($"In probation period you can take upto " +
                            $"{_leavePlanConfiguration.leavePlanRestriction.LeaveLimitInProbation} no. of days only.");
                }
            }

        }
    }
}
