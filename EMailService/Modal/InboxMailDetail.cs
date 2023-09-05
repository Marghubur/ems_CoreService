using System;

namespace ModalLayer.Modal
{
    public class InboxMailDetail
    {
        public int EMailIndex { get; set; }
        public string Subject { get; set; }
        public string From { get; set; }
        public string Body { get; set; }
        public string Text { set; get; }
        public string Priority { set; get; }
        public DateTime Date { set; get; }
        public string Name { get; set; } = "NA";
        public string SearchString { get; set; }
        public int RecordCount { get; set; } = 10;
    }
}
