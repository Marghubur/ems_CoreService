using System;

namespace EMailService.Modal
{
    public class ServiceJobStatus
    {
        public int ServiceJobStatusId { get; set; }
        public string ServiceName { get; set; }
        public DateTime JobStartedOn { get; set; }
        public DateTime? JobEndedOn { get; set; }
        public int JobStatus { get; set; }
        public string ServiceLog { get; set; }
    }
}
