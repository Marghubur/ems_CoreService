using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using EMailService.Service;
using Microsoft.AspNetCore.Hosting;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class TemplateService : ITemplateService
    {
        private readonly IDb _db;
        private readonly CurrentSession _currentSession;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ICompanyService _companyService;
        private readonly IEMailManager _eMailManager;

        public TemplateService(IDb db, CurrentSession currentSession, FileLocationDetail fileLocationDetail, IHostingEnvironment hostingEnvironment, ICompanyService companyService, IEMailManager eMailManager)
        {
            _db = db;
            _currentSession = currentSession;
            _fileLocationDetail = fileLocationDetail;
            _hostingEnvironment = hostingEnvironment;
            _companyService = companyService;
            _eMailManager = eMailManager;
        }

        public AnnexureOfferLetter AnnexureOfferLetterInsertUpdateService(AnnexureOfferLetter annexureOfferLetter, int LetterType)
        {
            if (annexureOfferLetter.CompanyId <= 0)
                throw new HiringBellException("Invalid company selected. Please select a valid company");

            if (string.IsNullOrEmpty(annexureOfferLetter.TemplateName))
                throw new HiringBellException("Template name is null or empty");

            if (string.IsNullOrEmpty(annexureOfferLetter.BodyContent))
                throw new HiringBellException("Body content is null or empty");

            AnnexureOfferLetter letter = _db.Get<AnnexureOfferLetter>("sp_annexure_offer_letter_getby_id", new { AnnexureOfferLetterId = annexureOfferLetter.AnnexureOfferLetterId });
            if (letter == null)
                letter = annexureOfferLetter;
            else
            {
                letter.TemplateName = annexureOfferLetter.TemplateName;
                letter.FileId = annexureOfferLetter.FileId;
            }

            var folderPath = Path.Combine(_fileLocationDetail.DocumentFolder, _fileLocationDetail.CompanyFiles, "Offer_Letter");
            if (!Directory.Exists(Path.Combine(_hostingEnvironment.ContentRootPath, folderPath)))
                Directory.CreateDirectory(Path.Combine(_hostingEnvironment.ContentRootPath, folderPath));
            string filename = annexureOfferLetter.TemplateName.Replace(" ", "") + ".txt";
            var filepath = Path.Combine(folderPath, filename);
            if (File.Exists(filepath))
                File.Delete(filepath);

            var txt = new StreamWriter(filepath);
            txt.Write(annexureOfferLetter.BodyContent);
            txt.Close();

            letter.AdminId = _currentSession.CurrentUserDetail.UserId;
            letter.LetterType = LetterType;
            letter.FilePath = filepath;
            var result = _db.Execute<AnnexureOfferLetter>("sp_annexure_offer_letter_insupd", letter, true);
            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("fail to insert or update");
            var letterId = Convert.ToInt32(result);
            annexureOfferLetter.AnnexureOfferLetterId = letterId;
            return annexureOfferLetter;
        }

        public AnnexureOfferLetter GetOfferLetterService(int CompanyId, int LetterType)
        {
            var result = _db.Get<AnnexureOfferLetter>("sp_annexure_offer_letter_getby_lettertype", new { CompanyId, LetterType });

            if (result != null)
            {
                if (File.Exists(result.FilePath))
                {
                    var txt = File.ReadAllText(result.FilePath);
                    result.BodyContent = txt;
                }
            }

            return result;
        }

        public EmailTemplate GetBillingTemplateDetailService()
        {
            var detail = _db.Get<EmailTemplate>("sp_email_template_get", new { EmailTemplateId = 1 });
            if (!string.IsNullOrEmpty(detail.BodyContent))
                detail.BodyContent = JsonConvert.DeserializeObject<string>(detail.BodyContent);

            return detail;
        }

        public List<AnnexureOfferLetter> GetAnnextureService(int CompanyId, int LetterType)
        {
            var result = _db.GetList<AnnexureOfferLetter>("sp_annexure_offer_letter_getby_lettertype", new { CompanyId, LetterType });
            if (result.Count > 0)
            {
                foreach (var item in result)
                {
                    if (item != null)
                    {
                        if (File.Exists(item.FilePath))
                        {
                            var txt = File.ReadAllText(item.FilePath);
                            item.BodyContent = txt;
                        }
                    }
                }
            }
            return result;
        }

        public string EmailLinkConfigInsUpdateService(EmailLinkConfig emailLinkConfig)
        {
            validateEmailLinkCOnfig(emailLinkConfig);
            EmailLinkConfig existEmailLinkConfig = _db.Get<EmailLinkConfig>("sp_email_template_get", new { emailLinkConfig.EmailTemplateId });
            if (existEmailLinkConfig == null)
            {
                existEmailLinkConfig = emailLinkConfig;
                existEmailLinkConfig.AdminId = _currentSession.CurrentUserDetail.UserId;
            }
            else
            {
                existEmailLinkConfig.TemplateName = emailLinkConfig.TemplateName;
                existEmailLinkConfig.SubjectLine = emailLinkConfig.SubjectLine;
                existEmailLinkConfig.Salutation = emailLinkConfig.Salutation;
                existEmailLinkConfig.EmailClosingStatement = emailLinkConfig.EmailClosingStatement;
                existEmailLinkConfig.EmailNote = emailLinkConfig.EmailNote;
                existEmailLinkConfig.SignatureDetail = emailLinkConfig.SignatureDetail;
                existEmailLinkConfig.ContactNo = emailLinkConfig.ContactNo;
                existEmailLinkConfig.PageName = emailLinkConfig.PageName;
                existEmailLinkConfig.PageDescription = emailLinkConfig.PageDescription;
                existEmailLinkConfig.IsEmailGroupUsed = emailLinkConfig.IsEmailGroupUsed;
                existEmailLinkConfig.IsTriggeredAutomatically = emailLinkConfig.IsTriggeredAutomatically;
                existEmailLinkConfig.EmailGroupId = emailLinkConfig.EmailGroupId;

                existEmailLinkConfig.AdminId = _currentSession.CurrentUserDetail.UserId;
            }
            existEmailLinkConfig.BodyContent = JsonConvert.SerializeObject(emailLinkConfig.BodyContent);
            existEmailLinkConfig.EmailsJson = JsonConvert.SerializeObject(emailLinkConfig.Emails);
            existEmailLinkConfig.FileId = emailLinkConfig.FileId;
            var tempId = _db.Execute<EmailLinkConfig>("sp_email_link_config_insupd", existEmailLinkConfig, true);
            if (string.IsNullOrEmpty(tempId))
                throw new HiringBellException("Fail to insert or updfate");

            return tempId;
        }

        private void validateEmailLinkCOnfig(EmailLinkConfig emailLinkConfig)
        {
            if (string.IsNullOrEmpty(emailLinkConfig.TemplateName))
                throw new HiringBellException("Template name is null or empty");

            if (string.IsNullOrEmpty(emailLinkConfig.PageName))
                throw new HiringBellException("Page name is null or empty");

            if (string.IsNullOrEmpty(emailLinkConfig.PageDescription))
                throw new HiringBellException("Page description is null or empty");

            if (string.IsNullOrEmpty(emailLinkConfig.SubjectLine))
                throw new HiringBellException("Subject is null or empty");

            if (string.IsNullOrEmpty(emailLinkConfig.EmailTitle))
                throw new HiringBellException("Title is null or empty");

            if (string.IsNullOrEmpty(emailLinkConfig.Salutation))
                throw new HiringBellException("Salutation is null or empty");

            if (string.IsNullOrEmpty(emailLinkConfig.BodyContent))
                throw new HiringBellException("Template body is null or empty");

            if (emailLinkConfig.IsEmailGroupUsed)
            {
                if (emailLinkConfig.EmailGroupId <= 0)
                    throw new HiringBellException("Email group is not selected");
            }

            if (emailLinkConfig.Emails.Count <= 0)
                throw new HiringBellException("Emails are null or empty");

            if (emailLinkConfig.Emails.Count > 0)
            {
                foreach (var item in emailLinkConfig.Emails)
                {
                    var mail = new MailAddress(item);
                    bool isValidEmail = mail.Host.Contains('.');
                    if (!isValidEmail)
                        throw new HiringBellException($"{item} is invalid email");
                }
            }
        }

        public async Task<dynamic> EmailLinkConfigGetByPageNameService(string PageName, int CompanyId)
        {
            if (string.IsNullOrEmpty(PageName))
                throw new HiringBellException("Invalid page selected");

            if (CompanyId <= 0)
                throw new HiringBellException("Invalid company selected");

            List<Files> companyFiles = await _companyService.GetCompanyFiles(CompanyId);
            var emaillinkconfig = _db.Get<EmailLinkConfig>("sp_email_link_config_getBy_pagename", new { CompanyId = CompanyId, PageName = PageName });
            return new { EmailLinkConfig = emaillinkconfig, Files = companyFiles};
        }

        public async Task<string> GenerateUpdatedPageMailService(EmailLinkConfig emailLinkConfig)
        {
            var template = _db.Get<EmailTemplate>("sp_email_template_get", new { EmailTemplateId = emailLinkConfig.EmailTemplateId });
            if (template == null)
                throw new HiringBellException("Fail to get Leave Request template. Please contact to admin.");

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
            string body = JsonConvert.DeserializeObject<string>(template.BodyContent);
            EmailSenderModal emailSenderModal = new EmailSenderModal
            {
                To = emailLinkConfig.Emails, //receiver.Email,
                CC = new List<string>(),
                BCC = new List<string>(),
                FileDetails = new List<FileDetail>(),
                Subject = template.SubjectLine,
                Body = string.Concat(body, builder),
                Title = template.EmailTitle
            };

            await _eMailManager.SendMailAsync(emailSenderModal);
            return await Task.FromResult("Email send successfuly");
        }
    }
}
