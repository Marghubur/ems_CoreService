using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using EMailService.Modal;
using ems_CoreService.Model;
using ModalLayer.Modal.Accounts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface ISettingService
    {
        string AddUpdateComponentService(SalaryComponents salaryComponents);
        PfEsiSetting GetSalaryComponentService(int CompanyId);
        Task<PfEsiSetting> PfEsiSetting(int CompanyId, PfEsiSetting PfesiSetting);
        List<OrganizationDetail> GetOrganizationInfo();
        BankDetail GetOrganizationBankDetailInfoService(int organizationId);
        string InsertUpdatePayrollSetting(Payroll payroll);
        Payroll GetPayrollSetting(int companyId);
        string InsertUpdateSalaryStructure(List<SalaryStructure> salaryStructure);
        Task<List<SalaryComponents>> ActivateCurrentComponentService(List<SalaryComponents> components);
        List<SalaryComponents> UpdateSalaryComponentDetailService(string componentId, SalaryComponents component);
        List<SalaryComponents> FetchComponentDetailByIdService(int componentTypeId);
        List<SalaryComponents> FetchActiveComponentService();
        Task<UserLayoutConfigurationJSON> LayoutConfigurationSettingService(UserLayoutConfigurationJSON userLayoutConfiguration);
        Task<string> SaveTDSSettingService(TDSSetting tDSSetting);
        Task<TDSSetting> GetTDSSettingService();
    }
}
