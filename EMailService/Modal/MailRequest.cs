using System.Collections.Generic;

namespace ModalLayer.Modal
{
    public class MailRequest
    {
        public List<string> To { set; get; }
        public string Subject { set; get; }
        public string Body { set; get; }
        public List<string> CC { set; get; } = new List<string>();
        public List<string> BCC { set; get; } = new List<string>();
        public List<FileDetail> FileDetails { set; get; }
        public string From { set; get; }
        public string UserName { set; get; }
    }
}
