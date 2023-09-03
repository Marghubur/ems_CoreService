using ModalLayer.Modal.Leaves;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IHolidaysAndWeekoffs
    {
        Task CheckHolidayWeekOffRules(LeaveCalculationModal leaveCalculationModal);
        int WeekOffCountIfBetweenLeaveDates(LeaveCalculationModal leaveCalculationModal);
    }
}
