using ModalLayer;

namespace EMailService.Modal.Notification
{
    public class CompanyNotification : CreationInfo
    {
        public long NotificationId { set; get; }
        public string Title { set; get; }
        public string SubTitle { set; get; }
        public string Departments { set; get; }
        public string NotificationMessage { set; get; }
        public string ParsedContentMessage { set; get; }
        public int NotificationTypeId { set; get; }
        public string FileIds { set; get; }
        public bool AutoDeleteEnabled { set; get; }
        public int LifeSpanInMinutes { set; get; }
    }
}
