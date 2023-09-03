using BottomhalfCore.Services.Interface;
using ModalLayer.Modal;
using ModalLayer.Modal.Leaves;
using System.Threading.Tasks;

namespace ServiceLayer.Code.Leaves
{
    public class Apply
    {
        private LeavePlanConfiguration _leavePlanConfiguration;
        private LeavePlanType _leavePlanType;

        private readonly ITimezoneConverter _timezoneConverter;

        public Apply(ITimezoneConverter timezoneConverter)
        {
            _timezoneConverter = timezoneConverter;
        }

        public async Task CheckLeaveApplyRules(LeaveCalculationModal leaveCalculationModal, LeavePlanType leavePlanType)
        {
            _leavePlanType = leavePlanType;
            _leavePlanConfiguration = leaveCalculationModal.leavePlanConfiguration;

            if (leaveCalculationModal.isApplyingForHalfDay)
                CheckForHalfDayRestriction(leaveCalculationModal);
            
            // IsAllowedToSeeAndApply();

            LeaveEligibilityCheck(leaveCalculationModal);

            DoesLeaveRequiredComments(leaveCalculationModal);

            RequiredDocumentForExtending(leaveCalculationModal);

            await Task.CompletedTask;
        }

        // step - 1
        public void CheckForHalfDayRestriction(LeaveCalculationModal leaveCalculationModal)
        {
            if (!_leavePlanConfiguration.leaveApplyDetail.IsAllowForHalfDay)
                throw HiringBellException.ThrowBadRequest("Half day leave not allow under current leave type.");

            if (leaveCalculationModal.toDate.Date.Subtract(leaveCalculationModal.fromDate.Date).TotalDays > 0)
                throw HiringBellException.ThrowBadRequest("You can't be apply more than one day as halfday");
        }

        // step - 2
        public void IsAllowedToSeeAndApply()
        {
            if (!_leavePlanConfiguration.leaveApplyDetail.EmployeeCanSeeAndApplyCurrentPlanLeave)
            {

            }
        }

        // step - 3, 4
        private void LeaveEligibilityCheck(LeaveCalculationModal leaveCalculationModal)
        {
            // if future date then > 0 else < 0
            var calculationDate = leaveCalculationModal.timeZonePresentDate.AddDays(_leavePlanConfiguration.leaveApplyDetail.ApplyPriorBeforeLeaveDate);
            if (leaveCalculationModal.timeZoneFromDate.Date.Subtract(leaveCalculationModal.timeZonePresentDate.Date).TotalDays >= 0)
            {
                // step - 4  future date
                if (leaveCalculationModal.timeZoneFromDate.Date.Subtract(calculationDate.Date).TotalDays < 0)
                {
                    throw HiringBellException.ThrowBadRequest($"Only applycable atleast, before " +
                        $"{_leavePlanConfiguration.leaveApplyDetail.ApplyPriorBeforeLeaveDate} calendar days.");
                }
            }
            else
            {
                // step - 3 past date
                calculationDate = leaveCalculationModal.timeZonePresentDate.AddDays(-_leavePlanConfiguration.leaveApplyDetail.BackDateLeaveApplyNotBeyondDays);

                if (calculationDate.Date.Subtract(leaveCalculationModal.fromDate.Date).TotalDays > 0)
                {
                    throw HiringBellException.ThrowBadRequest($"Can't apply back date leave beyond then " +
                        $"{_leavePlanConfiguration.leaveApplyDetail.BackDateLeaveApplyNotBeyondDays} calendar days.");
                }
            }

        }

        // step - 5
        public void DoesLeaveRequiredComments(LeaveCalculationModal leaveCalculationModal)
        {
            if (_leavePlanConfiguration.leaveApplyDetail.CurrentLeaveRequiredComments &&
                string.IsNullOrEmpty(leaveCalculationModal.leaveRequestDetail.Reason))
            {
                throw HiringBellException.ThrowBadRequest("Comment is required for this leave type");
            }
        }

        // step - 6
        public void RequiredDocumentForExtending(LeaveCalculationModal leaveCalculationModal)
        {
            if (_leavePlanConfiguration.leaveApplyDetail.ProofRequiredIfDaysExceeds)
            {
                var leaveDay = leaveCalculationModal.numberOfLeaveApplyring;
                if (leaveDay > _leavePlanConfiguration.leaveApplyDetail.NoOfDaysExceeded && !leaveCalculationModal.DocumentProffAttached)
                {
                    throw HiringBellException.ThrowBadRequest($"Your leave is exceeding by " +
                        $"{_leavePlanConfiguration.leaveApplyDetail.NoOfDaysExceeded - leaveDay}, to apply this, required document proof.");
                }
            }
        }
    }
}
