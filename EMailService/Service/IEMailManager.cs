using ModalLayer;
using ModalLayer.Modal;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMailService.Service
{
    public interface IEMailManager
    {
        Task SendMailAsync(EmailSenderModal emailSenderModal);
        List<InboxMailDetail> ReadMails(EmailSettingDetail emailSettingDetail);
        EmailTemplate GetTemplate(EmailRequestModal emailRequestModal);
    }
}
