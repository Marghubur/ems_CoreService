using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.Enums;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using DocMaker.ExcelMaker;
using EMailService.Modal;
using EMailService.Modal.Payroll;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModalLayer;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using Newtonsoft.Json;
using ServiceLayer.Code.PayrollCycle.Interface;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Code.PayrollCycle.Code
{
    public class DeclarationService : IDeclarationService
    {
        private readonly IDb _db;
        private readonly IFileService _fileService;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly CurrentSession _currentSession;
        private readonly Dictionary<string, List<string>> _sections;
        private readonly ISalaryComponentService _salaryComponentService;
        private readonly IComponentsCalculationService _componentsCalculationService;
        private readonly ITimezoneConverter _timezoneConverter;
        private readonly ILogger<DeclarationService> _logger;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ExcelWriter _excelWriter;
        private readonly IUtilityService _utilityService;

        public DeclarationService(IDb db,
            ILogger<DeclarationService> logger,
            IFileService fileService,
            FileLocationDetail fileLocationDetail,
            CurrentSession currentSession,
            IOptions<Dictionary<string, List<string>>> options,
            ISalaryComponentService salaryComponentService,
            IComponentsCalculationService componentsCalculationService,
            ITimezoneConverter timezoneConverter,
            IHostingEnvironment hostingEnvironment,
            IUtilityService utilityService,
            ExcelWriter excelWriter)
        {
            _db = db;
            _logger = logger;
            _fileService = fileService;
            _fileLocationDetail = fileLocationDetail;
            _currentSession = currentSession;
            _sections = options.Value;
            _salaryComponentService = salaryComponentService;
            _componentsCalculationService = componentsCalculationService;
            _timezoneConverter = timezoneConverter;
            _hostingEnvironment = hostingEnvironment;
            _utilityService = utilityService;
            _excelWriter = excelWriter;
        }

        public EmployeeDeclaration GetDeclarationByEmployee(long EmployeeId)
        {
            throw new NotImplementedException();
        }

        public async Task<EmployeeDeclaration> UpdateDeclarationDetail(long EmployeeDeclarationId, EmployeeDeclaration employeeDeclaration, IFormFileCollection FileCollection, List<Files> files)
        {
            EmployeeDeclaration empDeclaration = new EmployeeDeclaration();
            EmployeeDeclaration declaration = GetDeclarationById(EmployeeDeclarationId);
            if (declaration.EmployeeCurrentRegime != 1)
                throw HiringBellException.ThrowBadRequest("You can't submit the declration because you have selected new tax regime");

            declaration.Email = employeeDeclaration.Email;
            SalaryComponents salaryComponent = null;
            if (declaration != null && !string.IsNullOrEmpty(declaration.DeclarationDetail))
            {
                declaration.SalaryComponentItems = JsonConvert.DeserializeObject<List<SalaryComponents>>(declaration.DeclarationDetail);
                salaryComponent = declaration.SalaryComponentItems.Find(x => x.ComponentId == employeeDeclaration.ComponentId);

                if (salaryComponent == null)
                    throw HiringBellException.ThrowBadRequest("Requested component not found. Please contact to admin.");

                if (salaryComponent.MaxLimit > 0 && employeeDeclaration.DeclaredValue > salaryComponent.MaxLimit)
                    throw HiringBellException.ThrowBadRequest("Your declared value is greater than maximum limit");

                if (employeeDeclaration.DeclaredValue < 0)
                    throw HiringBellException.ThrowBadRequest("Declaration value must be greater than 0. Please check your detail once.");

                salaryComponent.DeclaredValue = employeeDeclaration.DeclaredValue;

                var maxLimit = 150000;
                _sections.TryGetValue(ApplicationConstants.ExemptionDeclaration, out List<string> taxexemptSection);
                employeeDeclaration.ExemptionDeclaration = declaration.SalaryComponentItems.FindAll(i => i.Section != null && taxexemptSection.Contains(i.Section));
                var totalAmountDeclared = employeeDeclaration.ExemptionDeclaration.Sum(a => a.DeclaredValue);
                var npsComponent = employeeDeclaration.ExemptionDeclaration.Find(x => x.Section == "80CCD(1)");

                if (salaryComponent.Section == "80CCD(1)")
                    maxLimit += 50000;
                else
                    totalAmountDeclared = totalAmountDeclared - npsComponent.DeclaredValue;
                if (totalAmountDeclared > maxLimit)
                    throw HiringBellException.ThrowBadRequest("Your limit for this section is exceed from maximum limit");

            }
            else
            {
                throw HiringBellException.ThrowBadRequest("Requested component not found. Please contact to admin.");
            }

            await ExecuteDeclarationDetail(files, declaration, FileCollection, salaryComponent);
            return await GetEmployeeDeclarationDetail(employeeDeclaration.EmployeeId, true);
        }

        public EmployeeDeclaration GetDeclarationById(long EmployeeDeclarationId)
        {
            EmployeeDeclaration declaration = GetDeclarationWithComponents(EmployeeDeclarationId);
            if (declaration == null)
                throw new HiringBellException("Fail to get current employee declaration detail");

            return declaration;
        }

        public EmployeeDeclaration GetDeclarationWithComponents(long EmployeeDeclarationId)
        {
            EmployeeDeclaration declaration = _db.Get<EmployeeDeclaration>(Procedures.Employee_Declaration_Get_ById, new
            {
                EmployeeDeclarationId
            });

            return declaration;
        }

        private async Task<bool> GetEmployeeDeclaration(EmployeeDeclaration employeeDeclaration, List<SalaryComponents> salaryComponents)
        {
            _logger.LogInformation("Starting method: GetEmployeeDeclaration");

            bool reCalculateFlag = false;

            if (string.IsNullOrEmpty(employeeDeclaration.DeclarationDetail))
            {
                employeeDeclaration.DeclarationDetail = JsonConvert.SerializeObject(salaryComponents);
                employeeDeclaration.SalaryComponentItems = salaryComponents;
                reCalculateFlag = true;
            }
            else
            {
                try
                {
                    employeeDeclaration.SalaryComponentItems = JsonConvert
                                       .DeserializeObject<List<SalaryComponents>>(employeeDeclaration.DeclarationDetail);

                    if (employeeDeclaration.SalaryComponentItems == null || employeeDeclaration.SalaryComponentItems.Count == 0)
                    {
                        employeeDeclaration.DeclarationDetail = JsonConvert.SerializeObject(salaryComponents);
                        employeeDeclaration.SalaryComponentItems = salaryComponents;
                        reCalculateFlag = true;
                        return await Task.FromResult(reCalculateFlag);
                    }

                    Parallel.ForEach(salaryComponents, x =>
                    {
                        if (employeeDeclaration.SalaryComponentItems.Find(i => i.ComponentId == x.ComponentId) == null)
                        {
                            employeeDeclaration.SalaryComponentItems.Add(x);
                            reCalculateFlag = true;
                        }
                    });
                }
                catch
                {
                    employeeDeclaration.DeclarationDetail = JsonConvert.SerializeObject(salaryComponents);
                    employeeDeclaration.SalaryComponentItems = salaryComponents;
                    reCalculateFlag = true;
                }
            }
            _logger.LogInformation("Leaving method: GetEmployeeDeclaration");

            return await Task.FromResult(reCalculateFlag);
        }

        public async Task<EmployeeDeclaration> GetEmployeeDeclarationDetail(long EmployeeId, bool reCalculateFlag = false)
        {
            List<Files> files = default;
            if (EmployeeId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid employee selected. Please select a valid employee");

            if (_currentSession?.TimeZoneNow == null)
                _currentSession.TimeZoneNow = _timezoneConverter.ToTimeZoneDateTime(DateTime.UtcNow, _currentSession.TimeZone);

            DataSet resultSet = _db.FetchDataSet(Procedures.Employee_Declaration_Get_ByEmployeeId, new
            {
                EmployeeId,
                UserTypeId = (int)UserType.Compnay
            });

            if ((resultSet == null || resultSet.Tables.Count == 0) && resultSet.Tables.Count != 2)
                throw HiringBellException.ThrowBadRequest("Unable to get the detail");

            EmployeeDeclaration employeeDeclaration = resultSet.Tables[0].ToType<EmployeeDeclaration>();
            if (employeeDeclaration == null)
                throw new HiringBellException("Employee declaration detail not defined. Please contact to admin.");

            if (resultSet.Tables[1].Rows.Count > 0)
                files = resultSet.Tables[1].ToList<Files>();

            employeeDeclaration.SalaryDetail = await CalculateSalaryDetail(EmployeeId, employeeDeclaration, reCalculateFlag, false);

            employeeDeclaration.FileDetails = files;
            employeeDeclaration.Sections = _sections;
            return await Task.FromResult(employeeDeclaration);
        }

        public async Task<EmployeeDeclaration> GetEmployeeIncomeDetailService(FilterModel filterModel)
        {
            EmployeeDeclaration employees = null;
            if (string.IsNullOrEmpty(filterModel.SearchString))
                filterModel.SearchString = "1=1";


            if (filterModel.CompanyId > 0)
                filterModel.SearchString += $" and l.CompanyId = {filterModel.CompanyId} ";
            else
                filterModel.SearchString += $" and l.CompanyId = {_currentSession.CurrentUserDetail.CompanyId} ";

            return await Task.FromResult(employees);
        }

        private async Task UpdateDeclarationDetail(List<Files> files, EmployeeDeclaration declaration, IFormFileCollection FileCollection, HousingDeclartion housingDeclartion)
        {
            SalaryComponents salaryComponent = declaration.SalaryComponentItems.Find(x => x.ComponentId == housingDeclartion.ComponentId);
            if (salaryComponent == null)
                throw new HiringBellException("Requested component not found. Please contact to admin.");

            salaryComponent.DeclaredValue = housingDeclartion.HousePropertyDetail.TotalRent;
            declaration.HouseRentDetail = JsonConvert.SerializeObject(housingDeclartion.HousePropertyDetail);
            await ExecuteDeclarationDetail(files, declaration, FileCollection, salaryComponent);
        }

        private async Task ExecuteDeclarationDetail(List<Files> files, EmployeeDeclaration declaration, IFormFileCollection FileCollection, SalaryComponents salaryComponent)
        {
            try
            {
                DbResult Result = null;
                List<int> fileIds = new List<int>();
                if (FileCollection != null && FileCollection.Count > 0)
                {
                    if (string.IsNullOrEmpty(declaration.DocumentPath))
                    {
                        declaration.DocumentPath = Path.Combine(
                            _fileLocationDetail.UserFolder,
                           $"Employee_{declaration.EmployeeId}",
                            ApplicationConstants.DeclarationDocumentPath
                        );
                    }
                    // save file to server filesystem
                    _fileService.SaveFileToLocation(declaration.DocumentPath, files, FileCollection);

                    foreach (var n in files)
                    {
                        Result = await _db.ExecuteAsync(Procedures.Userfiledetail_Upload, new
                        {
                            FileId = n.FileUid,
                            FileOwnerId = declaration.EmployeeId,
                            FilePath = declaration.DocumentPath,
                            n.FileName,
                            n.FileExtension,
                            UserTypeId = (int)UserType.Compnay,
                            AdminId = _currentSession.CurrentUserDetail.UserId
                        }, true);

                        if (Result.rowsEffected < 0)
                            throw HiringBellException.ThrowBadRequest("Fail to update documents. Please contact to admin.");

                        fileIds.Add(Convert.ToInt32(Result.statusMessage));
                    }
                }

                salaryComponent.UploadedFileIds = JsonConvert.SerializeObject(fileIds);
                declaration.DeclarationDetail = GetDeclarationBasicFields(declaration.SalaryComponentItems);

                Result = await _db.ExecuteAsync(Procedures.Employee_Declaration_Insupd, new
                {
                    declaration.EmployeeDeclarationId,
                    declaration.EmployeeId,
                    declaration.DocumentPath,
                    declaration.DeclarationDetail,
                    declaration.HouseRentDetail,
                    declaration.TotalDeclaredAmount,
                    declaration.TotalApprovedAmount,
                    declaration.TotalRejectedAmount,
                    declaration.EmployeeCurrentRegime
                }, true);

                if (!BotConstant.IsSuccess(Result))
                    throw HiringBellException.ThrowBadRequest("Fail to update housing property document detail. Please contact to admin.");
            }
            catch
            {
                _fileService.DeleteFiles(files);
                throw;
            }
        }

        private string GetDeclarationBasicFields(List<SalaryComponents> salaryComponents)
        {
            var basicFields = salaryComponents.Select(x => new
            {
                x.ComponentId,
                x.DeclaredValue,
                x.Section,
                x.MaxLimit,
                x.ComponentFullName,
                x.UploadedFileIds
            }).ToList();

            return JsonConvert.SerializeObject(basicFields);
        }

        public async Task<EmployeeDeclaration> HouseRentDeclarationService(long EmployeeDeclarationId, HousingDeclartion DeclarationDetail, IFormFileCollection FileCollection, List<Files> files)
        {
            try
            {
                EmployeeDeclaration declaration = GetDeclarationWithComponents(EmployeeDeclarationId);
                if (declaration == null)
                    throw new HiringBellException("Fail to get current employee declaration detail");

                if (declaration == null || string.IsNullOrEmpty(declaration.DeclarationDetail))
                    throw new HiringBellException("Requested component not found. Please contact to admin.");

                if (declaration.EmployeeCurrentRegime != 1)
                    throw HiringBellException.ThrowBadRequest("You can't submit house rent because you selected new tax regime");

                declaration.SalaryComponentItems = JsonConvert.DeserializeObject<List<SalaryComponents>>(declaration.DeclarationDetail);

                if (FileCollection.Count > 0)
                {

                    if (string.IsNullOrEmpty(declaration.DocumentPath))
                    {
                        declaration.DocumentPath = Path.Combine(
                            _fileLocationDetail.UserFolder,
                            $"Employee_{declaration.EmployeeId}",
                            ApplicationConstants.DeclarationDocumentPath
                        );
                    }
                }

                DeclarationDetail.ComponentId = ComponentNames.HRA;

                // update declaration detail with housing detail in database
                await UpdateDeclarationDetail(files, declaration, FileCollection, DeclarationDetail);
                return await GetEmployeeDeclarationDetail(DeclarationDetail.EmployeeId, true);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private List<CalculatedSalaryBreakupDetail> GetPresentMonthSalaryDetail(List<AnnualSalaryBreakup> completeSalaryBreakups)
        {
            _logger.LogInformation("Starting method: GetPresentMonthSalaryDetail");

            List<CalculatedSalaryBreakupDetail> calculatedSalaryBreakupDetails = new List<CalculatedSalaryBreakupDetail>();

            var currentMonthDateTime = _currentSession.TimeZoneNow;
            var currentMonthSalaryBreakup = completeSalaryBreakups.Find(i => i.MonthNumber == currentMonthDateTime.Month);
            if (currentMonthSalaryBreakup == null)
                throw new HiringBellException("Unable to find salary detail. Please contact to admin.");
            else
                calculatedSalaryBreakupDetails = currentMonthSalaryBreakup.SalaryBreakupDetails;
            _logger.LogInformation("Leaving method: GetPresentMonthSalaryDetail");

            return calculatedSalaryBreakupDetails;
        }

        private decimal CalculateTotalAmountWillBeReceived(List<AnnualSalaryBreakup> salary)
        {
            _logger.LogInformation("Starting method: CalculateTotalAmountWillBeReceived");

            return salary.Where(x => x.IsActive).SelectMany(i => i.SalaryBreakupDetails)
                .Where(x => x.ComponentName == ComponentNames.Gross)
                .Sum(x => x.ActualAmount);
        }

        public async Task<EmployeeSalaryDetail> CalculateSalaryDetail(long EmployeeId, EmployeeDeclaration employeeDeclaration, bool reCalculateFlag = false, bool isCTCChanged = false)
        {
            EmployeeCalculation employeeCalculation = new EmployeeCalculation
            {
                EmployeeId = EmployeeId,
                employeeDeclaration = employeeDeclaration,
                employee = new Employee { EmployeeUid = EmployeeId }
            };
            if (isCTCChanged)
                employeeCalculation.employee.IsCTCChanged = isCTCChanged;

            await _salaryComponentService.GetEmployeeSalaryDetail(employeeCalculation);
            return await CalculateSalaryNDeclaration(employeeCalculation, reCalculateFlag);
        }

        private async Task<bool> CalculateAndBuildDeclarationDetail(EmployeeCalculation employeeCalculation)
        {
            _logger.LogInformation("Starting method: CalculateAndBuildDeclarationDetail");

            bool flag = await GetEmployeeDeclaration(employeeCalculation.employeeDeclaration, employeeCalculation.salaryComponents);

            if (employeeCalculation.salaryComponents.Count != employeeCalculation.employeeDeclaration.SalaryComponentItems.Count)
                throw new HiringBellException("Salary component and Employee declaration count is not match. Please contact to admin");

            BuildSectionWiseComponents(employeeCalculation);

            _logger.LogInformation("Leaving method: CalculateAndBuildDeclarationDetail");
            return await Task.FromResult(flag);
        }

        private int GetNumberOfSalaryMonths(EmployeeCalculation empCal)
        {
            _logger.LogInformation("Starting method: GetNumberOfSalaryMonths");
            int numOfMonths = 0;
            int financialStartYear = empCal.companySetting.FinancialYear;
            int declarationStartMonth = empCal.companySetting.DeclarationStartMonth;
            int declarationEndMonth = empCal.companySetting.DeclarationEndMonth;
            var doj = _timezoneConverter.ToTimeZoneDateTime(empCal.Doj, _currentSession.TimeZone);

            if (doj.Year == financialStartYear || doj.Year == financialStartYear + 1)
                empCal.IsFirstYearDeclaration = true;
            else
                empCal.IsFirstYearDeclaration = false;

            if (doj.Year < financialStartYear)
            {
                numOfMonths = 12;
            }
            else if (doj.Year == financialStartYear && doj.Month < declarationStartMonth)
            {
                numOfMonths = 12;
            }
            else if (doj.Year == financialStartYear && doj.Month >= declarationStartMonth)
            {
                numOfMonths = 12 - (doj.Month - 1) + declarationEndMonth;
            }
            else if (doj.Year > financialStartYear)
            {
                numOfMonths = declarationEndMonth - doj.Month + 1;
            }

            if (numOfMonths <= 0)
                throw HiringBellException.ThrowBadRequest("Incorrect number of month calculated based on employee date of joining.");

            _logger.LogInformation("Leaving method: GetNumberOfSalaryMonths");
            return numOfMonths;
        }

        public async Task<EmployeeSalaryDetail> CalculateSalaryNDeclaration(EmployeeCalculation empCal, bool reCalculateFlag)
        {
            _logger.LogInformation("Starting method: CalculateSalaryNDeclaration");
            decimal totalDeduction = 0;
            int totalMonths = GetNumberOfSalaryMonths(empCal);

            empCal.employeeDeclaration.TotalMonths = totalMonths;

            EmployeeSalaryDetail salaryBreakup = empCal.employeeSalaryDetail;

            var flag = await CalculateAndBuildDeclarationDetail(empCal);
            if (!reCalculateFlag)
                reCalculateFlag = flag;

            // create breakup if new emp or any main setting changed
            List<AnnualSalaryBreakup> completeSalaryBreakups = CreateBreakUp(empCal, ref reCalculateFlag, salaryBreakup);

            // calculate and get gross income value and salary breakup detail
            var calculatedSalaryBreakupDetails = GetPresentMonthSalaryDetail(completeSalaryBreakups);

            // find total sum of gross from complete salary breakup list
            empCal.expectedAnnualGrossIncome = CalculateTotalAmountWillBeReceived(completeSalaryBreakups);

            //check and apply standard deduction
            totalDeduction += _componentsCalculationService.StandardDeductionComponent(empCal);

            // check and apply professional tax
            totalDeduction += _componentsCalculationService.ProfessionalTaxComponent(empCal, empCal.ptaxSlab, totalMonths);

            // check and apply employer providentfund
            totalDeduction += _componentsCalculationService.EmployerProvidentFund(empCal.employeeDeclaration, empCal.salaryGroup, totalMonths);

            // check and apply 1.5 lakhs components
            totalDeduction += _componentsCalculationService.Get_80C_DeclaredAmount(empCal.employeeDeclaration);

            // check and apply other components
            totalDeduction += _componentsCalculationService.OtherDeclarationComponent(empCal.employeeDeclaration);

            // check and apply tax saving components
            totalDeduction += _componentsCalculationService.TaxSavingComponent(empCal.employeeDeclaration);

            // check and apply HRA
            totalDeduction += _componentsCalculationService.HRACalculation(empCal.employeeDeclaration, calculatedSalaryBreakupDetails, totalMonths);


            // update salary if previous employer income if present
            if (empCal.previousEmployerDetail != null)
            {
                totalDeduction += UpdatePreviousEmployerIncome(empCal, completeSalaryBreakups);
                empCal.expectedAnnualGrossIncome = empCal.CTC / 12 * totalMonths + empCal.previousEmployerDetail.TotalIncome;
            }

            // final total taxable amount.
            empCal.employeeDeclaration.TotalAmount = empCal.expectedAnnualGrossIncome - totalDeduction;
            empCal.employeeDeclaration.TotalAmountOnNewRegim = empCal.expectedAnnualGrossIncome;

            salaryBreakup.GrossIncome = empCal.expectedAnnualGrossIncome;
            salaryBreakup.CompleteSalaryDetail = JsonConvert.SerializeObject(completeSalaryBreakups);

            //Tax regime calculation 
            if (empCal.employeeDeclaration.TotalAmount < 0)
                empCal.employeeDeclaration.TotalAmount = 0;

            var taxRegimeSlabs = _db.GetList<TaxRegime>(Procedures.Tax_Regime_By_Id_Age, new
            {
                empCal.EmployeeId
            });

            if (taxRegimeSlabs == null || taxRegimeSlabs.Count == 0)
                throw new Exception("Tax regime slabs are not found. Please configure tax regime for the current employee.");

            // generate for old tax regine detail
            var oldRegimeData = taxRegimeSlabs.Where(x => x.RegimeDescId == ApplicationConstants.OldRegim).ToList();
            _componentsCalculationService.TaxRegimeCalculation(empCal.employeeDeclaration, oldRegimeData, empCal.surchargeSlabs);

            // generate for new tax regine detail
            var newRegimeData = taxRegimeSlabs.Where(x => x.RegimeDescId == ApplicationConstants.NewRegim).ToList();
            _componentsCalculationService.NewTaxRegimeCalculation(empCal, newRegimeData, empCal.surchargeSlabs);

            //Tax Calculation for every month
            await TaxDetailsCalculation(empCal, reCalculateFlag);

            _logger.LogInformation("Leaving method: CalculateSalaryNDeclaration");
            return salaryBreakup;
        }

        private void ValidateCurrentSalaryGroupNComponents(EmployeeCalculation empCal)
        {
            if (empCal.salaryGroup == null || string.IsNullOrEmpty(empCal.salaryGroup.SalaryComponents)
                    || empCal.salaryGroup.SalaryComponents == ApplicationConstants.EmptyJsonArray)
                throw new HiringBellException("Salary group or its component not defined. Please contact to admin.");

            if (empCal.salaryGroup.GroupComponents == null || empCal.salaryGroup.GroupComponents.Count <= 0)
                empCal.salaryGroup.GroupComponents = JsonConvert
                    .DeserializeObject<List<SalaryComponents>>(empCal.salaryGroup.SalaryComponents);
        }

        private decimal UpdatePreviousEmployerIncome(EmployeeCalculation empCal, List<AnnualSalaryBreakup> completeSalaryBreakups)
        {
            var previousEmployerDetail = empCal.previousEmployerDetail;
            var settings = empCal.companySetting;

            var doj = empCal.Doj;
            if (_utilityService.CheckIsJoinedInCurrentFinancialYear(doj, settings))
            {
                doj = new DateTime(doj.Year, doj.Month, 1);
                foreach (var elem in completeSalaryBreakups)
                {
                    if (doj.Subtract(elem.PresentMonthDate).TotalDays > 0 && !elem.IsArrearMonth)
                    {
                        elem.IsPayrollExecutedForThisMonth = true;
                        elem.IsPreviouEmployer = true;
                        elem.SalaryBreakupDetails.ForEach(i => i.FinalAmount = 0);
                    }
                }
            }

            return previousEmployerDetail.TDS;
        }

        private List<AnnualSalaryBreakup> CreateBreakUp(EmployeeCalculation empCal, ref bool reCalculateFlag, EmployeeSalaryDetail salaryBreakup)
        {
            _logger.LogInformation("Starting method: CreateBreakUp");

            List<AnnualSalaryBreakup> completeSalaryBreakups = JsonConvert.DeserializeObject<List<AnnualSalaryBreakup>>(salaryBreakup.CompleteSalaryDetail);

            ValidateCurrentSalaryGroupNComponents(empCal);
            if (completeSalaryBreakups.Count == 0)
            {
                completeSalaryBreakups = _salaryComponentService.CreateSalaryBreakupWithValue(empCal);
                reCalculateFlag = true;
            }
            else if (empCal.employee.IsCTCChanged)
            {
                completeSalaryBreakups = _salaryComponentService.UpdateSalaryBreakUp(empCal, salaryBreakup);
            }
            //else if ()
            //{
            //    completeSalaryBreakups = _salaryComponentService.UpdateSalaryBreakUp(empCal, salaryBreakup);
            //    reCalculateFlag = true;
            //}

            _logger.LogInformation("Leaving method: CreateBreakUp");

            return completeSalaryBreakups;
        }

        private async Task UpdateEmployeeSalaryDetailChanges(long EmployeeId, EmployeeSalaryDetail salaryBreakup)
        {
            _logger.LogInformation("Starting method: UpdateEmployeeSalaryDetailChanges");

            if (string.IsNullOrEmpty(salaryBreakup.NewSalaryDetail))
                salaryBreakup.NewSalaryDetail = ApplicationConstants.EmptyJsonArray;

            var result = await _db.ExecuteAsync(Procedures.Employee_Salary_Detail_InsUpd, new
            {
                EmployeeId,
                salaryBreakup.CTC,
                salaryBreakup.GrossIncome,
                salaryBreakup.NetSalary,
                salaryBreakup.CompleteSalaryDetail,
                salaryBreakup.NewSalaryDetail,
                salaryBreakup.GroupId,
                salaryBreakup.TaxDetail,
                salaryBreakup.FinancialStartYear
            }, true);

            if (!BotConstant.IsSuccess(result.statusMessage))
                throw new HiringBellException("Fail to save calculation detail. Please contact to admin.");

            _logger.LogInformation("Leaving method: UpdateEmployeeSalaryDetailChanges");

        }

        private async Task TaxDetailsCalculation(EmployeeCalculation empCal, bool reCalculateFlag)
        {
            _logger.LogInformation("Starting method: TaxDetailsCalculation");

            empCal.financialYearDateTime = _timezoneConverter.ToTimeZoneDateTime(
                        new DateTime(empCal.companySetting.FinancialYear, empCal.companySetting.DeclarationStartMonth, 1, 0, 0, 0, DateTimeKind.Utc),
                        _currentSession.TimeZone
                    );

            decimal taxNeetToPay = 0;
            if (empCal.employeeDeclaration.EmployeeCurrentRegime == ApplicationConstants.OldRegim)
                taxNeetToPay = empCal.employeeDeclaration.TaxNeedToPay;
            else
                taxNeetToPay = empCal.employeeDeclaration.TaxNeedToPayOnNewRegim;

            if (empCal.companySetting == null)
                throw new HiringBellException("Company setting not found. Please contact to admin.");

            if (!string.IsNullOrEmpty(empCal.employeeSalaryDetail.TaxDetail) && empCal.employeeSalaryDetail.TaxDetail != "[]")
            {
                await ReCalculateTaxDetail(empCal, reCalculateFlag, taxNeetToPay);
            }
            else
            {
                await CreateTaxDetailForEmployee(empCal, taxNeetToPay);
            }

            _logger.LogInformation("Leaving method: TaxDetailsCalculation");
        }

        private async Task ReCalculateTaxDetail(EmployeeCalculation empCal, bool reCalculateFlag, decimal taxNeetToPay)
        {
            List<TaxDetails> taxdetails;
            var calculationOnRemainingMonth = false;
            taxdetails = JsonConvert.DeserializeObject<List<TaxDetails>>(empCal.employeeSalaryDetail.TaxDetail);
            if (empCal.financialYearDateTime.Year != DateTime.UtcNow.Year && DateTime.UtcNow.Month == 1 && DateTime.UtcNow.Day == 1)
            {
                reCalculateFlag = true;
                calculationOnRemainingMonth = true;
            }

            if (reCalculateFlag)
            {
                if (taxNeetToPay > 0)
                {
                    UpdateTaxDetail(empCal, taxNeetToPay, taxdetails, calculationOnRemainingMonth);

                    empCal.employeeSalaryDetail.TaxDetail = JsonConvert.SerializeObject(taxdetails);
                    await UpdateEmployeeSalaryDetailChanges(empCal.EmployeeId, empCal.employeeSalaryDetail);
                }
                else
                {
                    UpdatePerMonthTaxData(empCal, taxdetails);
                    empCal.employeeDeclaration.TaxPaid = Convert.ToDecimal(taxdetails
                                .Select(x => x.TaxPaid).Aggregate((i, k) => i + k));

                    empCal.employeeSalaryDetail.TaxDetail = JsonConvert.SerializeObject(taxdetails);
                    await UpdateEmployeeSalaryDetailChanges(empCal.EmployeeId, empCal.employeeSalaryDetail);
                }
            }
            else
            {
                empCal.employeeDeclaration.TaxPaid = Convert.ToDecimal(taxdetails
                        .Select(x => x.TaxPaid).Aggregate((i, k) => i + k));
            }
        }

        private async Task CreateTaxDetailForEmployee(EmployeeCalculation empCal, decimal taxNeetToPay)
        {
            List<TaxDetails> taxdetails;

            if (taxNeetToPay > 0)
            {
                taxdetails = GetPerMonthTaxInitialData(empCal);
            }
            else
            {
                taxdetails = GetPerMontTaxDetail(empCal);
            }

            empCal.employeeSalaryDetail.TaxDetail = JsonConvert.SerializeObject(taxdetails);
            await UpdateEmployeeSalaryDetailChanges(empCal.EmployeeId, empCal.employeeSalaryDetail);
        }

        private void UpdateTaxDetail(EmployeeCalculation empCal, decimal taxNeetToPay, List<TaxDetails> taxdetails, bool calculationOnRemainingMonth)
        {
            empCal.employeeDeclaration.TaxPaid = Convert.ToDecimal(taxdetails
                                        .Select(x => x.TaxPaid).Aggregate((i, k) => i + k));
            if (calculationOnRemainingMonth)
                taxNeetToPay = Convert.ToDecimal(taxdetails.Select(x => x.TaxDeducted).Aggregate((i, k) => i + k));

            empCal.employeeDeclaration.TaxNeedToPay = taxNeetToPay;

            DateTime doj = _timezoneConverter.ToTimeZoneDateTime(empCal.Doj, _currentSession.TimeZone);
            DateTime startDate = empCal.PayrollStartDate;

            decimal remaningTaxAmount = taxNeetToPay - empCal.employeeDeclaration.TaxPaid;
            int pendingMonths = taxdetails.Count(x => !x.IsPayrollCompleted);

            decimal singleMonthTax = Convert.ToDecimal(remaningTaxAmount / pendingMonths);
            foreach (var taxDetail in taxdetails)
            {
                if (!taxDetail.IsPayrollCompleted)
                {
                    if (taxDetail.Month == doj.Month && taxDetail.Year == doj.Year)
                    {
                        var daysInMonth = DateTime.DaysInMonth(doj.Year, doj.Month);
                        taxDetail.TaxDeducted = singleMonthTax / daysInMonth * (daysInMonth - doj.Day + 1);
                        taxDetail.TaxPaid = 0M;
                        if (pendingMonths > 1)
                            singleMonthTax = (remaningTaxAmount - taxDetail.TaxDeducted) / (pendingMonths - 1);
                    }
                    else
                    {
                        taxDetail.TaxDeducted = singleMonthTax;
                        taxDetail.TaxPaid = 0M;
                    }
                }
            }
        }

        private List<TaxDetails> GetPerMontTaxDetail(EmployeeCalculation empCal)
        {
            _logger.LogInformation("Starting method: GetPerMontTaxDetail");
            var taxdetails = new List<TaxDetails>();
            try
            {
                int i = 0;
                while (i <= 11)
                {
                    taxdetails.Add(new TaxDetails
                    {
                        Index = i + 1,
                        IsPayrollCompleted = false,
                        Month = empCal.financialYearDateTime.AddMonths(i).Month,
                        Year = empCal.financialYearDateTime.AddMonths(i).Year,
                        EmployeeId = empCal.EmployeeId,
                        TaxDeducted = 0,
                        TaxPaid = 0
                    });
                    i++;
                }

                _logger.LogInformation("Leaving method: GetPerMontTaxDetail");
            }
            catch
            {
                throw;
            }

            return taxdetails;
        }

        private List<TaxDetails> UpdatePerMonthTaxData(EmployeeCalculation eCal, List<TaxDetails> taxDetails)
        {
            DateTime doj = _timezoneConverter.ToTimeZoneDateTime(eCal.Doj, _currentSession.TimeZone);
            DateTime startDate = eCal.PayrollStartDate;

            CompanySetting companySetting = eCal.companySetting;
            EmployeeDeclaration employeeDeclaration = eCal.employeeDeclaration;

            var salary = JsonConvert.DeserializeObject<List<AnnualSalaryBreakup>>(eCal.employeeSalaryDetail.CompleteSalaryDetail);
            var totalWorkingMonth = salary.Count(x => x.IsActive);
            if (totalWorkingMonth == 0)
                throw HiringBellException.ThrowBadRequest($"Invalid working month count found in method: {nameof(UpdatePerMonthTaxData)}");

            var permonthTax = employeeDeclaration.TaxNeedToPay / totalWorkingMonth;
            List<TaxDetails> taxdetails = new List<TaxDetails>();

            var daysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
            var workingDays = daysInMonth - eCal.Doj.Day + 1;
            var currentMonthTax = ProrateAmountOnJoiningMonth(permonthTax, daysInMonth, workingDays);
            var remaningTaxAmount = permonthTax * totalWorkingMonth - currentMonthTax;
            permonthTax = remaningTaxAmount > 0 ? remaningTaxAmount / (totalWorkingMonth - 1) : 0;


            foreach (var x in taxDetails)
            {
                if (!x.IsPayrollCompleted)
                {
                    x.TaxDeducted = currentMonthTax;
                    x.IsPayrollCompleted = false;
                    x.TaxPaid = 0;
                }
            }
            return taxdetails;
        }

        //private int TotalMonthWorkedInFinancialYear(Company company, DateTime joiningDate)
        //{

        //}

        private List<TaxDetails> GetPerMonthTaxInitialData(EmployeeCalculation eCal)
        {
            _logger.LogInformation("Starting method: GetPerMonthTaxInitialData");
            DateTime doj = _timezoneConverter.ToTimeZoneDateTime(eCal.Doj, _currentSession.TimeZone);
            DateTime startDate = eCal.PayrollStartDate;

            CompanySetting companySetting = eCal.companySetting;
            EmployeeDeclaration employeeDeclaration = eCal.employeeDeclaration;

            var salary = JsonConvert.DeserializeObject<List<AnnualSalaryBreakup>>(eCal.employeeSalaryDetail.CompleteSalaryDetail);
            var totalWorkingMonth = salary.Count(x => x.IsActive);
            if (totalWorkingMonth == 0)
                throw HiringBellException.ThrowBadRequest($"Invalid working month count found in method: {nameof(GetPerMonthTaxInitialData)}");

            //if (doj.Year == DateTime.UtcNow.Year && doj.Month >= eCal.companySetting.DeclarationStartMonth && doj.Month < 12)
            //    totalWorkingMonth = totalWorkingMonth - companySetting.DeclarationEndMonth;

            if (doj.Year == eCal.companySetting.FinancialYear && doj.Month < 12 || doj.Year < eCal.companySetting.FinancialYear)
                totalWorkingMonth = totalWorkingMonth - companySetting.DeclarationEndMonth;

            var permonthTax = employeeDeclaration.TaxNeedToPay / totalWorkingMonth;
            List<TaxDetails> taxdetails = new List<TaxDetails>();

            int i = 0;
            while (i < 12)
            {
                if (startDate.Subtract(doj).TotalDays < 0 && startDate.Month != doj.Month && eCal.IsFirstYearDeclaration)
                {
                    taxdetails.Add(new TaxDetails
                    {
                        Index = i + 1,
                        Month = eCal.financialYearDateTime.AddMonths(i).Month,
                        Year = eCal.financialYearDateTime.AddMonths(i).Year,
                        EmployeeId = employeeDeclaration.EmployeeId,
                        TaxDeducted = 0,
                        IsPayrollCompleted = true,
                        TaxPaid = 0
                    });
                }
                else if (startDate.Month == doj.Month && startDate.Year == doj.Year)
                {
                    var daysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
                    var workingDays = daysInMonth - eCal.Doj.Day + 1;
                    var currentMonthTax = ProrateAmountOnJoiningMonth(permonthTax, daysInMonth, workingDays);
                    var remaningTaxAmount = permonthTax * totalWorkingMonth - currentMonthTax;
                    permonthTax = remaningTaxAmount > 0 ? remaningTaxAmount / (totalWorkingMonth - 1) : 0;

                    taxdetails.Add(new TaxDetails
                    {
                        Index = i + 1,
                        Month = eCal.financialYearDateTime.AddMonths(i).Month,
                        Year = eCal.financialYearDateTime.AddMonths(i).Year,
                        EmployeeId = employeeDeclaration.EmployeeId,
                        TaxDeducted = currentMonthTax,
                        IsPayrollCompleted = false,
                        TaxPaid = 0
                    });
                }
                else
                {
                    if (i < 9)
                    {
                        taxdetails.Add(new TaxDetails
                        {
                            Index = i + 1,
                            Month = eCal.financialYearDateTime.AddMonths(i).Month,
                            Year = eCal.financialYearDateTime.AddMonths(i).Year,
                            EmployeeId = employeeDeclaration.EmployeeId,
                            TaxDeducted = permonthTax,
                            IsPayrollCompleted = false,
                            TaxPaid = 0
                        });
                    }
                    else
                    {
                        taxdetails.Add(new TaxDetails
                        {
                            Index = i + 1,
                            Month = eCal.financialYearDateTime.AddMonths(i).Month,
                            Year = eCal.financialYearDateTime.AddMonths(i).Year,
                            EmployeeId = employeeDeclaration.EmployeeId,
                            TaxDeducted = 0,
                            IsPayrollCompleted = false,
                            TaxPaid = 0
                        });
                    }
                }

                startDate = startDate.AddMonths(1);
                i++;
            }
            _logger.LogInformation("Leaving method: GetPerMonthTaxInitialData");

            return taxdetails;
        }

        private decimal ProrateAmountOnJoiningMonth(decimal monthlyTaxAmount, int numOfDaysInMonth, int workingDays)
        {
            _logger.LogInformation("Starting method: ProrateAmountOnJoiningMonth");
            _logger.LogInformation("Leaving method: ProrateAmountOnJoiningMonth");

            if (numOfDaysInMonth != workingDays)
                return monthlyTaxAmount / numOfDaysInMonth * workingDays;
            else
                return monthlyTaxAmount;
        }

        private void BuildSectionWiseComponents(EmployeeCalculation employeeCalculation)
        {
            _logger.LogInformation("Starting method: BuildSectionWiseComponents");

            EmployeeDeclaration employeeDeclaration = employeeCalculation.employeeDeclaration;
            foreach (var x in _sections)
            {
                switch (x.Key)
                {
                    case ApplicationConstants.ExemptionDeclaration:
                        employeeDeclaration.ExemptionDeclaration = employeeDeclaration.SalaryComponentItems.FindAll(i => i.Section != null && x.Value.Contains(i.Section));
                        employeeDeclaration.Declarations.Add(new DeclarationReport
                        {
                            DeclarationName = ApplicationConstants.OneAndHalfLakhsExemptions,
                            NumberOfProofSubmitted = 0,
                            Declarations = employeeDeclaration.ExemptionDeclaration.Where(x => x.DeclaredValue > 0).Select(i => i.Section).ToList(),
                            TotalAmountDeclared = employeeDeclaration.ExemptionDeclaration.Sum(a => a.DeclaredValue),
                            MaxAmount = 150000
                        });
                        break;
                    case ApplicationConstants.OtherDeclaration:
                        employeeDeclaration.OtherDeclaration = employeeDeclaration.SalaryComponentItems.FindAll(i => i.Section != null && x.Value.Contains(i.Section));
                        employeeDeclaration.Declarations.Add(new DeclarationReport
                        {
                            DeclarationName = ApplicationConstants.OtherDeclarationName,
                            NumberOfProofSubmitted = 0,
                            Declarations = employeeDeclaration.OtherDeclaration.Where(x => x.DeclaredValue > 0).Select(i => i.Section).ToList(),
                            TotalAmountDeclared = employeeDeclaration.OtherDeclaration.Sum(a => a.DeclaredValue)
                        });
                        break;
                    case ApplicationConstants.TaxSavingAlloance:
                        employeeDeclaration.TaxSavingAlloance = employeeDeclaration.SalaryComponentItems.FindAll(i => i.Section != null && x.Value.Contains(i.Section));
                        employeeDeclaration.Declarations.Add(new DeclarationReport
                        {
                            DeclarationName = ApplicationConstants.TaxSavingAlloanceName,
                            NumberOfProofSubmitted = 0,
                            Declarations = employeeDeclaration.TaxSavingAlloance.Where(x => x.DeclaredValue > 0).Select(i => i.Section).ToList(),
                            TotalAmountDeclared = employeeDeclaration.TaxSavingAlloance.Sum(a => a.DeclaredValue)
                        });
                        break;
                    case ApplicationConstants.Section16TaxExemption:
                        employeeDeclaration.Section16TaxExemption = employeeDeclaration.SalaryComponentItems.FindAll(i => i.Section != null && x.Value.Contains(i.Section));
                        break;
                }
            };

            var houseProperty = employeeDeclaration.SalaryComponentItems.FindAll(x => x.ComponentId.ToLower() == ComponentNames.HRA.ToLower());
            employeeDeclaration.Declarations.Add(new DeclarationReport
            {
                DeclarationName = ComponentNames.HRA,
                NumberOfProofSubmitted = 0,
                Declarations = employeeDeclaration.TaxSavingAlloance.Where(x => x.DeclaredValue > 0).Select(i => i.Section).ToList(),
                TotalAmountDeclared = houseProperty.Sum(a => a.DeclaredValue)
            });

            //employeeDeclaration.Declarations.Add(new DeclarationReport
            //{
            //    DeclarationName = ApplicationConstants.IncomeFromOtherSources,
            //    NumberOfProofSubmitted = 0,
            //    Declarations = new List<string>(),
            //    TotalAmountDeclared = 0
            //});
            _logger.LogInformation("Leaving method: BuildSectionWiseComponents");

        }

        public async Task<string> UpdateTaxDetailsService(long EmployeeId, int PresentMonth, int PresentYear)
        {
            return await Task.FromResult("Done");
        }

        public async Task<string> UpdateTaxDetailsService(PayrollEmployeeData payrollEmployeeData,
            PayrollMonthlyDetail payrollMonthlyDetail, DateTime payrollDate, bool IsTaxCalculationRequired)
        {
            payrollMonthlyDetail.ForYear = payrollDate.Year;
            payrollMonthlyDetail.ForMonth = payrollDate.Month;
            payrollMonthlyDetail.PayrollStatus = 16;
            payrollMonthlyDetail.PaymentRunDate = payrollDate;
            payrollMonthlyDetail.ExecutedBy = _currentSession.CurrentUserDetail.UserId;
            payrollMonthlyDetail.ExecutedOn = DateTime.Now;
            payrollMonthlyDetail.CompanyId = _currentSession.CurrentUserDetail.CompanyId;

            var Result = await _db.ExecuteAsync(Procedures.PAYROLL_AND_SALARY_DETAIL_INSUPD, new
            {
                payrollMonthlyDetail.PayrollMonthlyDetailId,
                payrollEmployeeData.EmployeeId,
                payrollEmployeeData.TaxDetail,
                payrollEmployeeData.CompleteSalaryDetail,
                payrollMonthlyDetail.ForYear,
                payrollMonthlyDetail.ForMonth,
                payrollMonthlyDetail.GrossTotal,
                payrollMonthlyDetail.PayableToEmployee,
                payrollMonthlyDetail.PFByEmployer,
                payrollMonthlyDetail.PFByEmployee,
                payrollMonthlyDetail.ProfessionalTax,
                payrollMonthlyDetail.TotalDeduction,
                payrollMonthlyDetail.PayrollStatus,
                payrollMonthlyDetail.PaymentRunDate,
                payrollMonthlyDetail.ExecutedBy,
                payrollMonthlyDetail.ExecutedOn,
                payrollMonthlyDetail.CompanyId
            }, true);

            if (ApplicationConstants.IsExecuted(Result.statusMessage) && IsTaxCalculationRequired)
            {
                await GetEmployeeDeclarationDetail(payrollEmployeeData.EmployeeId, true);
            }

            return Result.statusMessage;
        }

        public async Task<string> SwitchEmployeeTaxRegimeService(EmployeeDeclaration employeeDeclaration)
        {
            if (employeeDeclaration.EmployeeId == 0)
                throw new HiringBellException("Invalid employee selected. Please select a valid employee");

            if (employeeDeclaration.EmployeeCurrentRegime == 0)
                throw new HiringBellException("Please select a valid tx regime type");

            var result = _db.Execute<EmployeeDeclaration>(Procedures.Employee_Taxregime_Update,
                new { employeeDeclaration.EmployeeId, employeeDeclaration.EmployeeCurrentRegime }, true);
            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Fail to switch the tax regime");

            await GetEmployeeDeclarationDetail(employeeDeclaration.EmployeeId, true);
            return result;
        }

        public async Task<EmployeeDeclaration> DeleteDeclarationValueService(long DeclarationId, string ComponentId)
        {
            if (DeclarationId <= 0)
                throw new HiringBellException("Invalid declaration id passed.");

            if (string.IsNullOrEmpty(ComponentId))
                throw new HiringBellException("Invalid declaration component selected. Please select a valid component");

            var resultset = _db.FetchDataSet(Procedures.Employee_Declaration_Components_Get_ById, new { EmployeeDeclarationId = DeclarationId });
            EmployeeDeclaration declaration = resultset.Tables[0].ToType<EmployeeDeclaration>();
            List<SalaryComponents> salaryComponent = resultset.Tables[1].ToList<SalaryComponents>();
            if (declaration == null || salaryComponent == null)
                throw new HiringBellException("Declaration detail not found. Please contact to admin.");

            List<SalaryComponents> salaryComponents = JsonConvert.DeserializeObject<List<SalaryComponents>>(declaration.DeclarationDetail);
            var component = salaryComponents.FirstOrDefault(x => x.ComponentId == ComponentId);
            if (component == null)
                throw new HiringBellException("Got internal error while cleaning up, please contact to admin.");
            return await ResetComponent(declaration, salaryComponents, component);
        }

        public async Task<EmployeeDeclaration> DeleteDeclaredHRAService(long DeclarationId)
        {
            if (DeclarationId <= 0)
                throw new HiringBellException("Invalid declaration id passed.");

            string ComponentId = ComponentNames.HRA;

            var resultset = _db.FetchDataSet(Procedures.Employee_Declaration_Components_Get_ById, new { EmployeeDeclarationId = DeclarationId });
            EmployeeDeclaration declaration = resultset.Tables[0].ToType<EmployeeDeclaration>();
            List<SalaryComponents> salaryComponent = resultset.Tables[1].ToList<SalaryComponents>();
            if (declaration == null || salaryComponent == null)
                throw new HiringBellException("Declaration detail not found. Please contact to admin.");

            declaration.HouseRentDetail = ApplicationConstants.EmptyJsonObject;

            List<SalaryComponents> salaryComponents = JsonConvert.DeserializeObject<List<SalaryComponents>>(declaration.DeclarationDetail);
            var component = salaryComponents.FirstOrDefault(x => x.ComponentId == ComponentId);
            if (component == null)
                throw new HiringBellException("Got internal error while cleaning up, please contact to admin.");

            return await ResetComponent(declaration, salaryComponents, component);
        }

        private async Task<EmployeeDeclaration> ResetComponent(EmployeeDeclaration declaration, List<SalaryComponents> salaryComponents, SalaryComponents component)
        {
            try
            {
                var allFileIds = JsonConvert.DeserializeObject<List<long>>(component.UploadedFileIds);
                string searchString = component.UploadedFileIds.Replace("[", "").Replace("]", "");
                List<Files> files = _db.GetList<Files>(Procedures.Userfiledetail_Get_Files, new { searchString });

                component.DeclaredValue = 0;
                component.UploadedFileIds = "[]";

                declaration.DeclarationDetail = JsonConvert.SerializeObject(salaryComponents);

                DbResult Result = null;
                if (files != null && files.Count > 0)
                {
                    foreach (var file in allFileIds)
                    {
                        Result = await _db.ExecuteAsync(Procedures.Userdetail_Del_By_File_Id, new { FileId = file }, true);
                        if (!BotConstant.IsSuccess(Result))
                            throw new HiringBellException("Fail to delete file record, Please contact to admin.");
                    }
                }

                await _db.ExecuteAsync(Procedures.Employee_Declaration_Insupd, new
                {
                    declaration.EmployeeDeclarationId,
                    declaration.EmployeeId,
                    declaration.DocumentPath,
                    declaration.DeclarationDetail,
                    declaration.HouseRentDetail,
                    declaration.TotalDeclaredAmount,
                    declaration.TotalApprovedAmount,
                    declaration.TotalRejectedAmount,
                    declaration.EmployeeCurrentRegime
                }, true);

                if (files != null)
                    _fileService.DeleteFiles(files);

                return await GetEmployeeDeclarationDetail(declaration.EmployeeId, true);
            }
            catch
            {
                throw;
            }
        }

        public async Task<EmployeeDeclaration> DeleteDeclarationFileService(long DeclarationId, int FileId, string ComponentId)
        {
            try
            {
                if (DeclarationId <= 0)
                    throw new HiringBellException("Invalid declaration id passed. Please try again.");

                if (FileId <= 0)
                    throw new HiringBellException("Invalid file selected. Please select a valid file");

                if (string.IsNullOrEmpty(ComponentId))
                    throw new HiringBellException("Invalid declaration component selected. Please select a valid component");

                (EmployeeDeclaration declaration, Files file) = _db.GetMulti<EmployeeDeclaration, Files>("sp_employee_declaration_and_file_get", new { DeclarationId, FileId });
                if (declaration == null)
                    throw new HiringBellException("Declaration detail not found. Please contact to admin.");

                List<SalaryComponents> salaryComponents = JsonConvert.DeserializeObject<List<SalaryComponents>>(declaration.DeclarationDetail);
                var component = salaryComponents.FirstOrDefault(x => x.ComponentId == ComponentId);
                if (component != null)
                {
                    var fileIds = JsonConvert.DeserializeObject<List<long>>(component.UploadedFileIds);
                    var existingFileId = fileIds.FirstOrDefault(i => i == FileId);
                    fileIds.Remove(existingFileId);
                    component.UploadedFileIds = JsonConvert.SerializeObject(fileIds);
                    declaration.DeclarationDetail = JsonConvert.SerializeObject(salaryComponents);
                }


                var Result = await _db.ExecuteAsync(Procedures.Userdetail_Del_By_File_Id, new { FileId }, true);
                if (ApplicationConstants.IsExecuted(Result.statusMessage))
                {
                    await _db.ExecuteAsync(Procedures.Employee_Declaration_Insupd, new
                    {
                        declaration.EmployeeDeclarationId,
                        declaration.EmployeeId,
                        declaration.DocumentPath,
                        declaration.DeclarationDetail,
                        declaration.HouseRentDetail,
                        declaration.TotalDeclaredAmount,
                        declaration.TotalApprovedAmount,
                        declaration.TotalRejectedAmount,
                        declaration.EmployeeCurrentRegime
                    }, true);

                    if (file != null)
                        _fileService.DeleteFiles(new List<Files> { file });
                }

                return await GetEmployeeDeclarationDetail(declaration.EmployeeId, false);
            }
            catch
            {
                throw;
            }
        }

        public async Task<DataSet> ManagePreviousEmployemntService(int EmployeeId, List<PreviousEmployementDetail> previousEmployementDetail)
        {
            if (EmployeeId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid employee selected. Please select a vlid employee");

            var dataSet = await _db.GetDataSetAsync(Procedures.Previous_Employement_And_Salary_Details_By_Empid, new { EmployeeId });
            if (dataSet.Tables.Count != 2)
                throw HiringBellException.ThrowBadRequest("Fail to get employee previous employment and salary detail.");

            var salaryDetail = dataSet.Tables[1].ToType<EmployeeSalaryDetail>();
            if (salaryDetail == null)
                throw HiringBellException.ThrowBadRequest("Fail to get employee salary detail.");

            List<PreviousEmployementDetail> employementDetails = dataSet.Tables[0].ToList<PreviousEmployementDetail>();
            if (employementDetails != null && employementDetails.Count > 0)
            {
                employementDetails.ForEach(x =>
                {
                    var employementDetail = previousEmployementDetail.Find(i => i.PreviousEmpDetailId == x.PreviousEmpDetailId);
                    if (employementDetail != null)
                    {
                        x.Gross = employementDetail.Gross;
                        x.Basic = employementDetail.Basic;
                        x.HouseRent = employementDetail.HouseRent;
                        x.EmployeePR = employementDetail.EmployeePR;
                        x.ESI = employementDetail.ESI;
                        x.LWF = employementDetail.LWF;
                        x.LWFEmp = employementDetail.LWFEmp;
                        x.Professional = employementDetail.Professional;
                        x.IncomeTax = employementDetail.IncomeTax;
                        x.OtherTax = employementDetail.OtherTax;
                        x.OtherTaxable = employementDetail.OtherTaxable;
                    }
                });
            }
            else
            {
                employementDetails = previousEmployementDetail;
            }

            var annualSalaryBreakup = JsonConvert.DeserializeObject<List<AnnualSalaryBreakup>>(salaryDetail.CompleteSalaryDetail);
            var taxDetails = JsonConvert.DeserializeObject<List<TaxDetails>>(salaryDetail.TaxDetail);
            var previousSalary = annualSalaryBreakup.Where(x => !x.IsActive).ToList();
            if (previousSalary.Count > 0)
            {
                foreach (var breakup in previousSalary)
                {
                    var workingMonth = employementDetails.Find(x => x.MonthNumber == breakup.MonthNumber);
                    var taxDetail = taxDetails.Find(x => x.Month == breakup.MonthNumber);
                    if (taxDetail != null && workingMonth != null)
                    {
                        foreach (var elem in breakup.SalaryBreakupDetails)
                        {
                            switch (elem.ComponentId)
                            {
                                case ComponentNames.GrossId:
                                    elem.ActualAmount = workingMonth.Gross;
                                    break;
                                case ComponentNames.Basic:
                                    elem.ActualAmount = workingMonth.Basic;
                                    break;
                                case ComponentNames.HRA:
                                    elem.ActualAmount = workingMonth.HouseRent;
                                    break;
                                case ComponentNames.ProfessionalTax:
                                    elem.ActualAmount = workingMonth.Professional;
                                    break;
                                case ComponentNames.EmployeePF:
                                    elem.ActualAmount = workingMonth.EmployeePR;
                                    break;
                                case ComponentNames.SpecialAllowanceId:
                                    elem.ActualAmount = workingMonth.LWF;
                                    break;
                            }
                        }
                        taxDetail.TaxPaid = workingMonth.IncomeTax;
                        taxDetail.TaxDeducted = workingMonth.IncomeTax;
                    }
                }
            }

            salaryDetail.CompleteSalaryDetail = JsonConvert.SerializeObject(annualSalaryBreakup);
            salaryDetail.TaxDetail = JsonConvert.SerializeObject(taxDetails);
            var item = (from n in employementDetails
                        select new
                        {
                            n.PreviousEmpDetailId,
                            n.EmployeeId,
                            n.Month,
                            n.MonthNumber,
                            n.Year,
                            n.Gross,
                            n.Basic,
                            n.HouseRent,
                            n.EmployeePR,
                            n.ESI,
                            n.LWF,
                            n.LWFEmp,
                            n.Professional,
                            n.IncomeTax,
                            n.OtherTax,
                            n.OtherTaxable,
                            CreatedBy = _currentSession.CurrentUserDetail.UserId,
                            UpdatedBy = _currentSession.CurrentUserDetail.UserId,
                            CreatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                            UpdatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
                        }).ToList<object>();

            var result = await _db.BatchInsetUpdate<PreviousEmployementDetail>(item);
            if (string.IsNullOrEmpty(result))
            {
                throw HiringBellException.ThrowBadRequest("Fail to insert or update previous employement details");
            }
            else
            {
                var state = _db.Execute(Procedures.Employee_Salary_Detail_Upd_Salarydetail, new
                {
                    salaryDetail.EmployeeId,
                    salaryDetail.CompleteSalaryDetail,
                    salaryDetail.TaxDetail,
                    salaryDetail.CTC,
                }, true);

                if (!ApplicationConstants.IsExecuted(state.statusMessage))
                    throw HiringBellException.ThrowBadRequest("Fail to update employement salary details");
            }

            return await GetPreviousEmployemntService(employementDetails.First().EmployeeId);
        }

        public async Task<dynamic> GetPreviousEmployemntandEmpService(int EmployeeId)
        {
            List<PreviousEmployementDetail> employementDetails = null;
            Employee emp = null;
            if (EmployeeId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid employee selected. Please select a vlid employee");

            DataSet ds = _db.FetchDataSet(Procedures.Previous_Employement_Details_And_Emp_By_Empid, new { EmployeeId });
            if (ds != null && ds.Tables.Count > 0)
            {
                employementDetails = ds.Tables[0].ToList<PreviousEmployementDetail>();
                emp = ds.Tables[1].ToType<Employee>();
            }

            return await Task.FromResult(new
            {
                EmployementDetails = employementDetails,
                EmployeeDetail = emp
            });
        }

        public async Task<DataSet> GetPreviousEmployemntService(int EmployeeId)
        {
            if (EmployeeId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid employee selected. Please select a vlid employee");

            var result = _db.FetchDataSet(Procedures.Previous_Employement_Details_By_Empid, new
            {
                EmployeeId,
                _currentSession.CurrentUserDetail.CompanyId
            });
            result.Tables[0].TableName = "PreviousSalary";
            result.Tables[1].TableName = "Employee";
            return await Task.FromResult(result);
        }

        //public async Task<string> EmptyEmpDeclarationService()
        //{
        //    EmployeeCalculation employeeCalculation = new EmployeeCalculation();
        //    employeeCalculation.employee = new Employee();
        //    this.GetEmployeeDetail(employeeCalculation);
        //    employeeCalculation.employeeDeclaration.EmployeeCurrentRegime = ApplicationConstants.DefaultTaxRegin;
        //    employeeCalculation.Doj = DateTime.UtcNow;
        //    employeeCalculation.IsFirstYearDeclaration = true;
        //    employeeCalculation.EmployeeId = employeeCalculation.employee.EmployeeUid;
        //    await CalculateSalaryNDeclaration(employeeCalculation, true);

        //    var Result = await _db.ExecuteAsync("sp_employee_declaration_insupd", new
        //    {
        //        EmployeeDeclarationId = employeeCalculation.employeeDeclaration.EmployeeDeclarationId,
        //        EmployeeId = employeeCalculation.EmployeeId,
        //        DocumentPath = "",
        //        DeclarationDetail = employeeCalculation.employeeDeclaration.DeclarationDetail,
        //        HouseRentDetail = employeeCalculation.employeeDeclaration.HouseRentDetail,
        //        TotalDeclaredAmount = 0,
        //        TotalApprovedAmount = 0,
        //        TotalRejectedAmount = 0,
        //        EmployeeCurrentRegime = employeeCalculation.employeeDeclaration.EmployeeCurrentRegime
        //    }, true);

        //    if (!Bot.IsSuccess(Result))
        //        throw new HiringBellException("Fail to update housing property document detail. Please contact to admin.");
        //    return await Task.FromResult("");
        //}

        //private void GetEmployeeDetail(EmployeeCalculation employeeCalculation)
        //{
        //    employeeCalculation.employee.EmployeeUid = 22;
        //    employeeCalculation.employee.Mobile = "8978777777";
        //    employeeCalculation.employee.Email = "atif@mail.com";
        //    employeeCalculation.employee.CompanyId = 1;
        //    employeeCalculation.employee.CTC = 2200000;
        //    employeeCalculation.CTC = employeeCalculation.employee.CTC;
        //    employeeCalculation.EmployeeId = employeeCalculation.employee.EmployeeId;
        //    DataSet resultSet = _db.FetchDataSet("sp_employee_getbyid_to_reg_or_upd", new
        //    {
        //        EmployeeId = employeeCalculation.employee.EmployeeUid,
        //        employeeCalculation.employee.Mobile,
        //        employeeCalculation.employee.Email,
        //        employeeCalculation.employee.CompanyId,
        //        employeeCalculation.employee.CTC,
        //        employeeCalculation.employee.SalaryGroupId
        //    });

        //    if (resultSet == null || resultSet.Tables.Count != 9)
        //        throw new HiringBellException("Fail to get employee relevent data. Please contact to admin.");

        //    if (resultSet.Tables[4].Rows.Count != 1)
        //        throw new HiringBellException("Company setting not found. Please contact to admin.");

        //    Employee employeeDetail = Converter.ToType<Employee>(resultSet.Tables[0]);
        //    employeeCalculation.employee = employeeDetail;
        //    if (employeeDetail.EmployeeUid > 0)
        //        employeeCalculation.Doj = employeeDetail.CreatedOn;
        //    else
        //        employeeCalculation.Doj = DateTime.UtcNow;

        //    employeeCalculation.salaryComponents = Converter.ToList<SalaryComponents>(resultSet.Tables[3]);

        //    // build and bind compnay setting
        //    employeeCalculation.companySetting = Converter.ToType<CompanySetting>(resultSet.Tables[4]);
        //    employeeCalculation.PayrollStartDate = new DateTime(employeeCalculation.companySetting.FinancialYear,
        //        employeeCalculation.companySetting.DeclarationStartMonth, 1, 0, 0, 0, DateTimeKind.Utc);

        //    // check and get Declaration object
        //    employeeCalculation.employeeDeclaration = GetDeclarationInstance(resultSet.Tables[1], employeeCalculation.employee, employeeCalculation.companySetting);
        //    employeeCalculation.employeeDeclaration.HouseRentDetail = "{}";

        //    // check and get employee salary detail object
        //    employeeCalculation.employeeSalaryDetail = GetEmployeeSalaryDetailInstance(resultSet.Tables[2]);
        //    employeeCalculation.employeeSalaryDetail.FinancialStartYear = employeeCalculation.companySetting.FinancialYear;
        //    employeeCalculation.employeeSalaryDetail.TaxDetail = "[]";
        //    employeeCalculation.employeeSalaryDetail.CompleteSalaryDetail = "[]";

        //    employeeCalculation.salaryGroup = Converter.ToType<SalaryGroup>(resultSet.Tables[6]);

        //    // getting professional tax detail based on company id
        //    employeeCalculation.ptaxSlab = Converter.ToList<PTaxSlab>(resultSet.Tables[7]);

        //    if (employeeCalculation.ptaxSlab.Count == 0)
        //        throw HiringBellException.ThrowBadRequest("Professional tax not found for the current employee. Please contact to admin.");

        //    // getting surcharges slab detail based on company id
        //    employeeCalculation.surchargeSlabs = Converter.ToList<SurChargeSlab>(resultSet.Tables[8]);

        //    if (employeeCalculation.surchargeSlabs.Count == 0)
        //        throw HiringBellException.ThrowBadRequest("Surcharges slab not found for the current employee. Please contact to admin.");

        //    if (employeeDetail != null)
        //        employeeCalculation.employee.OrganizationId = employeeCalculation.employee.OrganizationId;
        //    else
        //        employeeCalculation.employee.OrganizationId = employeeCalculation.employee.OrganizationId;
        //}

        private EmployeeDeclaration GetDeclarationInstance(DataTable declarationTable, Employee employee, CompanySetting companySetting)
        {
            EmployeeDeclaration employeeDeclaration = null;
            if (declarationTable.Rows.Count == 1)
            {
                employeeDeclaration = declarationTable.ToType<EmployeeDeclaration>();
                if (employeeDeclaration.SalaryDetail == null)
                {
                    employeeDeclaration.SalaryDetail = new EmployeeSalaryDetail();
                }
                var declarationComponent = JsonConvert.DeserializeObject<List<SalaryComponents>>(employeeDeclaration.DeclarationDetail);
                if (declarationComponent.Count <= 0)
                    throw new Exception("Declaration details not found");

                declarationComponent.ForEach(x =>
                {
                    x.DeclaredValue = 0;
                });
                employeeDeclaration.DeclarationDetail = GetDeclarationBasicFields(declarationComponent);
                employeeDeclaration.SalaryDetail.CTC = employee.CTC;
                employeeDeclaration.EmployeeDeclarationId = 0;
            }
            else
            {
                employeeDeclaration = new EmployeeDeclaration
                {
                    SalaryDetail = new EmployeeSalaryDetail
                    {
                        CTC = employee.CTC
                    }
                };
            }
            employeeDeclaration.DeclarationEndMonth = companySetting.DeclarationEndMonth;
            employeeDeclaration.DeclarationFromYear = companySetting.FinancialYear;
            employeeDeclaration.DeclarationToYear = companySetting.FinancialYear + 1;
            employeeDeclaration.DeclarationStartMonth = companySetting.DeclarationStartMonth;
            return employeeDeclaration;
        }

        private EmployeeSalaryDetail GetEmployeeSalaryDetailInstance(DataTable salaryDetailTable)
        {
            EmployeeSalaryDetail employeeSalaryDetail = null;
            if (salaryDetailTable.Rows.Count == 1)
            {
                employeeSalaryDetail = salaryDetailTable.ToType<EmployeeSalaryDetail>();
            }
            else
            {
                employeeSalaryDetail = new EmployeeSalaryDetail
                {
                    GrossIncome = 0,
                    NetSalary = 0,
                    CompleteSalaryDetail = "[]",
                    TaxDetail = "[]"
                };
            }

            return employeeSalaryDetail;
        }

        public async Task UpdateBulkDeclarationDetail(long EmployeeDeclarationId, List<EmployeeDeclaration> employeeDeclarationList)
        {
            IFormFileCollection FileCollection = null;
            List<Files> files = null;
            EmployeeDeclaration empDeclaration = new EmployeeDeclaration();
            SalaryComponents salaryComponent = null;

            EmployeeDeclaration declaration = GetDeclarationById(EmployeeDeclarationId);
            if (declaration.EmployeeCurrentRegime != 1)
                throw HiringBellException.ThrowBadRequest("You can't submit the declration because you selected new tax regime");

            _sections.TryGetValue(ApplicationConstants.ExemptionDeclaration, out List<string> taxexemptSection);
            bool isExtara_50k_Allowed = false;
            declaration.SalaryComponentItems = JsonConvert.DeserializeObject<List<SalaryComponents>>(declaration.DeclarationDetail);
            foreach (var employeeDeclaration in employeeDeclarationList)
            {
                try
                {
                    declaration.Email = employeeDeclaration.Email;
                    if (declaration != null && !string.IsNullOrEmpty(declaration.DeclarationDetail))
                    {
                        salaryComponent = declaration.SalaryComponentItems.Find(x => x.ComponentId == employeeDeclaration.ComponentId);

                        if (salaryComponent == null)
                            throw new HiringBellException("Requested component not found. Please contact to admin.");

                        if (salaryComponent.MaxLimit > 0 && employeeDeclaration.DeclaredValue > salaryComponent.MaxLimit)
                            throw new HiringBellException("Your declared value is greater than maximum limit");

                        if (employeeDeclaration.DeclaredValue < 0)
                            throw new HiringBellException("Declaration value must be greater than 0. Please check your detail once.");

                        salaryComponent.DeclaredValue = employeeDeclaration.DeclaredValue;

                        if (salaryComponent.DeclaredValue > 0)
                        {
                            var maxLimit = ApplicationConstants.Limit_80C;
                            employeeDeclaration.ExemptionDeclaration = declaration.SalaryComponentItems
                                .FindAll(i => i.Section != null && taxexemptSection.Contains(i.Section));
                            var totalAmountDeclared = employeeDeclaration.ExemptionDeclaration.Sum(a => a.DeclaredValue);
                            var npsComponent = employeeDeclaration.ExemptionDeclaration.Find(x => x.Section == ApplicationConstants.NPS_Section);

                            if (salaryComponent.Section == ApplicationConstants.NPS_Section && !isExtara_50k_Allowed)
                            {
                                maxLimit += ApplicationConstants.NPS_Allowed_Limit;
                                isExtara_50k_Allowed = true;
                            }
                            else
                            {
                                totalAmountDeclared = totalAmountDeclared - npsComponent.DeclaredValue;
                            }

                            if (totalAmountDeclared > maxLimit)
                            {
                                salaryComponent.DeclaredValue = 0;
                                throw HiringBellException.ThrowBadRequest("Your limit for this section is exceed from maximum limit");
                            }
                        }

                        await ExecuteDeclarationDetail(files, declaration, FileCollection, salaryComponent);
                    }
                    else
                    {
                        _logger.LogInformation($"Requested component: {employeeDeclaration.ComponentId} found. Please contact to admin.");
                    }
                }
                catch
                {
                    _logger.LogInformation($"Requested component: {employeeDeclaration.ComponentId} found.");
                }
            }
        }

        public async Task<string> ExportEmployeeDeclarationService(List<int> EmployeeIds)
        {
            if (EmployeeIds.Count <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid employees id");

            var filepath = string.Empty;
            var empIds = JsonConvert.SerializeObject(EmployeeIds);
            empIds = empIds.Replace("[", "").Replace("]", "");
            var result = _db.GetList<EmployeeDeclaration>(Procedures.Declaration_Get_Filter_By_Empid, new { searchString = empIds });
            if (result.Count > 0)
            {
                var folderPath = Path.Combine(_fileLocationDetail.DocumentFolder, "Employees_Declaration_Excel");
                if (!Directory.Exists(Path.Combine(_hostingEnvironment.ContentRootPath, folderPath)))
                    Directory.CreateDirectory(Path.Combine(_hostingEnvironment.ContentRootPath, folderPath));

                filepath = Path.Combine(folderPath, "Employees_Declaration_Excel" + $".{ApplicationConstants.Excel}");
                var destinationFilePath = Path.Combine(_hostingEnvironment.ContentRootPath, filepath);

                if (File.Exists(destinationFilePath))
                    File.Delete(destinationFilePath);

                List<string> header = new List<string>();
                List<dynamic> data = new List<dynamic>();
                int index = 0;
                while (index < result.Count)
                {
                    var declarationDetail = JsonConvert.DeserializeObject<List<SalaryComponents>>(result[index].DeclarationDetail);

                    List<object> excelData = new List<object>();
                    if (index == 0)
                    {
                        header.Add("EmployeeId");
                        header.Add("EmployeeName");
                        header.Add("Email");
                    }

                    excelData.Add(result[index].EmployeeId);
                    excelData.Add(result[index].FullName);
                    excelData.Add(result[index].Email);
                    declarationDetail.ForEach(n =>
                    {
                        if (index == 0)
                            header.Add(n.ComponentId + $" ({n.ComponentFullName})");

                        excelData.Add(n.DeclaredValue);
                    });
                    data.Add(excelData);
                    index++;
                }
                _excelWriter.ToExcelWithHeaderAnddata(header, data, destinationFilePath);
            }
            return await Task.FromResult(filepath);
        }
    }
}