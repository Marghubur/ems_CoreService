using System;

namespace EMailService.Modal.Jobs
{
    public class KafkaPayload
    {
        public string ServiceName { set; get; }
        public string Message { set; get; }
    }

    public enum ScheduledJobServiceName
    {
        MONTHLYLEAVEACCRUAL,
        WEEKLYTIMESHEET,
        YEARENDLEAVEPROCESSING,
        MONTHLYPAYROLL
    }
}
