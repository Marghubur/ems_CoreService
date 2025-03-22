using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using Bot.CoreBottomHalf.CommonModal.Enums;
using BottomHalf.Utilities.UtilService;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using Bt.Lib.PipelineConfig.MicroserviceHttpRequest;
using Bt.Lib.PipelineConfig.Model;
using DocMaker.ExcelMaker;
using DocMaker.PdfService;
using EMailService.Modal;
using EMailService.Modal.EmployeeModal;
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
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Department = EMailService.Modal.Department;
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
        private readonly ICommonService _commonService;
        private List<EmployeeRole> _designations;
        private List<Department> _departments;
        private Dictionary<string, Func<string, string, bool>> _validators;

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
            MicroserviceRegistry microserviceUrlLogs,
            ICommonService commonService)
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
            _commonService = commonService;
        }

        #region Code Used for employee insert or update

        /// <summary>
        /// This service will be used to register new employee and Employee object must contain complete information of the present employee
        /// </summary>
        /// <param name="employee"></param>
        /// <param name="fileCollection"></param>
        /// <returns></returns>
        public async Task<string> RegisterEmployeeService(Employee employee, IFormFileCollection fileCollection, bool IsNewRegistration = false)
        {
            var result = CheckMobileEmailExistence(employee.EmployeeId, employee.Email, employee.Mobile);
            if (result.EmailCount > 0)
                throw HiringBellException.ThrowBadRequest($"Email id: {employee.Email} already exists.");

            if (result.MobileCount > 0)
                throw HiringBellException.ThrowBadRequest($"Mobile no: {employee.Mobile} already exists.");

            await RegisterOrUpdateEmployeeDetail(employee, fileCollection, null, IsNewRegistration);

            return ApplicationConstants.Successfull;
        }

        public async Task<string> ManageEmpNomineeDetailService(EmployeeNomineeDetail employeeNomineeDetail)
        {
            if (employeeNomineeDetail.EmployeeId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid employee");

            var result = await _db.ExecuteAsync(Procedures.EMPLOYEES_NOMINEE_INS_UPD, new
            {
                employeeNomineeDetail.NomineeId,
                employeeNomineeDetail.EmployeeId,
                employeeNomineeDetail.NomineeName,
                employeeNomineeDetail.NomineeRelationship,
                employeeNomineeDetail.NomineeMobile,
                employeeNomineeDetail.NomineeEmail,
                employeeNomineeDetail.NomineeDOB,
                employeeNomineeDetail.NomineeAddress,
                employeeNomineeDetail.PercentageShare,
                employeeNomineeDetail.IsPrimaryNominee,
                ProfileStatusCode = "111111111",
            }, true);

            if (string.IsNullOrEmpty(result.statusMessage))
                throw HiringBellException.ThrowBadRequest("Fail to register new employee.");

            return ApplicationConstants.Successfull;
        }

        public async Task<string> ManageEmpBackgroundVerificationDetailService(EmployeeBackgroundVerification employeeBackgroundVerification)
        {
            try
            {
                if (employeeBackgroundVerification.EmployeeUid < 0)
                    throw HiringBellException.ThrowBadRequest("Invalid employee");

                var result = await _db.ExecuteAsync(Procedures.EMPLOYEES_BACKGROUNDVERIFICATION_DETAIL_UPD, new
                {
                    employeeBackgroundVerification.EmployeeUid,
                    employeeBackgroundVerification.AgencyName,
                    employeeBackgroundVerification.VerificationRemark,
                    employeeBackgroundVerification.VerificationStatus,
                    ProfileStatusCode = "111111100",
                    AdminId = _currentSession.CurrentUserDetail.UserId
                }, true);

                if (string.IsNullOrEmpty(result.statusMessage))
                    throw HiringBellException.ThrowBadRequest("Fail to register new employee.");

                return ApplicationConstants.Successfull;
            }
            catch
            {
                throw;
            }
        }

        public async Task<string> ManageEmpPrevEmploymentDetailService(PrevEmploymentDetail prevEmploymentDetail)
        {
            try
            {
                if (prevEmploymentDetail.EmployeeUid < 0)
                    throw HiringBellException.ThrowBadRequest("Invalid employee");

                if (prevEmploymentDetail.ExprienceInYear < 0)
                    throw HiringBellException.ThrowBadRequest("Invalid experience entered");

                var result = await _db.ExecuteAsync(Procedures.EMPLOYEES_PREVEMPLOYMENT_DETAIL_UPD, new
                {
                    prevEmploymentDetail.EmployeeUid,
                    prevEmploymentDetail.LastCompanyDesignation,
                    prevEmploymentDetail.WorkingFromDate,
                    prevEmploymentDetail.WorkingToDate,
                    prevEmploymentDetail.LastCompanyAddress,
                    prevEmploymentDetail.LastCompanyNatureOfDuty,
                    prevEmploymentDetail.LastDrawnSalary,
                    prevEmploymentDetail.ExprienceInYear,
                    prevEmploymentDetail.LastCompanyName,
                    ProfileStatusCode = "111110000",
                    AdminId = _currentSession.CurrentUserDetail.UserId
                }, true);

                if (string.IsNullOrEmpty(result.statusMessage))
                    throw HiringBellException.ThrowBadRequest("Fail to register previous employement detail.");

                return ApplicationConstants.Successfull;
            }
            catch
            {
                throw HiringBellException.ThrowBadRequest("Fail to insert employee previous employment detail");
            }
        }

        public async Task<string> ManageEmpProfessionalDetailService(EmployeeProfessionalDetail employeeProfessionalDetail)
        {
            try
            {
                ValidateEmployeeProfessionalDetail(employeeProfessionalDetail);
                var isEpforEsiChanged = await IsEpforEsiChanged(employeeProfessionalDetail.EmployeeUid, employeeProfessionalDetail.IsEmployeeEligibleForPF, employeeProfessionalDetail.IsEmployeeEligibleForESI);

                var result = await _db.ExecuteAsync(Procedures.EMPLOYEES_PROFESSIONALDETAIL_UPD, new
                {
                    employeeProfessionalDetail.EmployeeUid,
                    employeeProfessionalDetail.PANNo,
                    employeeProfessionalDetail.AadharNo,
                    employeeProfessionalDetail.BankName,
                    employeeProfessionalDetail.AccountNumber,
                    employeeProfessionalDetail.IFSCCode,
                    employeeProfessionalDetail.BranchName,
                    employeeProfessionalDetail.BankAccountType,
                    employeeProfessionalDetail.PFNumber,
                    employeeProfessionalDetail.UAN,
                    employeeProfessionalDetail.ESISerialNumber,
                    employeeProfessionalDetail.PFAccountCreationDate,
                    employeeProfessionalDetail.IsEmployeeEligibleForESI,
                    employeeProfessionalDetail.IsEmployeeEligibleForPF,
                    employeeProfessionalDetail.IsExistingMemberOfPF,
                    ProfileStatusCode = "111100000",
                    AdminId = _currentSession.CurrentUserDetail.UserId
                }, true);

                if (string.IsNullOrEmpty(result.statusMessage))
                    throw HiringBellException.ThrowBadRequest("Fail to register new employee.");

                if (isEpforEsiChanged)
                    await ReBuildSalaryBreakup(employeeProfessionalDetail.EmployeeUid, false);

                return ApplicationConstants.Successfull;
            }
            catch
            {
                throw HiringBellException.ThrowBadRequest("Fail to insert employee professional detail");
            }
        }

        private async Task<bool> IsEpforEsiChanged(long employeeId, bool isEmployeeEligibleForPF, bool isEmployeeEligibleForESI)
        {
            bool isEpforEsiChange = false;
            var (pfEsiSetting, employeePfDetail) = _db.GetMulti<PfEsiSetting, EmployeePfDetail>(Procedures.PF_ESI_SETTING_EMPLOYEE_PF_DETAIL_GET, new
            {
                _currentSession.CurrentUserDetail.CompanyId,
                EmployeeId = employeeId
            });

            if (employeePfDetail.IsEmployeeEligibleForPF != isEmployeeEligibleForPF || employeePfDetail.IsEmployeeEligibleForESI != isEmployeeEligibleForESI)
                isEpforEsiChange = true;

            return await Task.FromResult(isEpforEsiChange);
        }

        private void ValidateEmployeeProfessionalDetail(EmployeeProfessionalDetail employeeProfessionalDetail)
        {
            if (employeeProfessionalDetail.EmployeeUid < 0)
                throw HiringBellException.ThrowBadRequest("Invalid employee");

            if (string.IsNullOrEmpty(employeeProfessionalDetail.PANNo))
                throw HiringBellException.ThrowBadRequest("Invalid PAN No.");

            if (string.IsNullOrEmpty(employeeProfessionalDetail.AccountNumber))
                throw HiringBellException.ThrowBadRequest("Invalid account No.");

            if (string.IsNullOrEmpty(employeeProfessionalDetail.BankName))
                throw HiringBellException.ThrowBadRequest("Invalid bank name");

            if (string.IsNullOrEmpty(employeeProfessionalDetail.IFSCCode))
                throw HiringBellException.ThrowBadRequest("Invalid ifsc code name");
        }

        public async Task<string> ManageEmpAddressDetailService(EmployeeAddressDetail employeeAddressDetail)
        {
            try
            {
                if (employeeAddressDetail.EmployeeUid < 0)
                    throw HiringBellException.ThrowBadRequest("Invalid employee");

                var result = await _db.ExecuteAsync(Procedures.EMPLOYEES_ADDRESSDETAIL_UPD, new
                {
                    employeeAddressDetail.EmployeeUid,
                    employeeAddressDetail.Country,
                    employeeAddressDetail.State,
                    employeeAddressDetail.City,
                    employeeAddressDetail.Pincode,
                    employeeAddressDetail.Address,
                    employeeAddressDetail.PermanentCountry,
                    employeeAddressDetail.PermanentState,
                    employeeAddressDetail.PermanentCity,
                    employeeAddressDetail.PermanentPincode,
                    employeeAddressDetail.PermanentAddress,
                    ProfileStatusCode = "111000000",
                    AdminId = _currentSession.CurrentUserDetail.UserId
                }, true);

                if (string.IsNullOrEmpty(result.statusMessage))
                    throw HiringBellException.ThrowBadRequest("Fail to add employee address detail.");

                return ApplicationConstants.Successfull;
            }
            catch
            {
                throw HiringBellException.ThrowBadRequest("Fail to insert employee address detail");
            }
        }

        public async Task<string> ManageEmpPerosnalDetailService(EmpPersonalDetail empPersonalDetail)
        {
            try
            {
                ValidateEmployeeDetail(empPersonalDetail);

                var result = await _db.ExecuteAsync(Procedures.SP_EMPLOYEES_PERSONALDETAIL_UPD, new
                {
                    empPersonalDetail.EmployeeUid,
                    empPersonalDetail.FatherName,
                    empPersonalDetail.MotherName,
                    empPersonalDetail.SpouseName,
                    empPersonalDetail.MaritalStatus,
                    empPersonalDetail.MarriageDate,
                    empPersonalDetail.CountryOfOrigin,
                    empPersonalDetail.Religion,
                    empPersonalDetail.BloodGroup,
                    empPersonalDetail.IsPhChallanged,
                    empPersonalDetail.IsInternationalEmployee,
                    empPersonalDetail.EmergencyContactName,
                    empPersonalDetail.RelationShip,
                    empPersonalDetail.EmergencyMobileNo,
                    empPersonalDetail.EmergencyCountry,
                    empPersonalDetail.EmergencyState,
                    empPersonalDetail.EmergencyCity,
                    empPersonalDetail.EmergencyPincode,
                    empPersonalDetail.EmergencyAddress,
                    empPersonalDetail.Domain,
                    empPersonalDetail.Specification,
                    ProfileStatusCode = "110000000",
                    AdminId = _currentSession.CurrentUserDetail.UserId
                }, true);

                if (string.IsNullOrEmpty(result.statusMessage))
                    throw HiringBellException.ThrowBadRequest("Fail to add employee personal detail.");

                return ApplicationConstants.Successfull;
            }
            catch
            {
                throw HiringBellException.ThrowBadRequest("Fail to insert employee personal detail");
            }
        }

        private void ValidateEmployeeDetail(EmpPersonalDetail empPersonalDetail)
        {
            if (empPersonalDetail.EmployeeUid < 0)
                throw HiringBellException.ThrowBadRequest("Invalid employee");

            if (string.IsNullOrEmpty(empPersonalDetail.FatherName))
                throw HiringBellException.ThrowBadRequest("Invalid faher name");
        }

        public async Task<(EmployeeBasicInfo employeeBasic, List<FileDetail> fileDetails)> ManageEmployeeBasicInfoService(EmployeeBasicInfo employee, IFormFileCollection fileCollection)
        {
            var result = CheckMobileEmailExistence(employee.EmployeeUid, employee.Email, employee.Mobile);
            if (result.EmailCount > 0)
                throw HiringBellException.ThrowBadRequest($"Email id: {employee.Email} already exists.");

            if (result.MobileCount > 0)
                throw HiringBellException.ThrowBadRequest($"Mobile no: {employee.Mobile} already exists.");

            return await RegisterOrUpdateEmployeeBasicDetail(employee, fileCollection);
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

        private async Task<(EmployeeBasicInfo employeeBasic, List<FileDetail> fileDetails)> RegisterOrUpdateEmployeeBasicDetail(EmployeeBasicInfo employeeBasicInfo, IFormFileCollection fileCollection)
        {
            try
            {
                bool isNewRegistration = false;
                bool isCTCChanged = false;

                ValidateEmployeeBasicInfo(employeeBasicInfo);

                if (employeeBasicInfo.ReportingManagerId == 0)
                    employeeBasicInfo.ReportingManagerId = _currentSession.CurrentUserDetail.UserId;

                if (employeeBasicInfo.EmployeeUid == 0)
                {
                    isNewRegistration = true;

                    employeeBasicInfo.Password = UtilService.Encrypt(
                        _configuration.GetSection("DefaultNewEmployeePassword").Value,
                        _configuration.GetSection("EncryptSecret").Value
                    );
                }
                else
                {
                    isCTCChanged = await IsEmployeeCTCChanged(employeeBasicInfo.EmployeeUid, employeeBasicInfo.CTC);
                }

                await PrepareEmployeeBasicInfoInsertData(employeeBasicInfo);

                var fileDetails = await EmployeeFileInsertUpdate(fileCollection, employeeBasicInfo.EmployeeUid, employeeBasicInfo.OldFileName, employeeBasicInfo.FileId);
                EmployeeCalculation eCal = await GetEmployeeDeclarationDetail(employeeBasicInfo);
                await AddEmployeeSalaryLeaveAndDeclarationDetail(employeeBasicInfo.EmployeeUid, eCal);

                if (!isNewRegistration && isCTCChanged)
                    await ReBuildSalaryBreakup(employeeBasicInfo.EmployeeUid, false);
                else
                    await CheckRunLeaveAccrualCycle(employeeBasicInfo.EmployeeUid);

                return (employeeBasicInfo, fileDetails);
            }
            catch
            {
                throw;
            }
        }

        private async Task ReBuildSalaryBreakup(long employeeId, bool isRecalculateFromCurrentMonth)
        {
            string url = $"{_microserviceUrlLogs.RebuildBreakup}/{isRecalculateFromCurrentMonth}/{employeeId}";
            var microserviceRequest = MicroserviceRequest.Builder(url);
            microserviceRequest
            .SetDbConfig(_requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString))
            .SetConnectionString(_currentSession.LocalConnectionString)
            .SetCompanyCode(_currentSession.CompanyCode)
            .SetToken(_currentSession.Authorization);

            var response = await _requestMicroservice.GetRequest<string>(microserviceRequest);
            if (response is null)
                throw HiringBellException.ThrowBadRequest("fail to get response");
        }

        private async Task<bool> IsEmployeeCTCChanged(long employeeId, decimal ctc)
        {
            var employeeSalaryDetail = _db.Get<EmployeeSalaryDetail>(Procedures.EMPLOYEE_SALARY_DETAIL_GET_BY_EMPID, new
            {
                FinancialStartYear = _currentSession.FinancialStartYear,
                EmployeeId = employeeId
            });

            if (employeeSalaryDetail != null && employeeSalaryDetail.CTC != ctc)
                return await Task.FromResult(true);

            return await Task.FromResult(false);
        }

        private async Task<EmployeeCalculation> GetEmployeeDeclarationDetail(EmployeeBasicInfo employeeBasicInfo)
        {
            var dataSet = _db.FetchDataSet(Procedures.EMPLOYEE_DECLARATION_DETAIL_GET_BY_EMPID, new
            {
                EmployeeId = employeeBasicInfo.EmployeeUid,
                _currentSession.FinancialStartYear
            });

            if (dataSet == null || dataSet.Tables.Count != 2)
                throw HiringBellException.ThrowBadRequest("Fail to get salary detail and salary components");

            if (dataSet.Tables[0].Rows.Count == 0 || dataSet.Tables[1].Rows.Count == 0)
            {
                return await GetDeclarationDetail(employeeBasicInfo.EmployeeUid, employeeBasicInfo.CTC, ApplicationConstants.DefaultTaxRegin);
            }
            else
            {
                var eCal = new EmployeeCalculation();

                eCal.employeeSalaryDetail = Converter.ToType<EmployeeSalaryDetail>(dataSet.Tables[1]);
                eCal.employeeDeclaration = Converter.ToType<EmployeeDeclaration>(dataSet.Tables[0]);
                eCal.Doj = employeeBasicInfo.DateOfJoining;

                return await Task.FromResult(eCal);
            }
        }

        private async Task AddEmployeeSalaryLeaveAndDeclarationDetail(long employeeId, EmployeeCalculation eCal)
        {
            var result = await _db.ExecuteAsync(Procedures.GENERATE_EMP_LEAVE_DECLARATION_SALARYDETAIL, new
            {
                EmployeeId = employeeId,
                _currentSession.CurrentUserDetail.CompanyId,
                eCal.employeeDeclaration.DeclarationDetail,
                eCal.employeeSalaryDetail.CompleteSalaryDetail,
                NewSalaryDetail = "[]",
                eCal.employeeSalaryDetail.TaxDetail,
                eCal.employeeSalaryDetail.GrossIncome,
                eCal.employeeSalaryDetail.NetSalary
            }, true);

            if (string.IsNullOrEmpty(result.statusMessage))
                throw HiringBellException.ThrowBadRequest("Fail to add employee salary detail, leave and declaration");
        }

        private async Task PrepareEmployeeBasicInfoInsertData(EmployeeBasicInfo employeeBasicInfo)
        {
            if (employeeBasicInfo.AccessLevelId != (int)RolesName.Admin)
                employeeBasicInfo.UserTypeId = (int)RolesName.User;
            else
                employeeBasicInfo.UserTypeId = (int)RolesName.Admin;

            var result = await _db.ExecuteAsync(Procedures.EMPLOYEES_BASICINFO_INS_UPD, new
            {
                employeeBasicInfo.EmployeeUid,
                employeeBasicInfo.FirstName,
                employeeBasicInfo.LastName,
                employeeBasicInfo.Mobile,
                employeeBasicInfo.Email,
                employeeBasicInfo.ReportingManagerId,
                employeeBasicInfo.DesignationId,
                employeeBasicInfo.DepartmentId,
                employeeBasicInfo.UserTypeId,
                employeeBasicInfo.LeavePlanId,
                employeeBasicInfo.SalaryGroupId,
                employeeBasicInfo.PayrollGroupId,
                CompanyId = _currentSession.CurrentUserDetail.CompanyId,
                employeeBasicInfo.WorkShiftId,
                employeeBasicInfo.DateOfJoining,
                employeeBasicInfo.SecondaryMobile,
                employeeBasicInfo.Password,
                _currentSession.CurrentUserDetail.OrganizationId,
                employeeBasicInfo.CTC,
                employeeBasicInfo.AccessLevelId,
                employeeBasicInfo.DOB,
                employeeBasicInfo.Location,
                employeeBasicInfo.Gender,
                ProfileStatusCode = "100000000",
                AdminId = _currentSession.CurrentUserDetail.UserId
            }, true);

            if (string.IsNullOrEmpty(result.statusMessage))
                throw HiringBellException.ThrowBadRequest("Fail to register new employee.");

            long employeeId = Convert.ToInt64(result.statusMessage);
            if (employeeId == 0)
                throw HiringBellException.ThrowBadRequest("Fail to register new employee.");

            employeeBasicInfo.EmployeeUid = employeeId;

            await Task.CompletedTask;
        }

        private void ValidateEmployeeBasicInfo(EmployeeBasicInfo employeeBasicInfo)
        {
            if (string.IsNullOrEmpty(employeeBasicInfo.FirstName))
                throw HiringBellException.ThrowBadRequest("Invalid first name");

            if (string.IsNullOrEmpty(employeeBasicInfo.LastName))
                throw HiringBellException.ThrowBadRequest("Invalid last name");

            if (string.IsNullOrEmpty(employeeBasicInfo.Mobile))
                throw HiringBellException.ThrowBadRequest("Invalid mobile number");

            if (string.IsNullOrEmpty(employeeBasicInfo.Email))
                throw HiringBellException.ThrowBadRequest("Invalid email id");

            if (employeeBasicInfo.DOB == null)
                throw HiringBellException.ThrowBadRequest("Invalid date of birth");

            if (employeeBasicInfo.CTC <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid CTC");

            if (employeeBasicInfo.DesignationId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid designation selected");

            if (employeeBasicInfo.WorkShiftId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid shift selected");

            EmailAddressAttribute email = new EmailAddressAttribute();
            if (!email.IsValid(employeeBasicInfo.Email))
                throw HiringBellException.ThrowBadRequest("Please enter a valid email id");
        }

        private async Task<List<FileDetail>> EmployeeFileInsertUpdate(IFormFileCollection fileCollection, long empId, string oldFileName, int fileId)
        {
            if (fileCollection != null && fileCollection.Count > 0)
            {
                var ownerPath = Path.Combine(_currentSession.CompanyCode, _fileLocationDetail.User, $"{nameof(UserType.Employee)}_{empId}");
                string url = $"{_microserviceUrlLogs.SaveApplicationFile}";
                FileFolderDetail fileFolderDetail = new FileFolderDetail
                {
                    FolderPath = ownerPath,
                    OldFileName = string.IsNullOrEmpty(oldFileName) ? null : new List<string> { oldFileName },
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

                var file = files.FirstOrDefault();
                var result = await _db.ExecuteAsync(Procedures.Userfiledetail_Upload, new
                {
                    FileId = fileId,
                    FileOwnerId = empId,
                    FileName = file.FileName.Contains(".") ? file.FileName : file.FileName + "." + file.FileExtension,
                    FilePath = file.FilePath,
                    FileExtension = file.FileExtension,
                    UserTypeId = (int)UserType.Employee,
                    ItemStatusId = LocalConstants.Profile,
                    AdminId = _currentSession.CurrentUserDetail.UserId
                }, true);

                if (string.IsNullOrEmpty(result.statusMessage))
                    throw HiringBellException.ThrowBadRequest("Faile insert employee profile image");

                return await GetUserFileDetail(empId);
            }

            return await Task.FromResult(new List<FileDetail>());
        }

        private async Task<List<FileDetail>> GetUserFileDetail(long fileownerId, int userTypeId = (int)UserType.Employee, int itemStatusId = 1)
        {
            var result = _db.GetList<FileDetail>(Procedures.USERFILES_GETBY_OWNERID_ITEMSTATUS, new
            {
                FileOwnerId = fileownerId,
                UserTypeId = userTypeId,
                ItemStatusId = itemStatusId
            });

            return await Task.FromResult(result);
        }

        public async Task<string> RegisterOrUpdateEmployeeDetail(Employee employee, IFormFileCollection fileCollection, UploadedPayrollData uploadedPayrollData = null, bool IsNewRegistration = false)
        {
            //bool IsNewRegistration = false;
            long employeeUid = 0;

            try
            {
                string EncryptedPassword = string.Empty;
                //var empId = Convert.ToInt32(employee.EmployeeUid);

                // validate employee
                ValidateEmployee(employee);

                // validate employee detail
                ValidateEmployeeDetails(employee);

                await ManagerProfessionalDetail(employee);

                await AssignReportingManager(employee);

                _currentSession.TimeZoneNow = _timezoneConverter.ToTimeZoneDateTime(DateTime.UtcNow, _currentSession.TimeZone);

                // prepare for new insert of employee
                await PrepareEmployeeInsertData(employee, IsNewRegistration);

                employeeUid = employee.EmployeeUid;

                int currentRegimeId = ApplicationConstants.DefaultTaxRegin;

                if (uploadedPayrollData != null)
                {
                    await SetupPreviousEmployerIncome(employeeUid, uploadedPayrollData);
                    currentRegimeId = string.IsNullOrEmpty(uploadedPayrollData.Regime) && uploadedPayrollData.Regime.ToLower().Contains("new")
                                            ? ApplicationConstants.NewRegim : ApplicationConstants.OldRegim;
                }

                var isEpforEsiChanged = false;
                if (!IsNewRegistration)
                    isEpforEsiChanged = await IsEpforEsiChanged(employeeUid, employee.IsEmployeeEligibleForPF, employee.IsEmployeeEligibleForESI);

                var eCal = new EmployeeCalculation();

                if (IsNewRegistration)
                {
                    EncryptedPassword = UtilService.Encrypt(
                        _configuration.GetSection("DefaultNewEmployeePassword").Value,
                        _configuration.GetSection("EncryptSecret").Value
                    );

                    eCal = await GetDeclarationDetail(employee.EmployeeId, employee.CTC, currentRegimeId);
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

                if (isEpforEsiChanged)
                    await ReBuildSalaryBreakup(employeeUid, false);

                return employeeId;
            }
            catch
            {
                if (IsNewRegistration && employeeUid > 0)
                    _db.Execute(Procedures.Employee_Delete_by_EmpId, new { EmployeeId = employeeUid }, false);

                throw;
            }
        }

        private async Task PrepareEmployeeInsertData(Employee employee, bool IsNewRegistration)
        {
            if (employee.AccessLevelId != (int)RolesName.Admin)
                employee.UserTypeId = (int)RolesName.User;

            if (string.IsNullOrEmpty(employee.NewSalaryDetail))
                employee.NewSalaryDetail = "[]";

            employee.EmployeeId = employee.EmployeeUid;
            if (IsNewRegistration)
            {
                // create employee record
                employee.EmployeeId = await RegisterNewEmployee(employee, employee.DateOfJoining);

                employee.EmployeeUid = employee.EmployeeId;
            }

            await Task.CompletedTask;
        }

        private async Task<EmployeeCalculation> GetDeclarationDetail(long employeeId, decimal CTC, int currentRegimeId)
        {
            string url = $"{_microserviceUrlLogs.SalaryDeclarationCalculation}/{employeeId}/{CTC}/{currentRegimeId}";
            var microserviceRequest = MicroserviceRequest.Builder(url);
            microserviceRequest
            .SetDbConfig(_requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString))
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
                SecondaryMobile = employee.SecondaryMobile,
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
                EmployeeUid = employee.EmployeeId,
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
                employee.IsEmployeeEligibleForPF,
                employee.IsExistingMemberOfPF,
                employee.PFNumber,
                UniversalAccountNumber = employee.UAN,
                employee.ESISerialNumber,
                employee.IsEmployeeEligibleForESI,
                PFJoinDate = employee.PFAccountCreationDate,
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
                employee.IsEmployeeEligibleForPF,
                employee.IsExistingMemberOfPF,
                employee.IsEmployeeEligibleForESI,
                employee.MaritalStatus,
                employee.MarriageDate,
                employee.CountryOfOrigin,
                employee.Religion,
                employee.BloodGroup,
                employee.IsPhChallanged,
                employee.Country,
                employee.EmergencyContactName,
                employee.RelationShip,
                employee.EmergencyMobileNo,
                employee.EmergencyState,
                employee.EmergencyCity,
                employee.EmergencyPincode,
                employee.EmergencyAddress,
                employee.EmergencyCountry,
                employee.PermanentState,
                employee.PermanentCity,
                employee.PermanentPincode,
                employee.PermanentAddress,
                employee.PermanentCountry,
                employee.BankAccountType,
                employee.IsInternationalEmployee,
                employee.AgencyName,
                employee.VerificationStatus,
                employee.VerificationRemark,
                employee.Location,
                employee.DepartmentId,
                employee.LastCompanyDesignation,
                employee.WorkingFromDate,
                employee.WorkingToDate,
                employee.LastCompanyAddress,
                employee.LastCompanyNatureOfDuty,
                employee.LastDrawnSalary,
                NomineeId = 0,
                NomineeName = "",
                NomineeRelationship = "",
                NomineeMobile = "",
                NomineeEmail = "",
                NomineeAddress = "",
                PercentageShare = 0,
                IsPrimaryNominee = false,
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
                    OldFileName = string.IsNullOrEmpty(employee.OldFileName) ? null : new List<string> { employee.OldFileName },
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
            var result = GetEmployees(filterModel);
            var employees = result.employees.FindAll(x => x.EmployeeUid != 1);
            List<Organization> organizations = _db.GetList<Organization>(Procedures.Company_Get);

            if (employees.Count == 0 || organizations.Count == 0)
                throw HiringBellException.ThrowBadRequest("Unable to get employee and company detail. Please contact to admin.");

            return new { Employees = employees, Organizations = organizations };
        }

        private (List<Employee> employees, List<RecordHealthStatus> recordHealthStatus) FilterActiveEmployees(FilterModel filterModel)
        {
            (var employees, var recordHealthStatus) = _db.GetList<Employee, RecordHealthStatus>(Procedures.Employee_GetAll, new
            {
                filterModel.SearchString,
                filterModel.SortBy,
                filterModel.PageIndex,
                filterModel.PageSize,
                FinancialYear = _currentSession.FinancialStartYear
            });

            return (employees, recordHealthStatus);
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

        public (List<Employee> employees, List<RecordHealthStatus> recordHealthStatuse) GetEmployees(FilterModel filterModel)
        {
            List<Employee> employees = null;
            List<RecordHealthStatus> recordHealthStatuse = null;
            if (string.IsNullOrEmpty(filterModel.SearchString))
                filterModel.SearchString = "1=1";

            if (filterModel.IsActive != null && filterModel.IsActive == true)
            {
                if (filterModel.CompanyId > 0)
                    filterModel.SearchString += $" and l.CompanyId = {filterModel.CompanyId} and IsActive = true";
                else
                    filterModel.SearchString += $" and l.CompanyId = {_currentSession.CurrentUserDetail.CompanyId} and IsActive = true";

                var result = FilterActiveEmployees(filterModel);

                employees = result.employees;
                recordHealthStatuse = result.recordHealthStatus;
            }
            else
                employees = FilterInActiveEmployees(filterModel);

            employees.ForEach(x =>
            {
                x.EmployeeCode = _commonService.GetEmployeeCode(x.EmployeeUid, _currentSession.CurrentUserDetail.EmployeeCodePrefix, _currentSession.CurrentUserDetail.EmployeeCodeLength);
            });

            return (employees, recordHealthStatuse);
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

                if (resultset.Tables[0].Rows.Count > 0)
                {
                    long.TryParse(resultset.Tables[0].Rows[0]["EmployeeUid"].ToString(), out long empId);
                    resultset.Tables[0].Columns.Add("EmployeeCode", typeof(string));

                    resultset.Tables[0].AsEnumerable().ToList().ForEach(row => row["EmployeeCode"] = _commonService.GetEmployeeCode(empId, _currentSession.CurrentUserDetail.EmployeeCodePrefix, _currentSession.CurrentUserDetail.EmployeeCodeLength));
                }
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
                var result = FilterActiveEmployees(filterModel);
                employees = result.employees;
            }
            return employees;
        }

        public async Task<(List<Employee> employees, List<RecordHealthStatus> recordHealthStatus)> DeActiveEmployeeService(long employeeId)
        {
            if (employeeId == 1)
                throw HiringBellException.ThrowBadRequest("You can't delete the admin");

            var filterEmployee = FilterActiveEmployees(new FilterModel
            {
                SearchString = $"1=1 and emp.EmployeeUid = {employeeId}"
            });

            var employee = filterEmployee.employees.First();
            employee.IsActive = false;

            var result = await _db.ExecuteAsync(Procedures.EMPLOYEE_INACTIVE_UPDATE, new
            {
                EmployeeUid = employeeId
            }, true);
            if (string.IsNullOrEmpty(result.statusMessage))
                throw HiringBellException.ThrowBadRequest("Fail to in-active the employee");

            return GetEmployees(new FilterModel
            {
                IsActive = true
            });
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

            var result = await _db.ExecuteAsync(Procedures.EMPLOYEE_NOTICE_PERIOD_INSUPD, new
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
            if (string.IsNullOrEmpty(result.statusMessage))
                throw HiringBellException.ThrowBadRequest("Fail to submit resignation detail");

            return result.statusMessage;
        }

        private void validateManageInitiateExistDetail(EmployeeNoticePeriod employeeNoticePeriod)
        {
            if (employeeNoticePeriod.EmployeeId == 0)
                throw HiringBellException.ThrowBadRequest("Invalid employee");

            if (string.IsNullOrEmpty(employeeNoticePeriod.ManagerComment))
                throw HiringBellException.ThrowBadRequest("Invalid manager comment");

            if (string.IsNullOrEmpty(employeeNoticePeriod.Summary))
                throw HiringBellException.ThrowBadRequest("Invalid summary");

            if (employeeNoticePeriod.IsRecommendLastDay && employeeNoticePeriod.OfficialLastWorkingDay == null)
                throw HiringBellException.ThrowBadRequest("Invalid last working day selected");
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
                PageSize = 1000,
                FinancialYear = _currentSession.FinancialStartYear
            });

            if (employees.Tables[0].Rows.Count > 0)
            {
                var employeeData = Converter.ToList<Employee>(employees.Tables[0]);

                List<dynamic> empData = new List<dynamic>();
                foreach (var emp in employeeData)
                {
                    Dictionary<string, object> data = new Dictionary<string, object>
                    {
                        { "Employee Code", _commonService.GetEmployeeCode(emp.EmployeeUid, _currentSession.CurrentUserDetail.EmployeeCodePrefix, _currentSession.CurrentUserDetail.EmployeeCodeLength) },
                        { "First Name", emp.FirstName },
                        { "Last Name", emp.LastName },
                        { "Mobile", emp.Mobile },
                        { "Email", emp.Email },
                        { "Aadhar No", emp.AadharNo },
                        { "PAN No", emp.PANNo },
                        { "Account No", emp.AccountNumber },
                        { "Bank Name", emp.BankName },
                        { "IFSC Code", emp.IFSCCode },
                        { "Experience In Month", emp.ExprienceInYear },
                        { "Date Of Joining", FormatDate(emp.CreatedOn) }
                    };

                    empData.Add(data);
                }


                if (fileType == 2)
                {
                    var url = $"{_microserviceUrlLogs.GenerateExelWithHeader}";
                    var microserviceRequest = MicroserviceRequest.Builder(url);
                    microserviceRequest
                    .SetPayload(empData)
                    .SetDbConfig(_requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString))
                    .SetConnectionString(_currentSession.LocalConnectionString)
                    .SetCompanyCode(_currentSession.CompanyCode)
                    .SetToken(_currentSession.Authorization);

                    return await _requestMicroservice.PostRequest<byte[]>(microserviceRequest);
                }
            }

            return null;
        }

        private string FormatDate(DateTime date)
        {
            if (date.Year == 1)
                return "";

            return _timezoneConverter.ToTimeZoneDateTime(date, _currentSession.TimeZone).ToString("dd-MMM-yyyy");
        }

        private string FormatDate(DateTime? date)
        {
            if (date == null)
                return "";

            return _timezoneConverter.ToTimeZoneDateTime((DateTime)date, _currentSession.TimeZone).ToString("dd-MMM-yyyy");
        }

        public async Task<byte[]> ExportEmployeeWithDataService()
        {
            var dataset = _db.FetchDataSet(Procedures.EMPLOYEE_WITH_DEPARTMENT_DESIGNATION_ALL, new
            {
                FinancialYear = _currentSession.FinancialStartYear
            });

            if (dataset.Tables.Count != 3)
                throw HiringBellException.ThrowBadRequest("Fail to get excel detail");

            if (dataset.Tables[0].Rows.Count == 0)
                throw HiringBellException.ThrowBadRequest("Employee record not. Please employee first");

            var employees = Converter.ToList<Employee>(dataset.Tables[0]);
            var departmnts = Converter.ToList<Department>(dataset.Tables[1]);
            var designation = Converter.ToList<EmployeeRole>(dataset.Tables[2]);

            ExcelDataWithDropdown excelDataWithDropdown = new ExcelDataWithDropdown
            {
                data = new List<dynamic>(),
                dropdowndata = new Dictionary<string, List<string>>(),
                mandatoryHeaderColumn = new List<string> { "Mobile", "Employee Code", "Employee Name", "Date Of Joining", "DOB", "Gender", "Email", "Experience In Month", "CTC (Yearly)", "Father Name", " Account Number", "IFSC Code", "Bank Account Type", "Bank Name", "PAN No", "Designation" }
            };

            await CreateEmployeeTableDropdown(departmnts, designation, excelDataWithDropdown);
            await CreateEmployeeRecord(employees, excelDataWithDropdown);

            var url = $"{_microserviceUrlLogs.GenerateExelWithDropdown}";
            var microserviceRequest = MicroserviceRequest.Builder(url);
            microserviceRequest
            .SetPayload(excelDataWithDropdown)
            .SetDbConfig(_requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString))
            .SetConnectionString(_currentSession.LocalConnectionString)
            .SetCompanyCode(_currentSession.CompanyCode)
            .SetToken(_currentSession.Authorization);

            return await _requestMicroservice.PostRequest<byte[]>(microserviceRequest);
        }

        private async Task CreateEmployeeRecord(List<Employee> employees, ExcelDataWithDropdown excelDataWithDropdown)
        {
            foreach (var emp in employees)
            {
                Dictionary<string, object> data = new Dictionary<string, object>
                    {
                        { "Employee Code", _commonService.GetEmployeeCode(emp.EmployeeUid, _currentSession.CurrentUserDetail.EmployeeCodePrefix, _currentSession.CurrentUserDetail.EmployeeCodeLength) },
                        { "Employee Name", emp.FirstName + " " + emp.LastName },
                        { "Date of Joining", FormatDate(emp.DateOfJoining)},
                        { "DOB", FormatDate(emp.DOB) },
                        { "CTC (Yearly)", emp.CTC },
                        { "Gender", GetGenderValue(emp.Gender) },
                        { "Experience In Month", emp.ExprienceInYear },
                        { "Email", emp.Email },
                        { "Marital Status", GetMaritalStatus(emp.MaritalStatus) },
                        { "Marriage Date", FormatDate(emp.MarriageDate) },
                        { "Blood Group", emp.BloodGroup },
                        { "Father Name", emp.FatherName },
                        { "Spouse Name", emp.SpouseName },
                        { "Is Ph Challanged", ConvertBooleanValue(emp.IsPhChallanged) },
                        { "Is International Employee", ConvertBooleanValue(emp.IsInternationalEmployee) },
                        { "Verification Status", emp.VerificationStatus },
                        { "Emergency Contact Name", emp.EmergencyContactName },
                        { "Emergency Mobile No", emp.EmergencyMobileNo },
                        { "Account Number", emp.AccountNumber },
                        { "IFSC Code", emp.IFSCCode },
                        { "Bank Account Type", emp.BankAccountType },
                        { "Bank Name", emp.BankName },
                        { "PAN No", emp.PANNo },
                        { "Is Employee Eligible For PF", ConvertBooleanValue(emp.IsEmployeeEligibleForPF) },
                        { "PF Number", emp.PFNumber },
                        { "PF Account Creation Date", FormatDate(emp.PFAccountCreationDate) },
                        { "Is Existing Member Of PF", ConvertBooleanValue(emp.IsExistingMemberOfPF) },
                        { "Is Employee Eligible For ESI", ConvertBooleanValue(emp.IsEmployeeEligibleForESI) },
                        { "ESI Serial Number", emp.ESISerialNumber },
                        { "Aadhar No", emp.AadharNo },
                        { "UAN", emp.UAN },
                        { "Mobile", emp.Mobile },
                        { "Country Of Origin", emp.CountryOfOrigin},
                        { "Department", emp.Department },
                        { "Location", emp.Location },
                        { "Designation", emp.Deisgnation },
                    };

                excelDataWithDropdown.data.Add(data);
            }

            await Task.CompletedTask;
        }

        private async Task CreateEmployeeTableDropdown(List<Department> departmnts, List<EmployeeRole> designation, ExcelDataWithDropdown excelDataWithDropdown)
        {
            excelDataWithDropdown.dropdowndata.Add("Department", departmnts.Select(x => x.DepartmentName).ToList());
            excelDataWithDropdown.dropdowndata.Add("Designation", designation.Select(x => x.RoleName).ToList());
            excelDataWithDropdown.dropdowndata.Add("Gender", new List<string> { "Male", "Female", "Any" });
            excelDataWithDropdown.dropdowndata.Add("Marital Status", new List<string> { "Married", "Single", "Separated", "Widowed" });
            excelDataWithDropdown.dropdowndata.Add("Verification Status", new List<string> { "Cancelled", "Initiated", "On Hold", "Partially Verified", "Pending", "Rejected", "Verified" });
            excelDataWithDropdown.dropdowndata.Add("Is Ph Challanged", new List<string> { "Yes", "No" });
            excelDataWithDropdown.dropdowndata.Add("Is International Employee", new List<string> { "Yes", "No" });
            excelDataWithDropdown.dropdowndata.Add("Is Employee Eligible For PF", new List<string> { "Yes", "No" });
            excelDataWithDropdown.dropdowndata.Add("Is Existing Member Of PF", new List<string> { "Yes", "No" });
            excelDataWithDropdown.dropdowndata.Add("Is Employee Eligible For ESI", new List<string> { "Yes", "No" });

            await Task.CompletedTask;
        }

        private string ConvertBooleanValue(bool value)
        {
            if (value)
                return "Yes";
            else
                return "No";
        }

        private string GetMaritalStatus(int maritalStatus)
        {
            if (maritalStatus == LocalConstants.Married)
                return nameof(LocalConstants.Married);
            else if (maritalStatus == LocalConstants.Separated)
                return nameof(LocalConstants.Separated);
            else if (maritalStatus == LocalConstants.Widowed)
                return nameof(LocalConstants.Widowed);
            else
                return nameof(LocalConstants.Single);
        }

        private string GetGenderValue(int gender)
        {
            if (gender == 1)
                return "Male";
            else if (gender == 2)
                return "Female";
            else
                return "Any";
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
                    .SetDbConfig(_requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString))
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
        public async Task<List<UploadEmpExcelError>> ReadEmployeeDataService(IFormFileCollection files)
        {
            try
            {
                var uploadedEmployeeData = await ReadPayrollExcelData(files);
                return await UpdateEmployeeData(uploadedEmployeeData);
            }
            catch
            {
                throw;
            }
        }

        private async Task<List<UploadEmpExcelError>> UpdateEmployeeData(List<Employee> employeeData)
        {
            int i = 0;
            int skipIndex = 0;
            int chunkSize = 50;
            List<UploadEmpExcelError> uploadEmpExcelErrors = new List<UploadEmpExcelError>();
            while (i < employeeData.Count)
            {
                var emps = employeeData.Skip(skipIndex++ * chunkSize).Take(chunkSize).ToList();

                var ids = JsonConvert.SerializeObject(emps.Where(x => !string.IsNullOrEmpty(x.EmployeeCode))
                                                          .Select(x => _commonService.ExtractEmployeeId(x.EmployeeCode, _currentSession.CurrentUserDetail.EmployeeCodePrefix)).ToList());
                var employees = _db.GetList<Employee>(Procedures.Active_Employees_By_Ids, new { EmployeeIds = ids });

                foreach (Employee e in emps)
                {
                    try
                    {
                        var employeeId = string.IsNullOrEmpty(e.EmployeeCode) ? 0 : _commonService.ExtractEmployeeId(e.EmployeeCode, _currentSession.CurrentUserDetail.EmployeeCodePrefix);
                        var em = employees.Find(x => x.EmployeeUid == employeeId);
                        if (em != null)
                        {
                            if (e.CTC > 0)
                            {
                                e.EmployeeUid = employeeId;
                                em.IsCTCChanged = true;
                                e.ReportingManagerId = em.ReportingManagerId;
                                e.AccessLevelId = em.AccessLevelId;
                                e.CompanyId = _currentSession.CurrentUserDetail.CompanyId;
                                e.OrganizationId = _currentSession.CurrentUserDetail.OrganizationId;
                                e.WorkShiftId = em.WorkShiftId;
                                await UpdateEmployeeService(e, null);
                            }
                        }
                        else
                        {
                            e.EmployeeUid = employeeId;
                            e.WorkShiftId = LocalConstants.DefaultWorkShiftId;
                            e.CompanyId = _currentSession.CurrentUserDetail.CompanyId;
                            e.OrganizationId = _currentSession.CurrentUserDetail.OrganizationId;
                            e.ReportingManagerId = LocalConstants.DefaultReportingMangerId;
                            e.UserTypeId = (int)UserType.Employee;
                            e.AccessLevelId = (int)RolesName.User;
                            e.LeavePlanId = LocalConstants.DefaultLeavePlanId;
                            e.SalaryGroupId = LocalConstants.DefaultSalaryGroupId;
                            e.DesignationId = LocalConstants.DefaultDesignation;

                            await RegisterEmployeeService(e, null, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        var uploadError = new UploadEmpExcelError
                        {
                            FullName = e.FirstName + " " + e.LastName,
                            Email = e.Email,
                            MobileNo = e.Mobile,
                            EmployeeCode = e.EmployeeCode,
                            CreatedOn = DateTime.Now
                        };

                        try
                        {
                            HiringBellException exception = (HiringBellException)ex;
                            uploadError.Message = exception.UserMessage;
                        }
                        catch (Exception)
                        {
                            uploadError.Message = ex.Message;
                        }
                        uploadEmpExcelErrors.Add(uploadError);

                        continue;
                    }
                }

                i++;
            }

            if (uploadEmpExcelErrors.Any())
                await uploadEmployeeRecordError(uploadEmpExcelErrors);

            return await Task.FromResult(uploadEmpExcelErrors);
        }

        private async Task uploadEmployeeRecordError(List<UploadEmpExcelError> uploadEmpExcelErrors)
        {
            await _db.ExecuteAsync(Procedures.UPLOAD_EMPLOYEE_ERROR_DETAIL_DELETE_ALL, new
            {
                UploadEmpExcelErrorId = 0
            });
            await _db.BulkExecuteAsync(Procedures.UPLOAD_EMPLOYEE_ERROR_DETAIL_INS, uploadEmpExcelErrors);
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
                                dataTable.RemoveSpacesFromColumnNames();
                                if (dataTable.Columns.Contains("CTC(Yearly)"))
                                    dataTable.Columns["CTC(Yearly)"].ColumnName = "CTC";
                                if (dataTable.Columns.Contains("Designation"))
                                    dataTable.Columns["Designation"].ColumnName = "Deisgnation";
                                if (dataTable.Columns.Contains("DateofJoining"))
                                    dataTable.Columns["DateofJoining"].ColumnName = "DateOfJoining";

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

        private List<Employee> MappedEmployee(DataTable table)
        {
            string TypeName = string.Empty;
            DateTime date = DateTime.Now;
            DateTime defaultDate = Convert.ToDateTime("1976-01-01");
            List<Employee> items = new List<Employee>();
            string[] dateFormats = { "MM/dd/yyyy", "dd-MM-yyyy", "yyyy/MM/dd", "yyyy-MM-dd", "dd-MMM-yyyy" };
            ValidateEmployeeExcel(table);

            try
            {
                List<PropertyInfo> props = typeof(Employee).GetProperties().ToList();
                List<string> fieldNames = ValidateHeaders(table, props);

                if (table.Rows.Count > 0)
                {
                    GetEmployeeDepartmentAndDesignation();

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
                                        case nameof(System.Boolean):
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
                                                if (x.Name.Equals("Gender", StringComparison.OrdinalIgnoreCase))
                                                    x.SetValue(t, GetGenderValue(dr[x.Name].ToString()));
                                                else if (x.Name.Equals("MaritalStatus", StringComparison.OrdinalIgnoreCase))
                                                    x.SetValue(t, GetMaritalStatus(dr[x.Name].ToString()));
                                                else if (x.Name.ToString().Equals("ExperienceInMonth", StringComparison.OrdinalIgnoreCase))
                                                    t.ExperienceInYear = Convert.ToInt32(dr[x.Name]);
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
                                        case nameof(System.String):
                                            if (dr[x.Name] != DBNull.Value)
                                            {
                                                if (x.Name.Equals("EmployeeName", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    var name = GetFirstAndLastName(dr[x.Name].ToString());

                                                    t.FirstName = name.FirstName;
                                                    t.LastName = name.LastName;
                                                }
                                                else if (x.Name.Equals("Department", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    t.DepartmentId = GetDepartmentId(dr[x.Name].ToString());
                                                }
                                                else if (x.Name.Equals("Deisgnation", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    t.DesignationId = GetDesignationId(dr[x.Name].ToString());
                                                }
                                                else
                                                {
                                                    x.SetValue(t, dr[x.Name].ToString());
                                                }
                                            }
                                            else
                                                x.SetValue(t, string.Empty);
                                            break;
                                        case nameof(DateTime):
                                            if (dr[x.Name] == DBNull.Value || dr[x.Name].ToString() != null)
                                            {
                                                if (string.IsNullOrEmpty(dr[x.Name].ToString()))
                                                    break;
                                                else if (DateTime.TryParseExact(dr[x.Name].ToString(), dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateOfBirth))
                                                    date = dateOfBirth;
                                                else if (DateTime.TryParse(dr[x.Name].ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out dateOfBirth))
                                                    date = dateOfBirth;
                                                else
                                                    date = Convert.ToDateTime(dr[x.Name].ToString());

                                                date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                                                x.SetValue(t, date);
                                            }
                                            //else
                                            //{
                                            //    x.SetValue(t, defaultDate);
                                            //}
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

        private void ValidateEmployeeExcel(DataTable dt)
        {
            EmployeeExcelValidator();

            for (int rowIndex = 0; rowIndex < dt.Rows.Count; rowIndex++)
            {
                foreach (DataColumn column in dt.Columns)
                {
                    if (_validators.ContainsKey(column.ColumnName))
                    {
                        string cellValue = dt.Rows[rowIndex][column.ColumnName]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(cellValue) && (cellValue.Equals("NA", StringComparison.OrdinalIgnoreCase) || cellValue.Equals("N/A", StringComparison.OrdinalIgnoreCase)))
                        {
                            cellValue = string.Empty;
                            dt.Rows[rowIndex][column.ColumnName] = null;
                        }
                        if (!_validators[column.ColumnName](cellValue, column.ColumnName))
                        {
                            throw HiringBellException.ThrowBadRequest($"Invalid value '{cellValue}' in column '{column.ColumnName}' at row {rowIndex + 2}");
                        }
                    }
                }
            }
        }

        #region Validation Methods

        private bool IsValidName(string value, string fieldName)
        {
            return !string.IsNullOrEmpty(value) && Regex.IsMatch(value, @"^[A-Za-z\s\.]+$");
        }

        private bool IsValidNameOrEmpty(string value, string fieldName)
        {
            return string.IsNullOrEmpty(value) || Regex.IsMatch(value, @"^[A-Za-z\s\.]+$");
        }

        private bool IsValidDate(string value, string fieldName)
        {
            return DateTime.TryParse(value, out _);
        }

        private bool IsValidDateOrEmpty(string value, string fieldName)
        {
            return string.IsNullOrEmpty(value) || DateTime.TryParse(value, out _);
        }

        private bool IsValidDecimal(string value, string fieldName)
        {
            return decimal.TryParse(value, out _);
        }

        private bool IsValidInteger(string value, string fieldName)
        {
            return int.TryParse(value, out _);
        }

        private bool IsValidGender(string value, string fieldName)
        {
            var validGenders = new[] { nameof(LocalConstants.Male).ToUpper(), nameof(LocalConstants.Female).ToUpper(), nameof(LocalConstants.Any).ToUpper() };
            return validGenders.Contains(value?.ToUpper());
        }

        private bool IsValidEmail(string value, string fieldName)
        {
            return Regex.IsMatch(value, @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$");
        }

        private bool IsValidMaritalStatus(string value, string fieldName)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            var validStatuses = new[] { nameof(LocalConstants.Single).ToUpper(), nameof(LocalConstants.Married).ToUpper(), nameof(LocalConstants.Widowed).ToUpper(), nameof(LocalConstants.Separated).ToUpper() };
            return validStatuses.Contains(value?.ToUpper());
        }

        private bool IsValidBloodGroup(string value, string fieldName)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            var validGroups = new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
            return validGroups.Contains(value?.ToUpper());
        }

        private bool IsValidBoolean(string value, string fieldName)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            var validValues = new[] { "YES", "NO", "TRUE", "FALSE" };
            return validValues.Contains(value?.ToUpper());
        }

        private bool IsValidVerificationStatus(string value, string fieldName)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            var validStatuses = new[] { "CANCELLED", "INITIATED", "ON HOLD", "PARTIALLY VERIFIED", "PENDING", "REJECTED", "VERIFIED" };
            return validStatuses.Contains(value?.ToUpper());
        }

        private bool IsValidBankAccountType(string value, string fieldName)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            var validStatuses = new[] { "CURRENT", "SAVING", "SALARY" };
            return validStatuses.Contains(value?.ToUpper());
        }

        private bool IsValidMobileNumber(string value, string fieldName)
        {
            return Regex.IsMatch(value, @"^[0-9]{10}$");
        }

        private bool IsValidMobileNumberOrEmpty(string value, string fieldName)
        {
            return string.IsNullOrEmpty(value) || Regex.IsMatch(value, @"^[0-9]{10}$");
        }

        private bool IsValidBankAccountNumber(string value, string fieldName)
        {
            return Regex.IsMatch(value, @"^[0-9]{9,18}$");
        }

        private bool IsValidIFSCCode(string value, string fieldName)
        {
            return Regex.IsMatch(value, @"^[A-Z]{4}0[A-Z0-9]{6}$");
        }

        private bool IsValidPANNumber(string value, string fieldName)
        {
            return Regex.IsMatch(value, @"^[A-Z]{5}[0-9]{4}[A-Z]{1}$");
        }

        private bool IsValidAadharNumberOrEmpty(string value, string fieldName)
        {
            return string.IsNullOrEmpty(value) || Regex.IsMatch(value, @"^[0-9]{12}$");
        }

        private bool IsValidUANOrEmpty(string value, string fieldName)
        {
            return string.IsNullOrEmpty(value) || Regex.IsMatch(value, @"^[0-9]{12}$");
        }

        #endregion

        private void EmployeeExcelValidator()
        {
            _validators = new Dictionary<string, Func<string, string, bool>>
            {
                {"EmployeeName", IsValidName},
                {"DateOfJoining", IsValidDate},
                {"DOB", IsValidDate},
                {"CTC", IsValidDecimal},
                {"Gender", IsValidGender},
                {"ExperienceInMonth", IsValidInteger},
                {"Email", IsValidEmail},
                {"MaritalStatus", IsValidMaritalStatus},
                {"MarriageDate", IsValidDateOrEmpty},
                {"BloodGroup", IsValidBloodGroup},
                {"FatherName", IsValidName},
                {"SpouseName", IsValidNameOrEmpty},
                {"IsPhChallanged", IsValidBoolean},
                {"IsInternationalEmployee", IsValidBoolean},
                {"VerificationStatus", IsValidVerificationStatus},
                {"EmergencyContactName", IsValidNameOrEmpty},
                {"EmergencyMobileNo", IsValidMobileNumberOrEmpty},
                {"AccountNumber", IsValidBankAccountNumber},
                {"IFSCCode", IsValidIFSCCode},
                {"BankAccountType", IsValidBankAccountType},
                {"BankName", IsValidName},
                {"PANNo", IsValidPANNumber},
                {"IsEmployeeEligibleForPF", IsValidBoolean},
                {"PFAccountCreationDate", IsValidDateOrEmpty},
                {"IsExistingMemberOfPF", IsValidBoolean},
                {"IsEmployeeEligibleForESI", IsValidBoolean},
                {"AadharNo", IsValidAadharNumberOrEmpty},
                {"UAN", IsValidUANOrEmpty},
                {"Mobile", IsValidMobileNumber},
                {"CountryOfOrigin", IsValidNameOrEmpty},
                {"Designation", IsValidNameOrEmpty},
                {"Location", IsValidNameOrEmpty}
            };
        }

        private void GetEmployeeDepartmentAndDesignation()
        {
            var dataset = _db.FetchDataSet(Procedures.ORG_HIERARCHY_DEPARTMENT_GETALL);

            if (dataset == null || dataset.Tables.Count != 2)
                throw HiringBellException.ThrowBadRequest("Fail to get designation and department");

            _designations = Converter.ToList<EmployeeRole>(dataset.Tables[0]);
            _departments = Converter.ToList<Department>(dataset.Tables[1]);
        }

        private int GetDepartmentId(string department)
        {
            if (string.IsNullOrEmpty(department))
                return 0;

            var departmentDetail = _departments.Find(x => x.DepartmentName.Equals(department, StringComparison.OrdinalIgnoreCase));
            if (departmentDetail == null)
                throw HiringBellException.ThrowBadRequest("Please select a valid department from the dropdown menu.");

            return departmentDetail.DepartmentId;
        }

        private int GetDesignationId(string designation)
        {
            if (string.IsNullOrEmpty(designation))
                return 0;

            var designationDetail = _designations.Find(x => x.RoleName.Equals(designation, StringComparison.OrdinalIgnoreCase));
            if (designationDetail == null)
                throw HiringBellException.ThrowBadRequest("Please select a valid designation from the dropdown menu.");

            return (int)designationDetail.RoleId;
        }

        private int GetMaritalStatus(string maritalStatus)
        {
            if (maritalStatus.Equals(nameof(LocalConstants.Married), StringComparison.OrdinalIgnoreCase))
                return (int)LocalConstants.Married;
            else if (maritalStatus.Equals(nameof(LocalConstants.Single), StringComparison.OrdinalIgnoreCase))
                return (int)LocalConstants.Single;
            else if (maritalStatus.Equals(nameof(LocalConstants.Separated), StringComparison.OrdinalIgnoreCase))
                return (int)LocalConstants.Separated;
            else if (maritalStatus.Equals(nameof(LocalConstants.Widowed), StringComparison.OrdinalIgnoreCase))
                return (int)LocalConstants.Widowed;
            else
                throw HiringBellException.ThrowBadRequest("Invalid marital status selected");
        }

        private int GetGenderValue(string gender)
        {
            if (gender.Equals(LocalConstants.Male, StringComparison.OrdinalIgnoreCase))
                return 1;
            else if (gender.Equals(LocalConstants.Female, StringComparison.OrdinalIgnoreCase))
                return 2;
            else if (gender.Equals(LocalConstants.Any, StringComparison.OrdinalIgnoreCase))
                return 3;
            else
                throw HiringBellException.ThrowBadRequest("Invalid gender selected. Please select a valid gender type");
        }

        private (string FirstName, string LastName) GetFirstAndLastName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw HiringBellException.ThrowBadRequest("Name cannot be null or empty.");

            string[] parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            string firstName = parts[0];
            string lastName = string.Join(" ", parts[(1)..]);

            return (firstName, lastName);
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
                        columnList.Add(column.ColumnName.Replace(" ", ""));
                    }
                    else
                    {
                        throw HiringBellException.ThrowBadRequest($"Multiple header found \"{column.ColumnName}\" field.");
                    }
                }
            }

            return columnList;
        }

        public async Task<List<RecordHealthStatus>> GetEmployeesRecordHealthStatusService()
        {
            var result = _db.GetList<RecordHealthStatus>(Procedures.RECORD_HEALTH_STATUS_GET_INCOMPLETE_PROFILE, new
            {
                FinancialYear = _currentSession.FinancialStartYear
            });

            return await Task.FromResult(result);
        }

        public async Task<List<RecordHealthStatus>> FixEmployeesRecordHealthStatusService(List<long> employeeIds)
        {
            foreach (var employeeId in employeeIds)
            {
                if (employeeId <= 0)
                    throw HiringBellException.ThrowBadRequest("Invalid employee id");

                var empSalaryDetail = _db.Get<EmployeeSalaryDetail>(Procedures.EMPLOYEE_SALARY_DETAIL_GET_BY_EMPID, new
                {
                    _currentSession.FinancialStartYear,
                    EmployeeId = employeeId
                });

                if (empSalaryDetail == null)
                    throw HiringBellException.ThrowBadRequest("Employee salary details are not available. Please ensure that the CTC is entered on the employee registration page first.");

                var eCal = await GetDeclarationDetail(employeeId, empSalaryDetail.CTC, ApplicationConstants.DefaultTaxRegin);

                var result = await _db.ExecuteAsync(Procedures.GENERATE_EMP_LEAVE_DECLARATION_SALARYDETAIL, new
                {
                    EmployeeId = employeeId,
                    _currentSession.CurrentUserDetail.CompanyId,
                    eCal.employeeDeclaration.DeclarationDetail,
                    eCal.employeeSalaryDetail.CompleteSalaryDetail,
                    NewSalaryDetail = "[]",
                    eCal.employeeSalaryDetail.TaxDetail,
                    eCal.employeeSalaryDetail.GrossIncome,
                    eCal.employeeSalaryDetail.NetSalary
                }, true);

                if (string.IsNullOrEmpty(result.statusMessage))
                    throw HiringBellException.ThrowBadRequest("Fail to add employee salary detail, leave and declaration");

                await CheckRunLeaveAccrualCycle(employeeId);
            }


            return await GetEmployeesRecordHealthStatusService();
        }

        public async Task<byte[]> ExportEmployeeSkeletonExcelService()
        {
            var (designation, departmnts) = _db.GetList<EmployeeRole, Department>(Procedures.ORG_HIERARCHY_DEPARTMENT_GETALL, new
            {
                FinancialYear = _currentSession.FinancialStartYear
            });

            if (!designation.Any())
                throw HiringBellException.ThrowBadRequest("Designation not found. Please contact to admin");

            if (!departmnts.Any())
                throw HiringBellException.ThrowBadRequest("Department not found. Please contact to admin");

            ExcelDataWithDropdown excelDataWithDropdown = new ExcelDataWithDropdown
            {
                data = new List<dynamic>(),
                dropdowndata = new Dictionary<string, List<string>>(),
                mandatoryHeaderColumn = new List<string> { "Mobile", "Employee Name", "Date Of Joining", "DOB", "Gender", "Email", "Experience In Month", "CTC (Yearly)", "Father Name", " Account Number", "IFSC Code", "Bank Account Type", "Bank Name", "PAN No", "Designation" }
            };

            await CreateEmployeeTableDropdown(departmnts, designation, excelDataWithDropdown);
            Dictionary<string, object> dumyEmployeeData = new Dictionary<string, object>
                    {
                        { "Employee Code", "" },
                        { "Employee Name", "Adam Smith" },
                        { "Date of Joining", FormatDate(DateTime.UtcNow)},
                        { "DOB", FormatDate(DateTime.UtcNow) },
                        { "CTC (Yearly)", 10000000 },
                        { "Gender", "Male" },
                        { "Experience In Month", 24 },
                        { "Email", "adam@test.com" },
                        { "Marital Status", "Single" },
                        { "Marriage Date", FormatDate(DateTime.UtcNow) },
                        { "Blood Group", "O+" },
                        { "Father Name", "John Smith" },
                        { "Spouse Name", "Ema John" },
                        { "Is Ph Challanged", "No" },
                        { "Is International Employee", "No" },
                        { "Verification Status", "Pending" },
                        { "Emergency Contact Name", "Rishi Kumar" },
                        { "Emergency Mobile No", "1234567899" },
                        { "Account Number", "12341587455" },
                        { "IFSC Code", "SBIN0014544" },
                        { "Bank Account Type", "Saving" },
                        { "Bank Name", "State Bank of India" },
                        { "PAN No", "ABCDE0000A" },
                        { "Is Employee Eligible For PF", "No" },
                        { "PF Number", "12345678912" },
                        { "PF Account Creation Date", FormatDate(DateTime.UtcNow) },
                        { "Is Existing Member Of PF", "No" },
                        { "Is Employee Eligible For ESI", "No" },
                        { "ESI Serial Number", "123456789" },
                        { "Aadhar No", "123412341234" },
                        { "UAN", "123456124457" },
                        { "Mobile", "9000000000" },
                        { "Country Of Origin", "India"},
                        { "Department", "IT DEPERTMENT" },
                        { "Location", "Hyderabad" },
                        { "Designation", "SOFTWARE DEVELOPER" },
                    };
            excelDataWithDropdown.data.Add(dumyEmployeeData);

            var url = $"{_microserviceUrlLogs.GenerateExelWithDropdown}";
            var microserviceRequest = MicroserviceRequest.Builder(url);
            microserviceRequest
            .SetPayload(excelDataWithDropdown)
            .SetDbConfig(_requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString))
            .SetConnectionString(_currentSession.LocalConnectionString)
            .SetCompanyCode(_currentSession.CompanyCode)
            .SetToken(_currentSession.Authorization);

            return await _requestMicroservice.PostRequest<byte[]>(microserviceRequest);
        }

        public async Task<List<UploadEmpExcelError>> GetEmployeeUploadErrorLogsService()
        {
            var result = _db.GetList<UploadEmpExcelError>(Procedures.UPLOAD_EMPLOYEE_ERROR_DETAIL_GETALL);
            return await Task.FromResult(result);
        }

        #endregion

    }
}