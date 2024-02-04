using System;

namespace EMailService.Modal.Jobs
{
    public class LeaveYearEndCalculationKafkaModel : KafkaPayload
    {
        public DateTime RunDate { get; set; }
    }
}
