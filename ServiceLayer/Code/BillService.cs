using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using Bot.CoreBottomHalf.CommonModal.Enums;
using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using bt_lib_common_services.KafkaService.code;
using bt_lib_common_services.KafkaService.interfaces;
using bt_lib_common_services.MicroserviceHttpRequest;
using bt_lib_common_services.Model;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using DocMaker.ExcelMaker;
using DocMaker.HtmlToDocx;
using DocMaker.PdfService;
using EMailService.Modal;
using EMailService.Modal.Payroll;
using EMailService.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace ServiceLayer.Code
{
    public class BillService : IBillService
    {
        private readonly IDb db;
        private readonly IFileService fileService;
        private readonly IHTMLConverter iHTMLConverter;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly CurrentSession _currentSession;
        private readonly IFileMaker _fileMaker;
        private readonly ILogger<BillService> _logger;
        private readonly ExcelWriter _excelWriter;
        private readonly HtmlToPdfConverter _htmlToPdfConverter;
        private readonly IEMailManager _eMailManager;
        private readonly ITemplateService _templateService;
        private readonly ITimezoneConverter _timezoneConverter;
        private readonly IKafkaProducerService _kafkaProducerService;
        // private readonly IDeclarationService _declarationService;
        private readonly MasterDatabase _masterDatabase;
        private readonly MicroserviceRegistry _microserviceRegistry;
        private readonly RequestMicroservice _requestMicroservice;

        public BillService(IDb db, IFileService fileService, IHTMLConverter iHTMLConverter,
            FileLocationDetail fileLocationDetail,
            ILogger<BillService> logger,
            CurrentSession currentSession,
            ExcelWriter excelWriter,
            HtmlToPdfConverter htmlToPdfConverter,
            IEMailManager eMailManager,
            ITemplateService templateService,
            ITimezoneConverter timezoneConverter,
            IFileMaker fileMaker,
            IKafkaProducerService kafkaProducerService,
            // IDeclarationService declarationService,
            IOptions<MasterDatabase> options,
            RequestMicroservice requestMicroservice,
            IOptions<MicroserviceRegistry> microserviceoptions)
        {
            this.db = db;
            _logger = logger;
            _eMailManager = eMailManager;
            _htmlToPdfConverter = htmlToPdfConverter;
            this.fileService = fileService;
            this.iHTMLConverter = iHTMLConverter;
            _fileLocationDetail = fileLocationDetail;
            _currentSession = currentSession;
            _fileMaker = fileMaker;
            _excelWriter = excelWriter;
            _templateService = templateService;
            _timezoneConverter = timezoneConverter;
            _kafkaProducerService = kafkaProducerService;
            // _declarationService = declarationService;
            _masterDatabase = options.Value;
            _requestMicroservice = requestMicroservice;
            _microserviceRegistry = microserviceoptions.Value;
        }

        public FileDetail CreateFiles(BillGenerationModal billModal)
        {
            FileDetail fileDetail = new FileDetail();
            billModal.BillTemplatePath = Path.Combine(_fileLocationDetail.RootPath,
                _fileLocationDetail.Location,
                Path.Combine(_fileLocationDetail.HtmlTemplatePath),
                _fileLocationDetail.StaffingBillTemplate
            );

            billModal.PdfTemplatePath = Path.Combine(_fileLocationDetail.RootPath,
                _fileLocationDetail.Location,
                Path.Combine(_fileLocationDetail.HtmlTemplatePath),
                _fileLocationDetail.StaffingBillPdfTemplate
            );

            billModal.HeaderLogoPath = Path.Combine(_fileLocationDetail.RootPath, _fileLocationDetail.LogoPath, "logo.png");
            if (File.Exists(billModal.BillTemplatePath) && File.Exists(billModal.HeaderLogoPath))
            {
                fileDetail.LogoPath = billModal.HeaderLogoPath;
                string html = string.Empty;

                fileDetail.DiskFilePath = Path.Combine(_fileLocationDetail.RootPath, billModal.PdfModal.FilePath);
                if (!Directory.Exists(fileDetail.DiskFilePath)) Directory.CreateDirectory(fileDetail.DiskFilePath);
                fileDetail.FileName = billModal.PdfModal.FileName;
                string destinationFilePath = Path.Combine(fileDetail.DiskFilePath, fileDetail.FileName + $".{ApplicationConstants.Docx}");

                html = this.GetHtmlString(billModal.BillTemplatePath, billModal);
                this.iHTMLConverter.ToDocx(html, destinationFilePath, billModal.HeaderLogoPath);

                _fileMaker._fileDetail = fileDetail;
                destinationFilePath = Path.Combine(fileDetail.DiskFilePath, fileDetail.FileName + $".{ApplicationConstants.Pdf}");

                html = this.GetHtmlString(billModal.PdfTemplatePath, billModal, true);
                _htmlToPdfConverter.ConvertToPdf(html, destinationFilePath);
            }

            return fileDetail;
        }

        private string GetHtmlString(string templatePath, BillGenerationModal billModal, bool isHeaderLogoRequired = false)
        {
            string html = string.Empty;
            using (FileStream stream = File.Open(templatePath, FileMode.Open))
            {
                StreamReader reader = new StreamReader(stream);
                html = reader.ReadToEnd();

                html = html.Replace("[[BILLNO]]", billModal.PdfModal.billNo).
                Replace("[[dateOfBilling]]", billModal.PdfModal.dateOfBilling.ToString("dd MMM, yyyy")).
                Replace("[[senderFirstAddress]]", billModal.Sender.FirstAddress).
                Replace("[[senderCompanyName]]", billModal.Sender.CompanyName).
                Replace("[[senderGSTNo]]", billModal.Sender.GSTNo).
                Replace("[[senderSecondAddress]]", billModal.Sender.SecondAddress).
                Replace("[[senderPrimaryContactNo]]", billModal.Sender.PrimaryPhoneNo).
                Replace("[[senderEmail]]", billModal.Sender.Email).
                Replace("[[receiverCompanyName]]", billModal.Receiver.ClientName).
                Replace("[[receiverGSTNo]]", billModal.Receiver.GSTNo).
                Replace("[[receiverFirstAddress]]", billModal.Receiver.FirstAddress).
                Replace("[[receiverSecondAddress]]", billModal.Receiver.SecondAddress).
                Replace("[[receiverPrimaryContactNo]]", billModal.Receiver.PrimaryPhoneNo).
                Replace("[[receiverEmail]]", billModal.Receiver.Email).
                Replace("[[developerName]]", billModal.PdfModal.developerName).
                Replace("[[billingMonth]]", billModal.PdfModal.billingMonth.ToString("MMMM")).
                Replace("[[packageAmount]]", billModal.PdfModal.packageAmount.ToString()).
                Replace("[[cGST]]", billModal.PdfModal.cGST.ToString()).
                Replace("[[cGSTAmount]]", billModal.PdfModal.cGstAmount.ToString()).
                Replace("[[sGST]]", billModal.PdfModal.sGST.ToString()).
                Replace("[[sGSTAmount]]", billModal.PdfModal.sGstAmount.ToString()).
                Replace("[[iGST]]", billModal.PdfModal.iGST.ToString()).
                Replace("[[iGSTAmount]]", billModal.PdfModal.iGstAmount.ToString()).
                Replace("[[grandTotalAmount]]", billModal.PdfModal.grandTotalAmount.ToString()).
                Replace("[[state]]", billModal.Sender.State).
                Replace("[[clientName]]", billModal.Sender.CompanyName).
                Replace("[[city]]", billModal.SenderBankDetail.Branch).
                Replace("[[bankName]]", billModal.SenderBankDetail.BankName).
                Replace("[[accountNumber]]", billModal.SenderBankDetail.AccountNo).
                Replace("[[iFSCCode]]", billModal.SenderBankDetail.IFSC);
            }

            if (!string.IsNullOrEmpty(billModal.HeaderLogoPath) && isHeaderLogoRequired)
            {
                string extension = string.Empty;
                int lastPosition = billModal.HeaderLogoPath.LastIndexOf(".");
                extension = billModal.HeaderLogoPath.Substring(lastPosition + 1);
                ImageFormat imageFormat = null;
                if (extension == "png")
                    imageFormat = ImageFormat.Png;
                else if (extension == "gif")
                    imageFormat = ImageFormat.Gif;
                else if (extension == "bmp")
                    imageFormat = ImageFormat.Bmp;
                else if (extension == "jpeg")
                    imageFormat = ImageFormat.Jpeg;
                else if (extension == "tiff")
                {
                    // Convert tiff to gif.
                    extension = "gif";
                    imageFormat = ImageFormat.Gif;
                }
                else if (extension == "x-wmf")
                {
                    extension = "wmf";
                    imageFormat = ImageFormat.Wmf;
                }

                string encodeStart = $@"data:image/{imageFormat.ToString().ToLower()};base64";
                var fs = new FileStream(billModal.HeaderLogoPath, FileMode.Open);
                using (BinaryReader br = new BinaryReader(fs))
                {
                    Byte[] bytes = br.ReadBytes((Int32)fs.Length);
                    string base64String = Convert.ToBase64String(bytes, 0, bytes.Length);
                    html = html.Replace("[[COMPANYLOGO_PATH]]", $"{encodeStart}, {base64String}");
                }
            }
            return html;
        }

        private void FillSenderDetail(Organization organization, PdfModal pdfModal)
        {
            if (string.IsNullOrEmpty(organization.CompanyName))
                throw HiringBellException.ThrowBadRequest("Sender company name is null or empty");

            if (string.IsNullOrEmpty(organization.GSTNo))
                throw HiringBellException.ThrowBadRequest("Sender company gstn number is null or empty");

            if (string.IsNullOrEmpty(organization.FirstAddress))
                throw HiringBellException.ThrowBadRequest("Sender company first address is null or empty");

            if (string.IsNullOrEmpty(organization.SecondAddress))
                throw HiringBellException.ThrowBadRequest("Sender company second address is null or empty");

            if (string.IsNullOrEmpty(organization.PrimaryPhoneNo))
                throw HiringBellException.ThrowBadRequest("Sender company primary phone number is null or empty");

            pdfModal.senderCompanyName = organization.CompanyName;
            pdfModal.senderGSTNo = organization.GSTNo;
            pdfModal.senderEmail = organization.Email;
            pdfModal.senderFirstAddress = organization.FirstAddress;
            pdfModal.senderPrimaryContactNo = organization.PrimaryPhoneNo;
            pdfModal.senderSecondAddress = organization.SecondAddress;
        }

        private void FillReceiverDetail(Organization organization, PdfModal pdfModal)
        {
            if (string.IsNullOrEmpty(organization.ClientName))
                throw HiringBellException.ThrowBadRequest("CLient name is null or empty");

            if (string.IsNullOrEmpty(organization.GSTNo))
                throw HiringBellException.ThrowBadRequest("CLient gstn number is null or empty");

            if (string.IsNullOrEmpty(organization.FirstAddress))
                throw HiringBellException.ThrowBadRequest("CLient first address is null or empty");

            if (string.IsNullOrEmpty(organization.SecondAddress))
                throw HiringBellException.ThrowBadRequest("CLient second address is null or empty");

            if (string.IsNullOrEmpty(organization.PrimaryPhoneNo))
                throw HiringBellException.ThrowBadRequest("CLient primary phone number is null or empty");

            pdfModal.receiverCompanyId = organization.ClientId;
            pdfModal.receiverCompanyName = organization.ClientName;
            pdfModal.receiverEmail = organization.Email;
            pdfModal.receiverFirstAddress = organization.FirstAddress;
            pdfModal.receiverPrimaryContactNo = organization.PrimaryPhoneNo;
            pdfModal.receiverSecondAddress = organization.SecondAddress;
            pdfModal.receiverThirdAddress = organization.ThirdAddress;
            pdfModal.receiverGSTNo = organization.GSTNo;
        }

        public async Task<dynamic> UpdateGeneratedBillService(BillGenerationModal billModal)
        {
            if (billModal.TimesheetDetail == null)
                throw new HiringBellException("Invalid timesheet submitted. Please check you detail.");

            return await GenerateBillService(billModal);
        }

        private void ValidateRequestDetail(PdfModal pdfModal)
        {
            decimal grandTotalAmount = 0;
            decimal sGSTAmount = 0;
            decimal cGSTAmount = 0;
            decimal iGSTAmount = 0;
            int days = DateTime.DaysInMonth(pdfModal.billingMonth.Year, pdfModal.billingMonth.Month);

            if (pdfModal.ClientId <= 0)
                throw new HiringBellException { UserMessage = "Invald Client.", FieldName = nameof(pdfModal.ClientId), FieldValue = pdfModal.ClientId.ToString() };

            if (pdfModal.EmployeeId <= 0)
                throw new HiringBellException { UserMessage = "Invalid Employee", FieldName = nameof(pdfModal.EmployeeId), FieldValue = pdfModal.EmployeeId.ToString() };

            if (pdfModal.senderId <= 0)
                throw new HiringBellException { UserMessage = "Invalid Sender", FieldName = nameof(pdfModal.senderId), FieldValue = pdfModal.senderId.ToString() };

            if (pdfModal.packageAmount <= 0)
                throw new HiringBellException { UserMessage = "Invalid Package Amount", FieldName = nameof(pdfModal.packageAmount), FieldValue = pdfModal.packageAmount.ToString() };

            if (pdfModal.cGST < 0)
                throw new HiringBellException { UserMessage = "Invalid CGST", FieldName = nameof(pdfModal.cGST), FieldValue = pdfModal.cGST.ToString() };

            if (pdfModal.iGST < 0)
                throw new HiringBellException { UserMessage = "Invalid IGST", FieldName = nameof(pdfModal.iGST), FieldValue = pdfModal.iGST.ToString() };

            if (pdfModal.sGST < 0)
                throw new HiringBellException { UserMessage = "Invalid SGST", FieldName = nameof(pdfModal.sGST), FieldValue = pdfModal.sGST.ToString() };

            if (pdfModal.cGstAmount < 0)
                throw new HiringBellException { UserMessage = "Invalid CGST Amount", FieldName = nameof(pdfModal.cGstAmount), FieldValue = pdfModal.cGstAmount.ToString() };

            if (pdfModal.iGstAmount < 0)
                throw new HiringBellException { UserMessage = "Invalid IGST Amount", FieldName = nameof(pdfModal.iGstAmount), FieldValue = pdfModal.iGstAmount.ToString() };

            if (pdfModal.sGstAmount < 0)
                throw new HiringBellException { UserMessage = "Invalid CGST Amount", FieldName = nameof(pdfModal.cGstAmount), FieldValue = pdfModal.cGstAmount.ToString() };

            if (pdfModal.cGST > 0 || pdfModal.sGST > 0 || pdfModal.iGST > 0)
            {
                sGSTAmount = Converter.TwoDecimalValue((pdfModal.packageAmount * pdfModal.sGST) / 100);
                cGSTAmount = Converter.TwoDecimalValue((pdfModal.packageAmount * pdfModal.cGST) / 100);
                iGSTAmount = Converter.TwoDecimalValue((pdfModal.packageAmount * pdfModal.iGST) / 100);
                grandTotalAmount = Converter.TwoDecimalValue(pdfModal.packageAmount + (cGSTAmount + sGSTAmount + iGSTAmount));
            }
            else
            {
                grandTotalAmount = pdfModal.packageAmount;
            }

            if (pdfModal.grandTotalAmount != grandTotalAmount)
                throw new HiringBellException { UserMessage = "Total Amount calculation is not matching", FieldName = nameof(pdfModal.grandTotalAmount), FieldValue = pdfModal.grandTotalAmount.ToString() };

            if (pdfModal.sGstAmount != sGSTAmount)
                throw new HiringBellException { UserMessage = "SGST Amount invalid calculation", FieldName = nameof(pdfModal.sGstAmount), FieldValue = pdfModal.sGstAmount.ToString() };

            if (pdfModal.iGstAmount != iGSTAmount)
                throw new HiringBellException { UserMessage = "IGST Amount invalid calculation", FieldName = nameof(pdfModal.iGstAmount), FieldValue = pdfModal.iGstAmount.ToString() };

            if (pdfModal.cGstAmount != cGSTAmount)
                throw new HiringBellException { UserMessage = "CGST Amount invalid calculation", FieldName = nameof(pdfModal.cGstAmount), FieldValue = pdfModal.cGstAmount.ToString() };

            if (!pdfModal.IsCustomBill && (pdfModal.workingDay < 0 || pdfModal.workingDay > days))
                throw new HiringBellException { UserMessage = "Invalid Working days", FieldName = nameof(pdfModal.workingDay), FieldValue = pdfModal.workingDay.ToString() };

            if (pdfModal.billingMonth.Month < 0 || pdfModal.billingMonth.Month > 12)
                throw new HiringBellException { UserMessage = "Invalid billing month", FieldName = nameof(pdfModal.billingMonth), FieldValue = pdfModal.billingMonth.ToString() };

            if (pdfModal.daysAbsent < 0 || pdfModal.daysAbsent > days)
                throw new HiringBellException { UserMessage = "Invalid No of days absent", FieldName = nameof(pdfModal.daysAbsent), FieldValue = pdfModal.daysAbsent.ToString() };

            if (pdfModal.dateOfBilling == null)
            {
                throw new HiringBellException { UserMessage = "Invalid date of Billing", FieldName = nameof(pdfModal.dateOfBilling), FieldValue = pdfModal.dateOfBilling.ToString() };
            }
        }


        private async Task GetBillNumber(PdfModal pdfModal, BillGenerationModal billGenerationModal)
        {
            Bills bill = Converter.ToType<Bills>(billGenerationModal.ResultSet.Tables[4]);
            if (bill == null || string.IsNullOrEmpty(bill.GeneratedBillNo))
                throw new HiringBellException("Bill sequence number not found. Please contact to admin.");

            billGenerationModal.BillSequence = bill;
            if (string.IsNullOrEmpty(pdfModal.billNo))
            {
                pdfModal.billNo = bill.GeneratedBillNo.Replace("#", "");
            }
            else
            {
                pdfModal.billNo = pdfModal.billNo.Replace("#", "");
                string GeneratedBillNo = "";
                int len = pdfModal.billNo.Length;
                int i = 0;
                while (i < bill.BillNoLength)
                {
                    if (i < len)
                    {
                        GeneratedBillNo += pdfModal.billNo[i];
                    }
                    else
                    {
                        GeneratedBillNo = '0' + GeneratedBillNo;
                    }
                    i++;
                }
                pdfModal.billNo = GeneratedBillNo;
            }

            await Task.CompletedTask;
        }

        private async Task<DataSet> PrepareRequestForBillGeneration(BillGenerationModal billGenerationModal)
        {
            billGenerationModal.TimesheetDetail.TimesheetStartDate = new DateTime(billGenerationModal.PdfModal.billYear, billGenerationModal.PdfModal.billingMonth.Month, 1);
            billGenerationModal.TimesheetDetail.TimesheetEndDate = billGenerationModal.TimesheetDetail.TimesheetStartDate.AddMonths(1).AddDays(-1);
            DataSet ds = this.db.FetchDataSet(Procedures.Billing_Detail, new
            {
                Sender = billGenerationModal.PdfModal.senderId,
                Receiver = billGenerationModal.PdfModal.receiverCompanyId,
                BillNo = billGenerationModal.PdfModal.billNo,
                billGenerationModal.PdfModal.EmployeeId,
                UserTypeId = (int)UserType.Employee,
                StartDate = billGenerationModal.TimesheetDetail.TimesheetStartDate,
                EndDate = billGenerationModal.TimesheetDetail.TimesheetEndDate,
                BillTypeUid = 1,
                CompanyId = 1,
                FileRole = ApplicationConstants.CompanyPrimaryLogo
            });

            if (ds == null || ds.Tables.Count != 7)
                throw new HiringBellException("Fail to get billing detail. Please contact to admin.");

            billGenerationModal.ResultSet = ds;
            await GetBillNumber(billGenerationModal.PdfModal, billGenerationModal);

            await BuildBackAccountDetail(billGenerationModal);

            return ds;
        }

        private async Task BuildBackAccountDetail(BillGenerationModal billModal)
        {
            if (billModal.ResultSet.Tables[5] == null || billModal.ResultSet.Tables[5].Rows.Count != 1)
                throw HiringBellException.ThrowBadRequest("Unable to find sender back account detail.");

            billModal.SenderBankDetail = Converter.ToType<BankDetail>(billModal.ResultSet.Tables[5]);

            await Task.CompletedTask;
        }

        private async Task<Organization> GetSenderDetail(DataSet ds, List<string> emails)
        {
            if (ds.Tables[0].Rows.Count != 1)
                throw new HiringBellException("Fail to get company detail. Please contact to admin.");

            Organization sender = Converter.ToType<Organization>(ds.Tables[0]);
            if (sender == null)
                throw new HiringBellException("Fail to get company detail. Please contact to admin.");

            emails.Add(sender.Email);
            return await Task.FromResult(sender);
        }

        private async Task<Organization> GetReceiverDetail(DataSet ds, List<string> emails)
        {
            if (ds.Tables[1].Rows.Count != 1)
                throw new HiringBellException("Fail to get client detail. Please contact to admin.");

            Organization receiver = Converter.ToType<Organization>(ds.Tables[1]);
            if (receiver == null)
                throw new HiringBellException("Fail to get client detail. Please contact to admin.");

            emails.Add(receiver.Email);
            if (!string.IsNullOrEmpty(receiver.OtherEmail_1))
                emails.Add(receiver.OtherEmail_1);
            if (!string.IsNullOrEmpty(receiver.OtherEmail_2))
                emails.Add(receiver.OtherEmail_2);

            return await Task.FromResult(receiver);
        }

        private async Task<FileDetail> GetBillFileDetail(PdfModal pdfModal, DataSet ds)
        {
            FileDetail fileDetail = Converter.ToType<FileDetail>(ds.Tables[2]);
            if (fileDetail == null)
            {
                fileDetail = new FileDetail
                {
                    ClientId = pdfModal.ClientId
                };
            }

            return await Task.FromResult(fileDetail);
        }

        private async Task CaptureFileFolderLocations(BillGenerationModal billModal)
        {
            billModal.BillTemplatePath = Path.Combine(_fileLocationDetail.RootPath,
                        _fileLocationDetail.Location,
                        Path.Combine(_fileLocationDetail.HtmlTemplatePath),
                        _fileLocationDetail.StaffingBillTemplate
                    );

            if (!File.Exists(billModal.BillTemplatePath))
                throw new HiringBellException("Billing template not found. Please contact to admin.");

            billModal.PdfTemplatePath = Path.Combine(_fileLocationDetail.RootPath,
                _fileLocationDetail.Location,
                Path.Combine(_fileLocationDetail.HtmlTemplatePath),
                _fileLocationDetail.StaffingBillPdfTemplate
            );

            if (!File.Exists(billModal.PdfTemplatePath))
                throw new HiringBellException("PDF template not found. Please contact to admin.");

            await Task.CompletedTask;
        }

        private async Task GenerateUpdateTimesheet(BillGenerationModal billModal)
        {

            List<DailyTimesheetDetail> currentMonthTimesheet = new List<DailyTimesheetDetail>();
            if (billModal.ResultSet.Tables[3].Rows.Count > 0)
            {
                var timesheetDetail = Converter.ToList<TimesheetDetail>(billModal.ResultSet.Tables[3]);
                timesheetDetail.ForEach(x =>
                {
                    currentMonthTimesheet.AddRange(JsonConvert.DeserializeObject<List<DailyTimesheetDetail>>(x.TimesheetWeeklyJson));
                });
                var startDate = _timezoneConverter.ToSpecificTimezoneDateTime(_currentSession.TimeZone, billModal.TimesheetDetail.TimesheetStartDate);
                var endDate = _timezoneConverter.ToSpecificTimezoneDateTime(_currentSession.TimeZone, billModal.TimesheetDetail.TimesheetEndDate);

                currentMonthTimesheet = currentMonthTimesheet.Where(x => _timezoneConverter.ToSpecificTimezoneDateTime(_currentSession.TimeZone, x.PresentDate) >= startDate
                                                                  && _timezoneConverter.ToSpecificTimezoneDateTime(_currentSession.TimeZone, x.PresentDate) <= endDate).ToList();
            }

            string FolderLocation = Path.Combine(
                _fileLocationDetail.Location,
                _fileLocationDetail.BillsPath,
                billModal.PdfModal.billingMonth.ToString("MMM_yyyy"));

            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), FolderLocation);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string destinationFilePath = Path.Combine(
                folderPath,
                billModal.PdfModal.developerName.Replace(" ", "_") + "_" +
                billModal.PdfModal.billingMonth.ToString("MMM_yyyy") + "_" +
                billModal.PdfModal.billNo + "_" +
                billModal.PdfModal.UpdateSeqNo + $".{ApplicationConstants.Excel}");

            if (File.Exists(destinationFilePath))
                File.Delete(destinationFilePath);

            var timesheetData = (from n in currentMonthTimesheet
                                 orderby n.PresentDate ascending
                                 select new TimesheetModel
                                 {
                                     Date = n.PresentDate.ToString("dd MMM yyyy"),
                                     ResourceName = billModal.PdfModal.developerName,
                                     StartTime = "10:00 AM",
                                     EndTime = "06:00 PM",
                                     TotalHrs = 9,
                                     Comments = n.UserComments,
                                     Status = ItemStatus.Approved.ToString()
                                 }
            ).ToList<TimesheetModel>();

            // UpdateTimesheet
            //_timesheetService.UpdateTimesheetService(billModal.FullTimeSheet,
            //    billModal.TimesheetDetail, billModal.Comment);

            var timeSheetDataSet = Converter.ToDataSet<TimesheetModel>(timesheetData);
            _excelWriter.ToExcel(timeSheetDataSet.Tables[0],
                destinationFilePath,
                billModal.PdfModal.billingMonth.ToString("MMM_yyyy"));

            await Task.CompletedTask;
        }

        private async Task GeneratePdfFile(BillGenerationModal billModal)
        {
            GetFileDetail(billModal.PdfModal, billModal.FileDetail, ApplicationConstants.Pdf);
            _fileMaker._fileDetail = billModal.FileDetail;

            // Converting html context for pdf conversion.
            var html = this.GetHtmlString(billModal.PdfTemplatePath, billModal, true);

            var destinationFilePath = Path.Combine(billModal.FileDetail.DiskFilePath,
                billModal.FileDetail.FileName + $".{ApplicationConstants.Pdf}");
            _htmlToPdfConverter.ConvertToPdf(html, destinationFilePath);

            await Task.CompletedTask;
        }

        private async Task GenerateDocxFile(BillGenerationModal billModal)
        {
            GetFileDetail(billModal.PdfModal, billModal.FileDetail, ApplicationConstants.Docx);
            billModal.FileDetail.LogoPath = billModal.HeaderLogoPath;

            // Converting html context for docx conversion.
            string html = this.GetHtmlString(billModal.BillTemplatePath, billModal);

            var destinationFilePath = Path.Combine(billModal.FileDetail.DiskFilePath,
                billModal.FileDetail.FileName + $".{ApplicationConstants.Docx}");
            this.iHTMLConverter.ToDocx(html, destinationFilePath, billModal.HeaderLogoPath);

            await Task.CompletedTask;
        }

        private async Task SaveExecuteBill(BillGenerationModal billModal)
        {
            var dbResult = await this.db.ExecuteAsync(Procedures.Filedetail_Insupd, new
            {
                FileId = billModal.FileDetail.FileId,
                ClientId = billModal.PdfModal.receiverCompanyId,
                FileName = billModal.FileDetail.FileName,
                FilePath = billModal.FileDetail.FilePath,
                FileExtension = billModal.FileDetail.FileExtension,
                StatusId = billModal.PdfModal.StatusId,
                GeneratedBillNo = billModal.BillSequence.NextBillNo,
                BillUid = billModal.BillSequence.BillUid,
                BillDetailId = billModal.PdfModal.billId,
                BillNo = billModal.PdfModal.billNo,
                PaidAmount = billModal.PdfModal.packageAmount,
                BillForMonth = billModal.PdfModal.billingMonth.Month,
                BillYear = billModal.PdfModal.billYear,
                NoOfDays = billModal.PdfModal.workingDay,
                NoOfDaysAbsent = billModal.PdfModal.daysAbsent,
                IGST = billModal.PdfModal.iGST,
                SGST = billModal.PdfModal.sGST,
                CGST = billModal.PdfModal.cGST,
                TDS = ApplicationConstants.TDS,
                BillStatusId = ApplicationConstants.Pending,
                PaidOn = billModal.PdfModal.PaidOn,
                FileDetailId = billModal.PdfModal.FileId,
                UpdateSeqNo = billModal.PdfModal.UpdateSeqNo,
                EmployeeUid = billModal.PdfModal.EmployeeId,
                BillUpdatedOn = billModal.PdfModal.dateOfBilling,
                IsCustomBill = billModal.PdfModal.IsCustomBill,
                UserTypeId = (int)UserType.Employee,
                AdminId = _currentSession.CurrentUserDetail.UserId
            }, true);

            if (dbResult.rowsEffected != 0 || string.IsNullOrEmpty(dbResult.statusMessage))
            {
                List<Files> files = new List<Files>();
                files.Add(new Files
                {
                    FilePath = billModal.FileDetail.FilePath,
                    FileName = billModal.FileDetail.FileName
                });
                this.fileService.DeleteFiles(files);

                throw new HiringBellException("Bill generated task fail. Please contact to admin.");
            }

            billModal.FileDetail.FileId = Convert.ToInt32(dbResult.statusMessage);
            billModal.FileDetail.DiskFilePath = null;
        }

        public async Task<dynamic> GenerateBillService(BillGenerationModal billModal)
        {
            List<string> emails = new List<string>();
            try
            {
                billModal.PdfModal.billingMonth = _timezoneConverter.ToTimeZoneDateTime(
                    billModal.PdfModal.billingMonth,
                    _currentSession.TimeZone
                    );
                billModal.PdfModal.dateOfBilling = _timezoneConverter.ToTimeZoneDateTime(
                    billModal.PdfModal.dateOfBilling,
                    _currentSession.TimeZone
                    );


                // validate all the fields and request data.
                ValidateRequestDetail(billModal.PdfModal);

                // fetch and all the nessary data from database required to bill generation.
                var resultSet = await PrepareRequestForBillGeneration(billModal);

                Organization sender = await GetSenderDetail(resultSet, emails);
                billModal.Sender = sender;
                this.FillSenderDetail(sender, billModal.PdfModal);

                Organization receiver = await GetReceiverDetail(resultSet, emails);
                billModal.Receiver = receiver;
                this.FillReceiverDetail(receiver, billModal.PdfModal);

                FileDetail fileDetail = await GetBillFileDetail(billModal.PdfModal, resultSet);
                billModal.PdfModal.UpdateSeqNo++;
                fileDetail.FileExtension = string.Empty;
                billModal.FileDetail = fileDetail;

                // store template logo and file locations
                GetCompanyLogo(billModal, resultSet);
                await CaptureFileFolderLocations(billModal);
                this.CleanOldFiles(fileDetail);

                // execute and build timesheet if missing
                await GenerateUpdateTimesheet(billModal);

                // generate pdf, docx and excel files
                await GeneratePdfFile(billModal);

                // execute billing data and store into database.
                await GenerateDocxFile(billModal);

                // save file in filesystem
                await SaveExecuteBill(billModal);

                // get email notification detail
                EmailTemplate emailTemplate = GetEmailTemplateService();
                emailTemplate.Emails = emails;

                // return result data
                return await Task.FromResult(new { FileDetail = fileDetail, EmailTemplate = emailTemplate });
            }
            catch (HiringBellException e)
            {
                _logger.LogError($"{e.UserMessage} Field: {e.FieldName} Value: {e.FieldValue}");
                throw e.BuildBadRequest(e.UserMessage, e.FieldName, e.FieldValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw new HiringBellException(ex.Message, ex);
            }
        }

        private void GetCompanyLogo(BillGenerationModal billModal, DataSet resultSet)
        {
            var file = Converter.ToType<Files>(resultSet.Tables[6]);
            if (file != null)
            {
                billModal.HeaderLogoPath = Path.Combine(
                    _fileLocationDetail.RootPath,
                    file.FilePath,
                    file.FileName
                );
                if (!File.Exists(billModal.HeaderLogoPath))
                    billModal.HeaderLogoPath = "https://www.emstum.com/assets/images/logo.png";

            }
            else
            {
                billModal.HeaderLogoPath = "https://www.emstum.com/assets/images/logo.png";
            }
        }

        public async Task<dynamic> GetBillDetailWithTemplateService(string billNo, long employeeId)
        {
            var Result = db.FetchDataSet("sp_billdetail_and_template_by_billno", new
            {
                BillNo = billNo,
                EmployeeId = employeeId
            });

            if (Result == null || Result.Tables.Count != 3)
                throw new HiringBellException("Fail to get bill and template detail.");

            if (Result.Tables[0].Rows.Count != 1)
                throw new HiringBellException("Invalid bill no. No record found.");

            return await Task.FromResult(new
            {
                EmployeeDetail = Result.Tables[0],
                EmailTemplate = GetEmailTemplateService(),
                Receiver = Result.Tables[1],
                Sender = Result.Tables[2]
            });
        }

        private EmailTemplate GetEmailTemplateService()
        {
            string cs = $"server={_masterDatabase.Server};port={_masterDatabase.Port};database={_masterDatabase.Database};User Id={_masterDatabase.User_Id};password={_masterDatabase.Password};Connection Timeout={_masterDatabase.Connection_Timeout};Connection Lifetime={_masterDatabase.Connection_Lifetime};Min Pool Size={_masterDatabase.Min_Pool_Size};Max Pool Size={_masterDatabase.Max_Pool_Size};Pooling={_masterDatabase.Pooling};";
            db.SetupConnectionString(cs);
            var result = db.Get<EmailTemplate>(Procedures.Email_Template_Get, new
            {
                EmailTemplateId = ApplicationConstants.BillingEmailTemplate,
            });

            if (result == null)
                throw new HiringBellException("Invalid request. No email template found for this operation.");

            return result;
        }

        public dynamic GenerateDocument(PdfModal pdfModal, List<DailyTimesheetDetail> dailyTimesheetDetails,
            TimesheetDetail timesheetDetail, string Comment)
        {
            List<string> emails = new List<string>();
            FileDetail fileDetail = new FileDetail();
            try
            {
                pdfModal.billingMonth = TimeZoneInfo.ConvertTimeFromUtc(pdfModal.billingMonth, _currentSession.TimeZone);
                pdfModal.dateOfBilling = TimeZoneInfo.ConvertTimeFromUtc(pdfModal.dateOfBilling, _currentSession.TimeZone);

                Bills bill = null; //this.GetBillData();
                if (string.IsNullOrEmpty(pdfModal.billNo))
                {
                    if (bill == null || string.IsNullOrEmpty(bill.GeneratedBillNo))
                    {
                        throw new HiringBellException("Fail to generate bill no.");
                    }

                    pdfModal.billNo = bill.GeneratedBillNo;
                }
                else
                {
                    string GeneratedBillNo = "";
                    int len = pdfModal.billNo.Length;
                    int i = 0;
                    while (i < bill.BillNoLength)
                    {
                        if (i < len)
                        {
                            GeneratedBillNo += pdfModal.billNo[i];
                        }
                        else
                        {
                            GeneratedBillNo = '0' + GeneratedBillNo;
                        }
                        i++;
                    }
                    pdfModal.billNo = GeneratedBillNo;
                }

                pdfModal.billNo = pdfModal.billNo.Replace("#", "");

                Organization sender = null;
                Organization receiver = null;
                DataSet ds = this.db.GetDataSet(Procedures.Billing_Detail, new
                {
                    receiver = pdfModal.receiverCompanyId,
                    sender = pdfModal.senderId,
                    billNo = pdfModal.billNo,
                    employeeId = pdfModal.EmployeeId,
                    userTypeId = UserType.Employee,
                    forMonth = pdfModal.billingMonth.Month,
                    forYear = pdfModal.billYear
                });

                if (ds.Tables.Count == 4)
                {
                    sender = Converter.ToType<Organization>(ds.Tables[0]);
                    receiver = Converter.ToType<Organization>(ds.Tables[1]);

                    emails.Add(receiver.Email);
                    if (!string.IsNullOrEmpty(receiver.OtherEmail_1))
                        emails.Add(receiver.OtherEmail_1);
                    if (!string.IsNullOrEmpty(receiver.OtherEmail_2))
                        emails.Add(receiver.OtherEmail_2);
                    emails.Add(sender.Email);

                    fileDetail = Converter.ToType<FileDetail>(ds.Tables[2]);

                    this.FillReceiverDetail(receiver, pdfModal);
                    this.FillSenderDetail(sender, pdfModal);

                    // this.ValidateBillModal(pdfModal);

                    if (fileDetail == null)
                    {
                        fileDetail = new FileDetail
                        {
                            ClientId = pdfModal.ClientId
                        };
                    }

                    List<AttendenceDetail> attendanceSet = new List<AttendenceDetail>();
                    if (ds.Tables[3].Rows.Count > 0)
                    {
                        var currentAttendance = Converter.ToType<Attendance>(ds.Tables[3]);
                        attendanceSet = JsonConvert.DeserializeObject<List<AttendenceDetail>>(currentAttendance.AttendanceDetail);
                    }

                    string templatePath = Path.Combine(_fileLocationDetail.RootPath,
                        _fileLocationDetail.Location,
                        Path.Combine(_fileLocationDetail.HtmlTemplatePath),
                        _fileLocationDetail.StaffingBillTemplate
                    );

                    string pdfTemplatePath = Path.Combine(_fileLocationDetail.RootPath,
                        _fileLocationDetail.Location,
                        Path.Combine(_fileLocationDetail.HtmlTemplatePath),
                        _fileLocationDetail.StaffingBillPdfTemplate
                    );

                    string headerLogo = Path.Combine(_fileLocationDetail.RootPath, _fileLocationDetail.LogoPath, "logo.png");
                    if (File.Exists(templatePath) && File.Exists(pdfTemplatePath) && File.Exists(headerLogo))
                    {
                        this.CleanOldFiles(fileDetail);
                        pdfModal.UpdateSeqNo++;
                        fileDetail.FileExtension = string.Empty;

                        string MonthName = pdfModal.billingMonth.ToString("MMM_yyyy");
                        string FolderLocation = Path.Combine(_fileLocationDetail.Location, _fileLocationDetail.BillsPath, MonthName);
                        string folderPath = Path.Combine(Directory.GetCurrentDirectory(), FolderLocation);
                        if (!Directory.Exists(folderPath))
                            Directory.CreateDirectory(folderPath);

                        string destinationFilePath = Path.Combine(
                            folderPath,
                            pdfModal.developerName.Replace(" ", "_") + "_" +
                            pdfModal.billingMonth.ToString("MMM_yyyy") + "_" +
                            pdfModal.billNo + "_" +
                            pdfModal.UpdateSeqNo + $".{ApplicationConstants.Excel}");

                        if (File.Exists(destinationFilePath))
                            File.Delete(destinationFilePath);

                        var timesheetData = (from n in attendanceSet
                                             orderby n.AttendanceDay ascending
                                             select new TimesheetModel
                                             {
                                                 Date = n.AttendanceDay.ToString("dd MMM yyyy"),
                                                 ResourceName = pdfModal.developerName,
                                                 StartTime = "10:00 AM",
                                                 EndTime = "06:00 PM",
                                                 TotalHrs = 9,
                                                 Comments = n.UserComments,
                                                 Status = "Approved"
                                             }
                        ).ToList<TimesheetModel>();

                        // UpdateTimesheet
                        // _timesheetService.UpdateTimesheetService(dailyTimesheetDetails, timesheetDetail, Comment);

                        var timeSheetDataSet = Converter.ToDataSet<TimesheetModel>(timesheetData);
                        _excelWriter.ToExcel(timeSheetDataSet.Tables[0], destinationFilePath, pdfModal.billingMonth.ToString("MMM_yyyy"));

                        GetFileDetail(pdfModal, fileDetail, ApplicationConstants.Docx);
                        fileDetail.LogoPath = headerLogo;

                        // Converting html context for docx conversion.
                        // string html = this.GetHtmlString(templatePath, pdfModal, sender, receiver);
                        string html = this.GetHtmlString(templatePath, null);
                        destinationFilePath = Path.Combine(fileDetail.DiskFilePath, fileDetail.FileName + $".{ApplicationConstants.Docx}");
                        this.iHTMLConverter.ToDocx(html, destinationFilePath, headerLogo);

                        GetFileDetail(pdfModal, fileDetail, ApplicationConstants.Pdf);
                        _fileMaker._fileDetail = fileDetail;

                        // Converting html context for pdf conversion.
                        // html = this.GetHtmlString(pdfTemplatePath, pdfModal, sender, receiver, headerLogo);
                        html = this.GetHtmlString(pdfTemplatePath, null, true);
                        destinationFilePath = Path.Combine(fileDetail.DiskFilePath, fileDetail.FileName + $".{ApplicationConstants.Pdf}");
                        _htmlToPdfConverter.ConvertToPdf(html, destinationFilePath);

                        var dbResult = this.db.Execute(Procedures.Filedetail_Insupd, new
                        {
                            FileId = fileDetail.FileId,
                            ClientId = pdfModal.receiverCompanyId,
                            FileName = fileDetail.FileName,
                            FilePath = fileDetail.FilePath,
                            FileExtension = fileDetail.FileExtension,
                            StatusId = pdfModal.StatusId,
                            GeneratedBillNo = bill.NextBillNo,
                            BillUid = bill.BillUid,
                            BillDetailId = pdfModal.billId,
                            BillNo = pdfModal.billNo,
                            PaidAmount = pdfModal.packageAmount,
                            BillForMonth = pdfModal.billingMonth.Month,
                            BillYear = pdfModal.billYear,
                            NoOfDays = pdfModal.workingDay,
                            NoOfDaysAbsent = pdfModal.daysAbsent,
                            IGST = pdfModal.iGST,
                            SGST = pdfModal.sGST,
                            CGST = pdfModal.cGST,
                            TDS = ApplicationConstants.TDS,
                            BillStatusId = ApplicationConstants.Pending,
                            PaidOn = pdfModal.PaidOn,
                            FileDetailId = pdfModal.FileId,
                            UpdateSeqNo = pdfModal.UpdateSeqNo,
                            EmployeeUid = pdfModal.EmployeeId,
                            BillUpdatedOn = pdfModal.dateOfBilling,
                            IsCustomBill = pdfModal.IsCustomBill,
                            UserTypeId = UserType.Employee,
                            AdminId = _currentSession.CurrentUserDetail.UserId
                        }, true);

                        if (!string.IsNullOrEmpty(dbResult.statusMessage))
                        {
                            List<Files> files = new List<Files>();
                            files.Add(new Files
                            {
                                FilePath = fileDetail.FilePath,
                                FileName = fileDetail.FileName
                            });
                            this.fileService.DeleteFiles(files);
                            throw HiringBellException.ThrowBadRequest("Failt to execute. Please contact admin.");
                        }

                        var fileId = Convert.ToInt32(dbResult.statusMessage);
                        fileDetail.FileId = Convert.ToInt32(fileId);
                        fileDetail.DiskFilePath = null;

                    }
                    else
                    {
                        throw new HiringBellException("HTML template or Logo file path is invalid");
                    }
                }
                else
                {
                    throw new HiringBellException("Amount calculation is not matching", nameof(pdfModal.grandTotalAmount), pdfModal.grandTotalAmount.ToString());
                }
            }
            catch (HiringBellException e)
            {
                _logger.LogError($"{e.UserMessage} Field: {e.FieldName} Value: {e.FieldValue}");
                throw e.BuildBadRequest(e.UserMessage, e.FieldName, e.FieldValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw new HiringBellException(ex.Message, ex);
            }

            EmailTemplate emailTemplate = _templateService.GetBillingTemplateDetailService();
            emailTemplate.Emails = emails;
            return new { FileDetail = fileDetail, EmailTemplate = emailTemplate };
        }

        private void CleanOldFiles(FileDetail fileDetail)
        {
            // Old file name and path
            if (!string.IsNullOrEmpty(fileDetail.FilePath))
            {
                string ExistingFolder = Path.Combine(Directory.GetCurrentDirectory(), fileDetail.FilePath);
                if (Directory.Exists(ExistingFolder))
                {
                    if (Directory.GetFiles(ExistingFolder).Length == 0)
                    {
                        Directory.Delete(ExistingFolder);
                    }
                    else
                    {
                        string ExistingFilePath = Path.Combine(Directory.GetCurrentDirectory(), fileDetail.FilePath, fileDetail.FileName + "." + ApplicationConstants.Docx);
                        if (File.Exists(ExistingFilePath))
                            File.Delete(ExistingFilePath);

                        ExistingFilePath = Path.Combine(Directory.GetCurrentDirectory(), fileDetail.FilePath, fileDetail.FileName + "." + ApplicationConstants.Pdf);
                        if (File.Exists(ExistingFilePath))
                            File.Delete(ExistingFilePath);
                    }
                }
            }
        }

        private void GetFileDetail(PdfModal pdfModal, FileDetail fileDetail, string fileExtension)
        {
            fileDetail.Status = 0;
            if (pdfModal.ClientId > 0)
            {
                try
                {
                    string MonthName = pdfModal.billingMonth.ToString("MMM_yyyy");
                    string FolderLocation = Path.Combine(_fileLocationDetail.Location, _fileLocationDetail.BillsPath, MonthName);
                    string FileName = pdfModal.developerName.Replace(" ", "_") + "_" +
                                  MonthName + "_" +
                                  pdfModal.billNo.Replace("#", "") + "_" + pdfModal.UpdateSeqNo;

                    string folderPath = Path.Combine(Directory.GetCurrentDirectory(), FolderLocation);
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    fileDetail.FilePath = FolderLocation;
                    fileDetail.DiskFilePath = folderPath;
                    fileDetail.FileName = FileName;
                    if (string.IsNullOrEmpty(fileDetail.FileExtension))
                        fileDetail.FileExtension = fileExtension;
                    else
                        fileDetail.FileExtension += $",{fileExtension}";
                    if (pdfModal.FileId > 0)
                        fileDetail.FileId = pdfModal.FileId;
                    else
                        fileDetail.FileId = -1;
                    fileDetail.StatusId = 2;
                    fileDetail.PaidOn = null;
                    fileDetail.Status = 1;
                }
                catch (Exception ex)
                {
                    fileDetail.Status = -1;
                    throw ex;
                }
            }
        }

        public string UpdateGstStatus(GstStatusModel createPageModel, IFormFileCollection FileCollection, List<Files> fileDetail)
        {
            string result = string.Empty;
            TimeZoneInfo istTimeZome = TZConvert.GetTimeZoneInfo("India Standard Time");
            createPageModel.Paidon = TimeZoneInfo.ConvertTimeFromUtc(createPageModel.Paidon, istTimeZome);

            if (fileDetail.Count > 0)
            {
                string FolderPath = Path.Combine(_fileLocationDetail.Location, $"GSTFile_{createPageModel.Billno}");
                //List<Files> files = fileService.SaveFile(FolderPath, fileDetail, FileCollection, "0");

                List<Files> files = new List<Files>();
                if (files != null && files.Count > 0)
                {
                    if (files != null && files.Count > 0)
                    {
                        var fileInfo = (from n in fileDetail
                                        select new
                                        {
                                            FileId = n.FileUid,
                                            FileOwnerId = n.UserId,
                                            FileName = n.FileName,
                                            FilePath = n.FilePath,
                                            ParentFolder = n.ParentFolder,
                                            FileExtension = n.FileExtension,
                                            StatusId = 0,
                                            UserTypeId = (int)n.UserTypeId,
                                            AdminId = 1
                                        }).ToList();

                        this.db.BulkExecuteAsync(ApplicationConstants.InserUserFileDetail, fileInfo, true);
                    }
                }
            }

            result = this.db.Execute<string>(Procedures.Gstdetail_Insupd, new
            {
                gstId = createPageModel.GstId,
                billno = createPageModel.Billno,
                gststatus = createPageModel.Gststatus,
                paidon = createPageModel.Paidon,
                paidby = createPageModel.Paidby,
                amount = createPageModel.Amount,
                fileId = createPageModel.FileId,
            }, true);
            return result;
        }

        public async Task<string> SendBillToClientService(GenerateBillFileDetail generateBillFileDetail)
        {
            var resultSet = db.FetchDataSet(Procedures.SENDINGBILL_EMAIL_GET_DETAIL, new
            {
                generateBillFileDetail.SenderId,
                generateBillFileDetail.ClientId,
                generateBillFileDetail.FileId,
                generateBillFileDetail.EmployeeId
            });

            if (resultSet != null && resultSet.Tables.Count == 2)
            {
                var file = Converter.ToList<FileDetail>(resultSet.Tables[0]);
                var employee = Converter.ToType<Employee>(resultSet.Tables[1]);
                // var emailTempalte = Converter.ToType<EmailTemplate>(resultSet.Tables[2]);

                List<string> emails = generateBillFileDetail.EmailTemplateDetail.Emails;
                if (emails.Count == 0)
                    throw HiringBellException.ThrowBadRequest("No receiver address added. Please add atleast one email address.");

                //UpdateTemplateData(emailTempalte, employee, generateBillFileDetail.MonthName, generateBillFileDetail.ForYear, generateBillFileDetail);

                //EmailSenderModal emailSenderModal = new EmailSenderModal
                //{
                //    To = emails, //receiver.Email,
                //    CC = new List<string>(),
                //    BCC = new List<string>(),
                //    Subject = emailTempalte.SubjectLine,
                //    FileDetails = file,
                //    Body = emailTempalte.BodyContent,
                //    Title = emailTempalte.EmailTitle
                //};

                //_eMailManager.SendMailAsync(emailSenderModal);

                BillingTemplateModel billingTemplateModel = new BillingTemplateModel
                {
                    FileDetails = file,
                    DeveloperName = string.Concat(employee.FirstName, " ", employee.LastName),
                    Month = generateBillFileDetail.MonthName,
                    Year = generateBillFileDetail.ForYear,
                    ToAddress = emails,
                    kafkaServiceName = KafkaServiceName.Billing,
                    CompanyName = _currentSession.CurrentUserDetail.CompanyName,
                    Role = "Developer",
                    LocalConnectionString = _currentSession.LocalConnectionString,
                    CompanyId = _currentSession.CurrentUserDetail.CompanyId
                };

                await _kafkaProducerService.SendEmailNotification(billingTemplateModel, KafkaTopicNames.ATTENDANCE_REQUEST_ACTION);
            }

            return ApplicationConstants.Successfull;
        }

        //private void UpdateTemplateData(EmailTemplate template, Employee employee, string month, int year, GenerateBillFileDetail generateBillFileDetail)
        //{
        //    if (!string.IsNullOrEmpty(generateBillFileDetail.EmailTemplateDetail.BodyContent))
        //        template.BodyContent = generateBillFileDetail.EmailTemplateDetail.BodyContent;
        //    else
        //        template.BodyContent = JsonConvert.DeserializeObject<string>(template.BodyContent);

        //    if (template != null && !string.IsNullOrEmpty(template.BodyContent))
        //    {
        //        template.BodyContent = (template.BodyContent
        //            .Replace("[[DEVELOPER-NAME]]", employee.FirstName + " " + employee.LastName)
        //            .Replace("[[YEAR]]", year.ToString())
        //            .Replace("[[MONTH]]", month));
        //    }

        //    if (!string.IsNullOrEmpty(generateBillFileDetail.EmailTemplateDetail.EmailSubject))
        //        template.SubjectLine = generateBillFileDetail.EmailTemplateDetail.EmailSubject;

        //    if (!string.IsNullOrEmpty(generateBillFileDetail.EmailTemplateDetail.EmailTitle))
        //        template.EmailTitle = generateBillFileDetail.EmailTemplateDetail.EmailTitle;
        //}

        public async Task<dynamic> GeneratePayslipService(PayslipGenerationModal payslipGenerationModal)
        {
            try
            {
                if (payslipGenerationModal.EmployeeId <= 0)
                    throw new HiringBellException("Invalid employee selected. Please select a valid employee");

                // fetch and all the necessary data from database required to bill generation.
                await PrepareRequestForPayslipGeneration(payslipGenerationModal);

                FileDetail fileDetail = new FileDetail();
                fileDetail.FileExtension = string.Empty;
                payslipGenerationModal.FileDetail = fileDetail;

                // store template logo and file locations
                await CapturePayslipFileFolderLocations(payslipGenerationModal);
                this.CleanOldFiles(fileDetail);

                // generate pdf files
                await GeneratePayslipPdfFile(payslipGenerationModal);

                // return result data
                return await Task.FromResult(new { FileDetail = fileDetail });
            }
            catch (HiringBellException e)
            {
                _logger.LogError($"{e.UserMessage} Field: {e.FieldName} Value: {e.FieldValue}");
                throw e.BuildBadRequest(e.UserMessage, e.FieldName, e.FieldValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw new HiringBellException(ex.Message, ex);
            }
        }

        private async Task CapturePayslipFileFolderLocations(PayslipGenerationModal payslipModal)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var payslipPath = Path.Combine(_fileLocationDetail.HtmlTemplatePath, _fileLocationDetail.PaysliplTemplate);
                    var url = $"https://www.bottomhalf.in/bts/resources/applications/ems/{payslipPath}";

                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                        payslipModal.PdfTemplateHTML = await response.Content.ReadAsStringAsync();
                    else
                        Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);

                    client.Dispose();
                }
            }
            catch (Exception)
            {
                throw;
            }

            //payslipModal.PayslipTemplatePath = Path.Combine(_fileLocationDetail.RootPath,
            //            _fileLocationDetail.Location,
            //            Path.Combine(_fileLocationDetail.HtmlTemplatePath),
            //            _fileLocationDetail.PaysliplTemplate
            //        );

            //if (!File.Exists(payslipModal.PayslipTemplatePath))
            //    throw HiringBellException.ThrowBadRequest("Payslip template not found. Please contact to admin.");

            //payslipModal.PdfTemplatePath = Path.Combine(_fileLocationDetail.RootPath,
            //    _fileLocationDetail.Location,
            //    Path.Combine(_fileLocationDetail.HtmlTemplatePath),
            //    _fileLocationDetail.PaysliplTemplate
            //);

            //if (!File.Exists(payslipModal.PdfTemplatePath))
            //    throw HiringBellException.ThrowBadRequest("PDF template not found. Please contact to admin.");

            if (!payslipModal.HeaderLogoPath.Contains("https://") && !File.Exists(payslipModal.HeaderLogoPath))
            {
                //throw HiringBellException.ThrowBadRequest("Logo image not found. Please contact to admin.");
                payslipModal.HeaderLogoPath = "https://www.emstum.com/assets/images/logo.png";
            }

            await Task.CompletedTask;
        }

        private async Task GeneratePayslipPdfFile(PayslipGenerationModal payslipModal)
        {
            GetPayslipFileDetail(payslipModal, payslipModal.FileDetail, ApplicationConstants.Pdf);
            _fileMaker._fileDetail = payslipModal.FileDetail;

            // Converting html context for pdf conversion.
            var html = await GetPayslipHtmlString(payslipModal.PdfTemplatePath, payslipModal, true);

            var destinationFilePath = Path.Combine(payslipModal.FileDetail.DiskFilePath,
                payslipModal.FileDetail.FileName + $".{ApplicationConstants.Pdf}");

            var Email = payslipModal.Employee.Email.Replace("@", "_").Replace(".", "_");

            HtmlToPdfConvertorModal htmlToPdfConvertorModal = new HtmlToPdfConvertorModal
            {
                HTML = html,
                ServiceName = LocalConstants.EmstumFileService,
                FileName = payslipModal.FileDetail.FileName + $".{ApplicationConstants.Pdf}",
                FolderPath = Path.Combine(_currentSession.CompanyCode, _fileLocationDetail.User, Email)
            };

            string url = $"{_microserviceRegistry.ConvertHtmlToPdf}";

            MicroserviceRequest microserviceRequest = new MicroserviceRequest
            {
                Url = url,
                CompanyCode = _currentSession.CompanyCode,
                Token = _currentSession.Authorization,
                Database = _requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString),
                Payload = JsonConvert.SerializeObject(htmlToPdfConvertorModal)
            };

            payslipModal.FileDetail.FilePath = await _requestMicroservice.PostRequest<string>(microserviceRequest);

            //_htmlToPdfConverter.ConvertToPdf(html, destinationFilePath);

            await Task.CompletedTask;
        }

        private async Task<string> GetPayslipHtmlString(string templatePath, PayslipGenerationModal payslipModal, bool isHeaderLogoRequired = false)
        {
            string html = string.Empty;
            var salaryDetailsHTML = string.Empty;
            var salaryDetail = payslipModal.SalaryDetail.SalaryBreakupDetails.FindAll(x =>
                x.ComponentId != ComponentNames.GrossId &&
                x.ComponentId != ComponentNames.CTCId &&
                x.ComponentId != ComponentNames.EmployerPF &&
                x.ComponentId != LocalConstants.EPF &&
                x.ComponentId != LocalConstants.ESI &&
                x.ComponentId != LocalConstants.EESI &&
                x.ComponentId != ComponentNames.ProfessionalTax &&
                x.IsIncludeInPayslip == true
            );

            // EmployeeDeclaration employeeDeclaration = await _declarationService.GetEmployeeDeclarationDetail(payslipModal.EmployeeId);
            string url = $"{_microserviceRegistry.GetEmployeeDeclarationDetailById}/{payslipModal.EmployeeId}";
            MicroserviceRequest microserviceRequest = new MicroserviceRequest
            {
                Url = url,
                CompanyCode = _currentSession.CompanyCode,
                Token = _currentSession.Authorization,
                Database = _requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString)
            };

            EmployeeDeclaration employeeDeclaration = await _requestMicroservice.GetRequest<EmployeeDeclaration>(microserviceRequest);

            // here add condition that it detail will shown or not
            string declarationHTML = String.Empty;
            //declarationHTML = GetDeclarationDetailHTML(employeeDeclaration);

            var grossIncome = employeeDeclaration.SalaryDetail.GrossIncome;
            decimal totalYTDAmount = 0;

            salaryDetailsHTML = AddYTDComponent(payslipModal, salaryDetail, ref totalYTDAmount);
            salaryDetailsHTML = AddArrearComponent(payslipModal, salaryDetailsHTML);
            salaryDetailsHTML = AddBonusComponent(payslipModal, salaryDetailsHTML);

            string employeeContribution = string.Empty;
            decimal totalContribution = 0;

            employeeContribution = AddEmployeePfComponent(payslipModal, employeeContribution, ref totalContribution);
            employeeContribution = AddEmployeeESI(payslipModal, employeeContribution, ref totalContribution);

            // var pTaxAmount = PTaxCalculation(payslipModal.Gross, payslipModal.PTaxSlabs);
            var pTaxAmount = payslipModal.SalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId == ComponentNames.ProfessionalTax).FinalAmount;
            var totalEarning = salaryDetail.Sum(x => x.FinalAmount) + payslipModal.SalaryDetail.ArrearAmount + payslipModal.SalaryDetail.BonusAmount;
            var totalActualEarning = salaryDetail.Sum(x => x.ActualAmount);
            var totalDeduction = payslipModal.TaxDetail.TaxDeducted > pTaxAmount ? payslipModal.TaxDetail.TaxDeducted : pTaxAmount;
            var netSalary = totalEarning > 0 ? totalEarning - (totalContribution + totalDeduction) : 0;
            var netSalaryInWord = NumberToWords(netSalary);
            var designation = payslipModal.EmployeeRoles.Find(x => x.RoleId == payslipModal.Employee.DesignationId).RoleName;
            var ActualPayableDays = DateTime.DaysInMonth(payslipModal.Year, payslipModal.Month);
            //var TotalWorkingDays = GetWorkingDays(payslipModal.dailyAttendances, payslipModal.leaveRequestNotifications);
            var TotalWorkingDays = ActualPayableDays - payslipModal.PayrollMonthlyDetail.LOP;

            var LossOfPayDays = payslipModal.PayrollMonthlyDetail.LOP;

            html = payslipModal.PdfTemplateHTML.Replace("[[CompanyFirstAddress]]", payslipModal.Company.FirstAddress).
                Replace("[[CompanySecondAddress]]", payslipModal.Company.SecondAddress).
                Replace("[[CompanyThirdAddress]]", payslipModal.Company.ThirdAddress).
                Replace("[[CompanyFourthAddress]]", payslipModal.Company.ForthAddress).
                Replace("[[CompanyName]]", payslipModal.Company.CompanyName).
                Replace("[[EmployeeName]]", payslipModal.Employee.FirstName + " " + payslipModal.Employee.LastName).
                Replace("[[EmployeeNo]]", payslipModal.Employee.EmployeeUid.ToString()).
                Replace("[[JoiningDate]]", payslipModal.Employee.CreatedOn.ToString("dd MMM, yyyy")).
                Replace("[[Department]]", designation).
                Replace("[[SubDepartment]]", "NA").
                Replace("[[Designation]]", designation).
                Replace("[[Payment Mode]]", "Bank Transfer").
                Replace("[[Bank]]", payslipModal.Employee.BankName).
                Replace("[[BankIFSC]]", payslipModal.Employee.IFSCCode).
                Replace("[[Bank Account]]", payslipModal.Employee.AccountNumber).
                Replace("[[PAN]]", payslipModal.Employee.PANNo).
                Replace("[[UAN]]", payslipModal.Employee.UAN).
                Replace("[[PFNumber]]", payslipModal.Employee.PFNumber).
                Replace("[[ActualPayableDays]]", ActualPayableDays.ToString()).
                Replace("[[TotalWorkingDays]]", TotalWorkingDays.ToString()).
                Replace("[[LossOfPayDays]]", LossOfPayDays.ToString()).
                Replace("[[DaysPayable]]", TotalWorkingDays.ToString()).
                Replace("[[Month]]", payslipModal.SalaryDetail.MonthName.ToUpper()).
                Replace("[[Year]]", payslipModal.Year.ToString()).
                Replace("[[CompleteSalaryDetails]]", salaryDetailsHTML).
                Replace("[[CompleteContributions]]", employeeContribution).
                Replace("[[TotalEarnings]]", totalEarning.ToString("0.00")).
                Replace("[[TotalIncomeTax]]", (payslipModal.TaxDetail.TaxDeducted >= pTaxAmount ? payslipModal.TaxDetail.TaxDeducted - pTaxAmount : 0).ToString("0.00")).
                Replace("[[TotalDeduction]]", totalDeduction.ToString("0.00")).
                Replace("[[TotalContribution]]", totalContribution.ToString("0.00")).
                Replace("[[NetSalaryInWords]]", netSalaryInWord).
                Replace("[[PTax]]", pTaxAmount.ToString()).
                Replace("[[NetSalaryPayable]]", netSalary.ToString("0.00")).
                Replace("[[GrossIncome]]", grossIncome.ToString("0.00")).
                Replace("[[TotalActualEarnings]]", totalActualEarning.ToString("0.00")).
                Replace("[[TotalYTD]]", totalYTDAmount.ToString("0.00")).
                Replace("[[EmployeeDeclaration]]", declarationHTML)
                .Replace("[[CompanyLegalName]]", payslipModal.Company.CompanyName);

            if (!string.IsNullOrEmpty(payslipModal.HeaderLogoPath) && isHeaderLogoRequired)
                html = await AddCompanyLogo(payslipModal, html);

            return html;
        }

        private string AddYTDComponent(PayslipGenerationModal payslipModal, List<CalculatedSalaryBreakupDetail> salaryDetail, ref decimal totalYTDAmount)
        {
            var textinfo = CultureInfo.CurrentCulture.TextInfo;
            string salaryDetailsHTML = "";

            var YTDSalaryBreakup = payslipModal.AnnualSalaryBreakup.FindAll(x => x.IsActive && !x.IsPreviouEmployer && x.IsPayrollExecutedForThisMonth
                                                                                && payslipModal.SalaryDetail.PresentMonthDate.Subtract(x.PresentMonthDate).Days >= 0);
            foreach (var item in salaryDetail)
            {
                decimal YTDAmount = 0;
                YTDSalaryBreakup.ForEach(x =>
                {
                    YTDAmount += x.SalaryBreakupDetails.Find(i => i.ComponentId == item.ComponentId).FinalAmount;
                });
                salaryDetailsHTML += "<tr>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px;\">" + textinfo.ToTitleCase(item.ComponentName.ToLower()) + "</td>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\">" + item.ActualAmount.ToString("0.00") + "</td>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\">" + item.FinalAmount.ToString("0.00") + "</td>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\">" + YTDAmount.ToString("0.00") + "</td>";
                salaryDetailsHTML += "</tr>";
                totalYTDAmount += YTDAmount;
            }

            decimal arrearAmount = YTDSalaryBreakup.Sum(x => x.ArrearAmount);
            totalYTDAmount += arrearAmount;

            return salaryDetailsHTML;
        }

        private string AddEmployeeESI(PayslipGenerationModal payslipModal, string employeeContribution, ref decimal totalContribution)
        {
            var employeeESI = payslipModal.SalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId == LocalConstants.ESI);
            if (employeeESI != null && employeeESI.IsIncludeInPayslip)
            {
                totalContribution += employeeESI.FinalAmount;
                employeeContribution += "<tr>";
                employeeContribution += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px;\">" + "Employee ESI" + "</td>";
                employeeContribution += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\">" + employeeESI.FinalAmount.ToString("0.00") + "</td>";
                employeeContribution += "</tr>";
            }

            return employeeContribution;
        }

        private string AddEmployeePfComponent(PayslipGenerationModal payslipModal, string employeeContribution, ref decimal totalContribution)
        {
            var employeePF = payslipModal.SalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId == LocalConstants.EPF);
            if (employeePF != null && employeePF.IsIncludeInPayslip)
            {
                totalContribution += employeePF.FinalAmount;
                employeeContribution += "<tr>";
                employeeContribution += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px;\">" + "Employee PF" + "</td>";
                employeeContribution += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\">" + employeePF.FinalAmount.ToString("0.00") + "</td>";
                employeeContribution += "</tr>";
            }

            return employeeContribution;
        }

        private string AddBonusComponent(PayslipGenerationModal payslipModal, string salaryDetailsHTML)
        {
            if (payslipModal.SalaryDetail.BonusAmount != decimal.Zero)
            {
                salaryDetailsHTML += "<tr>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px;\">" + "Bonus Amount" + "</td>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\">" + "--" + "</td>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\">" + payslipModal.SalaryDetail.BonusAmount.ToString("0.00") + "</td>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\">" + "--" + "</td>";
                salaryDetailsHTML += "</tr>";
            }

            return salaryDetailsHTML;
        }

        private string AddArrearComponent(PayslipGenerationModal payslipModal, string salaryDetailsHTML)
        {
            if (payslipModal.SalaryDetail.ArrearAmount != decimal.Zero)
            {
                salaryDetailsHTML += "<tr>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px;\">" + "Arrear Amount" + "</td>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\">" + "--" + "</td>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\">" + payslipModal.SalaryDetail.ArrearAmount.ToString("0.00") + "</td>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\">" + "--" + "</td>";
                salaryDetailsHTML += "</tr>";
            }

            return salaryDetailsHTML;
        }

        private async Task<string> AddCompanyLogo(PayslipGenerationModal payslipModal, string html)
        {
            if (payslipModal.HeaderLogoPath.Contains("https://"))
            {
                html = html.Replace("[[COMPANYLOGO_PATH]]", $"{payslipModal.HeaderLogoPath}");
            }
            else
            {
                ImageFormat imageFormat = GetImageFormat(payslipModal.HeaderLogoPath);
                string encodeStart = $@"data:image/{imageFormat.ToString().ToLower()};base64";

                var fs = new FileStream(payslipModal.HeaderLogoPath, FileMode.Open);
                using (BinaryReader br = new BinaryReader(fs))
                {
                    Byte[] bytes = br.ReadBytes((Int32)fs.Length);
                    string base64String = Convert.ToBase64String(bytes, 0, bytes.Length);

                    html = html.Replace("[[COMPANYLOGO_PATH]]", $"{encodeStart}, {base64String}");
                }
            }

            return await Task.FromResult(html);
        }

        private ImageFormat GetImageFormat(string headerLogoPath)
        {
            int lastPosition = headerLogoPath.LastIndexOf(".");
            string extension = headerLogoPath.Substring(lastPosition + 1);
            ImageFormat imageFormat = null;
            if (extension == "png")
                imageFormat = ImageFormat.Png;
            else if (extension == "gif")
                imageFormat = ImageFormat.Gif;
            else if (extension == "bmp")
                imageFormat = ImageFormat.Bmp;
            else if (extension == "jpeg" || extension == "jpg")
                imageFormat = ImageFormat.Jpeg;
            else if (extension == "tiff")
            {
                // Convert tiff to gif.
                extension = "gif";
                imageFormat = ImageFormat.Gif;
            }
            else if (extension == "x-wmf")
            {
                extension = "wmf";
                imageFormat = ImageFormat.Wmf;
            }

            return imageFormat;
        }

        //private decimal GetWorkingDays(List<DailyAttendance> dailyAttendances, List<LeaveRequestNotification> leaveRequestNotifications)
        //{
        //    decimal totalDays = 0;
        //    dailyAttendances = dailyAttendances.OrderBy(x => x.AttendanceDate).ToList();
        //    var fromDate = dailyAttendances.First().AttendanceDate;
        //    var toDate = dailyAttendances.Last().AttendanceDate;
        //    //var approvedAttendance = dailyAttendances.FindAll(x => x.AttendanceStatus == (int)ItemStatus.Approved
        //    //                                                    || x.AttendanceStatus == (int)AttendanceEnum.WeekOff
        //    //                                                    || x.AttendanceStatus == (int)AttendanceEnum.Holiday);
        //    totalDays = dailyAttendances.Count(x => x.AttendanceStatus == (int)ItemStatus.Approved
        //                                            || x.AttendanceStatus == (int)AttendanceEnum.WeekOff
        //                                            || x.AttendanceStatus == (int)AttendanceEnum.Holiday);

        //    var leaves = leaveRequestNotifications.Where(x => x.FromDate >= fromDate && x.ToDate <= toDate).ToList();
        //    leaves.ForEach(x =>
        //    {
        //        if (x.ToDate <= toDate)
        //            totalDays += (decimal)x.ToDate.Subtract(x.FromDate).TotalDays + 1;
        //        else
        //            totalDays += (decimal)toDate.Subtract(x.FromDate).TotalDays + 1;
        //    });

        //    //if (approvedAttendance != null || approvedAttendance.Count > 0)
        //    //{
        //    //    totalDays = approvedAttendance.Count(x => x.SessionType == (int)SessionType.FullDay);
        //    //    totalDays = totalDays + (approvedAttendance.Count(x => x.SessionType != (int)SessionType.FullDay) * 0.5m);
        //    //}
        //    return totalDays;
        //}

        private async Task PrepareRequestForPayslipGeneration(PayslipGenerationModal payslipGenerationModal)
        {
            //var date = new DateTime(payslipGenerationModal.Year, payslipGenerationModal.Month, 1);
            //var FromDate = _timezoneConverter.ToUtcTime(date, _currentSession.TimeZone);
            //var ToDate = _timezoneConverter.ToUtcTime(date.AddMonths(1).AddDays(-1), _currentSession.TimeZone);
            DataSet ds = this.db.FetchDataSet(Procedures.Payslip_Detail, new
            {
                payslipGenerationModal.EmployeeId,
                payslipGenerationModal.Month,
                payslipGenerationModal.Year,
                FileRole = ApplicationConstants.CompanyPrimaryLogo
            });

            if (ds == null || ds.Tables.Count != 8)
                throw new HiringBellException("Fail to get payslip detail. Please contact to admin.");

            if (ds.Tables[0].Rows.Count != 1)
                throw new HiringBellException("Fail to get company detail. Please contact to admin.");

            payslipGenerationModal.Company = Converter.ToType<Organization>(ds.Tables[0]);
            if (ds.Tables[1].Rows.Count != 1)
                throw new HiringBellException("Fail to get employee detail. Please contact to admin.");

            payslipGenerationModal.Employee = Converter.ToType<Employee>(ds.Tables[1]);
            if (ds.Tables[2].Rows.Count != 1)
                throw new HiringBellException("Fail to get employee detail. Please contact to admin.");

            var SalaryDetail = Converter.ToType<EmployeeSalaryDetail>(ds.Tables[2]);
            if (SalaryDetail.CompleteSalaryDetail == null)
                throw new HiringBellException("Salary breakup not found. Please contact to admin");

            payslipGenerationModal.Gross = SalaryDetail.GrossIncome;
            payslipGenerationModal.AnnualSalaryBreakup = JsonConvert.DeserializeObject<List<AnnualSalaryBreakup>>(SalaryDetail.CompleteSalaryDetail);
            payslipGenerationModal.SalaryDetail = payslipGenerationModal.AnnualSalaryBreakup.Find(x => x.MonthNumber == payslipGenerationModal.Month);
            if (payslipGenerationModal.SalaryDetail == null)
                throw new HiringBellException("Salary breakup of your selected month is not found");

            if (SalaryDetail.TaxDetail == null)
                throw new HiringBellException("Tax details not found. Please contact to admin");

            var taxDetails = JsonConvert.DeserializeObject<List<TaxDetails>>(SalaryDetail.TaxDetail);
            payslipGenerationModal.TaxDetail = taxDetails.Find(x => x.Year == payslipGenerationModal.Year && x.Month == payslipGenerationModal.Month);
            if (payslipGenerationModal.TaxDetail == null)
                throw new HiringBellException("Tax details of your selected month is not found");

            if (ds.Tables[4].Rows.Count == 0)
                throw new HiringBellException("Fail to get ptax slab detail. Please contact to admin.");

            payslipGenerationModal.PTaxSlabs = Converter.ToList<PTaxSlab>(ds.Tables[4]);
            if (ds.Tables[5].Rows.Count == 0)
                throw new HiringBellException("Fail to get employee role. Please contact to admin.");

            payslipGenerationModal.EmployeeRoles = Converter.ToList<EmployeeRole>(ds.Tables[5]);
            if (ds.Tables[3].Rows.Count == 0)
                throw new HiringBellException("Fail to get attendance detail. Please contact to admin.");

            //payslipGenerationModal.dailyAttendances = Converter.ToList<DailyAttendance>(ds.Tables[3]);
            payslipGenerationModal.PayrollMonthlyDetail = Converter.ToType<PayrollMonthlyDetail>(ds.Tables[3]);

            if (ds.Tables[6].Rows.Count == 0)
                throw new HiringBellException("Company primary logo not found. Please contact to admin.");

            var file = Converter.ToType<Files>(ds.Tables[6]);
            if (file != null)
            {
                payslipGenerationModal.HeaderLogoPath = Path.Combine(
                    _fileLocationDetail.RootPath,
                    file.FilePath,
                    file.FileName
                );
            }
            else
            {
                payslipGenerationModal.HeaderLogoPath = "https://www.emstum.com/assets/images/logo.png";
            }

            payslipGenerationModal.leaveRequestNotifications = Converter.ToList<LeaveRequestNotification>(ds.Tables[7]);

            await Task.CompletedTask;
        }

        private void GetPayslipFileDetail(PayslipGenerationModal payslipModal, FileDetail fileDetail, string fileExtension)
        {
            fileDetail.Status = 0;
            try
            {
                var Email = payslipModal.Employee.Email.Replace("@", "_").Replace(".", "_");
                string FolderLocation = Path.Combine(_fileLocationDetail.UserFolder, Email);
                string FileName = payslipModal.Employee.FirstName.Replace(" ", "_") + "_" + payslipModal.Employee.LastName.Replace(" ", "_") + "_" +
                              "Payslip" + "_" + payslipModal.SalaryDetail.MonthName + "_" + payslipModal.Year;

                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), FolderLocation);
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                fileDetail.FilePath = FolderLocation;
                fileDetail.DiskFilePath = folderPath;
                fileDetail.FileName = FileName;
                if (string.IsNullOrEmpty(fileDetail.FileExtension))
                    fileDetail.FileExtension = fileExtension;
                else
                    fileDetail.FileExtension += $",{fileExtension}";
                fileDetail.Status = 1;
            }
            catch (Exception ex)
            {
                fileDetail.Status = -1;
                throw ex;
            }
        }

        private string NumberToWords(decimal amount)
        {
            try
            {
                Int64 amount_int = (Int64)amount;
                Int64 amount_dec = (Int64)Math.Round((amount - (decimal)(amount_int)) * 100);
                if (amount_dec == 0)
                    return ConvertNumber(amount_int) + " Only.";
                else
                    return ConvertNumber(amount_int) + " Point " + ConvertNumber(amount_dec) + " Only.";
            }
            catch (Exception e)
            {
                throw new HiringBellException(e.Message);
            }
        }

        private String ConvertNumber(Int64 i)
        {
            String[] units = { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            String[] tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };
            if (i < 20)
                return units[i];

            if (i < 100)
                return tens[i / 10] + ((i % 10 > 0) ? " " + ConvertNumber(i % 10) : "");

            if (i < 1000)
                return units[i / 100] + " Hundred" + ((i % 100 > 0) ? " And " + ConvertNumber(i % 100) : "");

            if (i < 100000)
                return ConvertNumber(i / 1000) + " Thousand " + ((i % 1000 > 0) ? " " + ConvertNumber(i % 1000) : "");

            if (i < 10000000)
                return ConvertNumber(i / 100000) + " Lakh " + ((i % 100000 > 0) ? " " + ConvertNumber(i % 100000) : "");

            if (i < 1000000000)
                return ConvertNumber(i / 10000000) + " Crore " + ((i % 10000000 > 0) ? " " + ConvertNumber(i % 10000000) : "");

            return ConvertNumber(i / 1000000000) + " Arab " + ((i % 1000000000 > 0) ? " " + ConvertNumber(i % 1000000000) : "");
        }

        private decimal PTaxCalculation(decimal CTC, List<PTaxSlab> pTaxSlabs)
        {
            decimal ptaxAmount = 0;
            var professtionalTax = pTaxSlabs;
            var monthlyIncome = CTC / 12;
            var maxMinimumIncome = professtionalTax.Max(i => i.MinIncome);
            PTaxSlab ptax = null;
            if (monthlyIncome >= maxMinimumIncome)
                ptax = professtionalTax.OrderByDescending(i => i.MinIncome).First();
            else
                ptax = professtionalTax.Find(x => monthlyIncome >= x.MinIncome && monthlyIncome <= x.MaxIncome);

            if (ptax != null)
            {
                ptaxAmount = ptax.TaxAmount;
            }

            return ptaxAmount;
        }

        //private string GetDeclarationDetailHTML(EmployeeDeclaration employeeDeclaration)
        //{
        //    string declarationHTML = string.Empty;
        //    if (employeeDeclaration.EmployeeCurrentRegime == 1)
        //    {
        //        if (employeeDeclaration.TaxSavingAlloance.FindAll(x => x.DeclaredValue > 0).Count == 0)
        //            employeeDeclaration.TaxSavingAlloance = new List<SalaryComponents>();

        //        decimal hraAmount = 0;
        //        var hraComponent = employeeDeclaration.SalaryComponentItems.Find(x => x.ComponentId == "HRA" && x.DeclaredValue > 0);
        //        if (hraComponent != null)
        //        {
        //            employeeDeclaration.TaxSavingAlloance.Add(hraComponent);
        //            hraAmount = employeeDeclaration.HRADeatils.HRAAmount;
        //        };
        //        var totalAllowTaxExemptAmount = ComponentTotalAmount(employeeDeclaration.TaxSavingAlloance) + hraAmount;
        //        if (totalAllowTaxExemptAmount > 0)
        //        {
        //            declarationHTML += "<table style=\"margin-top: 20px;\" width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\">";
        //            declarationHTML += "<thead>";
        //            declarationHTML += "<tr>";
        //            declarationHTML += "<th colspan = \"4\" style = \"padding-top:15px; padding-bottom: 10px; border-bottom: 1px solid #222; text-align: left;\">" + "Less: Allowance Tax Exemptions" + "</th>";
        //            declarationHTML += "</tr>";
        //            declarationHTML += "<tr>";
        //            declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span style=\"font-size:12px;\">" + "SECTION" + "</span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span style=\"font-size:12px;\">" + "ALLOWANCE" + " </span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span style=\"font-size:12px;\">" + "GROSS AMOUNT" + " </span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "<th style=\"text-align: rigt; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span style=\"font-size:12px;\">" + "DEDUCTABLE AMOUNT" + " </span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "</tr>";
        //            declarationHTML += "</thead>";
        //            declarationHTML += "<tbody>";
        //            employeeDeclaration.TaxSavingAlloance.ForEach(x =>
        //            {
        //                if (x.DeclaredValue > 0)
        //                {
        //                    declarationHTML += "<tr>";
        //                    declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //                    declarationHTML += "<span class=\"text-muted\">" + x.Section + "</span>";
        //                    declarationHTML += "</td>";
        //                    declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //                    declarationHTML += "<span class=\"text-muted\">" + x.ComponentId + " (" + x.ComponentFullName + ")" + "</span>";
        //                    declarationHTML += "</td>";
        //                    declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //                    declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", x.DeclaredValue) + "</span>";
        //                    declarationHTML += "</td>";
        //                    declarationHTML += "<td style=\"text-align: rigt; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //                    declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", x.DeclaredValue) + "</span>";
        //                    declarationHTML += "</td>";
        //                    declarationHTML += "</tr>";
        //                }
        //            });
        //            declarationHTML += "<tr>";
        //            declarationHTML += "<th colspan = \"3\" style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span class=\"text-muted\">" + "Total" + "</span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "<th style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", totalAllowTaxExemptAmount) + "</span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "</tr>";
        //            declarationHTML += "</tbody>";
        //            declarationHTML += "</table>";
        //        }

        //        decimal sec16TaxExemptAmount = employeeDeclaration.Section16TaxExemption.Sum(x => x.DeclaredValue);
        //        if (sec16TaxExemptAmount > 0)
        //        {
        //            declarationHTML += "<table style=\"margin-top: 20px;\" width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\">";
        //            declarationHTML += "<thead>";
        //            declarationHTML += "<tr>";
        //            declarationHTML += "<th colspan = \"3\" style = \"padding-top:15px; padding-bottom: 10px; border-bottom: 1px solid #222; text-align: left;\">" + "Less: Section 16 Tax Exemptions" + "</th>";
        //            declarationHTML += "</tr>";
        //            declarationHTML += "</thead>";
        //            declarationHTML += "<tbody>";
        //            employeeDeclaration.Section16TaxExemption.ForEach(x =>
        //            {
        //                declarationHTML += "<tr>";
        //                declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //                declarationHTML += "<span class=\"text-muted\">" + x.Section + "</span>";
        //                declarationHTML += "</td>";
        //                declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //                declarationHTML += "<span class=\"text-muted\">" + x.ComponentId + " (" + x.ComponentFullName + ")" + "</span>";
        //                declarationHTML += "</td>";
        //                declarationHTML += "<td style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //                declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", x.DeclaredValue) + "</span>";
        //                declarationHTML += "</td>";
        //                declarationHTML += "</tr>";
        //            });
        //            declarationHTML += "<tr>";
        //            declarationHTML += "<th colspan = \"2\"  style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span class=\"text-muted\">" + "Total" + "</span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "<th style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", sec16TaxExemptAmount) + "</span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "</tr>";
        //            declarationHTML += "</tbody>";
        //            declarationHTML += "</table>";
        //        }

        //        if (employeeDeclaration.SalaryDetail.GrossIncome - totalAllowTaxExemptAmount > 0)
        //        {
        //            declarationHTML += "<table width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\">";
        //            declarationHTML += "<tbody>";
        //            declarationHTML += "<tr>";
        //            declarationHTML += "<th style=\"text-align: left; padding-top: 5px; padding-bottom: 5px;\">";
        //            declarationHTML += "<span style=\"font-size: 12px;\">" + "Taxable Amount under Head Salaries" + " </span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "<th style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; \">";
        //            declarationHTML += "<span style=\"font-size: 12px;\">" + String.Format("{0:0.00}", employeeDeclaration.SalaryDetail.GrossIncome - totalAllowTaxExemptAmount - sec16TaxExemptAmount) + "</span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "</tr>";
        //            declarationHTML += "</tbody>";
        //            declarationHTML += "</table>";

        //            declarationHTML += "<table width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\">";
        //            declarationHTML += "<tbody>";
        //            declarationHTML += "<tr>";
        //            declarationHTML += "<th style=\"text-align: left;  padding-top: 5px; padding-bottom: 5px;\">";
        //            declarationHTML += "<span style=\"font-size: 12px;\">" + "Total Gross from all Heads" + " </span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "<th style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px;\">";
        //            declarationHTML += "<span style=\"font-size: 12px;\">" + String.Format("{0:0.00}", employeeDeclaration.SalaryDetail.GrossIncome - totalAllowTaxExemptAmount - sec16TaxExemptAmount) + "</span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "</tr>";
        //            declarationHTML += "</tbody>";
        //            declarationHTML += "</table>";
        //        }

        //        decimal totalSection80CExempAmount = 0;
        //        decimal totalOtherExemptAmount = 0;
        //        employeeDeclaration.Declarations.ForEach(x =>
        //        {
        //            if (x.DeclarationName == ApplicationConstants.OneAndHalfLakhsExemptions)
        //                totalSection80CExempAmount = x.TotalAmountDeclared;
        //            else if (x.DeclarationName == ApplicationConstants.OtherDeclarationName)
        //                totalOtherExemptAmount = x.TotalAmountDeclared;
        //        });

        //        if (totalSection80CExempAmount > 0)
        //        {
        //            declarationHTML += "<table style=\"margin-top: 20px;\" width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\">";
        //            declarationHTML += "<thead>";
        //            declarationHTML += "<tr>";
        //            declarationHTML += "<th colspan = \"4\" style = \"padding-top:15px; padding-bottom: 10px; border-bottom: 1px solid #222; text-align: left;\">" + "Less: 1.5 Lac Tax Exemption (Section 80C + Others)" + "</th>";
        //            declarationHTML += "</tr>";
        //            declarationHTML += "<tr>";
        //            declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span style=\"font-size:12px;\">" + "SECTION" + "</span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span style=\"font-size:12px;\">" + "ALLOWANCE" + " </span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span style=\"font-size:12px;\">" + "DECLARED AMOUNT" + " </span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "<th style=\"text-align: rigt; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span style=\"font-size:12px;\">" + "DEDUCTABLE AMOUNT" + " </span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "</tr>";
        //            declarationHTML += "</thead>";
        //            declarationHTML += "<tbody>";
        //            employeeDeclaration.ExemptionDeclaration.ForEach(x =>
        //            {
        //                if (x.DeclaredValue > 0)
        //                {
        //                    declarationHTML += "<tr>";
        //                    declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //                    declarationHTML += "<span class=\"text-muted\">" + x.Section + "</span>";
        //                    declarationHTML += "</td>";
        //                    declarationHTML += "<td  style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //                    declarationHTML += "<span class=\"text-muted\">" + x.ComponentId + " (" + x.ComponentFullName + ")" + "</span>";
        //                    declarationHTML += "</td>";
        //                    declarationHTML += "<td  style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //                    declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", x.DeclaredValue) + "</span>";
        //                    declarationHTML += "</td>";
        //                    declarationHTML += "<td style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //                    declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", x.DeclaredValue) + "</span>";
        //                    declarationHTML += "</td>";
        //                    declarationHTML += "</tr>";
        //                }
        //            });
        //            declarationHTML += "<tr>";
        //            declarationHTML += "<th colspan = \"3\" style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span class=\"text-muted\">" + "Total" + "</span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "<th style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", totalSection80CExempAmount) + "</span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "</tr>";
        //            declarationHTML += "</tbody>";
        //            declarationHTML += "</table>";
        //        }

        //        if (totalOtherExemptAmount > 0)
        //        {
        //            declarationHTML += "<table style=\"margin-top: 20px;\" width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\">";
        //            declarationHTML += "<thead>";
        //            declarationHTML += "<tr>";
        //            declarationHTML += "<th colspan = \"4\" style = \"padding-top:15px; padding-bottom: 10px; border-bottom: 1px solid #222; text-align: left;\">" + "Less: Other Tax Exemption" + "</th>";
        //            declarationHTML += "</tr>";
        //            declarationHTML += "<tr>";
        //            declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span style=\"font-size:12px;\">" + "SECTION" + "</span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span style=\"font-size:12px;\">" + "ALLOWANCE" + " </span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span style=\"font-size:12px;\">" + "DECLARED AMOUNT" + " </span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "<th style=\"text-align: end; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span style=\"font-size:12px;\">" + "DEDUCTABLE AMOUNT" + " </span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "</tr>";
        //            declarationHTML += "</thead>";
        //            declarationHTML += "<tbody>";
        //            employeeDeclaration.OtherDeclaration.ForEach(x =>
        //            {
        //                if (x.DeclaredValue > 0)
        //                {
        //                    declarationHTML += "<tr>";
        //                    declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //                    declarationHTML += "<span class=\"text-muted\">" + x.Section + "</span>";
        //                    declarationHTML += "</td>";
        //                    declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //                    declarationHTML += "<span class=\"text-muted\">" + x.ComponentId + " (" + x.ComponentFullName + ")" + "</span>";
        //                    declarationHTML += "</td>";
        //                    declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //                    declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", x.DeclaredValue) + "</span>";
        //                    declarationHTML += "</td>";
        //                    declarationHTML += "<td style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //                    declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", x.DeclaredValue) + "</span>";
        //                    declarationHTML += "</td>";
        //                    declarationHTML += "</tr>";
        //                }
        //            });
        //            declarationHTML += "<tr>";
        //            declarationHTML += "<th colspan = \"3\" style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span class=\"text-muted\">" + "Total" + "</span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "<th style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", totalOtherExemptAmount) + "</span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "</tr>";
        //            declarationHTML += "</tbody>";
        //            declarationHTML += "</table>";
        //        }

        //        declarationHTML += "<table style=\"margin-top: 20px;\" width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\">";
        //        declarationHTML += "<tbody>";
        //        declarationHTML += "<tr>";
        //        declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //        declarationHTML += "<span style=\"font-size:12px;\">" + "HRA Applied" + " </span>";
        //        declarationHTML += "</th>";
        //        declarationHTML += "<th style=\"text-align: right; border-bottom: 1px solid #d9d9d9;\">";
        //        declarationHTML += "<span style=\"font-size:12px;\">" + "AMOUNT DECLARED" + " </span>";
        //        declarationHTML += "</th>";
        //        declarationHTML += "</tr>";
        //        declarationHTML += "<tr>";
        //        declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //        declarationHTML += "<span style=\"font-size:12px;\">" + "Actual HRA [Per Month]" + " </span>";
        //        declarationHTML += "</td>";
        //        declarationHTML += "<td style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //        declarationHTML += "<span style=\"font-size:12px;\">" + String.Format("{0:0.00}", employeeDeclaration.HRADeatils.HRAAmount) + "</span>";
        //        declarationHTML += "</td>";
        //        declarationHTML += "</tr>";
        //        declarationHTML += "<tr>";
        //        declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //        declarationHTML += "<span class=\"text-muted\">" + "Total" + "</span>";
        //        declarationHTML += "</th>";
        //        declarationHTML += "<th style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //        declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", (employeeDeclaration.HRADeatils.HRAAmount * 12)) + "</span>";
        //        declarationHTML += "</th>";
        //        declarationHTML += "</tr>";
        //        declarationHTML += "</tbody>";
        //        declarationHTML += "</table>";

        //        decimal totalTaxableAmount = employeeDeclaration.SalaryDetail.GrossIncome - totalAllowTaxExemptAmount - sec16TaxExemptAmount - totalOtherExemptAmount - totalSection80CExempAmount - (employeeDeclaration.HRADeatils.HRAAmount * 12);
        //        declarationHTML += "<table style=\"margin-top: 20px;\" width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\">";
        //        declarationHTML += "<tbody>";
        //        declarationHTML += "<tr>";
        //        declarationHTML += "<th style=\"text-align: left; padding-top: 5px; padding-bottom: 5px; \">";
        //        declarationHTML += "<span style=\"font-size: 12px;\">" + "Total Taxable Amount" + " </span>";
        //        declarationHTML += "</th>";
        //        declarationHTML += "<th style=\"text-align: right; padding-top: 5px; padding-bottom: 5px; \">";
        //        declarationHTML += "<span style=\"font-size: 12px;\">" + String.Format("{0:0.00}", totalTaxableAmount) + " </span>";
        //        declarationHTML += "</th>";
        //        declarationHTML += "</tr>";
        //        declarationHTML += "</tbody>";
        //        declarationHTML += "</table>";

        //        declarationHTML += "<table width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\">";
        //        declarationHTML += "<tbody>";
        //        declarationHTML += "<tr>";
        //        declarationHTML += "<th style=\"text-align: left; padding-top: 5px; padding-bottom: 5px; \">";
        //        declarationHTML += "<span style=\"font-size: 12px;\">" + "Net taxable income is" + " </span>";
        //        declarationHTML += "</th>";
        //        declarationHTML += "<th style=\"text-align: right; padding-top: 5px; padding-bottom: 5px; \">";
        //        declarationHTML += "<span style=\"font-size: 12px;\">" + String.Format("{0:0.00}", totalTaxableAmount) + " </span>";
        //        declarationHTML += "</th>";
        //        declarationHTML += "</tr>";
        //        declarationHTML += "</tbody>";
        //        declarationHTML += "</table>";

        //        if (employeeDeclaration.IncomeTaxSlab.Count > 0 || employeeDeclaration.NewRegimIncomeTaxSlab.Count > 0)
        //        {
        //            declarationHTML += "<h5 style=\"font-weight: bold; color: #222; padding-bottom: 0; margin-bottom: 0;\">" + "Tax Calculation" + "</h5>";
        //            declarationHTML += "<table width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\">";
        //            declarationHTML += "<thead>";
        //            declarationHTML += "<tr>";
        //            declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span class=\"text-muted\">" + "TAXABLE INCOME SLAB" + "</span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "<th style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //            declarationHTML += "<span class=\"text-muted\">" + "TAX AMOUNT" + "</span>";
        //            declarationHTML += "</th>";
        //            declarationHTML += "</tr>";
        //            declarationHTML += "</thead>";
        //            declarationHTML += "<tbody>";
        //            if (employeeDeclaration.EmployeeCurrentRegime == 1)
        //            {
        //                foreach (var item in employeeDeclaration.IncomeTaxSlab.OrderByDescending(x => x.Key))
        //                {
        //                    declarationHTML += "<tr>";
        //                    declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //                    declarationHTML += "<span class=\"text-muted\">" + item.Value.Description + "</span>";
        //                    declarationHTML += "</td>";
        //                    declarationHTML += "<td style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //                    declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", item.Value.Value) + "</span>";
        //                    declarationHTML += "</td>";
        //                    declarationHTML += "</tr>";
        //                }
        //            }
        //            else
        //            {
        //                foreach (var item in employeeDeclaration.NewRegimIncomeTaxSlab.OrderByDescending(x => x.Key))
        //                {
        //                    declarationHTML += "<tr>";
        //                    declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //                    declarationHTML += "<span class=\"text-muted\">" + item.Value.Description + "</span>";
        //                    declarationHTML += "</td>";
        //                    declarationHTML += "<td style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
        //                    declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", item.Value.Value) + "</span>";
        //                    declarationHTML += "</td>";
        //                    declarationHTML += "</tr>";
        //                }
        //            };
        //            declarationHTML += "</tbody>";
        //            declarationHTML += "</table>";
        //        }
        //    }
        //    return declarationHTML;
        //}

        private decimal ComponentTotalAmount(List<SalaryComponents> salaryComponents)
        {
            var components = salaryComponents.FindAll(x => x.ComponentId != "HRA");
            decimal amount = 0;
            components.ForEach(x =>
            {
                if (x.DeclaredValue > 0)
                    amount = amount + x.DeclaredValue;
            });
            return amount;
        }
    }
}
