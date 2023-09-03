using ModalLayer.Modal;
using System.Collections.Generic;

namespace ServiceLayer.Interface
{
    public interface IShiftService
    {
        List<ShiftDetail> GetAllShiftService(FilterModel filterModel);
        List<ShiftDetail> InsertWorkShiftService(ShiftDetail shiftDetail);
        List<ShiftDetail> UpdateWorkShiftService(ShiftDetail shiftDetail);
        ShiftDetail GetWorkShiftByIdService(int WorkShiftId);
        ShiftDetail GetWorkShiftByEmpIdService(int EmployeeId);
    }
}
