using EMailService.Modal;
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
    }
}
