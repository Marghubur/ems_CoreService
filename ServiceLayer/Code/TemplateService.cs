using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using Bt.Lib.PipelineConfig.MicroserviceHttpRequest;
using Bt.Lib.PipelineConfig.Model;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using EMailService.Modal;
using EMailService.Service;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;
using ModalLayer.Modal;
using Newtonsoft.Json;
using OpenXmlPowerTools;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ServiceLayer.Code
{
    public class TemplateService : ITemplateService
    {
        private readonly IDb _db;
        private readonly CurrentSession _currentSession;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly ICompanyService _companyService;
        private readonly IEMailManager _eMailManager;
        private readonly RequestMicroservice _requestMicroservice;
        private readonly MicroserviceRegistry _microserviceUrlLogs;
        private readonly IHttpClientFactory _httpClientFactory;
        public TemplateService(
            IDb db,
            CurrentSession currentSession,
            FileLocationDetail fileLocationDetail,
            ICompanyService companyService,
            IEMailManager eMailManager,
            RequestMicroservice requestMicroservice,
            MicroserviceRegistry microserviceUrlLogs,
            IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _currentSession = currentSession;
            _fileLocationDetail = fileLocationDetail;
            _companyService = companyService;
            _eMailManager = eMailManager;
            _requestMicroservice = requestMicroservice;
            _microserviceUrlLogs = microserviceUrlLogs;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<AnnexureOfferLetter> AnnexureOfferLetterInsertUpdateService(AnnexureOfferLetter annexureOfferLetter, int LetterType)
        {
            if (annexureOfferLetter.CompanyId <= 0)
                throw new HiringBellException("Invalid company selected. Please select a valid company");

            if (string.IsNullOrEmpty(annexureOfferLetter.TemplateName))
                throw new HiringBellException("Template name is null or empty");

            if (string.IsNullOrEmpty(annexureOfferLetter.BodyContent))
                throw new HiringBellException("Body content is null or empty");

            AnnexureOfferLetter letter = _db.Get<AnnexureOfferLetter>(Procedures.ANNEXURE_OFFER_LETTER_GETBY_ID, new
            {
                annexureOfferLetter.AnnexureOfferLetterId
            });

            if (letter == null)
            {
                letter = annexureOfferLetter;
            }
            else
            {
                letter.TemplateName = annexureOfferLetter.TemplateName;
                letter.FileId = annexureOfferLetter.FileId;
            }

            string fileName = annexureOfferLetter.TemplateName.Replace(" ", "");
            fileName = fileName.Length > 15 ? fileName.Substring(0, 15) + ".txt" : fileName + "" + ".txt";
            string oldFileName = !string.IsNullOrEmpty(letter.FilePath) ? Path.GetFileName(letter.FilePath) : string.Empty;
            Files file = await SaveDataAsTextFileService(fileName, annexureOfferLetter.BodyContent, oldFileName);

            letter.AdminId = _currentSession.CurrentUserDetail.UserId;
            letter.LetterType = LetterType;
            letter.FilePath = Path.Combine(file.FilePath, file.FileName);

            var result = _db.Execute<AnnexureOfferLetter>(Procedures.ANNEXURE_OFFER_LETTER_INSUPD, letter, true);
            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("fail to insert or update");

            annexureOfferLetter.AnnexureOfferLetterId = Convert.ToInt32(result);
            return annexureOfferLetter;
        }

        private async Task<Files> SaveDataAsTextFileService(string fileName, string data, string oldFileName)
        {
            string url = $"{_microserviceUrlLogs.SaveAsTextFile}";
            TextFileFolderDetail textFileFolderDetail = new TextFileFolderDetail
            {
                FolderPath = Path.Combine(_currentSession.CompanyCode, _fileLocationDetail.CompanyFiles, "annexure_offerletter"),
                OldFileName = oldFileName,
                ServiceName = LocalConstants.EmstumFileService,
                FileName = fileName,
                TextDetail = data
            };

            var microserviceRequest = MicroserviceRequest.Builder(url);
            microserviceRequest
            .SetPayload(textFileFolderDetail)
            .SetDbConfig(_requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString))
            .SetConnectionString(_currentSession.LocalConnectionString)
            .SetCompanyCode(_currentSession.CompanyCode)
            .SetToken(_currentSession.Authorization);

            return await _requestMicroservice.PostRequest<Files>(microserviceRequest);
        }

        private async Task<string> ReadTextFile(string filePath)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_microserviceUrlLogs.ResourceBaseUrl}{filePath}");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStringAsync();

            return null;
        }

        public EmailTemplate GetBillingTemplateDetailService()
        {
            var detail = _db.Get<EmailTemplate>("sp_email_template_get", new { EmailTemplateId = 1 });
            if (!string.IsNullOrEmpty(detail.BodyContent))
                detail.BodyContent = JsonConvert.DeserializeObject<string>(detail.BodyContent);

            return detail;
        }

        public async Task<AnnexureOfferLetter> GetOfficiaLetterService(int CompanyId, int LetterType)
        {
            var result = _db.Get<AnnexureOfferLetter>("sp_annexure_offer_letter_getby_lettertype", new { CompanyId, LetterType });
            if (result != null && !string.IsNullOrEmpty(result.FilePath))
            {
                result.BodyContent = await ReadTextFile(result.FilePath);
            }

            return await Task.FromResult(result);
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
            return new { EmailLinkConfig = emaillinkconfig, Files = companyFiles };
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

        public async Task<string> GenerateOfferLetterPDFService()
        {
            var result = _db.Get<AnnexureOfferLetter>("sp_annexure_offer_letter_getby_lettertype", new
            {
                CompanyId = _currentSession.CurrentUserDetail.CompanyId,
                LetterType = 3
            });
            if (result == null)
                throw HiringBellException.ThrowBadRequest("Official letter not found");

            //if (result != null && !string.IsNullOrEmpty(result.FilePath))
            //    result.BodyContent = await ReadTextFile(result.FilePath);

            HtmlToPdfConvertorModal htmlToPdfConvertorModal = new HtmlToPdfConvertorModal
            {
                HTML = await GetHTMLFile(result.FilePath),
                ServiceName = LocalConstants.EmstumFileService,
                FileName = "Salary_Increment" + $".{ApplicationConstants.Pdf}",
                FolderPath = Path.Combine(_currentSession.CompanyCode, _fileLocationDetail.User, "official_letter")
            };

            string url = $"{_microserviceUrlLogs.ConvertHtmlToPdf}";

            MicroserviceRequest microserviceRequest = new MicroserviceRequest
            {
                Url = url,
                CompanyCode = _currentSession.CompanyCode,
                Token = _currentSession.Authorization,
                Database = _requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString),
                Payload = JsonConvert.SerializeObject(htmlToPdfConvertorModal)
            };

            return await _requestMicroservice.PostRequest<string>(microserviceRequest);
        }

        private async Task<string> GetHTMLFile(string filePath)
        {
            var bodyContent = await ReadTextFile(filePath);

            return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    line-height: 1.6;
                    margin: 0;
                    padding: 20px;
                }}
                table {{
                    width: 100%;
                    border-collapse: collapse;
                    word-wrap: break-word;
                    table-layout: auto;
                }}
                table, th, td {{
                    border: 1px solid #ddd;
                    padding: 8px;
                }}
                img {{
                    max-width: 100%;
                    height: auto;
                }}
                @media print {{
                    @page {{
                        size: A4;
                        margin: 20mm;
                    }}
                }}
            </style>
        </head>
        <body>
            {bodyContent}
        </body>
        </html>";
        }

        public async Task<byte[]> GenerateOfferLetterByteArrayService()
        {
            var result = _db.Get<AnnexureOfferLetter>("sp_annexure_offer_letter_getby_lettertype", new
            {
                CompanyId = _currentSession.CurrentUserDetail.CompanyId,
                LetterType = 3
            });
            if (result == null)
                throw HiringBellException.ThrowBadRequest("Official letter not found");

            var html = await GetHTMLFile(result.FilePath);

            return await ConvertHtmlToPdf(html);
        }

        private async Task<byte[]> ConvertHtmlToPdf(string html)
        {
            // Download Chromium browser if not already installed
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            // Launch browser in headless mode
            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            });

            html = PreprocessTableHTML(html);

            // Open a new page and set viewport size
            using var page = await browser.NewPageAsync();
            await page.SetViewportAsync(new ViewPortOptions
            {
                Width = 794,
                Height = 1123
            });

            // Set content on the page
            await page.SetContentAsync(html);


            // Wait for a moment to ensure all rendering is complete
            await Task.Delay(1000);

            // Generate PDF with optimal settings
            var pdfBytes = await page.PdfDataAsync(new PdfOptions
            {
                Width = "210mm",
                Height = "297mm",
                //Format = PaperFormat.A4,
                PrintBackground = true,
                MarginOptions = new MarginOptions
                {
                    Top = "10mm",
                    Bottom = "10mm",
                    Left = "10mm",
                    Right = "10mm"
                },
                PreferCSSPageSize = true,
                Scale = 1M
            });

            return pdfBytes;
        }

        private string PreprocessTableHTML(string originalHtml)
        {
            var document = new HtmlAgilityPack.HtmlDocument();
            document.LoadHtml(originalHtml);

            // Process tables to preserve structure
            var tables = document.DocumentNode.SelectNodes("//table");
            if (tables != null)
            {
                foreach (var table in tables)
                {
                    // Preserve column proportions
                    ProcessTableColumns(table);

                    // Remove problematic attributes
                    RemoveProblematicAttributes(table);
                }
            }

            return document.DocumentNode.OuterHtml;
        }

        private void ProcessTableColumns(HtmlNode table)
        {
            var rows = table.SelectNodes(".//tr");
            if (rows == null) return;

            // Analyze column widths across rows
            var columnWidths = new List<double>();
            foreach (var row in rows)
            {
                var cells = row.SelectNodes(".//td|.//th");
                if (cells == null) continue;

                for (int i = 0; i < cells.Count; i++)
                {
                    var cell = cells[i];
                    var widthAttr = cell.GetAttributeValue("width", "");
                    var styleWidth = GetWidthFromStyle(cell.GetAttributeValue("style", ""));

                    double width = 0;
                    if (!string.IsNullOrEmpty(widthAttr) && double.TryParse(widthAttr.Replace("%", "").Replace("px", ""), out width))
                    {
                        // Ensure list has enough elements
                        while (columnWidths.Count <= i)
                        {
                            columnWidths.Add(0);
                        }
                        columnWidths[i] = Math.Max(columnWidths[i], width);
                    }
                    else if (styleWidth > 0)
                    {
                        while (columnWidths.Count <= i)
                        {
                            columnWidths.Add(0);
                        }
                        columnWidths[i] = Math.Max(columnWidths[i], styleWidth);
                    }
                }
            }

            // Reapply proportional widths
            foreach (var row in rows)
            {
                var cells = row.SelectNodes(".//td|.//th");
                if (cells == null) continue;

                for (int i = 0; i < cells.Count; i++)
                {
                    if (i < columnWidths.Count && columnWidths[i] > 0)
                    {
                        // Calculate percentage width
                        double totalWidth = columnWidths.Sum();
                        double percentWidth = (columnWidths[i] / totalWidth) * 100;

                        // Set percentage width
                        cells[i].SetAttributeValue("width", $"{Math.Round(percentWidth, 2)}%");
                        cells[i].SetAttributeValue("style",
                            $"width:{Math.Round(percentWidth, 2)}%; {cells[i].GetAttributeValue("style", "")}");
                    }
                }
            }
        }

        private void RemoveProblematicAttributes(HtmlNode table)
        {
            // Remove absolute positioning and fixed width attributes
            var elementsToClean = table.SelectNodes(".//*");
            if (elementsToClean != null)
            {
                foreach (var element in elementsToClean)
                {
                    // Remove absolute positioning styles
                    element.SetAttributeValue("style",
                        RemoveStyleProperties(
                            element.GetAttributeValue("style", ""),
                            new[] { "position", "left", "top", "right", "bottom", "absolute" }
                        )
                    );
                }
            }
        }

        private double GetWidthFromStyle(string style)
        {
            if (string.IsNullOrEmpty(style)) return 0;

            var widthMatch = System.Text.RegularExpressions.Regex.Match(
                style,
                @"width\s*:\s*(\d+)(%|px)?"
            );

            if (widthMatch.Success)
            {
                return double.Parse(widthMatch.Groups[1].Value);
            }

            return 0;
        }

        private string RemoveStyleProperties(string style, string[] propertiesToRemove)
        {
            if (string.IsNullOrEmpty(style)) return "";

            var styleProperties = style.Split(';')
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            styleProperties.RemoveAll(prop =>
                propertiesToRemove.Any(removeKey =>
                    prop.Trim().StartsWith(removeKey, StringComparison.OrdinalIgnoreCase)
                )
            );

            return string.Join("; ", styleProperties);
        }

    }
}
