namespace EMailService.Modal.Jobs
{
    public class LeaveAccrualKafkaModel : KafkaPayload
    {
        public bool GenerateLeaveAccrualTillMonth { set; get; }
    }
}
