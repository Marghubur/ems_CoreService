using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface ICommonService
    {
        List<Employee> LoadEmployeeData();
        bool IsEmptyJson(string json);
        EmailTemplate GetTemplate(int EmailTemplateId);
        string GetUniquecode(long id, string name, int size = 10);
        long DecryptUniqueCoe(string code);

        /*------------  code convertion to json only required field -----------------*/
        string GetStringifySalaryGroupData(List<SalaryComponents> salaryComponents);
        Task<DataTable> ReadExcelData(IFormFileCollection files);
        Task<List<T>> ReadExcelData<T>(IFormFileCollection files);
    }
}
