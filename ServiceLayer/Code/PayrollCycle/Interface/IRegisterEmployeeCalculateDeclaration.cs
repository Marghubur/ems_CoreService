using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using System.Threading.Tasks;

namespace ServiceLayer.Code.PayrollCycle.Interface
{
    public interface IRegisterEmployeeCalculateDeclaration
    {
        Task<string> UpdateEmployeeService(Employee employee, UploadedPayrollData uploaded, IFormFileCollection fileCollection);
    }
}
