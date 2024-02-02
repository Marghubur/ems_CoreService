using ModalLayer;

namespace EMailService.Modal.Jobs
{
    public class KafkaServiceConfigExtend : KafkaServiceConfig
    {
        public string HourlyJobTopic { set; get; }
        public string GroupId { set; get; }
    }
}
