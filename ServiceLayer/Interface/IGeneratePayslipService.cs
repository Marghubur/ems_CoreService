using ModalLayer.Modal;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IGeneratePayslipService
    {
        Task<FileDetail> GeneratePayslip(PayslipGenerationModal payslipGenerationModal);
        Task<byte[]> GenerateBulkPayslipService(PayslipGenerationModal payslipGenerationModal);
    }
}