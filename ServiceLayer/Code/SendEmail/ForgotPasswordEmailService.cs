using BottomhalfCore.DatabaseLayer.Common.Code;
using ModalLayer.Modal;
using ModalLayer.Modal.Leaves;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Code.SendEmail
{
    public class ForgotPasswordEmailService
    {
        private readonly IDb _db;
        private readonly IEmailService _emailService;
        public ForgotPasswordEmailService(IDb db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        public async Task<EmailTemplate> GetForgotPasswordTemplate(int EmailTemplateId)
        {
            (EmailTemplate emailTemplate, EmailSettingDetail emailSetting) =
                _db.GetMulti<EmailTemplate, EmailSettingDetail>("sp_email_template_by_id", new { EmailTemplateId });

            if (emailSetting == null)
                throw new HiringBellException("Email setting detail not found. Please contact to admin.");

            if (emailTemplate == null)
                throw new HiringBellException("Email template not found. Please contact to admin.");

            return await Task.FromResult(emailTemplate);
        }

        private void BuildEmailBody(EmailTemplate emailTemplate, string password)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("<div>" + emailTemplate.Salutation + "</div>");
            string body = JsonConvert.DeserializeObject<string>(emailTemplate.BodyContent)
                          .Replace("[[NEW-PASSWORD]]", password);

            stringBuilder.Append("<div>" + emailTemplate.EmailClosingStatement + "</div>");
            stringBuilder.Append("<div>" + emailTemplate.SignatureDetail + "</div>");
            stringBuilder.Append("<div>" + emailTemplate.ContactNo + "</div>");

            emailTemplate.BodyContent = body + stringBuilder.ToString();
        }

        public async Task SendForgotPasswordEmail(string password, string email)
        {
            EmailSenderModal emailSenderModal = new EmailSenderModal();
            EmailTemplate emailTemplate = await GetForgotPasswordTemplate(ApplicationConstants.ForgotPasswordEmailTemplate);
            BuildEmailBody(emailTemplate, password);

            emailSenderModal.Body = emailTemplate.BodyContent;
            emailSenderModal.To = new List<string> { email };
            emailSenderModal.Subject = emailTemplate.SubjectLine;
            emailSenderModal.UserName = "BottomHalf";
            emailSenderModal.Title = "[BottomHalf] Temporary password request.";

            _emailService.SendEmailRequestService(emailSenderModal, null);
        }
    }
}
