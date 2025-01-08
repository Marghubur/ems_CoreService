using Bt.Lib.Common.Service.Model;

namespace EMailService.Modal
{
    public class MicroserviceUrlLogs : MicroserviceRegistry
    {
        public string SaveApplicationFile { get; set; }
        public string CreateFolder { get; set; }
        public string DeleteFiles { get; set; }
        public string ConvertHtmlToPdf { get; set; }
    }
}
