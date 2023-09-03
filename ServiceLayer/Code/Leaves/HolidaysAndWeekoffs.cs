using BottomhalfCore.Services.Interface;
using ModalLayer.Modal;
using ModalLayer.Modal.Leaves;
using ServiceLayer.Interface;
using System;
using System.Threading.Tasks;

namespace ServiceLayer.Code.Leaves
{
    public class HolidaysAndWeekoffs : IHolidaysAndWeekoffs
    {
        private readonly ITimezoneConverter _timezoneConverter;
        private readonly CurrentSession _currentSession;
        private LeavePlanConfiguration _leavePlanConfiguration;
        private readonly ICompanyCalendar _companyCalendar;
        private readonly Accrual _accrual;

        public HolidaysAndWeekoffs(
            ITimezoneConverter timezoneConverter,
            ICompanyCalendar companyCalendar,
            CurrentSession currentSession,
            Accrual accrual)
        {
            _timezoneConverter = timezoneConverter;
            _currentSession = currentSession;
            _companyCalendar = companyCalendar;
            _accrual = accrual;
        }

        public async Task CheckHolidayWeekOffRules(LeaveCalculationModal leaveCalculationModal)
        {
            _leavePlanConfiguration = leaveCalculationModal.leavePlanConfiguration;

            await CheckAdjoiningHolidyOnLeave(leaveCalculationModal);

            await CheckAdjoiningWeekOffOnLeave(leaveCalculationModal);

            var planType = leaveCalculationModal.leaveTypeBriefs.Find(x => x.LeavePlanTypeId == leaveCalculationModal.LeaveTypeId);
            decimal totalAvailableLeave = planType.AvailableLeaves + leaveCalculationModal.ProjectedFutureLeave;
            totalAvailableLeave = _accrual.RoundUpTheLeaves(totalAvailableLeave, _leavePlanConfiguration);

            if (totalAvailableLeave < leaveCalculationModal.numberOfLeaveApplyring)
                throw HiringBellException.ThrowBadRequest($"You don't have enough leave in your bucket to apply this leave. " +
                    $"{leaveCalculationModal.numberOfLeaveApplyring - planType.AvailableLeaves} days extra required due to weekoff policy.");

            await Task.CompletedTask;
        }

        // step - 1 -- adjoining holiday
        private async Task CheckAdjoiningHolidyOnLeave(LeaveCalculationModal leaveCalculationModal)
        {
            int holidays = 0;

            if (_leavePlanConfiguration.leaveHolidaysAndWeekoff.AdJoiningHolidayIsConsiderAsLeave)
            {
                if (leaveCalculationModal.numberOfLeaveApplyring > _leavePlanConfiguration.leaveHolidaysAndWeekoff.ConsiderLeaveIfIncludeDays)
                {
                    DateTime fromDate = leaveCalculationModal.fromDate;
                    DateTime toDate = leaveCalculationModal.toDate;
                    // Yes
                    var totalDays = toDate.Date.Subtract(fromDate.Date).TotalDays + 1;
                    if (totalDays >= (double)_leavePlanConfiguration.leaveHolidaysAndWeekoff.ConsiderLeaveIfNumOfDays)
                    {
                        // for below condition in case of true consider all days as leave
                        if (_leavePlanConfiguration.leaveHolidaysAndWeekoff.IfLeaveLieBetweenTwoHolidays)
                        {
                            holidays = await _companyCalendar.GetHolidayBetweenTwoDates(fromDate, toDate);
                        }

                        if (_leavePlanConfiguration.leaveHolidaysAndWeekoff.IfHolidayIsRightBeforLeave)
                        {
                            holidays = _companyCalendar.CountHolidaysAfterDate(toDate, leaveCalculationModal.shiftDetail);
                        }

                        else if (_leavePlanConfiguration.leaveHolidaysAndWeekoff.IfHolidayIsRightAfterLeave)
                        {
                            holidays = _companyCalendar.CountHolidaysBeforDate(fromDate, leaveCalculationModal.shiftDetail);
                        }

                        else if (_leavePlanConfiguration.leaveHolidaysAndWeekoff.IfHolidayIsRightBeforeAfterOrInBetween)
                        {
                            holidays = await _companyCalendar.GetHolidayBetweenTwoDates(fromDate, toDate);
                            holidays += _companyCalendar.CountHolidaysBeforDate(fromDate, leaveCalculationModal.shiftDetail);
                            holidays += _companyCalendar.CountHolidaysAfterDate(toDate, leaveCalculationModal.shiftDetail);
                        }
                    }
                }
                else
                {
                    // No = take only week days don't consider weekends as leave
                    holidays = await _companyCalendar.GetHolidayBetweenTwoDates(leaveCalculationModal.timeZoneFromDate, leaveCalculationModal.timeZoneToDate);
                }
            }
            else
            {
                // No = take only week days don't consider weekends as leave
                holidays = await _companyCalendar.GetHolidayBetweenTwoDates(leaveCalculationModal.timeZoneFromDate, leaveCalculationModal.timeZoneToDate);
                holidays *= -1;
            }

            if (holidays > 0)
            {
                leaveCalculationModal.numberOfLeaveApplyring += (decimal)holidays;
            }

            await Task.CompletedTask;
        }

