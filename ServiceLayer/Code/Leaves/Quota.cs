using ModalLayer.Modal.Leaves;
using System.Threading.Tasks;

namespace ServiceLayer.Code.Leaves
{
    public class Quota
    {
        private LeavePlanType _leavePlanType;

        public async Task CalculateFinalLeaveQuota(LeaveCalculationModal leaveCalculationModal, LeavePlanType leavePlanType)
        {
            if (leaveCalculationModal.employeeType == 1)
            {
                // check when leave quota will be avaialbe for new joinee.
                CheckWhenToAllocateLeave(leaveCalculationModal);
                await Task.CompletedTask;
                return;
            }

            _leavePlanType = leavePlanType;

            // calculate total leave quota plus extra leave if any
            CalculateTotalAvailableQuota(leaveCalculationModal);
            await Task.CompletedTask;
        }

        private void CalculateTotalAvailableQuota(LeaveCalculationModal leaveCalculationModal)
        {
            LeavePlanConfiguration leavePlanConfiguration = leaveCalculationModal.leavePlanConfiguration;
            if (leavePlanConfiguration.leaveDetail.ExtraLeaveLimit > 0)
            {
                _leavePlanType.AvailableLeave = leavePlanConfiguration.leaveDetail.ExtraLeaveLimit
                    + leavePlanConfiguration.leaveDetail.LeaveLimit;
            }
            else
            {
                _leavePlanType.AvailableLeave = leavePlanConfiguration.leaveDetail.LeaveLimit;
            }
        }

        private void CheckWhenToAllocateLeave(LeaveCalculationModal leaveCalculationModal)
        {
            LeavePlanConfiguration leavePlanConfiguration = leaveCalculationModal.leavePlanConfiguration;
            if (leavePlanConfiguration.leaveDetail.IsNoLeaveAfterDate)
            {
                if (leaveCalculationModal.probationEndDate.Subtract(leaveCalculationModal.timeZonePresentDate).TotalDays >= 0)
                {
                    _leavePlanType.AvailableLeave = 0;
                }
            }
        }
    }
}
