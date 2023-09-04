using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface ICompanyService
    {
        List<OrganizationDetail> GetAllCompany();
        List<OrganizationDetail> AddCompanyGroup(OrganizationDetail companyGroup);
        List<OrganizationDetail> UpdateCompanyGroup(OrganizationDetail companyGroup, int companyId);
        dynamic GetCompanyById(int companyId);
        OrganizationDetail GetOrganizationDetailService();
        Task<OrganizationDetail> InsertUpdateOrganizationDetailService(OrganizationDetail companyInfo, IFormFileCollection fileCollection);
        List<BankDetail> InsertUpdateCompanyAccounts(BankDetail bankDetail);
        List<BankDetail> GetCompanyBankDetail(FilterModel filterModel);
        Task<CompanySetting> UpdateSettingService(int companyId, CompanySetting companySetting, bool isRunLeaveAccrual);
        Task<dynamic> GetCompanySettingService(int companyId);
        Task<OrganizationDetail> InsertUpdateCompanyDetailService(OrganizationDetail companyInfo, IFormFileCollection fileCollection);
        Task<List<Files>> UpdateCompanyFiles(Files uploadedFileDetail, IFormFileCollection fileCollection);
        Task<List<Files>> GetCompanyFiles(int CompanyId);
        Task<List<Files>> DeleteCompanyFilesService(Files companyFile);
        Task<CompanySetting> GetCompanySettingByCompanyId(int companyId);
    }
}
