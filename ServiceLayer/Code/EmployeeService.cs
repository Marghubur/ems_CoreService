using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using Bot.CoreBottomHalf.CommonModal.Enums;
using BottomHalf.Utilities.UtilService;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using Bt.Lib.Common.Service.MicroserviceHttpRequest;
using Bt.Lib.Common.Service.Model;
using DocMaker.ExcelMaker;
using DocMaker.PdfService;
using EMailService.Modal;
using EMailService.Modal.Leaves;
using EMailService.Service;
using ExcelDataReader;
using FileManagerService.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using ModalLayer.Modal.Leaves;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Threading.Tasks;
using File = System.IO.File;

namespace ServiceLayer.Code
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IDb _db;
        private readonly CurrentSession _currentSession;
        private readonly MicroserviceRegistry _microserviceUrlLogs;
        private readonly IFileService _fileService;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly IConfiguration _configuration;
        private readonly ITimezoneConverter _timezoneConverter;
        private readonly HtmlToPdfConverter _htmlToPdfConverter;
        private readonly IEMailManager _eMailManager;
        private readonly ILeaveCalculation _leaveCalculation;
        private readonly ITimesheetService _timesheetService;
        private readonly ExcelWriter _excelWriter;
        private readonly RequestMicroservice _requestMicroservice;
        public EmployeeService(IDb db,
            CurrentSession currentSession,
            IFileService fileService,
            IConfiguration configuration,
            ITimezoneConverter timezoneConverter,
            FileLocationDetail fileLocationDetail,
            HtmlToPdfConverter htmlToPdfConverter,
            ILeaveCalculation leaveCalculation,
            IEMailManager eMailManager,
            ITimesheetService timesheetService,
            ExcelWriter excelWriter,
            RequestMicroservice requestMicroservice,
            MicroserviceRegistry microserviceUrlLogs)
        {
            _db = db;
            _leaveCalculation = leaveCalculation;
            _configuration = configuration;
            _currentSession = currentSession;
            _fileService = fileService;
            _fileLocationDetail = fileLocationDetail;
            _timezoneConverter = timezoneConverter;
            _htmlToPdfConverter = htmlToPdfConverter;
            _eMailManager = eMailManager;
            _timesheetService = timesheetService;
            _excelWriter = excelWriter;
            _requestMicroservice = requestMicroservice;
            _microserviceUrlLogs = microserviceUrlLogs;
        }

        #region Code Used for employee insert or update

        /// <summary>
        /// This service will be used to register new employee and Employee object must contain complete information of the present employee
        /// </summary>
        /// <param name="employee"></param>
        /// <param name="fileCollection"></param>
        /// <returns></returns>
        public async Task<string> RegisterEmployeeService(Employee employee, IFormFileCollection fileCollection)
        {
            var result = CheckMobileEmailExistence(employee.EmployeeId, employee.Email, employee.Mobile);
            if (result.EmailCount > 0)
                throw HiringBellException.ThrowBadRequest($"Email id: {employee.Email} already exists.");

            if (result.MobileCount > 0)
                throw HiringBellException.ThrowBadRequest($"Mobile no: {employee.Mobile} already exists.");

            await RegisterOrUpdateEmployeeDetail(employee, fileCollection);

            return ApplicationConstants.Successfull;
        }

        private EmployeeEmailMobileCheck CheckMobileEmailExistence(long employeeId, string email, string mobile)
        {
            var result = _db.Get<EmployeeEmailMobileCheck>(Procedures.CHECK_MOBILE_EMAIL_EXISTENCE, new
            {
                Email = email,
                Mobile = mobile,
                EmployeeId = employeeId
            });

            return result;
        }

        public async Task<string> RegisterOrUpdateEmployeeDetail(Employee employee, IFormFileCollection fileCollection, UploadedPayrollData uploadedPayrollData = null)
        {
            bool IsNewRegistration = false;
            long employeeUid = 0;

            try
            {
                string EncryptedPassword = string.Empty;
                var empId = Convert.ToInt32(employee.EmployeeUid);

                // validate employee
                ValidateEmployee(employee);

                // validate employee detail
                ValidateEmployeeDetails(employee);

                await ManagerProfessionalDetail(employee);

                await AssignReportingManager(employee);

                _currentSession.TimeZoneNow = _timezoneConverter.ToTimeZoneDateTime(DateTime.UtcNow, _currentSession.TimeZone);

                // prepare for new insert of employee
                IsNewRegistration = await PrepareEmployeeInsertData(employee);

                employeeUid = employee.EmployeeUid;

                int currentRegimeId = ApplicationConstants.DefaultTaxRegin;

                if (uploadedPayrollData != null)
                {
                    await SetupPreviousEmployerIncome(employeeUid, uploadedPayrollData);
                    currentRegimeId = string.IsNullOrEmpty(uploadedPayrollData.Regime) && uploadedPayrollData.Regime.ToLower().Contains("new")
                                            ? ApplicationConstants.NewRegim : ApplicationConstants.OldRegim;
                }

                var eCal = new EmployeeCalculation();

                if (IsNewRegistration)
                {
                    EncryptedPassword = UtilService.Encrypt(
                        _configuration.GetSection("DefaultNewEmployeePassword").Value,
                        _configuration.GetSection("EncryptSecret").Value
                    );

                    eCal = await GetDeclarationDetail(employee, currentRegimeId);
                }
                else
                {
                    var dataSet = _db.FetchDataSet(Procedures.EMPLOYEE_DECLARATION_DETAIL_GET_BY_EMPID, new
                    {
                        EmployeeId = employeeUid,
                        _currentSession.FinancialStartYear
                    });

                    if (dataSet == null || dataSet.Tables.Count != 2)
                        throw HiringBellException.ThrowBadRequest("Fail to get salary detail and salary components");

                    eCal.employeeSalaryDetail = Converter.ToType<EmployeeSalaryDetail>(dataSet.Tables[1]);
                    eCal.employeeDeclaration = Converter.ToType<EmployeeDeclaration>(dataSet.Tables[0]);

                    eCal.Doj = employee.DateOfJoining;
                }

                // make insert or update call for employee
                string employeeId = InsertUpdateEmployee(eCal, IsNewRegistration, EncryptedPassword, employee);

                await EmployeeFileInsertUpdate(eCal, fileCollection, employee, employeeId);

                //if (!isEmpByExcel)
                await CheckRunLeaveAccrualCycle(eCal.EmployeeId);

                return employeeId;
            }
            catch
            {
                if (IsNewRegistration && employeeUid > 0)
                    _db.Execute(Procedures.Employee_Delete_by_EmpId, new { employeeUid }, false);

                throw;
            }
        }

        private async Task<bool> PrepareEmployeeInsertData(Employee employee)
        {
            bool IsNewRegistration = false;
            if (employee.AccessLevelId != (int)RolesName.Admin)
                employee.UserTypeId = (int)RolesName.User;

            if (string.IsNullOrEmpty(employee.NewSalaryDetail))
                employee.NewSalaryDetail = "[]";

            employee.EmployeeId = employee.EmployeeUid;
            if (employee.EmployeeUid == 0)
            {
                // create employee record
                employee.EmployeeId = await RegisterNewEmployee(employee, employee.DateOfJoining);
                IsNewRegistration = true;

                employee.EmployeeUid = employee.EmployeeId;
            }

            return await Task.FromResult(IsNewRegistration);
        }

        private async Task<EmployeeCalculation> GetDeclarationDetail(Employee employee, int currentRegimeId)
        {
            string url = $"{_microserviceUrlLogs.SalaryDeclarationCalculation}/{employee.EmployeeId}/{employee.CTC}/{currentRegimeId}";
            var microserviceRequest = MicroserviceRequest.Builder(url);
            microserviceRequest
            .SetDbConfigModal(_requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString))
            .SetConnectionString(_currentSession.LocalConnectionString)
            .SetCompanyCode(_currentSession.CompanyCode)
            .SetToken(_currentSession.Authorization);

            var response = await _requestMicroservice.GetRequest<EmployeeCalculation>(microserviceRequest);
            if (response is null)
                throw HiringBellException.ThrowBadRequest("fail to get response");

            return response;
        }

        public async Task<string> UpdateEmployeeService(Employee employee, IFormFileCollection fileCollection)
        {
            if (employee.EmployeeUid <= 0)
                throw new HiringBellException { UserMessage = "Invalid EmployeeId.", FieldName = nameof(employee.EmployeeUid), FieldValue = employee.EmployeeUid.ToString() };

            var result = CheckMobileEmailExistence(employee.EmployeeUid, employee.Email, employee.Mobile);
            if (result.EmployeeCount == 0)
                throw HiringBellException.ThrowBadRequest("Employee record not found. Please contact to admin.");

            return await RegisterOrUpdateEmployeeDetail(employee, fileCollection);
        }

        private void ValidateEmployeeDetails(Employee employee)
        {

            if (employee.ActualPackage < 0)
                throw new HiringBellException { UserMessage = "Invalid Actual Package.", FieldName = nameof(employee.ActualPackage), FieldValue = employee.ActualPackage.ToString() };

            if (employee.FinalPackage < 0)
                throw new HiringBellException { UserMessage = "Invalid Final Package.", FieldName = nameof(employee.FinalPackage), FieldValue = employee.FinalPackage.ToString() };

            if (employee.TakeHomeByCandidate < 0)
                throw new HiringBellException { UserMessage = "Invalid TakeHome By Candidate.", FieldName = nameof(employee.TakeHomeByCandidate), FieldValue = employee.TakeHomeByCandidate.ToString() };

            if (employee.FinalPackage < employee.ActualPackage)
                throw new HiringBellException { UserMessage = "Final package must be greater that or equal to Actual package.", FieldName = nameof(employee.FinalPackage), FieldValue = employee.FinalPackage.ToString() };

            if (employee.ActualPackage < employee.TakeHomeByCandidate)
                throw new HiringBellException { UserMessage = "Actual package must be greater that or equal to TakeHome package.", FieldName = nameof(employee.ActualPackage), FieldValue = employee.ActualPackage.ToString() };
        }

        private void ValidateEmployeeMapDetails(EmployeeMappedClient employee)
        {

            if (employee.ActualPackage < 0)
                throw new HiringBellException { UserMessage = "Invalid Actual Package.", FieldName = nameof(employee.ActualPackage), FieldValue = employee.ActualPackage.ToString() };

            if (employee.FinalPackage < 0)
                throw new HiringBellException { UserMessage = "Invalid Final Package.", FieldName = nameof(employee.FinalPackage), FieldValue = employee.FinalPackage.ToString() };

            if (employee.TakeHomeByCandidate < 0)
                throw new HiringBellException { UserMessage = "Invalid TakeHome By Candidate.", FieldName = nameof(employee.TakeHomeByCandidate), FieldValue = employee.TakeHomeByCandidate.ToString() };

            if (employee.FinalPackage < employee.ActualPackage)
                throw new HiringBellException { UserMessage = "Final package must be greater that or equal to Actual package.", FieldName = nameof(employee.FinalPackage), FieldValue = employee.FinalPackage.ToString() };

            if (employee.ActualPackage < employee.TakeHomeByCandidate)
                throw new HiringBellException { UserMessage = "Actual package must be greater that or equal to TakeHome package.", FieldName = nameof(employee.ActualPackage), FieldValue = employee.ActualPackage.ToString() };
        }

        private void ValidateEmployee(Employee employee)
        {
            if (string.IsNullOrEmpty(employee.Email))
                throw new HiringBellException { UserMessage = "Email id is a mandatory field.", FieldName = nameof(employee.Email), FieldValue = employee.Email.ToString() };

            if (string.IsNullOrEmpty(employee.AccountNumber))
                throw new HiringBellException { UserMessage = "Account number is a mandatory field.", FieldName = nameof(employee.AccountNumber), FieldValue = employee.AccountNumber.ToString() };

            if (string.IsNullOrEmpty(employee.BankName))
                throw new HiringBellException { UserMessage = "Bank name is a mandatory field.", FieldName = nameof(employee.BankName), FieldValue = employee.BankName.ToString() };

            if (string.IsNullOrEmpty(employee.IFSCCode))
                throw new HiringBellException { UserMessage = "IFSC code is a mandatory field.", FieldName = nameof(employee.IFSCCode), FieldValue = employee.IFSCCode.ToString() };

            if (string.IsNullOrEmpty(employee.PANNo))
                throw new HiringBellException { UserMessage = "Pan No is a mandatory field.", FieldName = nameof(employee.PANNo), FieldValue = employee.PANNo.ToString() };

            if (string.IsNullOrEmpty(employee.FirstName))
                throw new HiringBellException { UserMessage = "First Name is a mandatory field.", FieldName = nameof(employee.FirstName), FieldValue = employee.FirstName.ToString() };

            if (string.IsNullOrEmpty(employee.LastName))
                throw new HiringBellException { UserMessage = "Last Name is a mandatory field.", FieldName = nameof(employee.LastName), FieldValue = employee.LastName.ToString() };

            if (string.IsNullOrEmpty(employee.Mobile) || employee.Mobile.Contains("."))
                throw new HiringBellException { UserMessage = "Mobile number is a mandatory field.", FieldName = nameof(employee.Mobile), FieldValue = employee.Mobile.ToString() };

            if (employee.Mobile.Length < 10 || employee.Mobile.Length > 10)
                throw new HiringBellException { UserMessage = "Mobile number must be only 10 digit.", FieldName = nameof(employee.Mobile), FieldValue = employee.Mobile.ToString() };

            if (employee.DesignationId <= 0)
                throw new HiringBellException { UserMessage = "Designation is a mandatory field.", FieldName = nameof(employee.DesignationId), FieldValue = employee.DesignationId.ToString() };

            if (employee.ReportingManagerId < 0)
                employee.ReportingManagerId = 0;

            if (employee.UserTypeId <= 0)
                throw new HiringBellException { UserMessage = "User Type is a mandatory field.", FieldName = nameof(employee.UserTypeId), FieldValue = employee.UserTypeId.ToString() };

            if (employee.AccessLevelId <= 0)
                throw new HiringBellException { UserMessage = "Role is a mandatory field.", FieldName = nameof(employee.AccessLevelId), FieldValue = employee.AccessLevelId.ToString() };

            if (employee.CTC <= 0)
                throw new HiringBellException { UserMessage = "CTC is a mandatory field.", FieldName = nameof(employee.CTC), FieldValue = employee.CTC.ToString() };

            if (employee.OrganizationId <= 0)
                throw new HiringBellException("Invalid organization selected. Please contact to admin");

            if (employee.CompanyId <= 0)
                throw new HiringBellException("Invalid company selected. Please contact to admin");

            if (employee.WorkShiftId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid shift selected. Please contact to admin");

            if (employee?.DOB == null)
                throw new HiringBellException { UserMessage = "Date of birth is a mandatory field.", FieldName = nameof(employee.DOB), FieldValue = employee.DOB.ToString() };

            var mail = new MailAddress(employee.Email);
            bool isValidEmail = mail.Host.Contains(".");
            if (!isValidEmail)
                throw new HiringBellException { UserMessage = "The email is invalid.", FieldName = nameof(employee.Email), FieldValue = employee.Email.ToString() };
        }

        private async Task ManagerProfessionalDetail(Employee employee)
        {
            var professionalDetail = new EmployeeProfessionDetail
            {
                AadharNo = employee.AadharNo,
                AccountNumber = employee.AccountNumber,
                BankName = employee.BankName,
                BranchName = employee.BranchName,
                CreatedBy = employee.EmployeeUid,
                CreatedOn = employee.DateOfJoining,
                Domain = employee.Domain,
                Email = employee.Email,
                EmployeeUid = employee.EmployeeUid,
                EmpProfDetailUid = employee.EmpProfDetailUid,
                ExperienceInYear = employee.ExperienceInYear,
                FirstName = employee.FirstName,
                IFSCCode = employee.IFSCCode,
                LastCompanyName = employee.LastCompanyName,
                LastName = employee.LastName,
                Mobile = employee.Mobile,
                PANNo = employee.PANNo,
                SecomdaryMobile = employee.SecondaryMobile,
                Specification = employee.Specification,
            };

            employee.ProfessionalDetail_Json = JsonConvert.SerializeObject(professionalDetail);
            await Task.CompletedTask;
        }

        private async Task CheckRunLeaveAccrualCycle(long EmployeeId)
        {
            var PresentDate = _timezoneConverter.ToSpecificTimezoneDateTime(_currentSession.TimeZone);
            var result = _db.Get<Leave>(Procedures.Employee_Leave_Request_By_Empid, new
            {
                EmployeeId,
                PresentDate.Year
            });

            if (result == null)
                throw HiringBellException.ThrowBadRequest("Leave detail not found. Please contact to admin");

            if (string.IsNullOrEmpty(result.LeaveQuotaDetail) || result.LeaveQuotaDetail == "[]")
            {
                RunAccrualModel runAccrualModel = new RunAccrualModel
                {
                    RunTillMonthOfPresnetYear = true,
                    EmployeeId = EmployeeId,
                    IsSingleRun = true
                };
                await _leaveCalculation.RunAccrualCycle(runAccrualModel);
            }

            await Task.CompletedTask;
        }

        private async Task<long> RegisterNewEmployee(Employee employee, DateTime doj)
        {
            var result = await _db.ExecuteAsync(Procedures.Employees_Create, new
            {
                employee.FirstName,
                employee.LastName,
                employee.Mobile,
                employee.Email,
                employee.LeavePlanId,
                employee.PayrollGroupId,
                employee.SalaryGroupId,
                employee.ReportingManagerId,
                employee.DesignationId,
                RegistrationDate = doj,
                employee.CompanyId,
                employee.NoticePeriodId,
                employee.WorkShiftId,
                employee.UserTypeId,
                AdminId = _currentSession.CurrentUserDetail.UserId
            }, true);

            if (string.IsNullOrEmpty(result.statusMessage))
                throw HiringBellException.ThrowBadRequest("Fail to register new employee.");

            long employeeId = Convert.ToInt64(result.statusMessage);
            if (employeeId == 0)
                throw HiringBellException.ThrowBadRequest("Fail to register new employee.");

            return employeeId;
        }

        private async Task AssignReportingManager(Employee employee)
        {
            if (employee.ReportingManagerId == 0)
            {
                employee.ReportingManagerId = _currentSession.CurrentUserDetail.UserId;
            }

            await Task.CompletedTask;
        }

        private string InsertUpdateEmployee(EmployeeCalculation eCal, bool IsNewRegistration, string EncryptedPassword, Employee employee)
        {
            var employeeId = _db.Execute<Employee>(Procedures.Employees_Ins_Upd, new
            {
                employee.EmployeeUid,
                employee.OrganizationId,
                employee.FirstName,
                employee.LastName,
                employee.Mobile,
                employee.Email,
                employee.LeavePlanId,
                employee.PayrollGroupId,
                employee.SalaryGroupId,
                employee.CompanyId,
                employee.NoticePeriodId,
                employee.SecondaryMobile,
                employee.FatherName,
                employee.MotherName,
                employee.SpouseName,
                employee.Gender,
                employee.State,
                employee.City,
                employee.Pincode,
                employee.Address,
                employee.PANNo,
                employee.AadharNo,
                employee.AccountNumber,
                employee.BankName,
                employee.BranchName,
                employee.IFSCCode,
                employee.Domain,
                employee.Specification,
                employee.ExprienceInYear,
                employee.LastCompanyName,
                employee.IsPermanent,
                employee.ActualPackage,
                employee.FinalPackage,
                employee.TakeHomeByCandidate,
                employee.ReportingManagerId,
                employee.DesignationId,
                employee.ProfessionalDetail_Json,
                Password = EncryptedPassword,
                employee.AccessLevelId,
                employee.UserTypeId,
                employee.CTC,
                eCal.employeeSalaryDetail.GrossIncome,
                eCal.employeeSalaryDetail.NetSalary,
                eCal.employeeSalaryDetail.CompleteSalaryDetail,
                eCal.employeeSalaryDetail.TaxDetail,
                employee.DOB,
                RegistrationDate = eCal.Doj,
                EmployeeDeclarationId = eCal.employeeDeclaration.EmployeeDeclarationId,// declarationId,
                DeclarationDetail = eCal.employeeDeclaration.DeclarationDetail, //GetDeclarationBasicFields(eCal.salaryComponents),
                employee.WorkShiftId,
                IsPending = false,
                employee.NewSalaryDetail,
                IsNewRegistration,
                employee.PFNumber,
                PFJoinDate = employee.PFAccountCreationDate,
                UniversalAccountNumber = employee.UAN,
                employee.ESISerialNumber,
                employee.SalaryDetailId,
                AdminId = _currentSession.CurrentUserDetail.UserId
            },
                true
            );

            if (string.IsNullOrEmpty(employeeId) || employeeId == "0")
            {
                throw HiringBellException.ThrowBadRequest("Fail to insert or update record. Contact to admin.");
            }

            return employeeId;
        }

        private async Task SetupPreviousEmployerIncome(long employeeId, UploadedPayrollData uploaded)
        {
            // save this value into database
            var result = await _db.ExecuteAsync(Procedures.PREVIOUS_EMPLOYEMENT_INS_UPD, new ModalLayer.Modal.Accounts.PreviousEmployementDetail
            {
                PreviousEmpDetailId = 0,
                EmployeeId = employeeId,
                Month = "NA",
                MonthNumber = 0,
                Year = DateTime.UtcNow.Year,
                Gross = uploaded.PR_EPER_TotalIncome,
                Basic = 0,
                HouseRent = 0,
                EmployeePR = 0,
                ESI = 0,
                LWF = 0,
                LWFEmp = 0,
                Professional = uploaded.PR_EPER_PT,
                IncomeTax = uploaded.PR_EPER_TDS,
                OtherTax = 0,
                DeclarationFor80C = uploaded.PR_EPER_PF_80C,
                OtherTaxable = 0,
                CreatedBy = _currentSession.CurrentUserDetail.UserId,
                UpdatedBy = _currentSession.CurrentUserDetail.UserId,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow
            }, true);

            if (string.IsNullOrEmpty(result.statusMessage))
                throw HiringBellException.ThrowBadRequest("Fail to insert or update previous employement details");
        }

        private async Task EmployeeFileInsertUpdate(EmployeeCalculation eCal, IFormFileCollection fileCollection, Employee employee, string employeeId)
        {
            eCal.EmployeeId = Convert.ToInt64(employeeId);
            if (fileCollection != null && fileCollection.Count > 0)
            {
                var ownerPath = Path.Combine(_currentSession.CompanyCode, _fileLocationDetail.User, $"{nameof(UserType.Employee)}_{eCal.EmployeeId}");
                string url = $"{_microserviceUrlLogs.SaveApplicationFile}";
                FileFolderDetail fileFolderDetail = new FileFolderDetail
                {
                    FolderPath = ownerPath,
                    OldFileName = new List<string> { employee.OldFileName },
                    ServiceName = LocalConstants.EmstumFileService
                };

                var microserviceRequest = MicroserviceRequest.Builder(url);
                microserviceRequest
                .SetFiles(fileCollection)
                .SetPayload(fileFolderDetail)
                .SetConnectionString(_currentSession.LocalConnectionString)
                .SetCompanyCode(_currentSession.CompanyCode)
                .SetToken(_currentSession.Authorization);

                List<Files> files = await _requestMicroservice.UploadFile<List<Files>>(microserviceRequest);
                var fileInfo = (from n in files
                                select new
                                {
                                    FileId = employee.FileId,
                                    FileOwnerId = eCal.EmployeeId,
                                    FileName = n.FileName.Contains(".") ? n.FileName : n.FileName + "." + n.FileExtension,
                                    FilePath = n.FilePath,
                                    FileExtension = n.FileExtension,
                                    UserTypeId = (int)UserType.Employee,
                                    ItemStatusId = LocalConstants.Profile,
                                    AdminId = _currentSession.CurrentUserDetail.UserId
                                }).ToList();

                var batchResult = await _db.BulkExecuteAsync(Procedures.Userfiledetail_Upload, fileInfo, true);
            }
        }

        #endregion

        #region Get Active and De-Active and Get All employees and Manage employee mapped clients
        public dynamic GetBillDetailForEmployeeService(FilterModel filterModel)
        {
            filterModel.PageSize = 100;
            List<Employee> employees = GetEmployees(filterModel);
            employees = employees.FindAll(x => x.EmployeeUid != 1);
            List<Organization> organizations = _db.GetList<Organization>(Procedures.Company_Get);

            if (employees.Count == 0 || organizations.Count == 0)
                throw HiringBellException.ThrowBadRequest("Unable to get employee and company detail. Please contact to admin.");

            return new { Employees = employees, Organizations = organizations };
        }

        private List<Employee> FilterActiveEmployees(FilterModel filterModel)
        {
            List<Employee> employees = _db.GetList<Employee>(Procedures.Employee_GetAll, new
            {
                filterModel.SearchString,
                filterModel.SortBy,
                filterModel.PageIndex,
                filterModel.PageSize
            });
            return employees;
        }

        private List<Employee> FilterInActiveEmployees(FilterModel filterModel)
        {
            List<Employee> employees = new List<Employee>();

            List<EmployeeArchiveModal> employeeArchiveModal = _db.GetList<EmployeeArchiveModal>(Procedures.Employee_GetAllInActive, new
            {
                filterModel.SearchString,
                filterModel.SortBy,
                filterModel.PageIndex,
                filterModel.PageSize
            });

            if (employeeArchiveModal == null || employeeArchiveModal.Count == 0)
                return employees;

            EmployeeCompleteDetailModal employeeJson = null;
            foreach (var item in employeeArchiveModal)
            {
                employeeJson = JsonConvert.DeserializeObject<EmployeeCompleteDetailModal>(item.EmployeeCompleteJsonData);
                if (employeeJson != null)
                {
                    employees.Add(new Employee
                    {
                        FirstName = employeeJson.EmployeeDetail.FirstName,
                        LastName = employeeJson.EmployeeDetail.LastName,
                        Mobile = employeeJson.EmployeeDetail.Mobile,
                        EmployeeUid = item.EmployeeId,
                        Email = employeeJson.EmployeeDetail.Email,
                        LeavePlanId = employeeJson.EmployeeDetail.LeavePlanId,
                        IsActive = employeeJson.EmployeeDetail.IsActive,
                        AadharNo = employeeJson.EmployeeProfessionalDetail.AadharNo,
                        PANNo = employeeJson.EmployeeProfessionalDetail.PANNo,
                        AccountNumber = employeeJson.EmployeeProfessionalDetail.AccountNumber,
                        BankName = employeeJson.EmployeeProfessionalDetail.BankName,
                        IFSCCode = employeeJson.EmployeeProfessionalDetail.IFSCCode,
                        Domain = employeeJson.EmployeeProfessionalDetail.Domain,
                        Specification = employeeJson.EmployeeProfessionalDetail.Specification,
                        ExprienceInYear = employeeJson.EmployeeProfessionalDetail.ExperienceInYear,
                        ActualPackage = employeeJson.EmployeeDetail.ActualPackage,
                        FinalPackage = employeeJson.EmployeeDetail.FinalPackage,
                        TakeHomeByCandidate = employeeJson.EmployeeDetail.TakeHomeByCandidate,
                        ClientJson = employeeJson.EmployeeDetail.ClientJson,
                        Total = employeeArchiveModal[0].Total,
                        UpdatedOn = employeeJson.EmployeeProfessionalDetail.UpdatedOn,
                        CreatedOn = employeeJson.EmployeeProfessionalDetail.CreatedOn,
                        FileName = item.FileName,
                        FileExtension = item.FileExtension,
                        FilePath = item.FilePath
                    });
                }
            }
            return employees;
        }

        public List<Employee> GetEmployees(FilterModel filterModel)
        {
            List<Employee> employees = null;
            if (string.IsNullOrEmpty(filterModel.SearchString))
                filterModel.SearchString = "1=1";


            if (filterModel.IsActive != null && filterModel.IsActive == true)
            {
                if (filterModel.CompanyId > 0)
                    filterModel.SearchString += $" and l.CompanyId = {filterModel.CompanyId} ";
                else
                    filterModel.SearchString += $" and l.CompanyId = {_currentSession.CurrentUserDetail.CompanyId} ";
                employees = FilterActiveEmployees(filterModel);

            }
            else
                employees = FilterInActiveEmployees(filterModel);

            return employees;
        }

        public DataSet GetManageEmployeeDetailService(long EmployeeId)
        {
            var resultset = _db.GetDataSet(Procedures.Manage_Employee_Detail_Get, new
            {
                EmployeeId,
                _currentSession.CurrentUserDetail.CompanyId,
                _currentSession.CurrentUserDetail.OrganizationId,
            });

            if (resultset.Tables.Count == 10)
            {
                resultset.Tables[0].TableName = "Employee";
                resultset.Tables[1].TableName = "AllocatedClients";
                resultset.Tables[2].TableName = "FileDetail";
                resultset.Tables[3].TableName = "SalaryDetail";
                resultset.Tables[4].TableName = "Clients";
                resultset.Tables[5].TableName = "EmployeesList";
                resultset.Tables[6].TableName = "Roles";
                resultset.Tables[7].TableName = "LeavePlans";
                resultset.Tables[8].TableName = "Companies";
                resultset.Tables[9].TableName = "WorkShift";
            }

            return resultset;
        }

        public DataSet GetManageClientService(long EmployeeId)
        {
            var resultset = _db.GetDataSet(Procedures.MappedClients_Get, new
            {
                employeeId = EmployeeId
            });

            if (resultset.Tables.Count == 1)
            {
                resultset.Tables[0].TableName = "AllocatedClients";
            }
            return resultset;
        }

        public DataSet UpdateEmployeeMappedClientDetailService(EmployeeMappedClient employeeMappedClient, bool IsUpdating)
        {
            if (employeeMappedClient.AssigneDate == null || employeeMappedClient.AssigneDate.Year <= 1900)
                throw HiringBellException.ThrowBadRequest("Assign date is a required field");

            if (employeeMappedClient.EmployeeUid <= 0)
                throw new HiringBellException { UserMessage = "Invalid EmployeeId.", FieldName = nameof(employeeMappedClient.EmployeeUid), FieldValue = employeeMappedClient.EmployeeUid.ToString() };

            if (employeeMappedClient.ClientUid <= 0)
                throw new HiringBellException { UserMessage = "Invalid ClientId.", FieldName = nameof(employeeMappedClient.ClientUid), FieldValue = employeeMappedClient.ClientUid.ToString() };

            var records = _db.GetList<EmployeeMappedClient>(Procedures.Employees_MappedClient_Get_By_Employee_Id, new
            {
                EmployeeId = employeeMappedClient.EmployeeUid
            });

            var first = records.Find(i => i.ClientUid == employeeMappedClient.ClientUid);
            if (first == null)
                first = employeeMappedClient;
            else
            {
                first.ActualPackage = employeeMappedClient.ActualPackage;
                first.ClientUid = employeeMappedClient.ClientUid;
                first.ClientName = employeeMappedClient.ClientName;
                first.FinalPackage = employeeMappedClient.FinalPackage;
                first.TakeHomeByCandidate = employeeMappedClient.TakeHomeByCandidate;
                first.IsPermanent = employeeMappedClient.IsPermanent;
                first.IsActive = employeeMappedClient.IsActive;
                first.BillingHours = employeeMappedClient.BillingHours;
                first.DaysPerWeek = employeeMappedClient.DaysPerWeek;
                first.DateOfJoining = employeeMappedClient.DateOfJoining;
                first.AssigneDate = employeeMappedClient.AssigneDate;
                first.DaysPerWeek = employeeMappedClient.DaysPerWeek;
            }
            this.ValidateEmployeeMapDetails(employeeMappedClient);

            var resultset = _db.GetDataSet(Procedures.Employees_Addupdate_Remote_Client, new
            {
                employeeMappedClientsUid = first.EmployeeMappedClientsUid,
                employeeUid = first.EmployeeUid,
                clientUid = first.ClientUid,
                finalPackage = first.FinalPackage,
                actualPackage = first.ActualPackage,
                takeHome = first.TakeHomeByCandidate,
                isPermanent = first.IsPermanent,
                BillingHours = first.BillingHours,
                DaysPerWeek = first.DaysPerWeek,
                DateOfLeaving = first.DateOfLeaving,
                AssigneDate = first.AssigneDate
            });

            if (!ApplicationConstants.ContainSingleRow(resultset))
                throw HiringBellException.ThrowBadRequest("Fail to insert or update record. Please contact to admin.");

            var weekFirstDay = _timezoneConverter.FirstDayOfWeekUTC(DateTime.UtcNow);
            _timesheetService.RunWeeklyTimesheetCreation(weekFirstDay.AddDays(1), null);
            return resultset;
        }

        public Employee GetEmployeeByIdService(int EmployeeId, int IsActive)
        {
            Employee employee = _db.Get<Employee>(Procedures.Employees_ById, new { EmployeeId = EmployeeId, IsActive = IsActive });
            return employee;
        }

        private EmployeeCompleteDetailModal GetEmployeeCompleteDetail(int EmployeeId)
        {
            DataSet ds = _db.FetchDataSet(Procedures.Employee_GetCompleteDetail, new { EmployeeId = EmployeeId });
            if (ds.Tables.Count != 10)
                throw HiringBellException.ThrowBadRequest("Unable to get employee completed detail");

            EmployeeCompleteDetailModal employeeCompleteDetailModal = new EmployeeCompleteDetailModal
            {
                EmployeeDetail = Converter.ToType<Employee>(ds.Tables[0]),
                PersonalDetail = Converter.ToType<EmployeePersonalDetail>(ds.Tables[1]),
                EmployeeProfessionalDetail = Converter.ToType<EmployeeProfessionDetail>(ds.Tables[2]),
                EmployeeLoginDetail = Converter.ToType<LoginDetail>(ds.Tables[3]),
                EmployeeDeclarations = Converter.ToType<EmployeeDeclaration>(ds.Tables[4]),
                LeaveRequestDetail = Converter.ToType<Leave>(ds.Tables[5]),
                NoticePeriod = Converter.ToType<EmployeeNoticePeriod>(ds.Tables[6]),
                SalaryDetail = Converter.ToType<EmployeeSalaryDetail>(ds.Tables[7]),
                TimesheetDetails = Converter.ToType<TimesheetDetail>(ds.Tables[8]),
                MappedClient = Converter.ToType<EmployeeMappedClient>(ds.Tables[9])
            };

            return employeeCompleteDetailModal;
        }

        private EmployeeArchiveModal GetEmployeeArcheiveCompleteDetail(long EmployeeId)
        {
            EmployeeArchiveModal employeeArcheiveDeatil = _db.Get<EmployeeArchiveModal>(Procedures.Employee_GetArcheiveCompleteDetail, new { EmployeeId = EmployeeId });
            return employeeArcheiveDeatil;
        }

        private string DeActivateEmployee(int EmployeeId)
        {
            EmployeeCompleteDetailModal employeeCompleteDetailModal = GetEmployeeCompleteDetail(EmployeeId);
            employeeCompleteDetailModal.EmployeeDetail.IsActive = false;
            var result = _db.Execute<EmployeeArchiveModal>(Procedures.Employee_DeActivate, new
            {
                EmployeeId,
                FullName = string.Concat(employeeCompleteDetailModal.EmployeeDetail.FirstName, " ", employeeCompleteDetailModal.EmployeeDetail.LastName),
                Mobile = employeeCompleteDetailModal.EmployeeDetail.Mobile,
                Email = employeeCompleteDetailModal.EmployeeDetail.Email,
                Package = employeeCompleteDetailModal.EmployeeDetail.FinalPackage,
                DateOfJoining = employeeCompleteDetailModal.EmployeeDetail.CreatedOn,
                DateOfLeaving = DateTime.UtcNow,
                EmployeeCompleteDetailModal = JsonConvert.SerializeObject(employeeCompleteDetailModal),
                AdminId = _currentSession.CurrentUserDetail.UserId
            }, true);
            if (string.IsNullOrEmpty(result))
                throw HiringBellException.ThrowBadRequest("Unable to dea-active the employee. Please contact to admin");

            return result;
        }

        private string ActivateEmployee(int EmployeeId)
        {
            EmployeeArchiveModal employeeArchiveDetail = GetEmployeeArcheiveCompleteDetail(EmployeeId);
            if (employeeArchiveDetail == null)
                throw HiringBellException.ThrowBadRequest("No record found");

            string newEncryptedPassword = "welcome@$Bot_001";
            EmployeeCompleteDetailModal employeeCompleteDetailModal = JsonConvert.DeserializeObject<EmployeeCompleteDetailModal>(employeeArchiveDetail.EmployeeCompleteJsonData);
            var result = _db.Execute<EmployeeCompleteDetailModal>(Procedures.Employee_Activate, new
            {
                EmployeeId = employeeArchiveDetail.EmployeeId,
                FirstName = employeeCompleteDetailModal.EmployeeDetail.FirstName,
                LastName = employeeCompleteDetailModal.EmployeeDetail.LastName,
                Mobile = employeeCompleteDetailModal.EmployeeDetail.Mobile,
                Email = employeeCompleteDetailModal.EmployeeDetail.Email,
                IsActive = true,
                ReportingManagerId = employeeCompleteDetailModal.EmployeeDetail.ReportingManagerId,
                DesignationId = employeeCompleteDetailModal.EmployeeDetail.DesignationId,
                UserTypeId = employeeCompleteDetailModal.EmployeeDetail.UserTypeId,
                LeavePlanId = employeeCompleteDetailModal.EmployeeDetail.LeavePlanId,
                PayrollGroupId = employeeCompleteDetailModal.EmployeeDetail.PayrollGroupId,
                SalaryGroupId = employeeCompleteDetailModal.EmployeeDetail.SalaryGroupId,
                CompanyId = employeeCompleteDetailModal.EmployeeDetail.CompanyId,
                NoticePeriodId = employeeCompleteDetailModal.EmployeeDetail.NoticePeriodId,
                SecondaryMobile = employeeCompleteDetailModal.EmployeeDetail.SecondaryMobile,
                PANNo = employeeCompleteDetailModal.EmployeeProfessionalDetail.PANNo,
                AadharNo = employeeCompleteDetailModal.EmployeeProfessionalDetail.AadharNo,
                AccountNumber = employeeCompleteDetailModal.EmployeeProfessionalDetail.AccountNumber,
                BankName = employeeCompleteDetailModal.EmployeeProfessionalDetail.BankName,
                BranchName = employeeCompleteDetailModal.EmployeeProfessionalDetail.BranchName,
                IFSCCode = employeeCompleteDetailModal.EmployeeProfessionalDetail.IFSCCode,
                Domain = employeeCompleteDetailModal.EmployeeProfessionalDetail.Domain,
                Specification = employeeCompleteDetailModal.EmployeeProfessionalDetail.Specification,
                ExprienceInYear = employeeCompleteDetailModal.EmployeeProfessionalDetail.ExperienceInYear,
                LastCompanyName = employeeCompleteDetailModal.EmployeeProfessionalDetail.LastCompanyName,
                ProfessionalDetail_Json = string.IsNullOrEmpty(employeeCompleteDetailModal.EmployeeProfessionalDetail.ProfessionalDetail_Json) ? "{}" : employeeCompleteDetailModal.EmployeeProfessionalDetail.ProfessionalDetail_Json,
                Gender = employeeCompleteDetailModal.PersonalDetail.Gender,
                FatherName = employeeCompleteDetailModal.PersonalDetail.FatherName,
                SpouseName = employeeCompleteDetailModal.PersonalDetail.SpouseName,
                MotherName = employeeCompleteDetailModal.PersonalDetail.MotherName,
                Address = employeeCompleteDetailModal.PersonalDetail.Address,
                State = employeeCompleteDetailModal.PersonalDetail.State,
                City = employeeCompleteDetailModal.PersonalDetail.City,
                Pincode = employeeCompleteDetailModal.PersonalDetail.Pincode,
                IsPermanent = employeeCompleteDetailModal.PersonalDetail.IsPermanent,
                ActualPackage = employeeCompleteDetailModal.PersonalDetail.ActualPackage,
                FinalPackage = employeeCompleteDetailModal.PersonalDetail.FinalPackage,
                TakeHomeByCandidate = employeeCompleteDetailModal.PersonalDetail.TakeHomeByCandidate,
                AccessLevelId = employeeCompleteDetailModal.EmployeeLoginDetail.AccessLevelId,
                Password = newEncryptedPassword,
                EmployeeDeclarationId = employeeCompleteDetailModal.EmployeeDeclarations.EmployeeDeclarationId,
                DocumentPath = employeeCompleteDetailModal.EmployeeDeclarations.DocumentPath,
                DeclarationDetail = string.IsNullOrEmpty(employeeCompleteDetailModal.EmployeeDeclarations.DeclarationDetail) ? "[]" : employeeCompleteDetailModal.EmployeeDeclarations.DeclarationDetail,
                HouseRentDetail = string.IsNullOrEmpty(employeeCompleteDetailModal.EmployeeDeclarations.HouseRentDetail) ? "[]" : employeeCompleteDetailModal.EmployeeDeclarations.HouseRentDetail,
                TotalDeclaredAmount = employeeCompleteDetailModal.EmployeeDeclarations.TotalAmount,
                TotalApprovedAmount = 0,
                LeaveRequestId = employeeCompleteDetailModal.LeaveRequestDetail.LeaveRequestId,
                LeaveDetail = string.IsNullOrEmpty(employeeCompleteDetailModal.LeaveRequestDetail.LeaveDetail) ? "[]" : employeeCompleteDetailModal.LeaveRequestDetail.LeaveDetail,
                Year = employeeCompleteDetailModal.LeaveRequestDetail.Year,
                EmployeeNoticePeriodId = employeeCompleteDetailModal.NoticePeriod.EmployeeNoticePeriodId,
                ApprovedOn = employeeCompleteDetailModal.NoticePeriod.ApprovedOn,
                ApplicableFrom = employeeCompleteDetailModal.NoticePeriod.ApplicableFrom,
                ApproverManagerId = employeeCompleteDetailModal.NoticePeriod.ApproverManagerId,
                ManagerDescription = employeeCompleteDetailModal.NoticePeriod.ManagerDescription,
                AttachmentPath = employeeCompleteDetailModal.NoticePeriod.AttachmentPath,
                EmailTitle = employeeCompleteDetailModal.NoticePeriod.EmailTitle,
                OtherApproverManagerIds = string.IsNullOrEmpty(employeeCompleteDetailModal.NoticePeriod.OtherApproverManagerIds) ? "[]" : employeeCompleteDetailModal.NoticePeriod.OtherApproverManagerIds,
                ITClearanceStatus = employeeCompleteDetailModal.NoticePeriod.ITClearanceStatus,
                ReportingManagerClearanceStatus = employeeCompleteDetailModal.NoticePeriod.ReportingManagerClearanceStatus,
                CanteenClearanceStatus = employeeCompleteDetailModal.NoticePeriod.CanteenClearanceStatus,
                ClientClearanceStatus = employeeCompleteDetailModal.NoticePeriod.ClientClearanceStatus,
                HRClearanceStatus = employeeCompleteDetailModal.NoticePeriod.HRClearanceStatus,
                OfficialLastWorkingDay = employeeCompleteDetailModal.NoticePeriod.OfficialLastWorkingDay,
                PeriodDuration = employeeCompleteDetailModal.NoticePeriod.PeriodDuration,
                EarlyLeaveStatus = employeeCompleteDetailModal.NoticePeriod.EarlyLeaveStatus,
                EmployeeComment = employeeCompleteDetailModal.NoticePeriod.EmployeeComment,
                CTC = employeeCompleteDetailModal.SalaryDetail.CTC,
                GrossIncome = employeeCompleteDetailModal.SalaryDetail.GrossIncome,
                NetSalary = employeeCompleteDetailModal.SalaryDetail.NetSalary,
                CompleteSalaryDetail = string.IsNullOrEmpty(employeeCompleteDetailModal.SalaryDetail.CompleteSalaryDetail) ? "[]" : employeeCompleteDetailModal.SalaryDetail.CompleteSalaryDetail,
                GroupId = employeeCompleteDetailModal.SalaryDetail.GroupId,
                TaxDetail = string.IsNullOrEmpty(employeeCompleteDetailModal.SalaryDetail.TaxDetail) ? "[]" : employeeCompleteDetailModal.SalaryDetail.TaxDetail,
                TimesheetId = employeeCompleteDetailModal.TimesheetDetails.TimesheetId,
                ClientId = employeeCompleteDetailModal.TimesheetDetails.ClientId,
                TimesheetWeeklyJson = string.IsNullOrEmpty(employeeCompleteDetailModal.TimesheetDetails.TimesheetWeeklyJson) ? "[]" : employeeCompleteDetailModal.TimesheetDetails.TimesheetWeeklyJson,
                ExpectedBurnedMinutes = employeeCompleteDetailModal.TimesheetDetails.ExpectedBurnedMinutes,
                ActualBurnedMinutes = employeeCompleteDetailModal.TimesheetDetails.ActualBurnedMinutes,
                TotalWeekDays = employeeCompleteDetailModal.TimesheetDetails.TotalWeekDays,
                TotalWorkingDays = employeeCompleteDetailModal.TimesheetDetails.TotalWorkingDays,
                TimesheetStatus = employeeCompleteDetailModal.TimesheetDetails.TimesheetStatus,
                MonthTimesheetApprovalState = employeeCompleteDetailModal.TimesheetDetails.TimesheetStatus,
                TimesheetStartDate = employeeCompleteDetailModal.TimesheetDetails.TimesheetStartDate,
                TimesheetEndDate = employeeCompleteDetailModal.TimesheetDetails.TimesheetEndDate,
                UserComments = employeeCompleteDetailModal.TimesheetDetails.UserComments,
                IsSaved = employeeCompleteDetailModal.TimesheetDetails.IsSaved,
                IsSubmitted = employeeCompleteDetailModal.TimesheetDetails.IsSubmitted,
                ForYear = employeeCompleteDetailModal.TimesheetDetails.ForYear,
                EmployeeMappedClientUid = employeeCompleteDetailModal.MappedClient.EmployeeMappedClientsUid,
                ClientName = employeeCompleteDetailModal.MappedClient.ClientName,
                BillingHours = employeeCompleteDetailModal.MappedClient.BillingHours,
                DaysPerWeek = employeeCompleteDetailModal.MappedClient.DaysPerWeek,
                DateOfJoining = employeeCompleteDetailModal.MappedClient.DateOfJoining,
                DateOfLeaving = employeeCompleteDetailModal.MappedClient.DateOfLeaving,
                DOB = employeeCompleteDetailModal.EmployeeDetail.DOB,
                OrganizationId = employeeCompleteDetailModal.EmployeeLoginDetail.OrganizationId,
                AvailableLeaves = employeeCompleteDetailModal.LeaveRequestDetail.AvailableLeaves,
                TotalLeaveApplied = employeeCompleteDetailModal.LeaveRequestDetail.TotalLeaveApplied,
                TotalApprovedLeave = employeeCompleteDetailModal.LeaveRequestDetail.TotalApprovedLeave,
                TotalLeaveQuota = employeeCompleteDetailModal.LeaveRequestDetail.TotalLeaveQuota,
                TotalRejectedAmount = employeeCompleteDetailModal.EmployeeDeclarations.TotalRejectedAmount,
                EmployeeCurrentRegime = employeeCompleteDetailModal.EmployeeDeclarations.EmployeeCurrentRegime,
                DeclarationStartMonth = employeeCompleteDetailModal.EmployeeDeclarations.DeclarationStartMonth,
                DeclarationEndMonth = employeeCompleteDetailModal.EmployeeDeclarations.DeclarationEndMonth,
                DeclarationFromYear = employeeCompleteDetailModal.EmployeeDeclarations.DeclarationFromYear,
                DeclarationToYear = employeeCompleteDetailModal.EmployeeDeclarations.DeclarationToYear,
                WorkShiftId = employeeCompleteDetailModal.EmployeeDetail.WorkShiftId,
                AdminId = _currentSession.CurrentUserDetail.UserId,
                LeaveQuotaDetail = string.IsNullOrEmpty(employeeCompleteDetailModal.LeaveRequestDetail.LeaveQuotaDetail) ?
                                    ApplicationConstants.EmptyJsonArray : employeeCompleteDetailModal.LeaveRequestDetail.LeaveQuotaDetail,
                IsPending = false,
                NewSalaryDetail = string.IsNullOrEmpty(employeeCompleteDetailModal.SalaryDetail.NewSalaryDetail) ?
                                    ApplicationConstants.EmptyJsonArray : employeeCompleteDetailModal.SalaryDetail.NewSalaryDetail,
                AssigneDate = employeeCompleteDetailModal.MappedClient.AssigneDate,
                FinancialStartYear = employeeCompleteDetailModal.EmployeeDeclarations.DeclarationFromYear
            }, true);

            if (string.IsNullOrEmpty(result))
                throw HiringBellException.ThrowBadRequest("Unable to active the employee. Please contact to admin");

            return result;
        }

        public List<Employee> ActivateOrDeActiveEmployeeService(int EmployeeId, bool IsActive)
        {
            if (EmployeeId == 1)
                throw HiringBellException.ThrowBadRequest("You can't delete the admin");

            List<Employee> employees = null;
            var status = string.Empty;
            FilterModel filterModel = new FilterModel
            {
                SearchString = "1=1",
                SortBy = "",
                PageIndex = 1,
                PageSize = 10
            };
            if (IsActive)
            {
                status = DeActivateEmployee(EmployeeId);
                employees = FilterInActiveEmployees(filterModel);
            }
            else
            {
                status = ActivateEmployee(EmployeeId);
                employees = FilterActiveEmployees(filterModel);
            }
            return employees;
        }

        #endregion

        #region Generate Employee Offer Letter

        public async Task<string> GenerateOfferLetterService(EmployeeOfferLetter employeeOfferLetter)
        {
            ValidateEmpOfferLetter(employeeOfferLetter);

            var company = _db.Get<OrganizationDetail>(Procedures.Company_GetById, new { CompanyId = employeeOfferLetter.CompanyId });
            string employeeName = employeeOfferLetter.FirstName + "_" + employeeOfferLetter.LastName;
            var html = GetHtmlString(company, employeeOfferLetter);
            var folderPath = GeneratedPdfOfferLetter(html, employeeName);
            var file = new FileDetail
            {
                FileName = employeeName,
                FilePath = folderPath
            };
            EmailSenderModal emailSenderModal = new EmailSenderModal
            {
                To = new List<string> { employeeOfferLetter.Email }, //receiver.Email,
                CC = new List<string>(),
                BCC = new List<string>(),
                FileDetails = new List<FileDetail> { file },
                Subject = "Offer Letter",
                Body = "Email Body",
                Title = "Title"
            };

            await _eMailManager.SendMailAsync(emailSenderModal);
            return "Generated successfuly";
        }

        private string GeneratedPdfOfferLetter(string html, string employeeName)
        {
            var folderPath = Path.Combine(_fileLocationDetail.DocumentFolder, "Employee_Offer_Letter");

            if (!Directory.Exists(Path.Combine(_fileLocationDetail.RootPath, folderPath)))
                Directory.CreateDirectory(Path.Combine(_fileLocationDetail.RootPath, folderPath));

            var destinationFilePath = Path.Combine(_fileLocationDetail.RootPath, folderPath,
               employeeName + $".{ApplicationConstants.Pdf}");
            _htmlToPdfConverter.ConvertToPdf(html, destinationFilePath);
            return folderPath;
        }

        private string GetHtmlString(OrganizationDetail organization, EmployeeOfferLetter employee)
        {
            string html = string.Empty;
            var LetterType = 1;
            var result = _db.Get<AnnexureOfferLetter>(Procedures.Annexure_Offer_Letter_Getby_Lettertype, new { CompanyId = 1, LetterType });
            if (File.Exists(result.FilePath))
                html = File.ReadAllText(result.FilePath);
            else
                throw HiringBellException.ThrowBadRequest("Offer letter is not found. Please contact to admin");

            html = html.Replace("[[Company-Name]]", organization.CompanyName).
            Replace("[[Generate-Date]]", DateTime.Now.ToString("dd MMM, yyyy")).
            Replace("[[Company-Address]]", organization.City).
            Replace("[[Employee-Name]]", employee.FirstName + " " + employee.LastName).
            Replace("[[CTC]]", employee.CTC.ToString()).
            Replace("[[Designation]]", employee.Designation).
            Replace("[[Joining-Date]]", employee.JoiningDate.ToString("dd MMM, yyyy"));

            return html;
        }

        private void ValidateEmpOfferLetter(EmployeeOfferLetter employeeOfferLetter)
        {
            if (employeeOfferLetter.CompanyId <= 0)
                throw new HiringBellException("Invalid company selected");

            if (employeeOfferLetter.CTC <= 0)
                throw new HiringBellException("CTC is invalid. Please enter a valid CTC");

            if (string.IsNullOrEmpty(employeeOfferLetter.FirstName))
                throw new HiringBellException("First name is null or empty. Please enter a valid first name");

            if (string.IsNullOrEmpty(employeeOfferLetter.LastName))
                throw new HiringBellException("Last name is null or empty. Please enter a valid last name");

            if (string.IsNullOrEmpty(employeeOfferLetter.Email))
                throw new HiringBellException("Email is null or empty. Please enter a valid email");

            if (string.IsNullOrEmpty(employeeOfferLetter.Designation))
                throw new HiringBellException("Designation is null or empty. Please enter a valid designation");

            if (employeeOfferLetter.JoiningDate == null)
                throw new HiringBellException("Date of joining is null. Please enter a valid date of joining");

            var mail = new MailAddress(employeeOfferLetter.Email);
            bool isValidEmail = mail.Host.Contains(".");
            if (!isValidEmail)
                throw new HiringBellException("Email is invalid. Please enter a valid email");
        }

        #endregion

        #region Employee Resignation

        public async Task<dynamic> GetEmployeeResignationByIdService(long employeeId)
        {
            if (employeeId < 0)
                throw HiringBellException.ThrowBadRequest("Invalid employee");

            (EmployeeNoticePeriod employeeNoticePeriod, CompanySetting companySetting) = _db.GetMulti<EmployeeNoticePeriod, CompanySetting>(Procedures.EMPLOYEE_NOTICE_PERIOD_GETBY_EMPID, new
            {
                EmployeeId = employeeId,
                CompanyId = _currentSession.CurrentUserDetail.CompanyId
            });

            if (companySetting == null)
                throw HiringBellException.ThrowBadRequest("Company setting not found. Please contact to admin");

            return await Task.FromResult(new { EmployeeNoticePeriod = employeeNoticePeriod, CompanySetting = companySetting });
        }

        public async Task<string> SubmitResignationService(EmployeeNoticePeriod employeeNoticePeriod)
        {
            validateResignationDetail(employeeNoticePeriod);
            var result = _db.Execute<EmployeeNoticePeriod>(Procedures.EMPLOYEE_NOTICE_PERIOD_INSUPD, new
            {
                EmployeeNoticePeriodId = employeeNoticePeriod.EmployeeNoticePeriodId,
                EmployeeId = employeeNoticePeriod.EmployeeId,
                IsDiscussWithManager = employeeNoticePeriod.IsDiscussWithManager,
                ApprovedOn = employeeNoticePeriod.ApprovedOn,
                ApplicableFrom = DateTime.UtcNow,
                ApproverManagerId = _currentSession.CurrentUserDetail.ReportingManagerId,
                ManagerDescription = "",
                AttachmentPath = "",
                EmailTitle = "",
                OtherApproverManagerIds = "[]",
                ITClearanceStatus = (int)ItemStatus.Pending,
                ReportingManagerClearanceStatus = (int)ItemStatus.Pending,
                CanteenClearanceStatus = (int)ItemStatus.Pending,
                ClientClearanceStatus = (int)ItemStatus.Pending,
                HRClearanceStatus = (int)ItemStatus.Pending,
                OfficialLastWorkingDay = DateTime.UtcNow.AddDays(employeeNoticePeriod.CompanyNoticePeriodInDays),
                PeriodDuration = employeeNoticePeriod.CompanyNoticePeriodInDays,
                EarlyLeaveStatus = (int)ItemStatus.Pending,
                EmployeeComment = employeeNoticePeriod.EmployeeComment,
                EmployeeReason = employeeNoticePeriod.EmployeeReason,
                IsDiscussWithEmployee = false,
                IsEmpResign = true,
                IsRecommendLastDay = true,
                IsRehire = false,
                Summary = "",
                ManagerComment = "",
                AdminId = _currentSession.CurrentUserDetail.AdminId
            }, true);
            if (string.IsNullOrEmpty(result))
                throw HiringBellException.ThrowBadRequest("Fail to submit resignation detail");

            return await Task.FromResult(result);
        }

        public async Task<string> ManageInitiateExistService(EmployeeNoticePeriod employeeNoticePeriod)
        {
            validateManageInitiateExistDetail(employeeNoticePeriod);
            EmployeeNoticePeriod existNoticePeriodDetail = null;
            if (employeeNoticePeriod.EmployeeNoticePeriodId > 0)
            {
                existNoticePeriodDetail = _db.Get<EmployeeNoticePeriod>(Procedures.EMPLOYEE_NOTICE_PERIOD_GETBY_ID, new { employeeNoticePeriod.EmployeeNoticePeriodId });
                if (existNoticePeriodDetail == null)
                    throw HiringBellException.ThrowBadRequest("Notice period detail not fount");

                existNoticePeriodDetail.IsEmpResign = employeeNoticePeriod.IsEmpResign;
                existNoticePeriodDetail.IsDiscussWithEmployee = employeeNoticePeriod.IsDiscussWithEmployee;
                existNoticePeriodDetail.Summary = employeeNoticePeriod.Summary;
                existNoticePeriodDetail.IsRecommendLastDay = employeeNoticePeriod.IsRecommendLastDay;
                existNoticePeriodDetail.OfficialLastWorkingDay = employeeNoticePeriod.OfficialLastWorkingDay;
                existNoticePeriodDetail.IsRehire = employeeNoticePeriod.IsRehire;
                existNoticePeriodDetail.ManagerComment = employeeNoticePeriod.ManagerComment;
            }
            else
            {
                existNoticePeriodDetail = employeeNoticePeriod;
            }

            var result = _db.Execute<EmployeeNoticePeriod>(Procedures.EMPLOYEE_NOTICE_PERIOD_INSUPD, new
            {
                EmployeeNoticePeriodId = existNoticePeriodDetail.EmployeeNoticePeriodId,
                EmployeeId = existNoticePeriodDetail.EmployeeId,
                IsDiscussWithManager = existNoticePeriodDetail.IsDiscussWithManager,
                ApprovedOn = existNoticePeriodDetail.ApprovedOn,
                ApplicableFrom = existNoticePeriodDetail.ApplicableFrom,
                ApproverManagerId = _currentSession.CurrentUserDetail.UserId,
                ManagerDescription = existNoticePeriodDetail.ManagerDescription,
                AttachmentPath = "",
                EmailTitle = "",
                OtherApproverManagerIds = "[]",
                ITClearanceStatus = (int)ItemStatus.Pending,
                ReportingManagerClearanceStatus = (int)ItemStatus.Approved,
                CanteenClearanceStatus = (int)ItemStatus.Pending,
                ClientClearanceStatus = (int)ItemStatus.Pending,
                HRClearanceStatus = (int)ItemStatus.Pending,
                OfficialLastWorkingDay = existNoticePeriodDetail.OfficialLastWorkingDay,
                PeriodDuration = existNoticePeriodDetail.CompanyNoticePeriodInDays,
                EarlyLeaveStatus = (int)ItemStatus.Pending,
                EmployeeComment = existNoticePeriodDetail.EmployeeComment,
                EmployeeReason = existNoticePeriodDetail.EmployeeReason,
                IsDiscussWithEmployee = existNoticePeriodDetail.IsDiscussWithEmployee,
                IsEmpResign = existNoticePeriodDetail.IsEmpResign,
                IsRecommendLastDay = existNoticePeriodDetail.IsRecommendLastDay,
                IsRehire = existNoticePeriodDetail.IsRehire,
                Summary = existNoticePeriodDetail.Summary,
                ManagerComment = existNoticePeriodDetail.ManagerComment,
                AdminId = _currentSession.CurrentUserDetail.UserId
            }, true);
            if (string.IsNullOrEmpty(result))
                throw HiringBellException.ThrowBadRequest("Fail to submit resignation detail");

            return await Task.FromResult(result);
        }

        private void validateManageInitiateExistDetail(EmployeeNoticePeriod employeeNoticePeriod)
        {
            if (employeeNoticePeriod.EmployeeId == 0)
                throw HiringBellException.ThrowBadRequest("Invalid employee");

            if (string.IsNullOrEmpty(employeeNoticePeriod.ManagerComment))
                throw HiringBellException.ThrowBadRequest("Invalid manager comment");

            if (string.IsNullOrEmpty(employeeNoticePeriod.Summary))
                throw HiringBellException.ThrowBadRequest("Invalid summary");

            if (employeeNoticePeriod.IsRecommendLastDay)
            {
                if (employeeNoticePeriod.OfficialLastWorkingDay == null)
                    throw HiringBellException.ThrowBadRequest("Invalid last working day selected");
            }
        }

        private void validateResignationDetail(EmployeeNoticePeriod employeeNoticePeriod)
        {
            if (employeeNoticePeriod.EmployeeId == 0)
                throw HiringBellException.ThrowBadRequest("Invalid employee");

            if (string.IsNullOrEmpty(employeeNoticePeriod.EmployeeComment))
                throw HiringBellException.ThrowBadRequest("Invalid employee comment");

            if (string.IsNullOrEmpty(employeeNoticePeriod.EmployeeReason))
                throw HiringBellException.ThrowBadRequest("Invalid reason selected");
        }

        #endregion

        #region Export Employee detail in excel

        public async Task<byte[]> ExportEmployeeService(int companyId, int fileType)
        {
            if (companyId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid company. Please login again");

            if (fileType == 0)
                throw HiringBellException.ThrowBadRequest("Invalid type selected. Please select a valid type");

            FilterModel filterModel = new FilterModel();
            var employees = _db.FetchDataSet(Procedures.Employee_GetAll, new
            {
                filterModel.SearchString,
                filterModel.SortBy,
                filterModel.PageIndex,
                PageSize = 1000
            });

            if (employees.Tables[0].Rows.Count > 0)
            {
                if (fileType == 2)
                {
                    var url = $"{_microserviceUrlLogs.GenerateExcel}/EmployeeSheet";
                    DataTable abc = employees.Tables[0];
                    var microserviceRequest = MicroserviceRequest.Builder(url);
                    microserviceRequest
                    .SetPayload(abc)
                    .SetDbConfigModal(_requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString))
                    .SetConnectionString(_currentSession.LocalConnectionString)
                    .SetCompanyCode(_currentSession.CompanyCode)
                    .SetToken(_currentSession.Authorization);

                    return await _requestMicroservice.PostRequest<byte[]>(microserviceRequest);
                }
            }

            return null;
        }

        #endregion

        #region Employee registion with Declaration by using excel
        public async Task RegisterEmployeeByExcelService(Employee employee, UploadedPayrollData uploaded)
        {
            var employeeEmailMobileCheck = CheckMobileEmailExistence(employee.EmployeeId, employee.Email, employee.Mobile);
            if (employeeEmailMobileCheck.EmailCount > 0)
                throw HiringBellException.ThrowBadRequest($"Email id: {employee.Email} already exists.");

            if (employeeEmailMobileCheck.MobileCount > 0)
                throw HiringBellException.ThrowBadRequest($"Mobile no: {employee.Mobile} already exists.");

            var result = await RegisterOrUpdateEmployeeDetail(employee, null, uploaded);

            if (!string.IsNullOrEmpty(result))
            {
                string componentId = string.Empty;
                long employeeId = Convert.ToInt64(result);

                List<EmployeeDeclaration> employeeDeclarations = new List<EmployeeDeclaration>();
                Employee emp = _db.Get<Employee>(Procedures.Employee_And_Declaration_Get_Byid, new { EmployeeId = employeeId });

                foreach (var item in uploaded.Investments)
                {
                    var values = item.Key.Split(" (");
                    if (values.Length > 0)
                    {
                        componentId = values[0].Trim();
                        EmployeeDeclaration employeeDeclaration = new EmployeeDeclaration
                        {
                            ComponentId = componentId,
                            DeclaredValue = item.Value,
                            Email = emp.Email,
                            EmployeeId = emp.EmployeeUid,
                            EmployeeDeclarationId = emp.EmployeeDeclarationId
                        };

                        employeeDeclarations.Add(employeeDeclaration);
                    }
                }
                try
                {
                    string url = $"{_microserviceUrlLogs.UpdateBulkDeclarationDetail}/{emp.EmployeeDeclarationId}";
                    var microserviceRequest = MicroserviceRequest.Builder(url);
                    microserviceRequest
                    .SetPayload(employeeDeclarations)
                    .SetDbConfigModal(_requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString))
                    .SetConnectionString(_currentSession.LocalConnectionString)
                    .SetCompanyCode(_currentSession.CompanyCode)
                    .SetToken(_currentSession.Authorization);

                    await _requestMicroservice.PutRequest<string>(microserviceRequest);
                }
                catch
                {
                    throw HiringBellException.ThrowBadRequest($"Investment not found. Component id: {componentId}. Investment id: {emp.EmployeeDeclarationId}");
                }
            }
        }

        #endregion

        #region Employee registion by using excel
        public async Task<List<Employee>> ReadEmployeeDataService(IFormFileCollection files)
        {
            try
            {
                var uploadedEmployeeData = await ReadPayrollExcelData(files);
                await UpdateEmployeeData(uploadedEmployeeData);
                return uploadedEmployeeData;
            }
            catch
            {
                throw;
            }
        }

        private async Task UpdateEmployeeData(List<Employee> employeeData)
        {
            int i = 0;
            int skipIndex = 0;
            int chunkSize = 50;
            while (i < employeeData.Count)
            {
                var emps = employeeData.Skip(skipIndex++ * chunkSize).Take(chunkSize).ToList();

                var ids = JsonConvert.SerializeObject(emps.Select(x => x.EmployeeId).ToList());
                var employees = _db.GetList<Employee>(Procedures.Active_Employees_By_Ids, new { EmployeeIds = ids });

                foreach (Employee e in emps)
                {
                    e.WorkShiftId = LocalConstants.DefaultWorkShiftId;
                    e.CompanyId = _currentSession.CurrentUserDetail.CompanyId;
                    e.OrganizationId = _currentSession.CurrentUserDetail.OrganizationId;
                    e.ReportingManagerId = LocalConstants.DefaultReportingMangerId;
                    e.UserTypeId = (int)UserType.Employee;
                    e.AccessLevelId = (int)RolesName.User;
                    e.LeavePlanId = LocalConstants.DefaultLeavePlanId;
                    e.SalaryGroupId = LocalConstants.DefaultSalaryGroupId;
                    e.DesignationId = LocalConstants.DefaultDesignation;

                    var em = employees.Find(x => x.EmployeeUid == e.EmployeeId);
                    if (em != null)
                    {
                        if (e.CTC > 0)
                        {
                            em.CTC = e.CTC;
                            em.IsCTCChanged = true;
                            await UpdateEmployeeService(em, null);
                        }
                    }
                    else
                    {
                        await RegisterEmployeeService(e, null);
                    }
                }

                i++;
            }
            await Task.CompletedTask;
        }

        private async Task<List<Employee>> ReadPayrollExcelData(IFormFileCollection files)
        {
            DataTable dataTable = null;
            List<Employee> employeesList = new List<Employee>();

            try
            {
                using (var ms = new MemoryStream())
                {
                    foreach (IFormFile file in files)
                    {
                        await file.CopyToAsync(ms);
                        ms.Seek(0, SeekOrigin.Begin);
                        FileInfo fileInfo = new FileInfo(file.FileName);
                        if (fileInfo.Extension == ".xlsx" || fileInfo.Extension == ".xls")
                        {
                            ms.Seek(0, SeekOrigin.Begin);
                            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                            using (var reader = ExcelReaderFactory.CreateReader(ms))
                            {
                                var result = reader.AsDataSet(new ExcelDataSetConfiguration
                                {
                                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                                    {
                                        UseHeaderRow = true
                                    }
                                });

                                dataTable = result.Tables[0];

                                employeesList = MappedEmployee(dataTable);
                            }
                        }
                        else
                        {
                            throw HiringBellException.ThrowBadRequest("Please select a valid excel file");
                        }
                    }
                }
            }
            catch
            {
                throw;
            }

            return employeesList;
        }

        public static List<Employee> MappedEmployee(DataTable table)
        {
            string TypeName = string.Empty;
            DateTime date = DateTime.Now;
            DateTime defaultDate = Convert.ToDateTime("1976-01-01");
            List<Employee> items = new List<Employee>();
            string[] dateFormats = { "MM/dd/yyyy", "dd-MM-yyyy", "yyyy/MM/dd", "yyyy-MM-dd", "dd-MMM-yyyy" };

            try
            {
                List<PropertyInfo> props = typeof(Employee).GetProperties().ToList();
                List<string> fieldNames = ValidateHeaders(table, props);

                if (table.Rows.Count > 0)
                {
                    int i = 0;
                    DataRow dr = null;
                    while (i < table.Rows.Count)
                    {
                        dr = table.Rows[i];

                        Employee t = new Employee();
                        fieldNames.ForEach(n =>
                        {
                            var x = props.Find(i => i.Name == n);
                            if (x != null)
                            {
                                try
                                {
                                    if (x.PropertyType.IsGenericType)
                                        TypeName = x.PropertyType.GenericTypeArguments.First().Name;
                                    else
                                        TypeName = x.PropertyType.Name;

                                    switch (TypeName)
                                    {
                                        case nameof(Boolean):
                                            if (dr[x.Name] != DBNull.Value)
                                            {
                                                if (dr[x.Name].ToString().Equals("Yes", StringComparison.OrdinalIgnoreCase))
                                                    x.SetValue(t, true);
                                                else if (dr[x.Name].ToString().Equals("No", StringComparison.OrdinalIgnoreCase))
                                                    x.SetValue(t, false);
                                                else if (dr[x.Name].ToString().Equals("Any", StringComparison.OrdinalIgnoreCase))
                                                    x.SetValue(t, false);
                                                else
                                                    x.SetValue(t, Convert.ToBoolean(dr[x.Name]));
                                            }
                                            else
                                            {
                                                x.SetValue(t, default(bool));
                                            }
                                            break;
                                        case nameof(Int32):
                                            if (dr[x.Name] != DBNull.Value)
                                            {
                                                if (dr[x.Name].ToString().Equals(LocalConstants.Male, StringComparison.OrdinalIgnoreCase))
                                                    x.SetValue(t, 1);
                                                else if (dr[x.Name].ToString().Equals(LocalConstants.Female, StringComparison.OrdinalIgnoreCase))
                                                    x.SetValue(t, 2);
                                                else if (dr[x.Name].ToString().Equals(LocalConstants.Any, StringComparison.OrdinalIgnoreCase))
                                                    x.SetValue(t, 3);
                                                else
                                                    x.SetValue(t, Convert.ToInt32(dr[x.Name]));
                                            }
                                            else
                                            {
                                                x.SetValue(t, 0);
                                            }
                                            break;
                                        case nameof(Int64):
                                            if (dr[x.Name] != DBNull.Value)
                                                x.SetValue(t, Convert.ToInt64(dr[x.Name]));
                                            else
                                                x.SetValue(t, 0);
                                            break;
                                        case nameof(Decimal):
                                            if (dr[x.Name] != DBNull.Value)
                                                x.SetValue(t, Convert.ToDecimal(dr[x.Name]));
                                            else
                                                x.SetValue(t, Decimal.Zero);
                                            break;
                                        case nameof(String):
                                            if (dr[x.Name] != DBNull.Value)
                                                x.SetValue(t, dr[x.Name].ToString());
                                            else
                                                x.SetValue(t, string.Empty);
                                            break;
                                        case nameof(DateTime):
                                            if (dr[x.Name] == DBNull.Value || dr[x.Name].ToString() != null)
                                            {
                                                if (DateTime.TryParseExact(dr[x.Name].ToString(), dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateOfBirth))
                                                    date = dateOfBirth;
                                                else if (DateTime.TryParse(dr[x.Name].ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out dateOfBirth))
                                                    date = dateOfBirth;
                                                else
                                                    date = Convert.ToDateTime(dr[x.Name].ToString());

                                                date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                                                x.SetValue(t, date);
                                            }
                                            else
                                            {
                                                x.SetValue(t, defaultDate);
                                            }
                                            break;
                                        default:
                                            x.SetValue(t, dr[x.Name]);
                                            break;
                                    }
                                }
                                catch
                                {
                                    throw;
                                }
                            }
                        });

                        items.Add(t);
                        i++;
                    }
                }
            }
            catch
            {
                throw;
            }

            return items;
        }

        private static List<string> ValidateHeaders(DataTable table, List<PropertyInfo> fileds)
        {
            List<string> columnList = new List<string>();

            foreach (DataColumn column in table.Columns)
            {
                if (!column.ColumnName.ToLower().Contains("column"))
                {
                    if (!columnList.Contains(column.ColumnName))
                    {
                        columnList.Add(column.ColumnName);
                    }
                    else
                    {
                        throw HiringBellException.ThrowBadRequest($"Multiple header found \"{column.ColumnName}\" field.");
                    }
                }
            }

            return columnList;
        }

        #endregion

    }
}