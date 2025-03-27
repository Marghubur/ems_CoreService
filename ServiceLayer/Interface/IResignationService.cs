using EMailService.Modal;
using ModalLayer.Modal;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IResignationService
    {
        Task<dynamic> GetEmployeeResignationByIdService(long employeeId);
        Task<string> SubmitResignationService(EmployeeNoticePeriod employeeNoticePeriod);
        Task<List<EmployeeResignation>> GetAllEmployeeResignationService(FilterModel filterModel);
        Task<List<EmployeeAssetsAllocation>> GetEmployeeAssetsAllocationByEmpIdService(long employeeId);
        Task<string> ApproveEmployeeAssetsAllocationService(List<EmployeeAssetsAllocation> employeeAssetsAllocations, long employeeId);
        Task<List<EmployeeResignation>> ApproveEmployeeResignationService(long employeeId, string content);
        Task<List<EmployeeResignation>> RejectEmployeeResignationService(long employeeId, string comment);
        Task<List<EmployeeResignation>> EmployeeResignAssignToMeService(long employeeId);
        Task<List<EmployeeExitConfiguration>> ManageEmployeeExitConfigurationService(List<EmployeeExitConfiguration>  employeeExitConfigurations);
    }
}
