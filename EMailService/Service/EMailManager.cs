using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.Modal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using MailKit.Net.Pop3;
using ModalLayer;
using ModalLayer.Modal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace EMailService.Service
{
    public class EMailManager : IEMailManager
    {
        private readonly FileLocationDetail _fileLocationDetail;
        private EmailSettingDetail _emailSettingDetail;
        private readonly IDb _db;

        public EMailManager(FileLocationDetail fileLocationDetail, IDb db)
        {
            _fileLocationDetail = fileLocationDetail;
            _db = db;
        }

        public EmailTemplate GetTemplate(EmailRequestModal emailRequestModal)
        {
            if (emailRequestModal.TemplateId <= 0)
                throw new HiringBellException("No email template has been selected.");

            if (string.IsNullOrEmpty(emailRequestModal.ManagerName))
                throw new HiringBellException("Manager name is missing.");

            if (string.IsNullOrEmpty(emailRequestModal.DeveloperName))
                throw new HiringBellException("Developer name is missing.");

            EmailTemplate emailTemplate = _db.Get<EmailTemplate>("sp_email_template_get", new { EmailTemplateId = emailRequestModal.TemplateId });

            if (emailTemplate == null)
                throw new HiringBellException("Email template not found. Please contact to admin.");

            var footer = new StringBuilder();
            footer.Append($"<div>{emailTemplate.EmailClosingStatement}</div>");
            footer.Append($"<div>{emailTemplate.SignatureDetail}</div>");
            footer.Append($"<div>{emailTemplate.ContactNo}</div>");

            var logoPath = Path.Combine(_fileLocationDetail.RootPath, _fileLocationDetail.LogoPath, ApplicationConstants.HiringBellLogoSmall);
            if (File.Exists(logoPath))
            {
                footer.Append($"<div><img src=\"cid:{ApplicationConstants.LogoContentId}\" style=\"width: 10rem;margin-top: 1rem;\"></div>");
            }


            emailTemplate.Footer = footer.ToString();

            emailTemplate.SubjectLine = emailTemplate.EmailTitle
                .Replace("[[REQUEST-TYPE]]", emailRequestModal.RequestType)
                .Replace("[[ACTION-TYPE]]", emailRequestModal.ActionType);

            emailTemplate.BodyContent = emailTemplate.BodyContent
                .Replace("[[DEVELOPER-NAME]]", emailRequestModal.DeveloperName)
                .Replace("[[ACTION-TYPE]]", emailRequestModal.ActionType)
                .Replace("[[DAYS-COUNT]]", emailRequestModal.TotalNumberOfDays.ToString())
                .Replace("[[FROM-DATE]]", emailRequestModal.FromDate.ToString("dd MMM, yyy"))
                .Replace("[[TO-DATE]]", emailRequestModal.ToDate.ToString("dd MMM, yyy"))
                .Replace("[[MANAGER-NAME]]", emailRequestModal.ManagerName)
                .Replace("[[USER-MESSAGE]]", emailRequestModal.Message)
                .Replace("[[REQUEST-TYPE]]", emailRequestModal.RequestType);

            emailTemplate.EmailTitle = emailTemplate.EmailTitle.Replace("[[REQUEST-TYPE]]", emailRequestModal.RequestType)
                                    .Replace("[[DEVELOPER-NAME]]", emailRequestModal.DeveloperName)
                                    .Replace("[[ACTION-TYPE]]", emailRequestModal.ActionType);
            return emailTemplate;
        }

        private List<InboxMailDetail> ReadPOP3Email(EmailSettingDetail emailSettingDetail)
        {
            List<InboxMailDetail> inboxMailDetails = new List<InboxMailDetail>();

            // Create a new POP3 client
            using (Pop3Client client = new Pop3Client())
            {
                // Connect to the server
                client.Connect("pop.secureserver.net", 995, true);

                // Authenticate with the server
                client.Authenticate(emailSettingDetail.EmailAddress, emailSettingDetail.Credentials);

                // Get the list of messages
                int messageCount = client.GetMessageCount();

                int i = messageCount - 1;
                int mailCounter = 0;
                // Loop through the messages
                while (i > 0 && mailCounter != 15)
                {
                    // Get the message
                    var message = client.GetMessage(i);

                    inboxMailDetails.Add(new InboxMailDetail
                    {
                        Subject = message.Subject,
                        From = message.From.ToString(),
                        Body = message.HtmlBody,
                        EMailIndex = i,
                        Text = message.GetTextBody(MimeKit.Text.TextFormat.Plain),
                        Priority = message.Priority.ToString(),
                        Date = message.Date.DateTime
                    });

                    mailCounter++;
                    i--;
                }
            }

            return inboxMailDetails;
        }

        public List<InboxMailDetail> ReadMails(EmailSettingDetail emailSettingDetail)
        {
            if (emailSettingDetail == null)
            {
                GetDefaultEmailDetail();
                emailSettingDetail = _emailSettingDetail;
            }

            return ReadPOP3Email(emailSettingDetail);
        }

        private string _generateFileName(int sequence)
        {
            DateTime currentDateTime = DateTime.Now;
            return string.Format("{0}-{1:000}-{2:000}.eml",
                currentDateTime.ToString("yyyyMMddHHmmss", new CultureInfo("en-US")),
                currentDateTime.Millisecond,
                sequence);
        }

        public async Task SendMailAsync(EmailSenderModal emailSenderModal)
        {
            GetDefaultEmailDetail();
            //if (_emailSettingDetail == null)
            //    throw new HiringBellException("Email setting detail not found. Please contact to admin.");

            await Task.Run(() => Send(emailSenderModal));
            await Task.CompletedTask;
        }

        private string Send(EmailSenderModal emailSenderModal)
        {
            if (emailSenderModal == null || emailSenderModal.To == null || emailSenderModal.To.Count == 0)
                throw new HiringBellException("To send email receiver address is mandatory. Receiver address not found.");

            if (string.IsNullOrEmpty(emailSenderModal.Title))
                throw new HiringBellException("Please add emial Title.");

            var fromAddress = new System.Net.Mail.MailAddress(_emailSettingDetail.EmailAddress, emailSenderModal.Title);

            var smtp = new SmtpClient
            {
                Host = _emailSettingDetail.EmailHost,
                Port = _emailSettingDetail.PortNo,
                EnableSsl = _emailSettingDetail.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = _emailSettingDetail.UserDefaultCredentials,
                Credentials = new NetworkCredential(fromAddress.Address, _emailSettingDetail.Credentials)
            };

            var message = new MailMessage();
            message.Subject = emailSenderModal.Subject;
            message.Body = emailSenderModal.Body;
            message.IsBodyHtml = true;
            message.From = fromAddress;
            //message.AlternateViews.Add(CreateHtmlMessage(emailSenderModal.Body, logoPath));

            foreach (var emailAddress in emailSenderModal.To)
                message.To.Add(emailAddress);

            if (emailSenderModal.CC != null && emailSenderModal.CC.Count > 0)
                foreach (var emailAddress in emailSenderModal.CC)
                    message.CC.Add(emailAddress);

            if (emailSenderModal.BCC != null && emailSenderModal.BCC.Count > 0)
                foreach (var emailAddress in emailSenderModal.BCC)
                    message.Bcc.Add(emailAddress);

            try
            {
                if (emailSenderModal.FileDetails != null && emailSenderModal.FileDetails.Count > 0)
                {
                    foreach (var files in emailSenderModal.FileDetails)
                    {
                        message.Attachments.Add(
                            new System.Net.Mail.Attachment(Path.Combine(_fileLocationDetail.RootPath, files.FilePath, files.FileName + ".pdf"))
                        );
                    }
                }

                var logoPath = Path.Combine(_fileLocationDetail.RootPath, _fileLocationDetail.LogoPath, ApplicationConstants.HiringBellLogoSmall);
                if (File.Exists(logoPath))
                {
                    var attachment = new System.Net.Mail.Attachment(logoPath);
                    attachment.ContentId = ApplicationConstants.LogoContentId;
                    message.Attachments.Add(attachment);
                }

                smtp.Send(message);
            }
            catch (Exception ex)
            {
                var _e = ex;
                throw;
            }

            return ApplicationConstants.Successfull;
        }

        private void GetDefaultEmailDetail()
        {
            // _emailSettingDetail = _db.Get<EmailSettingDetail>("sp_email_setting_detail_get", new { EmailSettingDetailId = 0 });
            _emailSettingDetail = new EmailSettingDetail
            {
                EmailHost = "smtp.secureserver.net",
                PortNo = 465,
                Credentials = "Bottomhalf@i9_0012",
                EmailAddress = "info@bottomhalf.in",
                EnableSsl = true,
            };
        }
    }
}
