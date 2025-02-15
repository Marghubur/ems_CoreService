using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.Modal;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IEmailService
    {
        string SendEmailRequestService(EmailSenderModal mailRequest, IFormFileCollection files);
        List<InboxMailDetail> GetMyMailService();
        Task<EmailSettingDetail> GetEmailSettingByCompIdService(int CompanyId);
        Task<EmailSettingDetail> InsertUpdateEmailSettingService(EmailSettingDetail emailSettingDetail);
        Task<string> InsertUpdateEmailTemplateService(EmailTemplate emailTemplate, IFormFileCollection file);
        Task<List<EmailTemplate>> GetEmailTemplateService(FilterModel filterModel);
        Task<dynamic> GetEmailTemplateByIdService(long EmailTemplateId, int CompanyId);
        Task<EmailSenderModal> SendEmailWithTemplate(int TemplateId, TemplateReplaceModal templateReplaceModal);
        Task<dynamic> EmailTempMappingInsertUpdateService(EmailMappedTemplate emailMappedTemplate);
        Task<dynamic> GetEmailTempMappingService(FilterModel filterModel);
    }
}
