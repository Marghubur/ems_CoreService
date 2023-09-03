using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Code.PayrollCycle.Interface
{
    public interface IUploadPayrollDataService
    {
        Task<List<UploadedPayrollData>> ReadPayrollDataService(IFormFileCollection file);
    }
}
