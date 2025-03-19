using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using Bot.CoreBottomHalf.Modal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using Bt.Lib.PipelineConfig.MicroserviceHttpRequest;
using Bt.Lib.PipelineConfig.Model;
using EMailService.Modal;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class CommonService : ICommonService
    {
        private readonly IDb _db;
        private readonly RequestMicroservice _requestMicroservice;
        private readonly CurrentSession _currentSession;
        private readonly MicroserviceRegistry _microserviceRegistry;
        public CommonService(IDb db, RequestMicroservice requestMicroservice, CurrentSession currentSession, MicroserviceRegistry microserviceRegistry)
        {
            _db = db;
            _requestMicroservice = requestMicroservice;
            _currentSession = currentSession;
            _microserviceRegistry = microserviceRegistry;
        }

        public List<Employee> LoadEmployeeData()
        {
            FilterModel filterModel = new FilterModel();
            List<Employee> employeeTable = _db.GetList<Employee>(Procedures.Employees_Get, new
            {
                filterModel.SearchString,
                filterModel.SortBy,
                filterModel.PageIndex,
                filterModel.PageSize
            });

            return employeeTable;
        }

        public EmailTemplate GetTemplate(int EmailTemplateId)
        {
            (EmailTemplate emailTemplate, EmailSettingDetail emailSetting) =
                _db.GetMulti<EmailTemplate, EmailSettingDetail>(Procedures.Email_Template_By_Id, new { EmailTemplateId });

            if (emailSetting == null)
                throw new HiringBellException("Email setting detail not found. Please contact to admin.");

            if (emailTemplate == null)
                throw new HiringBellException("Email template not found. Please contact to admin.");

            return emailTemplate;
        }

        public bool IsEmptyJson(string json)
        {
            if (!string.IsNullOrEmpty(json))
            {
                if (json == "" || json == "{}" || json == "[]")
                    return true;
            }
            else
                return true;

            return false;
        }

        public string GetUniquecode(long id, string name, int size = 10)
        {
            if (string.IsNullOrEmpty(name))
                throw HiringBellException.ThrowBadRequest("Name variable is null or empty");

            var uniqueCode = name.First() + "" + name.Last();
            var zeroCount = size - (uniqueCode.Length + id.ToString().Length);
            var i = 0;
            while (i < zeroCount)
            {
                uniqueCode += "0";
                i++;
            }
            uniqueCode += id.ToString();
            return uniqueCode;
        }

        public string GetEmployeeCode(long id, string employeeCodePrefix, int size = 5)
        {
            StringBuilder empCode = new StringBuilder();
            if (!string.IsNullOrEmpty(employeeCodePrefix))
                empCode.Append(employeeCodePrefix);

            string empId = id.ToString();

            var zeroCount = size - empId.Length;
            var i = 0;
            while (i < zeroCount)
            {
                empCode.Append(0);
                i++;
            }

            empCode.Append(empId);

            return empCode.ToString();
        }

        public int ExtractEmployeeId(string empCode, string employeeCodePrefix)
        {
            if (string.IsNullOrEmpty(empCode))
                throw HiringBellException.ThrowBadRequest("Invalid employee code");

            if (!string.IsNullOrEmpty(employeeCodePrefix))
                empCode = empCode.Replace(employeeCodePrefix, "");

            return int.Parse(empCode);
        }

        public long DecryptUniqueCoe(string code)
        {
            string value = Regex.Replace(code, "[A-Za-z ]", "");
            long id = long.Parse(value);
            return id;
        }

        public string GetStringifySalaryGroupData(List<SalaryComponents> salaryComponents)
        {
            return JsonConvert.SerializeObject(salaryComponents.Select(x => new
            {
                Formula = x.Formula,
                Section = x.Section,
                TaxExempt = x.TaxExempt,
                ComponentId = x.ComponentId,
                ComponentTypeId = x.ComponentTypeId,
                IncludeInPayslip = x.IncludeInPayslip,
                ComponentFullName = x.ComponentFullName,
                IsComponentEnabled = x.IsComponentEnabled,
                ComponentCatagoryId = x.ComponentCatagoryId
            }));
        }

        public async Task<DataTable> ReadExcelData(IFormFileCollection files)
        {
            DataTable dataTable = null;

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

            return dataTable;
        }

        public async Task<List<T>> ReadExcelData<T>(IFormFileCollection files)
        {
            DataTable dataTable = null;
            List<T> data = new List<T>();

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

                                data = MappedDate<T>(dataTable);
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

            return data;
        }

        private List<T> MappedDate<T>(DataTable table)
        {
            DateTime defaultDate = new DateTime(1976, 1, 1);
            List<T> items = new List<T>();

            try
            {
                List<PropertyInfo> props = typeof(T).GetProperties().ToList();
                List<string> fieldNames = ValidateHeaders(table, props); // Ensure headers are validated.

                foreach (DataRow dr in table.Rows)
                {
                    T t = (T)Activator.CreateInstance(typeof(T)); ;

                    foreach (var prop in props)
                    {
                        if (table.Columns.Contains(prop.Name))
                        {
                            object value = dr[prop.Name];

                            if (value == DBNull.Value)
                            {
                                value = prop.PropertyType.IsValueType ? Activator.CreateInstance(prop.PropertyType) : null;
                            }

                            try
                            {
                                string typeName = prop.PropertyType.Name;
                                switch (typeName)
                                {
                                    case nameof(System.Boolean):
                                        if (value == null || value.ToString() == "")
                                            prop.SetValue(t, default(bool));
                                        else if (value.ToString().Equals("True", StringComparison.OrdinalIgnoreCase) || value.ToString().Equals("yes", StringComparison.OrdinalIgnoreCase))
                                            prop.SetValue(t, true);
                                        else
                                            prop.SetValue(t, false);
                                        break;
                                    case nameof(Int32):
                                        if (value == null || value.ToString() == "")
                                            prop.SetValue(t, 0);
                                        else if (value.ToString().Equals(nameof(ItemStatus.Submitted), StringComparison.OrdinalIgnoreCase) || value.ToString().Equals(nameof(ItemStatus.Pending), StringComparison.OrdinalIgnoreCase))
                                            prop.SetValue(t, (int)ItemStatus.Submitted);
                                        else if (value.ToString().Equals(nameof(ItemStatus.Approved), StringComparison.OrdinalIgnoreCase))
                                            prop.SetValue(t, (int)ItemStatus.Approved);
                                        else if (value.ToString().Equals(nameof(ItemStatus.Rejected), StringComparison.OrdinalIgnoreCase))
                                            prop.SetValue(t, (int)ItemStatus.Rejected);
                                        else if (value.ToString().Equals(nameof(AttendanceEnum.WeekOff), StringComparison.OrdinalIgnoreCase))
                                            prop.SetValue(t, (int)AttendanceEnum.WeekOff);
                                        else if (value.ToString().Equals(nameof(AttendanceEnum.Holiday), StringComparison.OrdinalIgnoreCase))
                                            prop.SetValue(t, (int)AttendanceEnum.Holiday);
                                        else if (value.ToString().Equals(nameof(AttendanceEnum.NotSubmitted), StringComparison.OrdinalIgnoreCase))
                                            prop.SetValue(t, (int)AttendanceEnum.NotSubmitted);
                                        else
                                            prop.SetValue(t, Convert.ChangeType(value, prop.PropertyType));
                                        break;
                                    case nameof(Int64):
                                        if (value == null || value.ToString() == "")
                                            prop.SetValue(t, 0);
                                        else
                                            prop.SetValue(t, Convert.ChangeType(value, prop.PropertyType));
                                        break;
                                    case nameof(Decimal):
                                        if (value == null || value.ToString() == "")
                                            prop.SetValue(t, decimal.Zero);
                                        else
                                            prop.SetValue(t, Convert.ChangeType(value, prop.PropertyType));
                                        break;
                                    case nameof(System.String):
                                        if (value == null || value.ToString() == "")
                                            prop.SetValue(t, string.Empty);
                                        else
                                            prop.SetValue(t, Convert.ChangeType(value, prop.PropertyType));
                                        break;
                                    case nameof(DateTime):
                                        if (value == null || value.ToString() == "")
                                            prop.SetValue(t, defaultDate);
                                        else
                                        {
                                            DateTime date = Convert.ToDateTime(value);
                                            prop.SetValue(t, DateTime.SpecifyKind(date, DateTimeKind.Unspecified));
                                        }
                                        break;
                                    default:
                                        prop.SetValue(t, Convert.ChangeType(value, prop.PropertyType));
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidOperationException($"Error setting property {prop.Name} with value {value}.", ex);
                            }
                        }
                    }

                    items.Add(t);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error mapping data.", ex);
            }

            return items;
        }


        private List<string> ValidateHeaders(DataTable table, List<PropertyInfo> fileds)
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

        public async Task<string> ReGenerateJWTTokenService()
        {
            var url = _microserviceRegistry.ReGenearateToken;

            var microserviceRequest = MicroserviceRequest.Builder(url);
            microserviceRequest
            .SetDbConfig(_requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString))
            .SetCompanyCode(_currentSession.CompanyCode)
            .SetToken(_currentSession.Authorization);

            return await _requestMicroservice.GetRequest<string>(microserviceRequest);
        }
    }
}