        // step - 2 -- adjoining weekoff
        private async Task CheckAdjoiningWeekOffOnLeave(LeaveCalculationModal leaveCalculationModal)
        {
            int totalWeekends = 0;
            if (_leavePlanConfiguration.leaveHolidaysAndWeekoff.AdjoiningWeekOffIsConsiderAsLeave)
            {
                if (leaveCalculationModal.numberOfLeaveApplyring >= _leavePlanConfiguration.leaveHolidaysAndWeekoff.ConsiderLeaveIfIncludeDays)
                {
                    // if this condition is true then calculate all days
                    if (_leavePlanConfiguration.leaveHolidaysAndWeekoff.IfLeaveLieBetweenWeekOff)
                    {
                        totalWeekends = WeekOffCountIfBetweenLeaveDates(leaveCalculationModal);
                    }

                    if (_leavePlanConfiguration.leaveHolidaysAndWeekoff.IfWeekOffIsRightBeforLeave)
                    {
                        totalWeekends = WeekOffCountAfterLeaveStartDate(leaveCalculationModal);
                    }
                    else if (_leavePlanConfiguration.leaveHolidaysAndWeekoff.IfWeekOffIsRightAfterLeave)
                    {
                        totalWeekends = WeekOffCountBeforeLeaveStartDate(leaveCalculationModal);
                    }
                    else if (_leavePlanConfiguration.leaveHolidaysAndWeekoff.IfWeekOffIsRightBeforeAfterOrInBetween)
                    {
                        totalWeekends = WeekOffCountIfBetweenLeaveDates(leaveCalculationModal);
                        totalWeekends += WeekOffCountBeforeLeaveStartDate(leaveCalculationModal);
                        totalWeekends += WeekOffCountAfterLeaveStartDate(leaveCalculationModal);
                    }
                }
                else
                {
                    totalWeekends = WeekOffCountIfBetweenLeaveDates(leaveCalculationModal);
                }
            }
            else
            {
                totalWeekends = WeekOffCountIfBetweenLeaveDates(leaveCalculationModal);
                totalWeekends *= -1;
            }

            if (totalWeekends > 0)
            {
                leaveCalculationModal.numberOfLeaveApplyring += totalWeekends;
            }

            await Task.CompletedTask;
        }

        private int WeekOffCountBeforeLeaveStartDate(LeaveCalculationModal leaveCalculationModal)
        {
            DateTime startDate = leaveCalculationModal.fromDate;
            return CalculateWeekOffs(leaveCalculationModal, startDate, -1);
        }

        private int WeekOffCountAfterLeaveStartDate(LeaveCalculationModal leaveCalculationModal)
        {
            DateTime startDate = leaveCalculationModal.toDate;
            return CalculateWeekOffs(leaveCalculationModal, startDate, 1);
        }

