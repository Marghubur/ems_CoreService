namespace ModalLayer.Modal
{
    public class EmailLinkConfig: EmailTemplate
    {
        public string PageName {get; set;}
        public string PageDescription {get; set;}
        public bool IsEmailGroupUsed {get; set;}
        public int EmailGroupId {get; set;}
        public bool IsTriggeredAutomatically {get; set;}
        public string EmailsJson { get; set; }
    }
}
