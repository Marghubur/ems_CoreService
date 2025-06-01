using System;

namespace ModalLayer.Modal
{
    public class EmployeeNotification : CreationInfo
    {
        public int NotificationId { set; get; }
        public string Title { set; get; }
        public string SubTitle { set; get; }
        public string PlainMessage { set; get; }
        public string ParsedContentLink { set; get; }
        public string Attachment { set; get; }
        public string FileIds { set; get; }
        public long NotifierId { set; get; }
        public bool AutoDeleteEnabled { set; get; }
        public int LifeSpanInMinutes { set; get; }
        public int NotificationTypeId { set; get; }
        public bool IsViewed { set; get; }
    }
}
