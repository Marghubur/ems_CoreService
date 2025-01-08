using Bt.Lib.Common.Service.Model;

namespace EMailService.Modal
{
    public class MicroserviceRegistry
    {
        public string SaveApplicationFile { get; set; }
        public string CreateFolder { get; set; }
        public string DeleteFiles { get; set; }
        public string ConvertHtmlToPdf { get; set; }
        public string DatabaseConfigurationUrl { set; get; }
        public string RunPayroll {  get; set; }
        public string GetEmployeeDeclarationDetailById {  get; set; }
        public string SalaryDeclarationCalculation {  get; set; }
        public string UpdateBulkDeclarationDetail {  get; set; }
        public string CalculateSalaryDetail {  get; set; }
    }
}
