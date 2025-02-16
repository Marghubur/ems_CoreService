using EMailService.Modal;
using ModalLayer.Modal;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IOvertimeService
    {
        Task<List<OvertimeConfiguration>> ManageOvertimeConfigService(OvertimeConfiguration overtimeDetail);
        Task<DataSet> GetOvertimeTypeAndConfigService();
        Task<(List<EmployeeOvertime> EmployeeOvertimes, List<OvertimeConfiguration> OvertimeConfigurations)> ApplyOvertimeService(EmployeeOvertime employeeOvertime);
        Task<(List<EmployeeOvertime> EmployeeOvertimes, List<OvertimeConfiguration> OvertimeConfigurations)> GetEmployeeOvertimeService();
        Task<List<EmployeeOvertime>> GetEmployeeOTByMangerService(FilterModel filterModel);
        Task<List<EmployeeOvertime>> ApproveEmployeeOvertimeService(List<EmployeeOvertime> employeeOvertimes);
        Task<List<EmployeeOvertime>> RejectEmployeeOvertimeService(List<EmployeeOvertime> employeeOvertimes);
    }
}
