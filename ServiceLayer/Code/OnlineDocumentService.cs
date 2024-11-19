using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using Bot.CoreBottomHalf.CommonModal.Enums;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using EMailService.Modal;
using ems_CommonUtility.MicroserviceHttpRequest;
using ems_CommonUtility.Model;
using FileManagerService.Model;
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace ServiceLayer.Code
{
    public class OnlineDocumentService : IOnlineDocumentService
    {
        private readonly IDb db;
        private readonly IFileService _fileService;
        private readonly CommonFilterService _commonFilterService;
        private readonly CurrentSession _currentSession;
        private readonly IBillService _billService;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly ILogger<OnlineDocumentService> _logger;
        private readonly ITimezoneConverter _timezoneConverter;
        private readonly MicroserviceRegistry _microserviceRegistry;
        private readonly RequestMicroservice _requestMicroservice;

        public OnlineDocumentService(IDb db,
            IFileService fileService,
            ILogger<OnlineDocumentService> logger,
            CommonFilterService commonFilterService,
            CurrentSession currentSession,
            FileLocationDetail fileLocationDetail,
            IBillService billService,
            ITimezoneConverter timezoneConverter,
            IOptions<MicroserviceRegistry> microserviceOptions,
            RequestMicroservice requestMicroservice)
        {
            this.db = db;
            _logger = logger;
            _currentSession = currentSession;
            _fileService = fileService;
            _commonFilterService = commonFilterService;
            _billService = billService;
            _fileLocationDetail = fileLocationDetail;
            _timezoneConverter = timezoneConverter;
            _microserviceRegistry = microserviceOptions.Value;
            _requestMicroservice = requestMicroservice;
        }

        public string InsertOnlineDocument(CreatePageModel createPageModel)
        {
            var result = this.db.Execute<string>(Procedures.OnlineDocument_InsUpd, new
            {
                Title = createPageModel.OnlineDocumentModel.Title,
                Description = createPageModel.OnlineDocumentModel.Description,
                DocumentId = createPageModel.OnlineDocumentModel.DocumentId,
                Mobile = createPageModel.Mobile,
                Email = createPageModel.Email,
                DocPath = createPageModel.OnlineDocumentModel.DocPath
            }, true);

            return result;
        }

        public List<OnlineDocumentModel> CreateDocument(CreatePageModel createPageModel)
        {
            InsertOnlineDocument(createPageModel);

            return _commonFilterService.GetResult<OnlineDocumentModel>(new FilterModel
            {
                SearchString = createPageModel.SearchString,
                PageIndex = createPageModel.PageIndex,
                PageSize = createPageModel.PageSize,
                SortBy = createPageModel.SortBy
            }, Procedures.OnlineDocument_Get);
        }

        public DocumentWithFileModel GetOnlineDocumentsWithFiles(FilterModel filterModel)
        {
            DocumentWithFileModel documentWithFileModel = new DocumentWithFileModel();
            var Result = this.db.GetDataSet(Procedures.OnlineDocument_With_Files_Get, new
            {
                SearchString = filterModel.SearchString,
                PageIndex = filterModel.PageIndex,
                PageSize = filterModel.PageSize,
                SortBy = filterModel.SortBy,
            });

            if (Result.Tables.Count == 3)
            {
                documentWithFileModel.onlineDocumentModel = Converter.ToList<OnlineDocumentModel>(Result.Tables[0]);
                documentWithFileModel.files = Converter.ToList<Files>(Result.Tables[1]);
                documentWithFileModel.TotalRecord = Convert.ToInt64(Result.Tables[2].Rows[0]["TotalRecord"].ToString());
            }
            return documentWithFileModel;
        }

        public string DeleteFilesService(List<Files> fileDetails)
        {

            string Result = "Fail";
            if (fileDetails != null && fileDetails.Count > 0)
            {
                var deletingFiles = new List<DocumentFile>();
                DocumentFile documentFile = default;
                Parallel.ForEach(fileDetails, item =>
                {
                    documentFile = new DocumentFile();
                    documentFile.DocumentId = item.DocumentId;
                    documentFile.FileUid = item.FileUid;
                    deletingFiles.Add(documentFile);
                });


                DataSet documentFileSet = Converter.ToDataSet<DocumentFile>(deletingFiles);
                DataSet FileSet = db.GetDataSet(Procedures.OnlieDocument_GetFiles, new
                {
                    DocumentId = fileDetails.FirstOrDefault().DocumentId,
                    FileUid = fileDetails.Select(x => x.FileUid.ToString()).Aggregate((x, y) => x + "," + y),
                });

                if (FileSet.Tables.Count > 0)
                {
                    // db.InsertUpdateBatchRecord("sp_OnlieDocument_Del_Multi", documentFileSet.Tables[0]);
                    db.BulkExecuteAsync<DocumentFile>(Procedures.OnlieDocument_Del_Multi, deletingFiles);
                    List<Files> files = Converter.ToList<Files>(FileSet.Tables[0]);
                    _fileService.DeleteFiles(files);
                    Result = "Success";
                }
            }
            return Result;
        }

        public async Task<string> EditCurrentFileService(Files editFile)
        {
            string Result = "Fail";
            if (editFile != null)
            {
                editFile.BillTypeId = 1;
                editFile.UserId = 1;

                int rowsAffected = await db.BulkExecuteAsync<Files>(Procedures.Files_InsUpd, new List<Files>() { editFile });
                Result = "Fail";
                if (rowsAffected > 0)
                    Result = "Success";
            }
            return Result;
        }

        public Bills GetBillData()
        {
            Bills bill = db.Get<Bills>(Procedures.Billdata_Get, new { BillTypeUid = 1 });
            return bill;
        }

        public DataSet GetFilesAndFolderByIdService(string Type, string Uid, FilterModel filterModel)
        {
            var Result = this.db.GetDataSet(Procedures.Billdetail_Filter, new
            {
                Type = Type,
                Uid = Uid,
                searchString = filterModel.SearchString,
                sortBy = filterModel.SortBy,
                pageIndex = filterModel.PageIndex,
                pageSize = filterModel.PageSize,
            });

            if (Result.Tables.Count == 3)
            {
                Result.Tables[0].TableName = "Files";
                Result.Tables[1].TableName = "Employee";
                Result.Tables[2].TableName = "EmployeesList";
            }
            else
            {
                Result = null;
            }
            return Result;
        }

        public List<Files> EditFileService(Files files)
        {
            //List<Files> filses = new List<Files>();
            //FileDetail fileDetail = new FileDetail();
            //DbParam[] dbParams = new DbParam[]
            //{
            //    new DbParam(fileDetail.FileExtension, typeof(string), "_FileExtension"),
            //    new DbParam(fileDetail.FileName, typeof(string), "_FileName"),
            //    new DbParam(fileDetail.FileId, typeof(int), "_FileDetailId")
            //};

            //var Result = this.db.ExecuteNonQuery("sp_Files_GetById", dbParams, true);
            //files = Converter.ToList<Files>(Result.Tables[0]);
            //return files;
            return null;
        }

        public FileDetail ReGenerateService(GenerateBillFileDetail generateBillFileDetail)
        {
            FileDetail fileDetail = new FileDetail
            {
                ClientId = generateBillFileDetail.ClientId,
                EmployeeId = generateBillFileDetail.EmployeeId,
                FileExtension = generateBillFileDetail.FileExtension,
                FileId = generateBillFileDetail.FileId,
                FileName = generateBillFileDetail.FileName,
                FilePath = generateBillFileDetail.FilePath
            };

            try
            {
                BillDetail billDetail = null;
                FileDetail currentFileDetail = null;
                Organization receiverOrganization = null;
                Organization organization = null;
                BankDetail senderBankDetail = null;
                List<DailyTimesheetDetail> currentMonthTimesheet = new List<DailyTimesheetDetail>();

                int monthNumber = 0;
                if (DateTime.TryParseExact(generateBillFileDetail.MonthName, "MMMM", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                    monthNumber = parsedDate.Month;

                DateTime firstDateOfMonth = new DateTime(generateBillFileDetail.ForYear, monthNumber, 1);
                DateTime lastDateOfMonth = firstDateOfMonth.AddMonths(1).AddDays(-1);

                var Result = this.db.GetDataSet(Procedures.ExistingBill_GetById, new
                {
                    AdminId = _currentSession.CurrentUserDetail.UserId,
                    EmployeeId = fileDetail.EmployeeId,
                    ClientId = fileDetail.ClientId,
                    FileId = fileDetail.FileId,
                    UserTypeId = (int)UserType.Employee,
                    StartDate = firstDateOfMonth,
                    EndDate = lastDateOfMonth
                });

                if (Result.Tables.Count == 6)
                {
                    billDetail = Converter.ToType<BillDetail>(Result.Tables[0]);
                    currentFileDetail = Converter.ToType<FileDetail>(Result.Tables[1]);
                    receiverOrganization = Converter.ToType<Organization>(Result.Tables[2]);
                    organization = Converter.ToType<Organization>(Result.Tables[3]);
                    senderBankDetail = Converter.ToType<BankDetail>(Result.Tables[5]);
                    if (Result.Tables[4].Rows.Count > 0)
                    {
                        var timesheetDetail = Converter.ToList<TimesheetDetail>(Result.Tables[4]);
                        timesheetDetail.ForEach(x =>
                        {
                            currentMonthTimesheet.AddRange(JsonConvert.DeserializeObject<List<DailyTimesheetDetail>>(x.TimesheetWeeklyJson));
                        });
                        var startDate = _timezoneConverter.ToSpecificTimezoneDateTime(_currentSession.TimeZone, firstDateOfMonth);
                        var endDate = _timezoneConverter.ToSpecificTimezoneDateTime(_currentSession.TimeZone, lastDateOfMonth);

                        currentMonthTimesheet = currentMonthTimesheet.Where(x => _timezoneConverter.ToSpecificTimezoneDateTime(_currentSession.TimeZone, x.PresentDate) >= startDate
                                                                          && _timezoneConverter.ToSpecificTimezoneDateTime(_currentSession.TimeZone, x.PresentDate) <= endDate).ToList();
                    }
                }
                else
                {
                    throw new HiringBellException("Unable to get file detail.");
                }

                string Extension = Utility.GetExtension(currentFileDetail.FileExtension, "pdf");
                if (Extension == null)
                {
                    fileDetail.FileExtension = "pdf,docx";
                    Extension = "pdf";

                }

                string filePath = Path.Combine(Directory.GetCurrentDirectory(), currentFileDetail.FilePath, $"{currentFileDetail.FileName}.{Extension}");
                if (!File.Exists(filePath))
                {
                    var billmonth = billDetail.BillYear.ToString() + billDetail.BillForMonth.ToString().PadLeft(2, '0') + billDetail.BillUpdatedOn.ToString("dd");
                    DateTime billingForMonth = DateTime.ParseExact(billmonth, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);

                    PdfModal pdfModal = new PdfModal
                    {
                        header = null,
                        billingMonth = billingForMonth,
                        billNo = billDetail.BillNo,
                        billId = billDetail.BillDetailUid,
                        dateOfBilling = billDetail.BillUpdatedOn,
                        cGST = billDetail.CGST,
                        sGST = billDetail.SGST,
                        iGST = billDetail.IGST,
                        cGstAmount = Converter.TwoDecimalValue((decimal)(billDetail.SGST * billDetail.PaidAmount) / 100),
                        sGstAmount = Converter.TwoDecimalValue((decimal)(billDetail.CGST * billDetail.PaidAmount) / 100),
                        iGstAmount = Converter.TwoDecimalValue((decimal)(billDetail.IGST * billDetail.PaidAmount) / 100),
                        workingDay = billDetail.NoOfDays - (int)billDetail.NoOfDaysAbsent,
                        packageAmount = billDetail.PaidAmount,
                        grandTotalAmount = Converter.TwoDecimalValue(billDetail.PaidAmount + (billDetail.PaidAmount * (billDetail.CGST + billDetail.SGST + billDetail.IGST)) / 100),
                        senderCompanyName = organization.CompanyName,
                        receiverFirstAddress = receiverOrganization.FirstAddress,
                        receiverCompanyId = receiverOrganization.ClientId,
                        receiverCompanyName = receiverOrganization.ClientName,
                        senderId = organization.CompanyId,
                        developerName = billDetail.DeveloperName,
                        receiverSecondAddress = receiverOrganization.SecondAddress,
                        receiverThirdAddress = receiverOrganization.ThirdAddress,
                        senderFirstAddress = receiverOrganization.FirstAddress,
                        daysAbsent = billDetail.NoOfDaysAbsent,
                        senderSecondAddress = organization.SecondAddress,
                        senderPrimaryContactNo = organization.PrimaryPhoneNo,
                        senderEmail = organization.Email,
                        senderGSTNo = organization.GSTNo,
                        receiverGSTNo = receiverOrganization.GSTNo,
                        receiverPrimaryContactNo = receiverOrganization.PrimaryPhoneNo,
                        receiverEmail = receiverOrganization.Email,
                        UpdateSeqNo = billDetail.UpdateSeqNo,
                        ClientId = receiverOrganization.ClientId,
                        EmployeeId = billDetail.EmployeeUid,
                        FileId = currentFileDetail.FileId,
                        FileName = currentFileDetail.FileName,
                        FilePath = currentFileDetail.FilePath,
                        LogoPath = currentFileDetail.LogoPath,
                        DiskFilePath = currentFileDetail.DiskFilePath,
                        FileExtension = currentFileDetail.FileExtension,
                        StatusId = billDetail.BillStatusId,
                        PaidOn = billDetail.PaidOn,
                        Status = (int)currentFileDetail.StatusId,
                        GeneratedBillNo = billDetail.BillNo,
                        UpdatedOn = currentFileDetail.UpdatedOn,
                        Notes = null
                    };

                    string MonthName = pdfModal.billingMonth.ToString("MMM_yyyy");
                    string FolderLocation = Path.Combine(_fileLocationDetail.Location, _fileLocationDetail.BillsPath, MonthName);
                    string folderPath = Path.Combine(Directory.GetCurrentDirectory(), FolderLocation);
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    string destinationFilePath = Path.Combine(
                        folderPath,
                        pdfModal.developerName.Replace(" ", "_") + "_" +
                        pdfModal.billingMonth.ToString("MMM_yyyy") + "_" +
                        pdfModal.billNo + $".{ApplicationConstants.Excel}");

                    if (File.Exists(destinationFilePath))
                        File.Delete(destinationFilePath);

                    var timesheetData = (from n in currentMonthTimesheet
                                         orderby n.PresentDate ascending
                                         select new TimesheetModel
                                         {
                                             Date = n.PresentDate.ToString("dd MMM yyyy"),
                                             ResourceName = pdfModal.developerName,
                                             StartTime = "10:00 AM",
                                             EndTime = "06:00 PM",
                                             TotalHrs = 9,
                                             Comments = n.UserComments,
                                             Status = "Approved"
                                         }
                    ).ToList<TimesheetModel>();

                    var timeSheetDataSet = Converter.ToDataSet<TimesheetModel>(timesheetData);
                    //_excelWriter.ToExcel(timeSheetDataSet.Tables[0], destinationFilePath, pdfModal.billingMonth.ToString("MMM_yyyy"));
                    generateExce(timeSheetDataSet, destinationFilePath);
                    BillGenerationModal billModal = new BillGenerationModal
                    {
                        PdfModal = pdfModal,
                        Sender = organization,
                        Receiver = receiverOrganization,
                        SenderBankDetail = senderBankDetail
                    };
                    _billService.CreateFiles(billModal);
                }

                return fileDetail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
        }

        private void generateExce(DataSet dataSet, string filePath)
        {
            // Create a new Excel document
            using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook))
            {
                // Add a WorkbookPart to the document
                WorkbookPart workbookPart = spreadsheetDocument.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                // Add a WorksheetPart to the WorkbookPart
                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());

                // Add Sheets to the Workbook
                Sheets sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild(new Sheets());
                Sheet sheet = new Sheet() { Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "EmployeeTimesheet" };
                sheets.Append(sheet);

                // Get the sheetData
                SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                // Add column headers
                AddColumnHeaders(dataSet.Tables[0], sheetData);

                // Add data rows
                AddDataRows(dataSet.Tables[0], sheetData);

                // Save the changes to the spreadsheet document
                worksheetPart.Worksheet.Save();
            }
        }

        private void AddColumnHeaders(DataTable dataTable, SheetData sheetData)
        {
            Row headerRow = new Row();

            foreach (DataColumn column in dataTable.Columns)
            {
                headerRow.AppendChild(CreateCell(column.ColumnName));
            }

            sheetData.AppendChild(headerRow);
        }

        private void AddDataRows(DataTable dataTable, SheetData sheetData)
        {
            foreach (DataRow row in dataTable.Rows)
            {
                Row dataRow = new Row() { RowIndex = (uint)(sheetData.Elements<Row>().Count() + 1) }; // Set the row index manually

                foreach (var item in row.ItemArray)
                {
                    dataRow.AppendChild(CreateCell(item.ToString()));
                }

                // Check if the current row is a weekend or holiday
                if (IsWeekend(row) || IsHoliday(row))
                {
                    // Merge cells for Status, Description, Hours, and Minute columns
                    MergeCells(sheetData, row.Table.Columns.Count, dataRow.RowIndex);

                    // Add "Weekend" or "Holiday" text to the merged cell
                    Cell cell = CreateCell(IsWeekend(row) ? "Weekend" : "Holiday");
                    cell.CellReference = $"A{dataRow.RowIndex}";
                    dataRow.InsertAt(cell, 0);
                }

                sheetData.AppendChild(dataRow);
            }
        }

        private bool IsWeekend(DataRow row)
        {
            if (row.Table.Columns.Contains("Date"))
            {
                DateTime date;
                if (DateTime.TryParse(row["Date"].ToString(), out date))
                {
                    return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
                }
            }
            // If the "Date" column does not exist or cannot be parsed, assume it's not a weekend
            return false;
        }

        private bool IsHoliday(DataRow row)
        {
            // Add logic to check if the date is a holiday
            // For demonstration, we'll assume there are no holidays in this example
            return false;
        }

        private string GetColumnName(int columnIndex)
        {
            int dividend = columnIndex;
            string columnName = String.Empty;
            int modulo;

            while (dividend > 0)
            {
                modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
                dividend = (int)((dividend - modulo) / 26);
            }

            return columnName;
        }

        private void MergeCells(SheetData sheetData, int columnCount, uint rowIndex)
        {
            if (columnCount > 1)
            {
                MergeCells mergeCells = sheetData.Elements<MergeCells>().FirstOrDefault();
                if (mergeCells == null)
                {
                    mergeCells = new MergeCells();
                    sheetData.InsertAfter(mergeCells, sheetData.Elements<SheetData>().First());
                }

                // Merge cells for all columns in the row
                string startCell = GetColumnName(1) + rowIndex;
                string endCell = GetColumnName(columnCount) + rowIndex;
                MergeCell mergeCell = new MergeCell() { Reference = new StringValue(startCell + ":" + endCell) };
                mergeCells.Append(mergeCell);
            }
        }

        private Cell CreateCell(string text)
        {
            Cell cell = new Cell(new InlineString(new Text(text)));
            cell.DataType = CellValues.InlineString;
            return cell;
        }

        public string DeleteDataService(string Uid)
        {
            throw new NotImplementedException();
        }

        public string UpdateRecord(FileDetail fileDetail, long Uid)
        {
            string status = string.Empty;
            TimeZoneInfo istTimeZome = TZConvert.GetTimeZoneInfo("India Standard Time");
            fileDetail.UpdatedOn = TimeZoneInfo.ConvertTimeFromUtc(fileDetail.UpdatedOn, istTimeZome);
            status = this.db.Execute<string>(Procedures.FileDetail_PatchRecord, new
            {
                FileId = Uid,
                StatusId = fileDetail.StatusId,
                UpdatedOn = fileDetail.UpdatedOn,
                AdminId = _currentSession.CurrentUserDetail.UserId,
                Notes = fileDetail.Notes,
            }, true);

            return status;
        }

        public async Task<string> UploadDocumentRecord(List<ProfessionalUserDetail> uploadDocuments)
        {
            string result = "Fail to insert or update";
            var rowsAffected = await this.db.BulkExecuteAsync(Procedures.ProfessionalCandidates_InsUpdate, uploadDocuments, true);
            if (rowsAffected > 0)
                result = "Uploaded success";
            return result;

        }

        public DataSet GetProfessionalCandidatesRecords(FilterModel filterModel)
        {
            DataSet Result = this.db.GetDataSet(Procedures.Professionalcandidates_Filter, new
            {
                SearchString = filterModel.SearchString,
                PageIndex = filterModel.PageIndex,
                PageSize = filterModel.PageSize,
                SortBy = filterModel.SortBy,
            });

            return Result;
        }

        public async Task<string> UploadDocumentDetail(CreatePageModel createPageModel, IFormFileCollection FileCollection, List<Files> fileDetail)
        {
            string Result = "Fail";
            var NewDocId = InsertOnlineDocument(createPageModel);
            if (!string.IsNullOrEmpty(NewDocId))
            {
                if (FileCollection.Count > 0 && fileDetail.Count > 0)
                {
                    string FolderPath = Path.Combine(_currentSession.CompanyCode,
                        createPageModel.OnlineDocumentModel.Title.Replace(" ", "_"));
                    //List<Files> files = _fileService.SaveFile(FolderPath, fileDetail, FileCollection, NewDocId);

                    var url = $"{_microserviceRegistry.SaveApplicationFile}";
                    FileFolderDetail fileFolderDetail = new FileFolderDetail
                    {
                        FolderPath = FolderPath,
                        OldFileName = null,
                        ServiceName = LocalConstants.EmstumFileService
                    };

                    var microserviceRequest = MicroserviceRequest.Builder(url);
                    microserviceRequest
                    .SetFiles(FileCollection)
                    .SetPayload(fileFolderDetail)
                    .SetConnectionString(_currentSession.LocalConnectionString)
                    .SetCompanyCode(_currentSession.CompanyCode)
                    .SetToken(_currentSession.Authorization);

                    var files = await _requestMicroservice.UploadFile<List<Files>>(microserviceRequest);


                    if (files != null && files.Count > 0)
                    {
                        Parallel.ForEach(files, item =>
                        {
                            item.Status = "Pending";
                            item.BillTypeId = 1;
                            item.UserId = 1;
                            item.PaidOn = null;
                        });

                        int rowsAffected = await db.BulkExecuteAsync<Files>(Procedures.Files_InsUpd, files);
                        Result = "Success";
                        if (rowsAffected == 0)
                            Result = "Fail";
                    }
                }
            }
            return Result;
        }

        public async Task<DataSet> UploadFilesOrDocuments(List<Files> fileDetail, IFormFileCollection FileCollection)
        {
            DataSet Result = null;
            Files file = fileDetail.FirstOrDefault();
            try
            {
                if (FileCollection.Count > 0 && fileDetail.Count > 0)
                {
                    var ownerPath = string.Empty;
                    string userEmail = null;
                    if (file.UserTypeId == UserType.Employee)
                    {
                        var employee = this.db.Get<Employee>(Procedures.Employees_ById, new
                        {
                            EmployeeId = file.UserId,
                            IsActive = 1,
                        });

                        userEmail = employee.Email;
                        ownerPath = Path.Combine(_currentSession.CompanyCode, _fileLocationDetail.User, file.FilePath);
                    }
                    else if (file.UserTypeId == UserType.Client)
                    {
                        //var userDetail = this.db.Get<UserDetail>("sp_UserDetail_ById", new { userId = file.UserId });
                        //userEmail = userDetail.EmailId;
                        userEmail = file.Email;
                        ownerPath = Path.Combine(_currentSession.CompanyCode, _fileLocationDetail.User, file.FilePath);
                    }

                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        //fileDetail.ForEach(item =>
                        //{
                        //    if (string.IsNullOrEmpty(item.ParentFolder))
                        //    {
                        //        item.ParentFolder = string.Empty;  // Path.Combine(ApplicationConstants.DocumentRootPath, ApplicationConstants.User);
                        //    }
                        //    else
                        //    {
                        //        item.Email = userEmail;
                        //    }
                        //});

                        // ---- save document in by another microservice call ------
                        string url = $"{_microserviceRegistry.SaveApplicationFile}";
                        FileFolderDetail fileFolderDetail = new FileFolderDetail
                        {
                            FolderPath = ownerPath,
                            OldFileName = new List<string> { $"{file.UserId}" },
                            ServiceName = LocalConstants.EmstumFileService
                        };

                        var microserviceRequest = MicroserviceRequest.Builder(url);
                        microserviceRequest
                        .SetFiles(FileCollection)
                        .SetPayload(fileFolderDetail)
                        .SetConnectionString(_currentSession.LocalConnectionString)
                        .SetCompanyCode(_currentSession.CompanyCode)
                        .SetToken(_currentSession.Authorization);

                        _logger.LogInformation("Calling to microservice..................");
                        List<Files> files = await _requestMicroservice.UploadFile<List<Files>>(microserviceRequest);

                        _logger.LogInformation("File server successfully");
                        if (files != null && files.Count > 0)
                        {
                            Result = InsertFileDetails(files, file.UserId);
                        }
                    }
                    else
                    {
                        throw new Exception("Invalid user detail.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                throw;
            }

            return Result;
        }

        public DataSet GetDocumentResultById(Files fileDetail)
        {
            DataSet Result = null;
            if (fileDetail != null)
            {
                Result = this.db.GetDataSet(Procedures.Document_Filedetail_Get, new
                {
                    OwnerId = fileDetail.UserId,
                    UserTypeId = (int)fileDetail.UserTypeId,
                });
            }

            return Result;
        }

        public DataSet InsertFileDetails(List<Files> fileDetail, long fileOwnerId)
        {
            var fileInfo = (from n in fileDetail.AsEnumerable()
                            select new
                            {
                                FileId = n.FileUid,
                                FileOwnerId = fileOwnerId,
                                FileName = n.FileName,
                                FilePath = n.FilePath,
                                ParentFolder = string.IsNullOrEmpty(n.ParentFolder) ? string.Empty : n.ParentFolder,
                                FileExtension = n.FileExtension,
                                StatusId = 0,
                                UserTypeId = (int)UserType.Employee,
                                AdminId = 1
                            });

            //DataTable table = Converter.ToDataTable(fileInfo);
            //var dataSet = new DataSet();
            //dataSet.Tables.Add(table);

            return this.db.GetDataSet(ApplicationConstants.InserUserFileDetail, new { InsertFileJsonData = JsonConvert.SerializeObject(fileInfo) });
        }
    }
}
