using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ServiceLayer.Code.PayrollCycle.Interface;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ServiceLayer.Code.PayrollCycle.Code
{
    public class UploadPayrollDataService : IUploadPayrollDataService
    {
        private readonly IDb _db;
        private readonly IEmployeeService _employeeService;
        private readonly CurrentSession _currentSession;

        public UploadPayrollDataService(IDb db, IEmployeeService employeeService, CurrentSession currentSession)
        {
            _db = db;
            _employeeService = employeeService;
            _currentSession = currentSession;

        }

        public async Task<List<UploadedPayrollData>> ReadPayrollDataService(IFormFileCollection files)
        {
            try
            {
                var uploadedPayrollData = await ReadPayrollExcelData(files);
                await UpdateEmployeeData(uploadedPayrollData);
                return uploadedPayrollData;
            }
            catch
            {
                throw;
            }
        }

        private async Task UpdateEmployeeData(List<UploadedPayrollData> uploadedPayrolls)
        {
            int i = 0;
            int skipIndex = 0;
            int chunkSize = 2;
            while (i < uploadedPayrolls.Count)
            {
                var emps = uploadedPayrolls.Skip(skipIndex++ * chunkSize).Take(chunkSize).ToList();

                var ids = JsonConvert.SerializeObject(emps.Select(x => x.EmployeeId).ToList());
                var employees = _db.GetList<Employee>("sp_active_employees_by_ids", new { EmployeeIds = ids });

                foreach (UploadedPayrollData e in emps)
                {
                    var em = employees.Find(x => x.EmployeeUid == e.EmployeeId);
                    if (emps.FindAll(x => x.Email == e.Email).Count > 1)
                        throw HiringBellException.ThrowBadRequest($"Email id: {e.Email} of {e.EmployeeName} is duplicate.");

                    if (emps.FindAll(x => x.Mobile == e.Mobile).Count > 1)
                        throw HiringBellException.ThrowBadRequest($"Mobile No.: {e.Mobile} of {e.EmployeeName} is duplicate.");

                    if (em != null)
                    {
                        if (e.CTC > 0)
                        {
                            em.CTC = e.CTC;
                            em.IsCTCChanged = true;
                            await _employeeService.UpdateEmployeeByExcelService(em, null, null);
                        }
                    }
                    else
                    {
                        EmployeeEmailMobileCheck employeeEmailMobileCheck = _db.Get<EmployeeEmailMobileCheck>("sp_employee_email_mobile_duplicate_checked", new
                        {
                            e.Mobile,
                            e.Email
                        });

                        if (employeeEmailMobileCheck.MobileCount > 0)
                            throw HiringBellException.ThrowBadRequest($"Mobile No.: {e.Mobile} of {e.EmployeeName} is already exist.");

                        if (employeeEmailMobileCheck.EmailCount > 0)
                            throw HiringBellException.ThrowBadRequest($"Email id: {e.Email} of {e.EmployeeName} is already exist.");

                        await RegisterNewEmployee(e);
                    }
                }

                i++;
            }
        }

        private async Task RegisterNewEmployee(UploadedPayrollData emp)
        {
            var salarygroup = _db.Get<SalaryGroup>("sp_salary_group_get_by_ctc", new { CTC = emp.CTC });
            if (salarygroup == null)
                salarygroup = _db.Get<SalaryGroup>("sp_salary_group_getById", new { SalaryGroupId = 1 });
            else
                throw HiringBellException.ThrowBadRequest("Salary group not found. Please contact to admin");

            Employee employee = new Employee
            {
                AadharNo = "NA",
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
                OrganizationId = 1,
                LeavePlanId = 1,
                PayrollGroupId = 0,
                SalaryGroupId = salarygroup.SalaryGroupId,
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
                ReportingManagerId = 1,
                DesignationId = 13,
                UserTypeId = 1,
                CTC = emp.CTC,
                DateOfJoining = emp.DOJ,
                DOB = new DateTime(1990, 5, 16),
                WorkShiftId = 1,
                IsCTCChanged = false
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

            await _employeeService.RegisterEmployeeByExcelService(employee, emp);
        }

        private async Task<List<UploadedPayrollData>> ReadPayrollExcelData(IFormFileCollection files)
        {
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

                            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

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

                if (data.DOJ == null)
                    throw HiringBellException.ThrowBadRequest("DOJ is invalid");

            }
        }

        public static List<UploadedPayrollData> MapEmployeePayAndInvestment(DataTable table)
        {
            string TypeName = string.Empty;
            DateTime date = DateTime.Now;
            DateTime defaultDate = Convert.ToDateTime("1976-01-01");
            List<UploadedPayrollData> items = new List<UploadedPayrollData>();

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
                                            if (dr[x.Name] != DBNull.Value)
                                                x.SetValue(t, Convert.ToBoolean(dr[x.Name]));
                                            else
                                                x.SetValue(t, default(bool));
                                            break;
                                        case nameof(Int32):
                                            if (dr[x.Name] != DBNull.Value)
                                                x.SetValue(t, Convert.ToInt32(dr[x.Name]));
                                            else
                                                x.SetValue(t, Int32.MinValue);
                                            break;
                                        case nameof(Int64):
                                            if (dr[x.Name] != DBNull.Value)
                                                x.SetValue(t, Convert.ToInt64(dr[x.Name]));
                                            else
                                                x.SetValue(t, Int64.MinValue);
                                            break;
                                        case nameof(Decimal):
                                            if (dr[x.Name] != DBNull.Value)
                                                x.SetValue(t, Convert.ToDecimal(dr[x.Name]));
                                            else
                                                x.SetValue(t, Decimal.MinValue);
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
