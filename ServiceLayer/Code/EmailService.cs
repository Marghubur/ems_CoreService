﻿using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.Modal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using Bt.Lib.PipelineConfig.Services;
using EMailService.Modal;
using EMailService.Service;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class EmailService : IEmailService
    {
        private readonly IEMailManager _eMailManager;
        private readonly IDb _db;
        private readonly CurrentSession _currentSession;
        private EmailSettingDetail _emailSettingDetail;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly IFileService _fileService;
        private readonly ICompanyService _companyService;
        private readonly ITimezoneConverter _timezoneConverter;
        private readonly MicroserviceRegistry _microserviceUrlLogs;
        private readonly GitHubConnector _gitHubConnector;
        public EmailService(IDb db,
            IEMailManager eMailManager,
            CurrentSession currentSession,
            ICompanyService companyService,
            FileLocationDetail fileLocationDetail,
            IFileService fileService,
            ITimezoneConverter timezoneConverter,
            MicroserviceRegistry microserviceUrlLogs,
            GitHubConnector gitHubConnector)
        {
            _db = db;
            _eMailManager = eMailManager;
            _currentSession = currentSession;
            _fileLocationDetail = fileLocationDetail;
            _fileService = fileService;
            _companyService = companyService;
            _timezoneConverter = timezoneConverter;
            _microserviceUrlLogs = microserviceUrlLogs;
            _gitHubConnector = gitHubConnector;
        }

        public List<InboxMailDetail> GetMyMailService()
        {
            this.GetSettingDetail();
            return _eMailManager.ReadMails(_emailSettingDetail);
        }

        public async Task<EmailSenderModal> SendEmailWithTemplate(int TemplateId, TemplateReplaceModal templateReplaceModal)
        {
            var template = _db.Get<EmailTemplate>("sp_email_template_get", new { EmailTemplateId = TemplateId });
            if (template == null)
                throw new HiringBellException("Fail to get Leave Request template. Please contact to admin.");

            switch (TemplateId)
            {
                case 2:
                    templateReplaceModal.Subject = _currentSession.CurrentUserDetail.FullName + " | " + templateReplaceModal.RequestType + " | " + templateReplaceModal.FromDate.ToString("dd MMM, yyyy");
                    break;
                default:
                    templateReplaceModal.Subject = template.SubjectLine;
                    templateReplaceModal.Title = template.EmailTitle;
                    break;
            }

            templateReplaceModal.CompanyName = template.SignatureDetail;
            templateReplaceModal.BodyContent = template.BodyContent;
            var emailSenderModal = await ReplaceActualData(templateReplaceModal, template);

            await _eMailManager.SendMailAsync(emailSenderModal);
            return await Task.FromResult(emailSenderModal);
        }

        public async Task<EmailSenderModal> ReplaceActualData(TemplateReplaceModal templateReplaceModal, EmailTemplate template)
        {
            EmailSenderModal emailSenderModal = null;
            var fromDate = _timezoneConverter.ToTimeZoneDateTime(templateReplaceModal.FromDate, _currentSession.TimeZone);
            var toDate = _timezoneConverter.ToTimeZoneDateTime(templateReplaceModal.ToDate, _currentSession.TimeZone);
            if (templateReplaceModal != null)
            {
                var totalDays = templateReplaceModal.ToDate.Date.Subtract(templateReplaceModal.FromDate.Date).TotalDays + 1;
                string subject = templateReplaceModal.Subject
                                 .Replace("[[REQUEST-TYPE]]", templateReplaceModal.RequestType)
                                 .Replace("[[ACTION-TYPE]]", templateReplaceModal.ActionType);

                string body = JsonConvert.DeserializeObject<string>(templateReplaceModal.BodyContent)
                                .Replace("[[DEVELOPER-NAME]]", templateReplaceModal.DeveloperName)
                                .Replace("[[DAYS-COUNT]]", $"{totalDays}")
                                .Replace("[[REQUEST-TYPE]]", templateReplaceModal.RequestType)
                                .Replace("[[TO-DATE]]", fromDate.ToString("dd MMM, yyyy"))
                                .Replace("[[FROM-DATE]]", toDate.ToString("dd MMM, yyyy"))
                                .Replace("[[ACTION-TYPE]]", templateReplaceModal.ActionType)
                                .Replace("[[MANAGER-NAME]]", templateReplaceModal.ManagerName)
                                .Replace("[[USER-MESSAGE]]", templateReplaceModal.Message);

                StringBuilder builder = new StringBuilder();
                builder.Append("<div style=\"border-bottom:1px solid black; margin-top: 14px; margin-bottom:5px\">" + "" + "</div>");
                builder.AppendLine();
                builder.AppendLine();
                builder.Append("<div>" + template.EmailClosingStatement + "</div>");
                builder.Append("<div>" + template.SignatureDetail + "</div>");
                builder.Append("<div>" + template.ContactNo + "</div>");

                var logoPath = Path.Combine(_fileLocationDetail.RootPath, _fileLocationDetail.LogoPath, ApplicationConstants.HiringBellLogoSmall);
                if (File.Exists(logoPath))
                {
                    builder.Append($"<div><img src=\"cid:{ApplicationConstants.LogoContentId}\" style=\"width: 10rem;margin-top: 1rem;\"></div>");
                }

                emailSenderModal = new EmailSenderModal
                {
                    To = templateReplaceModal.ToAddress,
                    Subject = subject,
                    Body = string.Concat(body, builder.ToString()),
                };
            }

            emailSenderModal.Title = templateReplaceModal.Title.Replace("[[REQUEST-TYPE]]", templateReplaceModal.Title)
                                    .Replace("[[DEVELOPER-NAME]]", templateReplaceModal.DeveloperName)
                                    .Replace("[[ACTION-TYPE]]", templateReplaceModal.ActionType);

            return await Task.FromResult(emailSenderModal);
        }


        public string SendEmailRequestService(EmailSenderModal mailRequest, IFormFileCollection files)
        {
            string result = null;
            this.GetSettingDetail();
            EmailSenderModal emailSenderModal = new EmailSenderModal
            {
                To = mailRequest.To, //receiver.Email,
                CC = mailRequest.CC,    //new List<string>(),
                BCC = mailRequest.BCC,  //new List<string>(),
                Subject = mailRequest.Subject,
                Body = mailRequest.Body,
                FileDetails = new List<FileDetail>() //Converter.ToList<FileDetail>(mailRequest.FileDetails)
            };

            result = SendMail(emailSenderModal, files);
            return result;
        }

        private void GetSettingDetail()
        {
            _emailSettingDetail = _db.Get<EmailSettingDetail>(Procedures.Email_Setting_Detail_Get, new { EmailSettingDetailId = 0 });
            if (_emailSettingDetail == null)
                throw new HiringBellException("Fail to get emaill detail. Please contact to admin.");
        }

        private string SendMail(EmailSenderModal emailSenderModal, IFormFileCollection files)
        {
            string status = string.Empty;

            if (emailSenderModal.To == null || emailSenderModal.To.Count == 0)
                throw new HiringBellException("To send email receiver address is mandatory. Receiver address not found.");

            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            var fromAddress = new System.Net.Mail.MailAddress(_emailSettingDetail.EmailAddress, emailSenderModal.Subject);

            var smtp = new SmtpClient
            {
                Host = _emailSettingDetail.EmailHost,
                Port = _emailSettingDetail.PortNo,
                EnableSsl = _emailSettingDetail.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = _emailSettingDetail.UserDefaultCredentials,
                Credentials = new NetworkCredential(_emailSettingDetail.EmailAddress, _emailSettingDetail.Credentials)
            };

            var mailMessage = new MailMessage();
            mailMessage.Subject = emailSenderModal.Subject;
            mailMessage.Body = emailSenderModal.Body;
            mailMessage.IsBodyHtml = true;
            mailMessage.From = fromAddress;

            foreach (var emailAddress in emailSenderModal.To)
                mailMessage.To.Add(new MailAddress(emailAddress));

            if (emailSenderModal.CC != null && emailSenderModal.CC.Count > 0)
                foreach (var emailAddress in emailSenderModal.CC)
                    mailMessage.CC.Add(emailAddress);

            if (emailSenderModal.BCC != null && emailSenderModal.BCC.Count > 0)
                foreach (var emailAddress in emailSenderModal.BCC)
                    mailMessage.Bcc.Add(emailAddress);

            try
            {
                if (files != null && files.Count > 0)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        foreach (var file in files)
                        {
                            file.CopyTo(ms);
                            ms.Position = 0;
                            mailMessage.Attachments.Add(
                                new System.Net.Mail.Attachment(ms, file.Name)
                            );
                            ms.Flush();
                        }
                        smtp.Send(mailMessage);
                    }
                }
                else
                {
                    smtp.Send(mailMessage);
                }

                status = "success";
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return status;
        }

        public async Task<EmailSettingDetail> GetEmailSettingByCompIdService(int CompanyId)
        {
            if (CompanyId == 0)
                throw new HiringBellException("Invalid company selected");

            string cs = await _gitHubConnector.FetchTypedConfiguraitonAsync<string>(_microserviceUrlLogs.DatabaseConfigurationUrl);
            _db.SetupConnectionString(cs);
            EmailSettingDetail emailSettingDetail = _db.Get<EmailSettingDetail>(Procedures.Email_Setting_Detail_By_CompanyId, new { CompanyId = CompanyId });
            return emailSettingDetail;
        }

        public async Task<EmailSettingDetail> InsertUpdateEmailSettingService(EmailSettingDetail emailSettingDetail)
        {
            EmailSettingDetail emailSetting = null;
            EmailSettingsValidation(emailSettingDetail);

            string cs = await _gitHubConnector.FetchTypedConfiguraitonAsync<string>(_microserviceUrlLogs.DatabaseConfigurationUrl);
            _db.SetupConnectionString(cs);
            emailSetting = _db.Get<EmailSettingDetail>(Procedures.Email_Setting_Detail_By_CompanyId, new { CompanyId = emailSettingDetail.CompanyId });
            if (emailSetting != null)
            {
                emailSetting.EmailAddress = emailSettingDetail.EmailAddress;
                emailSetting.EmailHost = emailSettingDetail.EmailHost;
                emailSetting.PortNo = emailSettingDetail.PortNo;
                emailSetting.EnableSsl = emailSettingDetail.EnableSsl;
                emailSetting.DeliveryMethod = emailSettingDetail.DeliveryMethod;
                emailSetting.UserDefaultCredentials = emailSettingDetail.UserDefaultCredentials;
                emailSetting.Credentials = emailSettingDetail.Credentials;
                emailSetting.EmailName = emailSettingDetail.EmailName;
                emailSetting.POP3EmailHost = emailSettingDetail.POP3EmailHost;
                emailSetting.POP3PortNo = emailSettingDetail.POP3PortNo;
                emailSetting.POP3EnableSsl = emailSettingDetail.POP3EnableSsl;
                emailSetting.IsPrimary = emailSettingDetail.IsPrimary;
                emailSetting.UpdatedBy = _currentSession.CurrentUserDetail.UserId;
            }
            else
            {
                emailSetting = emailSettingDetail;
                emailSetting.UpdatedBy = _currentSession.CurrentUserDetail.UserId;
            }
            var result = _db.Execute<EmailSettingDetail>(Procedures.Email_Setting_Detail_Insupd, emailSetting, true);
            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Fail to insert or update.");

            return await GetEmailSettingByCompIdService(emailSettingDetail.CompanyId);
        }

        private void EmailSettingsValidation(EmailSettingDetail emailSettingDetail)
        {
            if (emailSettingDetail.CompanyId == 0)
                throw new HiringBellException("Invalid company selected");

            if (string.IsNullOrEmpty(emailSettingDetail.EmailName))
                throw new HiringBellException("Email Name is null or empty");

            if (string.IsNullOrEmpty(emailSettingDetail.EmailAddress))
                throw new HiringBellException("Email Id is null or empty");

            if (string.IsNullOrEmpty(emailSettingDetail.EmailHost))
                throw new HiringBellException("Email Host is null or empty");

            if (string.IsNullOrEmpty(emailSettingDetail.Credentials))
                throw new HiringBellException("Email Credential is null or empty");

            if (emailSettingDetail.EnableSsl == null)
                throw new HiringBellException("Invalid SSL select");

            if (emailSettingDetail.UserDefaultCredentials == null)
                throw new HiringBellException("Invalid User Default Credentials option");

            if (emailSettingDetail.PortNo <= 0)
                throw new HiringBellException("Invalid port number");
        }

        public async Task<string> InsertUpdateEmailTemplateService(EmailTemplate emailTemplate, IFormFileCollection fileCollection)
        {
            var filepath = string.Empty;
            ValidateEmailTemplate(emailTemplate);
            string cs = await _gitHubConnector.FetchTypedConfiguraitonAsync<string>(_microserviceUrlLogs.DatabaseConfigurationUrl);
            _db.SetupConnectionString(cs);
            if (fileCollection.Count > 0)
            {
                var files = fileCollection.Select(x => new Files
                {
                    FileUid = emailTemplate.FileId,
                    FileName = fileCollection[0].Name,
                    FileExtension = string.Empty
                }).ToList<Files>();
                _fileService.SaveFileToLocation(_fileLocationDetail.LogoPath, files, fileCollection);

                filepath = files[0].FilePath;

            }
            var existingTemplate = _db.Get<EmailTemplate>(Procedures.Email_Template_Get, new { emailTemplate.EmailTemplateId });
            if (existingTemplate == null)
            {
                existingTemplate = emailTemplate;
                existingTemplate.BodyContent = JsonConvert.SerializeObject(emailTemplate.BodyContent);
                existingTemplate.AdminId = _currentSession.CurrentUserDetail.UserId;
            }
            else
            {
                existingTemplate.TemplateName = emailTemplate.TemplateName;
                existingTemplate.SubjectLine = emailTemplate.SubjectLine;
                existingTemplate.Salutation = emailTemplate.Salutation;
                existingTemplate.EmailClosingStatement = emailTemplate.EmailClosingStatement;
                existingTemplate.EmailNote = emailTemplate.EmailNote;
                existingTemplate.SignatureDetail = emailTemplate.SignatureDetail;
                existingTemplate.ContactNo = emailTemplate.ContactNo;
                existingTemplate.EmailTitle = emailTemplate.EmailTitle;
                existingTemplate.BodyContent = JsonConvert.SerializeObject(emailTemplate.BodyContent);
                existingTemplate.Description = emailTemplate.Description;
                existingTemplate.AdminId = _currentSession.CurrentUserDetail.UserId;
            }
            existingTemplate.FileId = emailTemplate.FileId;
            var tempId = _db.Execute<EmailTemplate>(Procedures.Email_Template_Insupd, existingTemplate, true);
            if (string.IsNullOrEmpty(tempId))
                throw new HiringBellException("Fail to insert or updfate");

            return tempId;
        }

        private void ValidateEmailTemplate(EmailTemplate emailTemplate)
        {
            if (emailTemplate.CompanyId <= 0)
                throw new HiringBellException("Invalid company selected");

            if (string.IsNullOrEmpty(emailTemplate.TemplateName))
                throw new HiringBellException("Template name is mandatory");

            if (string.IsNullOrEmpty(emailTemplate.EmailTitle))
                throw new HiringBellException("Tiltle is mandatory");

            if (string.IsNullOrEmpty(emailTemplate.SubjectLine))
                throw new HiringBellException("Subject is mandatory");

            if (string.IsNullOrEmpty(emailTemplate.Salutation))
                throw new HiringBellException("Salutation is mandatory");

            if (string.IsNullOrEmpty(emailTemplate.EmailClosingStatement))
                throw new HiringBellException("Email closing statement is mandatory");

            if (string.IsNullOrEmpty(emailTemplate.SignatureDetail))
                throw new HiringBellException("Signature is mandatory");
        }

        public async Task<List<EmailTemplate>> GetEmailTemplateService(FilterModel filterModel)
        {
            var masterDatabse = await _gitHubConnector.FetchTypedConfiguraitonAsync<string>(_microserviceUrlLogs.DatabaseConfigurationUrl);
            _db.SetupConnectionString(masterDatabse);
            var result = _db.GetList<EmailTemplate>(Procedures.Email_Template_Getby_Filter, new
            {
                filterModel.SearchString,
                filterModel.SortBy,
                filterModel.PageIndex,
                filterModel.PageSize
            });
            return result;
        }

        public async Task<dynamic> GetEmailTemplateByIdService(long EmailTemplateId, int CompanyId)
        {
            if (EmailTemplateId == 0)
                throw new HiringBellException("Invalid Email template selected");

            var companyFiles = await GetCompanyFiles(CompanyId);

            string cs = await _gitHubConnector.FetchTypedConfiguraitonAsync<string>(_microserviceUrlLogs.DatabaseConfigurationUrl);
            _db.SetupConnectionString(cs);
            var template = _db.Get<EmailTemplate>(Procedures.Email_Template_Get, new { EmailTemplateId });
            return new { EmailTemplate = template, Files = companyFiles };
        }

        private async Task<List<Files>> GetCompanyFiles(int companyId)
        {
            var companyFiles = await _companyService.GetCompanyFiles(companyId);
            return companyFiles;
        }

        public async Task<dynamic> EmailTempMappingInsertUpdateService(EmailMappedTemplate emailMappedTemplate)
        {
            if (emailMappedTemplate.CompanyId <= 0)
                throw new HiringBellException("Invalid company selected. Please select a valid company");

            if (emailMappedTemplate.TemplateId <= 0)
                throw new HiringBellException("Invalid email template selected. Please select a valid template");

            if (string.IsNullOrEmpty(emailMappedTemplate.EmailTemplateName))
                throw new HiringBellException("Email template name is null or empty. Please select a valid template name");

            var mappedTemplate = _db.Get<EmailMappedTemplate>(Procedures.Email_Mapped_Template_GetById, new { EmailTempMappingId = emailMappedTemplate.EmailTempMappingId });
            if (mappedTemplate == null)
                mappedTemplate = emailMappedTemplate;
            else
            {
                mappedTemplate.CompanyId = emailMappedTemplate.CompanyId;
                mappedTemplate.TemplateId = emailMappedTemplate.TemplateId;
                mappedTemplate.EmailTemplateName = emailMappedTemplate.EmailTemplateName;
            }
            mappedTemplate.AdminId = _currentSession.CurrentUserDetail.UserId;
            var result = _db.Execute<EmailMappedTemplate>(Procedures.Email_Mapped_Template_Insupd, mappedTemplate, true);
            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Fail to insert or update Email mapped template");

            FilterModel filterModel = new FilterModel
            {
                SearchString = "1=1 and CompanyId = " + _currentSession.CurrentUserDetail.CompanyId
            };

            return await GetEmailTempMappingService(filterModel);
        }

        public async Task<dynamic> GetEmailTempMappingService(FilterModel filterModel)
        {
            (List<EmailMappedTemplate> emailMappedTemplate, List<EmailTemplate> emailTemplate) = _db.GetList<EmailMappedTemplate, EmailTemplate>(Procedures.Email_Mapped_Template_By_Comid, new
            {
                filterModel.SearchString,
                filterModel.SortBy,
                filterModel.PageIndex,
                filterModel.PageSize
            });

            return await Task.FromResult(new { emailMappedTemplate, emailTemplate });
        }
    }
}
