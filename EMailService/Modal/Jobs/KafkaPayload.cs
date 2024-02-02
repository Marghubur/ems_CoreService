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
        DAILY,
        WEEKLY,
        MONTHLY,
        YEARLY,
        DAILYWEEKLY,
        DAILYMONTHLY,
        DAILYYEARLY,
        WEEKLYMONTHLY,
        WEEKLYYEARLY,
        MONTHLYYEARLY,
        YEARLYMONTHLY,
        DAILYWEEKLYMONTHLY,
        DAILYWEEKLYYEARLY,        
        WEEKLYMONTHLYYEARLY,
        DAILYWEEKLYMONTHLYYEARLY,
    }
}
