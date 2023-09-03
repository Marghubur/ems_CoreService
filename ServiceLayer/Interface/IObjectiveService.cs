using ModalLayer.Modal;
using System.Collections.Generic;

namespace ServiceLayer.Interface
{
    public interface IObjectiveService
    {
        dynamic ObjectiveInsertUpdateService(ObjectiveDetail objectiveDetail);
        dynamic GetPerformanceObjectiveService(FilterModel filterModel);
        List<ObjectiveDetail> GetEmployeeObjectiveService(int designationId, int companyId, long employeeId);
        EmployeePerformance UpdateEmployeeObjectiveService(EmployeePerformance employeeObjective);
    }
}
