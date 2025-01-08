using Bt.Lib.Common.Service.KafkaService.interfaces;
using Bt.Lib.Common.Service.Model;
using ExcelDataReader;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using MySql.Data.MySqlClient;
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
    public class UtilityService(IKafkaProducerService _kafkaProducerService,
                                IWebHostEnvironment  _env) : IUtilityService
    {
        public bool CheckIsJoinedInCurrentFinancialYear(DateTime doj, CompanySetting companySetting)
        {
            if (doj.Year == companySetting.FinancialYear)
                if (doj.Month >= companySetting.DeclarationStartMonth)
                    return true;
                else if (doj.Year == companySetting.FinancialYear + 1)
                    if (doj.Month <= companySetting.DeclarationEndMonth) return true;

            return false;
        }

        public async Task SendNotification(dynamic requestBody, KafkaTopicNames kafkaTopicNames)
        {
            if (_env.IsProduction())
                await _kafkaProducerService.SendEmailNotification(requestBody, kafkaTopicNames);

            await Task.CompletedTask;
        }


        #region Read Excel file
        public async Task<List<T>> ReadExcelData<T>(IFormFileCollection files)
        {
            DataTable dataTable = null;
            List<T> components = new List<T>();

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

                                components = MappedData<T>(dataTable);
                            }
                        }
                        else
                        {
                            throw HiringBellException.ThrowBadRequest("Please select a valid excel file");
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return components;
        }

        public static List<T> MappedData<T>(DataTable table)
        {
            string TypeName = string.Empty;
            DateTime date = DateTime.Now;
            DateTime defaultDate = Convert.ToDateTime("1976-01-01");
            List<T> items = new List<T>();

            try
            {
                List<PropertyInfo> props = typeof(T).GetProperties().ToList();
                List<string> fieldNames = ValidateHeaders(table, props);

                if (table.Rows.Count > 0)
                {
                    int i = 0;
                    DataRow dr = null;
                    string[] formats = { "dd-MM-yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "yyyy/MM/dd", "dd.MM.yyyy", "yyyyMMdd" };
                    while (i < table.Rows.Count)
                    {
                        dr = table.Rows[i];

                        T t = (T)Activator.CreateInstance(typeof(T));
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
                                            else
                                                x.SetValue(t, Convert.ToBoolean(dr[x.Name]));
                                            break;
                                        case nameof(Int32):
                                            x.SetValue(t, Convert.ToInt32(dr[x.Name]));
                                            break;
                                        case nameof(Int64):
                                            x.SetValue(t, Convert.ToInt64(dr[x.Name]));
                                            break;
                                        case nameof(Decimal):
                                            x.SetValue(t, Convert.ToDecimal(dr[x.Name]));
                                            break;
                                        case nameof(String):
                                            x.SetValue(t, dr[x.Name].ToString());
                                            break;
                                        case nameof(DateTime):
                                            if (dr[x.Name].ToString() != null)
                                            {
                                                DateTime result;
                                                if (DateTime.TryParseExact(dr[x.Name].ToString(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                                                {
                                                    date = DateTime.SpecifyKind(result, DateTimeKind.Unspecified);
                                                    x.SetValue(t, date);
                                                }
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
                                catch (Exception ex)
                                {
                                    throw ex;
                                }
                            }
                        });

                        items.Add(t);
                        i++;
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
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