        private int CalculateWeekOffs(LeaveCalculationModal leaveCalculationModal, DateTime startDate, int sign)
        {
            bool flag = false;
            int weekOffCount = 0;
            int i = 0;
            var zoneDate = _timezoneConverter.ToTimeZoneDateTime(startDate, _currentSession.TimeZone);
            while (i < 6)
            {
                i++;
                zoneDate = zoneDate.AddDays(sign);
                switch (zoneDate.DayOfWeek)
                {
                    case DayOfWeek.Sunday:
                        if (!leaveCalculationModal.shiftDetail.IsSun)
                        {
                            weekOffCount++;
                            flag = true;
                        }
                        else
                        {
                            flag = false;
                        }
                        break;
                    case DayOfWeek.Monday:
                        if (!leaveCalculationModal.shiftDetail.IsMon)
                        {
                            weekOffCount++;
                            flag = true;
                        }
                        else
                        {
                            flag = false;
                        }
                        break;
                    case DayOfWeek.Tuesday:
                        if (!leaveCalculationModal.shiftDetail.IsTue)
                        {
                            weekOffCount++;
                            flag = true;
                        }
                        else
                        {
                            flag = false;
                        }
                        break;
                    case DayOfWeek.Wednesday:
                        if (!leaveCalculationModal.shiftDetail.IsWed)
                        {
                            weekOffCount++;
                            flag = true;
                        }
                        else
                        {
                            flag = false;
                        }
                        break;
                    case DayOfWeek.Thursday:
                        if (!leaveCalculationModal.shiftDetail.IsThu)
                        {
                            weekOffCount++;
                            flag = true;
                        }
                        else
                        {
                            flag = false;
                        }
                        break;
                    case DayOfWeek.Friday:
                        if (!leaveCalculationModal.shiftDetail.IsFri)
                        {
                            weekOffCount++;
                            flag = true;
                        }
                        else
                        {
                            flag = false;
                        }
                        break;
                    case DayOfWeek.Saturday:
                        if (!leaveCalculationModal.shiftDetail.IsSat)
                        {
                            weekOffCount++;
                            flag = true;
                        }
                        else
                        {
                            flag = false;
                        }
                        break;
                }

                if (!flag)
                    break;
            }

            return weekOffCount;
        }

        public int WeekOffCountIfBetweenLeaveDates(LeaveCalculationModal leaveCalculationModal)
        {
            int totalWeekends = 0;
            var fromDate = _timezoneConverter.ToTimeZoneDateTime(leaveCalculationModal.fromDate, _currentSession.TimeZone);
            var toDate = _timezoneConverter.ToTimeZoneDateTime(leaveCalculationModal.toDate, _currentSession.TimeZone);

            while (toDate.Subtract(fromDate).TotalDays >= 0)
            {
                switch (fromDate.DayOfWeek)
                {
                    case DayOfWeek.Sunday:
                        if (!leaveCalculationModal.shiftDetail.IsSun)
                            totalWeekends++;
                        break;
                    case DayOfWeek.Monday:
                        if (!leaveCalculationModal.shiftDetail.IsMon)
                            totalWeekends++;
                        break;
                    case DayOfWeek.Tuesday:
                        if (!leaveCalculationModal.shiftDetail.IsTue)
                            totalWeekends++;
                        break;
                    case DayOfWeek.Wednesday:
                        if (!leaveCalculationModal.shiftDetail.IsWed)
                            totalWeekends++;
                        break;
                    case DayOfWeek.Thursday:
                        if (!leaveCalculationModal.shiftDetail.IsThu)
                            totalWeekends++;
                        break;
                    case DayOfWeek.Friday:
                        if (!leaveCalculationModal.shiftDetail.IsFri)
                            leaveCalculationModal.numberOfLeaveApplyring++;
                        break;
                    case DayOfWeek.Saturday:
                        if (!leaveCalculationModal.shiftDetail.IsSat)
                            totalWeekends++;
                        break;
                }

                fromDate = fromDate.AddDays(1);
            }

            return totalWeekends;
        }
    }
}
