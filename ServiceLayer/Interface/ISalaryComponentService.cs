using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface ISalaryComponentService
    {
        SalaryComponents GetSalaryComponentByIdService();
        List<SalaryComponents> GetSalaryComponentsDetailService();
        Task<List<SalaryComponents>> UpdateSalaryComponentService(List<SalaryComponents> salaryComponents);
        Task<List<SalaryComponents>> InsertUpdateSalaryComponentsByExcelService(IFormFileCollection file);
        List<SalaryGroup> GetSalaryGroupService(int CompanyId);
        dynamic GetCustomSalryPageDataService(int CompanyId);
        SalaryGroup GetSalaryGroupsByIdService(int SalaryGroupId);
        List<SalaryGroup> AddSalaryGroup(SalaryGroup salaryGroup);
        List<SalaryGroup> UpdateSalaryGroup(SalaryGroup salaryGroup);
        SalaryGroup RemoveAndUpdateSalaryGroupService(string componentId, int groupId);
        List<SalaryComponents> UpdateSalaryGroupComponentService(SalaryGroup salaryGroup);
        List<SalaryComponents> GetSalaryGroupComponents(int salaryGroupId, decimal CTC);
        Task<List<SalaryComponents>> AddUpdateRecurringComponents(SalaryStructure salaryStructure);
        List<SalaryComponents> AddAdhocComponents(SalaryStructure salaryStructure);
        List<SalaryComponents> AddBonusComponents(SalaryComponents salaryStructure);
        List<SalaryComponents> AddDeductionComponents(SalaryStructure salaryStructure);
        string SalaryDetailService(long EmployeeId, List<CalculatedSalaryBreakupDetail> calculatedSalaryBreakupDetail, int PresentMonth, int PresentYear);
        Task<List<AnnualSalaryBreakup>> SalaryBreakupCalcService(long EmployeeId, decimal CTCAnnually);
        dynamic GetSalaryBreakupByEmpIdService(long EmployeeId);
        SalaryGroup GetSalaryGroupByCTC(decimal CTC, long EmployeeId);
        List<AnnualSalaryBreakup> CreateSalaryBreakupWithValue(EmployeeCalculation empCal);
        List<SalaryComponents> GetBonusComponentsService();
        DataSet GetAllSalaryDetailService(FilterModel filterModel);
        List<AnnualSalaryBreakup> UpdateSalaryBreakUp(EmployeeCalculation eCal, EmployeeSalaryDetail salaryBreakup);
        Task GetEmployeeSalaryDetail(EmployeeCalculation employeeCalculation);
        List<SalaryGroup> CloneSalaryGroupService(SalaryGroup salaryGroup);
    }
}
