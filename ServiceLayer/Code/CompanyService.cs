using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.Enums;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using EMailService.Modal;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class CompanyService : ICompanyService
    {
        private readonly IDb _db;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly IFileService _fileService;
        private readonly CurrentSession _currentSession;
        public CompanyService(IDb db, FileLocationDetail fileLocationDetail, IFileService fileService, CurrentSession currentSession)
        {
            _db = db;
            _fileLocationDetail = fileLocationDetail;
            _fileService = fileService;
            _currentSession = currentSession;
        }
        public List<OrganizationDetail> GetAllCompany()
        {
            var result = _db.GetList<OrganizationDetail>(Procedures.Company_Get, false);
            return result;
        }

        public List<OrganizationDetail> UpdateCompanyGroup(OrganizationDetail companyGroup, int companyId)
        {
            if (companyId <= 0)
                throw new HiringBellException("Invalid compnay id. Unable to update detail.");

            OrganizationDetail companyGrp = _db.Get<OrganizationDetail>(Procedures.Company_GetById, new { CompanyId = companyId });
            if (companyGrp == null)
                throw new HiringBellException("Compnay detail not found");

            companyGrp.Email = companyGroup.Email;
            companyGrp.InCorporationDate = companyGroup.InCorporationDate;
            companyGrp.CompanyDetail = companyGroup.CompanyDetail;
            companyGrp.CompanyName = companyGroup.CompanyName;

            var value = _db.Execute<OrganizationDetail>(Procedures.Company_Intupd, companyGrp, true);
            if (string.IsNullOrEmpty(value))
                throw new HiringBellException("Fail to insert company group.");

            var companies = this.GetAllCompany();
            // _cacheManager.ReLoad(CacheTable.Company, Converter.ToDataTable<OrganizationDetail>(companies));
            return companies;
        }

        public List<OrganizationDetail> AddCompanyGroup(OrganizationDetail companyGroup)
        {
            ValidateCompanyGroup(companyGroup);

            List<OrganizationDetail> companyGrp = null;
            companyGrp = _db.GetList<OrganizationDetail>(Procedures.Company_Get, false);
            OrganizationDetail result = companyGrp.Find(x => x.CompanyName == companyGroup.CompanyName);
            if (result != null)
                throw new HiringBellException("Company Already exist.");

            //companyGroup.OrganizationId = _currentSession.CurrentUserDetail.OrganizationId;
            result = companyGroup;
            companyGrp.Add(result);

            var value = _db.Execute<OrganizationDetail>(Procedures.Company_Intupd, result, true);
            if (string.IsNullOrEmpty(value))
                throw new HiringBellException("Fail to insert company group.");

            companyGrp = _db.GetList<OrganizationDetail>(Procedures.Company_Get, false);
            // _cacheManager.ReLoad(CacheTable.Company, Converter.ToDataTable<OrganizationDetail>(companyGrp));
            return companyGrp;
        }

        private void ValidateCompanyGroup(OrganizationDetail companyGroup)
        {
            if (companyGroup.OrganizationId == 0)
                throw new HiringBellException("Invalid organization id.");

            if (string.IsNullOrEmpty(companyGroup.CompanyName))
                throw HiringBellException.ThrowBadRequest("Company name is null or empty");

            if (string.IsNullOrEmpty(companyGroup.CompanyDetail))
                throw HiringBellException.ThrowBadRequest("Company detail is null or empty");

            if (companyGroup.InCorporationDate == null)
                throw HiringBellException.ThrowBadRequest("Company incorporation date is null");

            if (string.IsNullOrEmpty(companyGroup.Email))
                throw HiringBellException.ThrowBadRequest("Email is null or empty");
        }

        public dynamic GetCompanyById(int CompanyId)
        {
            OrganizationDetail result = _db.Get<OrganizationDetail>(Procedures.Company_GetById, new { CompanyId });
            List<Files> files = _db.GetList<Files>(Procedures.UserFiles_GetBy_OwnerId, new { FileOwnerId = CompanyId, UserTypeId = (int)UserType.Compnay });
            return new { OrganizationDetail = result, Files = files };
        }

        public OrganizationDetail GetOrganizationDetailService()
        {
            var ResultSet = _db.FetchDataSet(Procedures.Organization_Detail_Get);
            if (ResultSet.Tables.Count != 2)
                throw new HiringBellException("Unable to get organization detail.");

            OrganizationDetail organizationDetail = Converter.ToType<OrganizationDetail>(ResultSet.Tables[0]);
            organizationDetail.Files = Converter.ToType<Files>(ResultSet.Tables[1]);
            return organizationDetail;
        }

        public async Task<OrganizationDetail> InsertUpdateOrganizationDetailService(OrganizationDetail companyInfo, IFormFileCollection fileCollection)
        {
            OrganizationDetail company = new OrganizationDetail();
            if (string.IsNullOrEmpty(companyInfo.Email))
                throw new HiringBellException("Invalid organization email.");

            if (string.IsNullOrEmpty(companyInfo.CompanyName))
                throw new HiringBellException("Invalid company name.");

            var ResultSet = _db.FetchDataSet(Procedures.Organization_Detail_Get);
            if (ResultSet.Tables.Count != 2)
                throw new HiringBellException("Unable to get organization detail.");

            company = Converter.ToType<OrganizationDetail>(ResultSet.Tables[0]);
            if (ResultSet.Tables[1].Rows.Count > 0)
                companyInfo.Files = Converter.ToType<Files>(ResultSet.Tables[1]);
            else
                companyInfo.Files = new Files();

            if (company != null)
            {
                company.OrganizationName = companyInfo.OrganizationName;
                company.OrgEmail = companyInfo.OrgEmail;
                company.OrgFax = companyInfo.OrgFax;
                company.OrgMobileNo = companyInfo.OrgMobileNo;
                company.OrgPrimaryPhoneNo = companyInfo.OrgPrimaryPhoneNo;
                company.OrgSecondaryPhoneNo = companyInfo.OrgSecondaryPhoneNo;
                company.CompanyName = companyInfo.CompanyName;
                company.CompanyDetail = companyInfo.CompanyDetail;
                company.FirstAddress = companyInfo.FirstAddress;
                company.SecondAddress = companyInfo.SecondAddress;
                company.ThirdAddress = companyInfo.ThirdAddress;
                company.ForthAddress = companyInfo.ForthAddress;
                company.Email = companyInfo.Email;
                company.PrimaryPhoneNo = companyInfo.PrimaryPhoneNo;
                company.SecondaryPhoneNo = companyInfo.SecondaryPhoneNo;
                company.Fax = companyInfo.Fax;
                company.FirstEmail = companyInfo.FirstEmail;
                company.SecondEmail = companyInfo.SecondEmail;
                company.ThirdEmail = companyInfo.ThirdEmail;
                company.ForthEmail = companyInfo.ForthEmail;
                company.Pincode = companyInfo.Pincode;
                company.FileId = companyInfo.FileId;
                company.MobileNo = companyInfo.MobileNo;
                company.City = companyInfo.City;
                company.Country = companyInfo.Country;
                company.FullAddress = companyInfo.FullAddress;
                company.GSTNo = companyInfo.GSTNo;
                company.InCorporationDate = companyInfo.InCorporationDate;
                company.LegalDocumentPath = companyInfo.LegalDocumentPath;
                company.LegalEntity = companyInfo.LegalEntity;
                company.PANNo = companyInfo.PANNo;
                company.SectorType = companyInfo.SectorType;
                company.State = companyInfo.State;
                company.TradeLicenseNo = companyInfo.TradeLicenseNo;
                company.TypeOfBusiness = companyInfo.TypeOfBusiness;
                company.AccountNo = companyInfo.AccountNo;
                company.BankName = companyInfo.BankName;
                company.Branch = companyInfo.Branch;
                company.IFSC = companyInfo.IFSC;
                company.IsPrimaryCompany = companyInfo.IsPrimaryCompany;
                if (string.IsNullOrEmpty(companyInfo.FixedComponentsId))
                    companyInfo.FixedComponentsId = "[]";
                company.FixedComponentsId = companyInfo.FixedComponentsId;
                company.BranchCode = companyInfo.BranchCode;
                company.OpeningDate = companyInfo.OpeningDate;
                company.ClosingDate = companyInfo.ClosingDate;
                company.IsPrimaryAccount = true;
                company.AdminId = _currentSession.CurrentUserDetail.UserId;
            }
            else
            {
                company = companyInfo;
                company.IsPrimaryCompany = true;
                company.FixedComponentsId = "[]";
                company.IsPrimaryAccount = true;
            }

            var status = _db.Execute<OrganizationDetail>(Procedures.Organization_Intupd, company, true);

            if (string.IsNullOrEmpty(status))
                throw new HiringBellException("Fail to insert or update.");

            if (fileCollection.Count == 1)
                await UpdateOrganizationLogo(companyInfo, fileCollection, (int)UserType.Organization);

            // _cacheManager.ReLoad(CacheTable.Company, Converter.ToDataTable<OrganizationDetail>(organizationDetails));

            return await Task.FromResult(this.GetOrganizationDetailService());
        }

        private async Task UpdateOrganizationLogo(OrganizationDetail companyInfo, IFormFileCollection fileCollection, int userType)
        {
            string companyLogo = String.Empty;
            try
            {
                if (fileCollection.Count > 0)
                    companyLogo = Path.Combine(_fileLocationDetail.RootPath, _fileLocationDetail.LogoPath, fileCollection[0].Name);

                if (File.Exists(companyLogo))
                    File.Delete(companyLogo);
                else
                {
                    FileDetail fileDetailWSig = new FileDetail();
                    fileDetailWSig.DiskFilePath = Path.Combine(_fileLocationDetail.RootPath, companyLogo);
                }

                var files = fileCollection.Select(x => new Files
                {
                    FileUid = userType == (int)UserType.Compnay ? companyInfo.FileId : companyInfo.Files.FileId,
                    FileName = x.Name,
                    Email = companyInfo.Email,
                    FileExtension = string.Empty
                }).ToList<Files>();
                _fileService.SaveFile(_fileLocationDetail.LogoPath, files, fileCollection, (companyInfo.OrganizationId).ToString());

                var fileInfo = (from n in files
                                select new
                                {
                                    FileId = n.FileUid,
                                    FileOwnerId = userType == (int)UserType.Compnay ? companyInfo.CompanyId : companyInfo.OrganizationId,
                                    FileName = n.FileName,
                                    FilePath = n.FilePath,
                                    FileExtension = n.FileExtension,
                                    UserTypeId = userType,
                                    AdminId = _currentSession.CurrentUserDetail.UserId
                                }).ToList();

                var batchResult = await _db.BulkExecuteAsync(Procedures.Userfiledetail_Upload, fileInfo, true);
            }
            catch
            {
                if (File.Exists(companyLogo))
                    File.Delete(companyLogo);

                throw;
            }

            await Task.CompletedTask;
        }

        public async Task<OrganizationDetail> InsertUpdateCompanyDetailService(OrganizationDetail companyInfo, IFormFileCollection fileCollection)
        {
            OrganizationDetail company = new OrganizationDetail();
            ValidateCompany(companyInfo);

            company = _db.Get<OrganizationDetail>(Procedures.Company_GetById, new { companyInfo.CompanyId });

            if (company == null)
                throw new HiringBellException("Unable to find company. Please contact to admin.");

            company.CompanyName = companyInfo.CompanyName;
            company.FirstAddress = companyInfo.FirstAddress;
            company.SecondAddress = companyInfo.SecondAddress;
            company.ThirdAddress = companyInfo.ThirdAddress;
            company.ForthAddress = companyInfo.ForthAddress;
            company.Email = companyInfo.Email;
            company.PrimaryPhoneNo = companyInfo.PrimaryPhoneNo;
            company.SecondaryPhoneNo = companyInfo.SecondaryPhoneNo;
            company.Fax = companyInfo.Fax;
            company.FirstEmail = companyInfo.FirstEmail;
            company.SecondEmail = companyInfo.SecondEmail;
            company.ThirdEmail = companyInfo.ThirdEmail;
            company.ForthEmail = companyInfo.ForthEmail;
            company.Pincode = companyInfo.Pincode;
            company.FileId = companyInfo.FileId;
            company.MobileNo = companyInfo.MobileNo;
            company.City = companyInfo.City;
            company.Country = companyInfo.Country;
            company.State = companyInfo.State;
            company.TypeOfBusiness = companyInfo.TypeOfBusiness;
            company.LegalEntity = companyInfo.LegalEntity;
            company.FullAddress = companyInfo.FullAddress;
            company.InCorporationDate = companyInfo.InCorporationDate;
            company.SectorType = companyInfo.SectorType;
            company.PANNo = companyInfo.PANNo;
            company.GSTNo = companyInfo.GSTNo;
            company.TradeLicenseNo = companyInfo.TradeLicenseNo;

            var status = _db.Execute<OrganizationDetail>(Procedures.Company_Intupd, company, true);
            if (string.IsNullOrEmpty(status))
                throw new HiringBellException("Fail to insert or update.");

            if (fileCollection.Count > 0)
                await UpdateOrganizationLogo(companyInfo, fileCollection, (int)UserType.Compnay);

            return company;
        }

        private void ValidateCompany(OrganizationDetail companyInfo)
        {
            if (string.IsNullOrEmpty(companyInfo.Email))
                throw new HiringBellException("Invalid organization email.");

            if (string.IsNullOrEmpty(companyInfo.PrimaryPhoneNo))
                throw new HiringBellException("Invalid organization primary phone No#");

            if (string.IsNullOrEmpty(companyInfo.CompanyName))
                throw new HiringBellException("Invalid company name.");

            if (string.IsNullOrEmpty(companyInfo.FirstAddress))
                throw new HiringBellException("First address is null or empty");

            if (string.IsNullOrEmpty(companyInfo.SecondAddress))
                throw new HiringBellException("Second address is null or empty");

            if (string.IsNullOrEmpty(companyInfo.GSTNo))
                throw new HiringBellException("GSTIN number is null or empty");

            if (string.IsNullOrEmpty(companyInfo.Country))
                throw new HiringBellException("Country is null or empty");

            if (string.IsNullOrEmpty(companyInfo.State))
                throw new HiringBellException("State is null or empty");

            if (string.IsNullOrEmpty(companyInfo.City))
                throw new HiringBellException("City is null or empty");

            if (companyInfo.Pincode <= 0)
                throw new HiringBellException("Pincode is invalid. Please enter a valid pincode");
        }

        public List<BankDetail> InsertUpdateCompanyAccounts(BankDetail bankDetail)
        {
            List<BankDetail> bankDetails = null;
            ValidateBankDetail(bankDetail);

            var bank = _db.Get<BankDetail>(Procedures.Bank_Accounts_GetById, new { bankDetail.BankAccountId });

            if (bank == null)
                bank = bankDetail;
            else
            {
                bank.CompanyId = bankDetail.CompanyId;
                bank.AccountNo = bankDetail.AccountNo;
                bank.BankName = bankDetail.BankName;
                bank.Branch = bankDetail.Branch;
                bank.IFSC = bankDetail.IFSC;
                bank.OpeningDate = bankDetail.OpeningDate;
                bank.BranchCode = bankDetail.BranchCode;
                bank.OpeningDate = bankDetail.OpeningDate;
                bank.ClosingDate = bankDetail.ClosingDate;
                bank.IsPrimaryAccount = bankDetail.IsPrimaryAccount;
            }
            bank.AdminId = _currentSession.CurrentUserDetail.UserId;

            var status = _db.Execute<BankDetail>(Procedures.Bank_Accounts_Intupd, bank, true);

            if (string.IsNullOrEmpty(status))
            {
                throw new HiringBellException("Fail to insert or update.");
            }
            else
            {
                FilterModel filterModel = new FilterModel();
                filterModel.SortBy = "";
                filterModel.PageSize = 10;
                filterModel.PageIndex = 1;
                filterModel.SearchString = $"1=1 And CompanyId = {bankDetail.CompanyId}";
                bankDetails = this.GetCompanyBankDetail(filterModel);
            }

            return bankDetails;
        }

        private void ValidateBankDetail(BankDetail bankDetail)
        {
            if (bankDetail.CompanyId <= 0)
                throw new HiringBellException("Invalid company detail submitted. Please login again.");

            if (string.IsNullOrEmpty(bankDetail.AccountNo))
                throw new HiringBellException("Invalid account number submitted.");

            if (bankDetail.OrganizationId <= 0)
                throw new HiringBellException("Organizatin or compnay is not selected.");

            if (string.IsNullOrEmpty(bankDetail.BankName))
                throw HiringBellException.ThrowBadRequest("Bank name is null or empty");

            if (string.IsNullOrEmpty(bankDetail.IFSC))
                throw new HiringBellException("Invalid ifsc code submitted.");

            if (string.IsNullOrEmpty(bankDetail.Branch))
                throw HiringBellException.ThrowBadRequest("Branch name is null or empty");
        }

        public List<BankDetail> GetCompanyBankDetail(FilterModel filterModel)
        {
            List<BankDetail> result = _db.GetList<BankDetail>(Procedures.Bank_Accounts_Getby_CmpId, new
            {
                filterModel.SearchString,
                filterModel.SortBy,
                filterModel.PageIndex,
                filterModel.PageSize
            });
            return result;
        }

        public async Task<CompanySetting> UpdateSettingService(int companyId, CompanySetting companySetting, bool isRunLeaveAccrual)
        {
            if (companyId <= 0)
                throw new HiringBellException("Invalid company id supplied.");

            var result = _db.FetchDataSet(Procedures.Company_Setting_Get_Byid, new { CompanyId = companyId });
            if (result == null || result.Tables.Count != 2)
                throw new HiringBellException("Fail to get company setting details. Please contact to admin");

            CompanySetting companySettingDetail = null;
            if (result.Tables[0].Rows.Count > 0)
                companySettingDetail = Converter.ToType<CompanySetting>(result.Tables[0]);

            if (companySettingDetail == null)
                companySettingDetail = companySetting;
            else
            {
                companySettingDetail.ProbationPeriodInDays = companySetting.ProbationPeriodInDays;
                companySettingDetail.NoticePeriodInDays = companySetting.NoticePeriodInDays;
                companySettingDetail.DeclarationStartMonth = companySetting.DeclarationStartMonth;
                companySettingDetail.DeclarationEndMonth = companySetting.DeclarationEndMonth;
                companySettingDetail.IsPrimary = companySetting.IsPrimary;
                companySettingDetail.FinancialYear = companySetting.FinancialYear;
                companySettingDetail.AttendanceSubmissionLimit = companySetting.AttendanceSubmissionLimit;
                companySettingDetail.LeaveAccrualRunCronDayOfMonth = companySetting.LeaveAccrualRunCronDayOfMonth;
                companySettingDetail.EveryMonthLastDayOfDeclaration = companySetting.EveryMonthLastDayOfDeclaration;
                companySettingDetail.IsJoiningBarrierDayPassed = companySetting.IsJoiningBarrierDayPassed;
                companySettingDetail.NoticePeriodInProbation = companySetting.NoticePeriodInProbation;
                companySettingDetail.ExcludePayrollFromJoinDate = companySetting.ExcludePayrollFromJoinDate;
            }

            var status = await _db.ExecuteAsync(Procedures.Company_Setting_Insupd, new
            {
                companySettingDetail.CompanyId,
                companySettingDetail.SettingId,
                companySettingDetail.ProbationPeriodInDays,
                companySettingDetail.NoticePeriodInDays,
                companySettingDetail.DeclarationStartMonth,
                companySettingDetail.DeclarationEndMonth,
                companySettingDetail.IsPrimary,
                companySettingDetail.FinancialYear,
                companySettingDetail.AttendanceSubmissionLimit,
                companySettingDetail.LeaveAccrualRunCronDayOfMonth,
                companySettingDetail.EveryMonthLastDayOfDeclaration,
                companySettingDetail.TimezoneName,
                companySetting.IsJoiningBarrierDayPassed,
                companySettingDetail.NoticePeriodInProbation,
                companySettingDetail.ExcludePayrollFromJoinDate,
                AdminId = _currentSession.CurrentUserDetail.UserId,
            }, true);

            if (!ApplicationConstants.IsExecuted(status.statusMessage))
                throw new HiringBellException("Fail to update company setting detail. CompanyId: ",
                    nameof(companySettingDetail.CompanyId),
                    " Value: " + companyId, System.Net.HttpStatusCode.BadRequest);

            return companySettingDetail;
        }

        public async Task<dynamic> GetCompanySettingService(int companyId)
        {
            if (companyId <= 0)
                throw new HiringBellException("Invalid company id supplied.");
            var result = _db.FetchDataSet(Procedures.Company_Setting_Get_Byid, new { CompanyId = companyId });
            if (result == null || result.Tables.Count != 2)
                throw new HiringBellException("Fail to get company setting details. Please contact to admin");

            CompanySetting companySettingDetail = null;
            List<EmployeeRole> roles = null;
            if (result.Tables[0].Rows.Count > 0)
                companySettingDetail = Converter.ToType<CompanySetting>(result.Tables[0]);

            if (result.Tables[1].Rows.Count > 0)
                roles = Converter.ToList<EmployeeRole>(result.Tables[1]);

            return await Task.FromResult(new { companySettingDetail, roles });
        }

        public async Task<CompanySetting> GetCompanySettingByCompanyId(int companyId)
        {
            if (companyId <= 0)
                throw new HiringBellException("Invalid company id supplied.");
            var companySettingDetail = _db.Get<CompanySetting>(Procedures.Company_Setting_Get_Byid, new { CompanyId = companyId });
            if (companySettingDetail == null)
                throw new HiringBellException("Fail to get company setting details. Please contact to admin");

            return await Task.FromResult(companySettingDetail);
        }

        public async Task<List<Files>> UpdateCompanyFiles(Files uploadedFileDetail, IFormFileCollection fileCollection)
        {
            string _folderPath = String.Empty;

            string FolderPath = Path.Combine(_fileLocationDetail.DocumentFolder, _fileLocationDetail.CompanyFiles);

            if (string.IsNullOrEmpty(FolderPath))
                throw new HiringBellException("Invalid file path has been given. Please contact to admin.");

            var files = fileCollection.Select(x => new Files
            {
                FileUid = uploadedFileDetail.FileId,
                FileName = x.Name,
                Email = uploadedFileDetail.Email,
                FileExtension = string.Empty
            }).ToList<Files>();

            _fileService.SaveFileToLocation(FolderPath, files, fileCollection);

            Files fileDetail = files.First();
            var result = await _db.ExecuteAsync(Procedures.Company_Files_Insupd, new
            {
                CompanyFileId = uploadedFileDetail.FileId,
                CompanyId = uploadedFileDetail.CompanyId,
                FileName = fileDetail.FileName,
                FileDescription = uploadedFileDetail.FileDescription,
                FileExtension = fileDetail.FileExtension,
                FilePath = fileDetail.FilePath,
                FileRole = uploadedFileDetail.FileRole,
                AdminId = _currentSession.CurrentUserDetail.UserId
            }, true);

            if (string.IsNullOrEmpty(result.statusMessage))
                throw new HiringBellException("Fail to insert or udpate file data.");

            var fileList = _db.GetList<Files>(Procedures.Company_Files_Get_Byid, new { CompanyId = uploadedFileDetail.CompanyId });
            return fileList;
        }

        public async Task<List<Files>> GetCompanyFiles(int CompanyId)
        {
            var fileList = _db.GetList<Files>(Procedures.Company_Files_Get_Byid, new { CompanyId });
            return await Task.FromResult(fileList);
        }

        public async Task<List<Files>> DeleteCompanyFilesService(Files companyFile)
        {
            if (companyFile == null)
                throw new HiringBellException("Invalid file selected");

            var result = await _db.ExecuteAsync("sp_company_files_delete_by_id", new { CompanyFileId = companyFile.FileId });
            if (result.rowsEffected == 0)
                throw new HiringBellException("Fail to delete the file.");

            if (Directory.Exists(Path.Combine(_fileLocationDetail.DocumentFolder, _fileLocationDetail.CompanyFiles)))
            {
                string ActualPath = Path.Combine(_fileLocationDetail.DocumentFolder, _fileLocationDetail.CompanyFiles, companyFile.FileName);
                if (File.Exists(ActualPath))
                    File.Delete(ActualPath);
            }
            var fileList = _db.GetList<Files>(Procedures.Company_Files_Get_Byid, new { CompanyId = companyFile.CompanyId });
            return fileList;
        }

        public async Task<CompanySetting> UpdateCompanyInitialSettingService(int companyId, CompanySetting companySetting)
        {
            if (companyId <= 0)
                throw new HiringBellException("Invalid company id supplied.");

            var result = _db.FetchDataSet(Procedures.Company_Setting_Get_Byid, new { CompanyId = companyId });
            if (result == null || result.Tables.Count != 2)
                throw new HiringBellException("Fail to get company setting details. Please contact to admin");

            CompanySetting companySettingDetail = null;
            if (result.Tables[0].Rows.Count > 0)
                companySettingDetail = Converter.ToType<CompanySetting>(result.Tables[0]);

            if (companySettingDetail == null)
                throw HiringBellException.ThrowBadRequest("COmpany setting not found. Please contact to admin");


            companySettingDetail.DeclarationStartMonth = companySetting.DeclarationStartMonth;
            companySettingDetail.DeclarationEndMonth = companySetting.DeclarationEndMonth;
            companySettingDetail.FinancialYear = companySetting.FinancialYear;
            companySettingDetail.EveryMonthLastDayOfDeclaration = companySetting.EveryMonthLastDayOfDeclaration;
            var status = await _db.ExecuteAsync(Procedures.Company_Setting_Insupd, new
            {
                companySettingDetail.CompanyId,
                companySettingDetail.SettingId,
                companySettingDetail.ProbationPeriodInDays,
                companySettingDetail.NoticePeriodInDays,
                companySettingDetail.DeclarationStartMonth,
                companySettingDetail.DeclarationEndMonth,
                companySettingDetail.IsPrimary,
                companySettingDetail.FinancialYear,
                companySettingDetail.AttendanceSubmissionLimit,
                companySettingDetail.LeaveAccrualRunCronDayOfMonth,
                companySettingDetail.EveryMonthLastDayOfDeclaration,
                companySettingDetail.TimezoneName,
                companySetting.IsJoiningBarrierDayPassed,
                companySettingDetail.NoticePeriodInProbation,
                companySettingDetail.ExcludePayrollFromJoinDate,
                AdminId = _currentSession.CurrentUserDetail.UserId,
            }, true);

            if (!ApplicationConstants.IsExecuted(status.statusMessage))
                throw new HiringBellException("Fail to update company setting detail. CompnayId: ",
                    nameof(companySettingDetail.CompanyId),
                    " Value: " + companyId, System.Net.HttpStatusCode.BadRequest);

            return companySettingDetail;
        }
    }
}