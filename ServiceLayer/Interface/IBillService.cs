using Bot.CoreBottomHalf.CommonModal;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IBillService
    {
        string UpdateGstStatus(GstStatusModel createPageModel, IFormFileCollection FileCollection, List<Files> fileDetail);
        dynamic GenerateDocument(PdfModal pdfmodal, List<DailyTimesheetDetail> dailyTimesheetDetails,
            TimesheetDetail timesheetDetail, string Comment);

        Task<dynamic> UpdateGeneratedBillService(BillGenerationModal billModal);

        Task<dynamic> GenerateBillService(BillGenerationModal billModal);
        FileDetail CreateFiles(BillGenerationModal billModal);
        Task<string> SendBillToClientService(GenerateBillFileDetail generateBillFileDetail);
        Task<dynamic> GetBillDetailWithTemplateService(string billNo, long employeeId);
        Task<FileDetail> GeneratePayslipService(PayslipGenerationModal payslipGenerationModal);
        Task<byte[]> GenerateBulkPayslipService(PayslipGenerationModal payslipGenerationModal);
        Task<string> GetDocxHtmlService(FileDetail fileDetail);
    }
}
