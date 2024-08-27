using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using Bot.CoreBottomHalf.CommonModal.Enums;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using EMailService.Modal;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using ServiceLayer.Code.PayrollCycle.Interface;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class UploadPayrollDataService : IUploadPayrollDataService
    {
        private readonly IDb _db;
        private readonly IEmployeeService _employeeService;
        private readonly CurrentSession _currentSession;
        private readonly IRegisterEmployeeCalculateDeclaration _registerEmployeeCalculateDeclaration;

        public UploadPayrollDataService(IDb db,
            IEmployeeService employeeService,
            CurrentSession currentSession,
            IRegisterEmployeeCalculateDeclaration registerEmployeeCalculateDeclaration)
        {
            _db = db;
            _employeeService = employeeService;
            _currentSession = currentSession;
            _registerEmployeeCalculateDeclaration = registerEmployeeCalculateDeclaration;
        }

        public async Task<List<UploadedPayrollData>> ReadPayrollDataService(IFormFileCollection files)
        {
            try
            {
                var uploadedPayrollData = await ReadPayrollExcelData(files);
                CheckDuplicateEmailAndMobile(uploadedPayrollData);

                EmployeeCalculation employeeCalculation = await GetEmployeeRegistrationCommonData();
                await UpdateEmployeeData(uploadedPayrollData, employeeCalculation);
                return uploadedPayrollData;
            }
            catch
            {
                throw;
            }
        }

        private void CheckDuplicateEmailAndMobile(List<UploadedPayrollData> uploadedPayrollData)
        {
            var duplicateEmails = uploadedPayrollData.Select(x => x.Email).GroupBy(x => x).SelectMany(x => x.Skip(1)).ToList();
            if (duplicateEmails.Count > 0)
                throw HiringBellException.ThrowBadRequest($"Duplicate email found");

            var duplicateMobiles = uploadedPayrollData.Select(x => x.Mobile).GroupBy(x => x).SelectMany(x => x.Skip(1)).ToList();
            if (duplicateMobiles.Count > 0)
                throw HiringBellException.ThrowBadRequest($"Duplicate email found");
        }

        private async Task<EmployeeCalculation> GetEmployeeRegistrationCommonData()
        {
            var resultSet = _db.FetchDataSet(Procedures.EMPLOYEE_REGISTRATION_COMMON_DATA, new
            {
                _currentSession.CurrentUserDetail.CompanyId
            });

            if (resultSet == null || resultSet.Tables.Count != 4)
                throw HiringBellException.ThrowBadRequest("Fail to get employee relevant data. Please contact to admin.");

            EmployeeCalculation employeeCalculation = new EmployeeCalculation();

            employeeCalculation.salaryComponents = Converter.ToList<SalaryComponents>(resultSet.Tables[0]);
            employeeCalculation.companySetting = Converter.ToType<CompanySetting>(resultSet.Tables[1]);

            employeeCalculation.ptaxSlab = Converter.ToList<PTaxSlab>(resultSet.Tables[2]);
            if (employeeCalculation.ptaxSlab.Count == 0)
                throw HiringBellException.ThrowBadRequest("Professional tax not found for the current employee. Please contact to admin.");

            employeeCalculation.surchargeSlabs = Converter.ToList<SurChargeSlab>(resultSet.Tables[3]);
            if (employeeCalculation.surchargeSlabs.Count == 0)
                throw HiringBellException.ThrowBadRequest("Surcharges slab not found for the current employee. Please contact to admin.");

            if (employeeCalculation.companySetting.FinancialYear == 0)
            {
                if (DateTime.UtcNow.Month < 4)
                    employeeCalculation.companySetting.FinancialYear = DateTime.UtcNow.Year - 1;
                else
                    employeeCalculation.companySetting.FinancialYear = DateTime.UtcNow.Year;
            }

            if (employeeCalculation.companySetting.DeclarationStartMonth == 0)
                employeeCalculation.companySetting.DeclarationStartMonth = 4;

            employeeCalculation.PayrollLocalTimeStartDate = new DateTime(employeeCalculation.companySetting.FinancialYear,
                employeeCalculation.companySetting.DeclarationStartMonth, 1, 0, 0, 0, DateTimeKind.Utc);

            return await Task.FromResult(employeeCalculation);
        }

        private async Task UpdateEmployeeData(List<UploadedPayrollData> uploadedPayrolls, EmployeeCalculation employeeCalculation)
        {
            int i = 0;
            int skipIndex = 0;
            int chunkSize = 50;
            while (i < uploadedPayrolls.Count)
            {
                var emps = uploadedPayrolls.Skip(skipIndex++ * chunkSize).Take(chunkSize).ToList();

                //var ids = JsonConvert.SerializeObject(emps.Select(x => x.EmployeeId).ToList());
                //var employees = _db.GetList<Employee>("sp_active_employees_by_ids", new { EmployeeIds = ids });

                foreach (UploadedPayrollData e in emps)
                {
                    //var em = employees.Find(x => x.EmployeeUid == e.EmployeeId);
                    //if (uploadedPayrollData.FindAll(x => x.Email == e.Email).Count > 1)
                    //    throw HiringBellException.ThrowBadRequest($"Email id: {e.Email} of {e.EmployeeName} is duplicate.");

                    //if (uploadedPayrollData.FindAll(x => x.Mobile == e.Mobile).Count > 1)
                    //    throw HiringBellException.ThrowBadRequest($"Mobile No.: {e.Mobile} of {e.EmployeeName} is duplicate.");

                    //if (em != null)
                    //{
                    //    if (e.CTC > 0)
                    //    {
                    //        em.CTC = e.CTC;
                    //        em.IsCTCChanged = true;
                    //        // await _employeeService.UpdateEmployeeByExcelService(em, null, null);
                    //        await _registerEmployeeCalculateDeclaration.UpdateEmployeeService(em, null, null);
                    //    }
                    //}
                    //else
                    //{
                    //    EmployeeEmailMobileCheck employeeEmailMobileCheck = _db.Get<EmployeeEmailMobileCheck>("sp_employee_email_mobile_duplicate_checked", new
                    //    {
                    //        e.Mobile,
                    //        e.Email
                    //    });

                    //    if (employeeEmailMobileCheck.MobileCount > 0)
                    //        throw HiringBellException.ThrowBadRequest($"Mobile No.: {e.Mobile} of {e.EmployeeName} is already exist.");

                    //    if (employeeEmailMobileCheck.EmailCount > 0)
                    //        throw HiringBellException.ThrowBadRequest($"Email id: {e.Email} of {e.EmployeeName} is already exist.");

                    //    await RegisterNewEmployee(e);
                    //}

                    //EmployeeEmailMobileCheck employeeEmailMobileCheck = _db.Get<EmployeeEmailMobileCheck>("sp_employee_email_mobile_duplicate_checked", new
                    //{
                    //    e.Mobile,
                    //    e.Email
                    //});

                    //if (employeeEmailMobileCheck.MobileCount > 0)
                    //    throw HiringBellException.ThrowBadRequest($"Mobile No.: {e.Mobile} of {e.EmployeeName} is already exist.");

                    //if (employeeEmailMobileCheck.EmailCount > 0)
                    //    throw HiringBellException.ThrowBadRequest($"Email id: {e.Email} of {e.EmployeeName} is already exist.");

                    EmployeeCalculation empCalc = new EmployeeCalculation
                    {
                        salaryComponents = employeeCalculation.salaryComponents,
                        companySetting = employeeCalculation.companySetting,
                        ptaxSlab = employeeCalculation.ptaxSlab,
                        surchargeSlabs = employeeCalculation.surchargeSlabs,
                        PayrollLocalTimeStartDate = employeeCalculation.PayrollLocalTimeStartDate,
                    };

                    empCalc.companySetting.DeclarationStartMonth = employeeCalculation.companySetting.DeclarationStartMonth;
                    empCalc.companySetting.FinancialYear = employeeCalculation.companySetting.FinancialYear;

                    await RegisterNewEmployee(e, empCalc);
                }

                i += chunkSize;
            }
        }

        private async Task RegisterNewEmployee(UploadedPayrollData emp, EmployeeCalculation employeeCalculation)
        {
            Employee employee = new Employee
            {
                AadharNo = emp.AadharNo,
                AccountNumber = "NA",
                BankName = "NA",
                BranchName = "NA",
                EmployeeUid = emp.EmployeeId,
                CreatedOn = emp.DOJ,
                Domain = "NA",
                Email = emp.Email,
                Mobile = emp.Mobile,
                EmpProfDetailUid = 0,
                ExperienceInYear = 0,
                FirstName = emp.EmployeeName,
                IFSCCode = "NA",
                LastCompanyName = "NA",
                LastName = emp.EmployeeName,
                PANNo = emp.PAN,
                SecondaryMobile = "NA",
                Specification = "NA",
                AccessLevelId = (int)RolesName.User,
                OrganizationId = _currentSession.CurrentUserDetail.OrganizationId,
                LeavePlanId = LocalConstants.DefaultLeavePlanId,
                PayrollGroupId = 0,
                SalaryGroupId = 0,
                CompanyId = _currentSession.CurrentUserDetail.CompanyId,
                NoticePeriodId = 0,
                FatherName = "NA",
                MotherName = "NA",
                SpouseName = "NA",
                Gender = true,
                State = "NA",
                City = "NA",
                Pincode = 000000,
                Address = emp.Address,
                ExprienceInYear = 0,
                IsPermanent = true,
                ActualPackage = 0,
                FinalPackage = 0,
                TakeHomeByCandidate = 0,
                ReportingManagerId = LocalConstants.DefaultReportingMangerId,
                DesignationId = 13,
                UserTypeId = (int)UserType.Employee,
                CTC = emp.CTC,
                DateOfJoining = emp.DOJ,
                DOB = new DateTime(1990, 5, 16),
                WorkShiftId = LocalConstants.DefaultWorkShiftId,
                IsCTCChanged = false,
                EmployerPF = emp.EmployerPF,
                EmployeePF = emp.EmployeePF,
            };

            var Names = emp.EmployeeName.Split(' ');
            if (Names.Length > 1)
            {
                employee.FirstName = Names[0];
                employee.LastName = string.Join(" ", Names.Skip(1).ToList());
            }
            else
            {
                employee.FirstName = Names[0];
                employee.LastName = "NA";
            }

            await _employeeService.RegisterEmployeeByExcelService(employee, emp, employeeCalculation);
        }

        private async Task<List<UploadedPayrollData>> ReadPayrollExcelData(IFormFileCollection files)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            DataTable dataTable = null;
            List<UploadedPayrollData> uploadedPayrollList = new List<UploadedPayrollData>();

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

                            uploadedPayrollList = MapEmployeePayAndInvestment(dataTable);
                        }
                    }
                    else
                    {
                        throw HiringBellException.ThrowBadRequest("Please select a valid excel file");
                    }
                }

                validateExcelValue(uploadedPayrollList);
            }

            return uploadedPayrollList;
        }

        private void validateExcelValue(List<UploadedPayrollData> uploadedPayrollList)
        {
            if (uploadedPayrollList.Count > 0)
            {
                var data = uploadedPayrollList[0];
                if (string.IsNullOrEmpty(data.EmployeeName))
                    throw HiringBellException.ThrowBadRequest("Employee name is null");

                if (string.IsNullOrEmpty(data.Email))
                    throw HiringBellException.ThrowBadRequest("Email is null");

                if (string.IsNullOrEmpty(data.Mobile))
                    throw HiringBellException.ThrowBadRequest("Mobile is null");

                if (data.CTC <= 0)
                    throw HiringBellException.ThrowBadRequest("CTC is zero");

                if (data?.DOJ == null)
                    throw HiringBellException.ThrowBadRequest("DOJ is invalid");

            }
        }

        public static List<UploadedPayrollData> MapEmployeePayAndInvestment(DataTable table)
        {
            string TypeName = string.Empty;
            DateTime date = DateTime.Now;
            DateTime defaultDate = Convert.ToDateTime("1976-01-01");
            List<UploadedPayrollData> items = new List<UploadedPayrollData>();
            string[] dateFormats = { "MM/dd/yyyy", "dd-MM-yyyy", "yyyy/MM/dd", "yyyy-MM-dd", "dd-MMM-yyyy" };

            try
            {
                List<PropertyInfo> props = typeof(UploadedPayrollData).GetProperties().ToList();
                List<string> fieldNames = ValidateHeaders(table, props);
                Dictionary<string, decimal> investments = null;

                if (table.Rows.Count > 0)
                {
                    int i = 0;
                    DataRow dr = null;
                    while (i < table.Rows.Count)
                    {
                        dr = table.Rows[i];

                        investments = new Dictionary<string, decimal>();
                        UploadedPayrollData t = new UploadedPayrollData();
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
                                            if (dr[x.Name].ToString().Equals("Yes", StringComparison.OrdinalIgnoreCase))
                                                x.SetValue(t, true);
                                            else if (dr[x.Name].ToString().Equals("No", StringComparison.OrdinalIgnoreCase))
                                                x.SetValue(t, false);
                                            else if (dr[x.Name].ToString().Equals("Any", StringComparison.OrdinalIgnoreCase))
                                                x.SetValue(t, false);
                                            else
                                                x.SetValue(t, Convert.ToBoolean(dr[x.Name]));
                                            break;
                                        case nameof(Int32):
                                            if (dr[x.Name] != DBNull.Value)
                                                x.SetValue(t, Convert.ToInt32(dr[x.Name]));
                                            else
                                                x.SetValue(t, 0);
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
                            else
                            {
                                try
                                {
                                    var value = Convert.ToDecimal(dr[n]);
                                    investments.Add(n, value);
                                }
                                catch
                                {
                                    investments.Add(n, 0);
                                }
                            }
                        });

                        t.Investments = investments;
                        items.Add(t);
                        i++;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
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

            if (string.IsNullOrEmpty(columnList.Find(x => x == "EmployeeName")))
                throw HiringBellException.ThrowBadRequest("EmployeeName column is not found");

            if (string.IsNullOrEmpty(columnList.Find(x => x == "Email")))
                throw HiringBellException.ThrowBadRequest("Email column is not found");

            if (string.IsNullOrEmpty(columnList.Find(x => x == "Mobile")))
                throw HiringBellException.ThrowBadRequest("Mobile column is not found");

            if (string.IsNullOrEmpty(columnList.Find(x => x == "DOJ")))
                throw HiringBellException.ThrowBadRequest("DOJ column is not found");

            if (string.IsNullOrEmpty(columnList.Find(x => x == "CTC")))
                throw HiringBellException.ThrowBadRequest("CTC column is not found");

            foreach (PropertyInfo pinfo in fileds)
            {
                if (pinfo.Name != "Investments")
                {
                    var field = columnList.Find(x => x == pinfo.Name);
                    if (field == null)
                        throw HiringBellException.ThrowBadRequest($"Excel doesn't contain \"{pinfo.Name}\" field.");
                }
            }

            return columnList;
        }
    }
}
