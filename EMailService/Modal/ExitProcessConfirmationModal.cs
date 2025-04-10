using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using System.Collections.Generic;

namespace EMailService.Modal
{
    public class ExitProcessConfirmationModal
    {
        public string EmployeeName { get; set; }
        public string CompanyName { get; set; }
        public string LastWorkingDay { get; set; }
        public string FullAndFinalSattlementStatus { get; set; }
        public string RelievingAndExperienceLetterStatus { get; set; }
        public string CompanyAssetsReturnStatus { get; set; }
        public string HRClearanceStatus { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyContactNumber { get; set; }
        public string Name { get; set; }
        public string Designation { get; set; }
        public List<string> EmployeeEmail { get; set; }
        public KafkaServiceName kafkaServiceName { get; set; }
        public string LocalConnectionString { get; set; }
    }
}
