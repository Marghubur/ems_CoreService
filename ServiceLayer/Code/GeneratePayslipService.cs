using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using Bot.CoreBottomHalf.CommonModal.Leave;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using Bt.Lib.PipelineConfig.MicroserviceHttpRequest;
using Bt.Lib.PipelineConfig.Model;
using EMailService.Modal;
using EMailService.Modal.Payroll;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
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

namespace ServiceLayer.Code
{
    public class GeneratePayslipService(MicroserviceRegistry _microserviceUrlLogs,
                                        RequestMicroservice _requestMicroservice,
                                        FileLocationDetail _fileLocationDetail,
                                        CurrentSession _currentSession,
                                        IWebHostEnvironment _env,
                                        ICommonService _commonService,
                                        ITimezoneConverter _timezoneConverter,
                                        IDb _db) : IGeneratePayslipService
    {
        public async Task<FileDetail> GeneratePayslip(PayslipGenerationModal payslipGenerationModal)
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
                await ReadPayslipTemplateHTML(payslipGenerationModal);
                this.CleanOldFiles(fileDetail);

                // generate pdf files
                await GeneratePayslipPdfFile(payslipGenerationModal);

                // return result data
                return fileDetail;
            }
            catch (HiringBellException e)
            {
                throw e.BuildBadRequest(e.UserMessage, e.FieldName, e.FieldValue);
            }
            catch (Exception ex)
            {
                throw new HiringBellException(ex.Message, ex);
            }
        }

        private async Task ReadPayslipTemplateHTML(PayslipGenerationModal payslipModal)
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

            if (!payslipModal.HeaderLogoPath.Contains("https://") && !File.Exists(payslipModal.HeaderLogoPath))
                payslipModal.HeaderLogoPath = "https://www.emstum.com/assets/images/logo.png";

            await Task.CompletedTask;
        }

        private async Task GeneratePayslipPdfFile(PayslipGenerationModal payslipModal)
        {
            GetPayslipFileDetail(payslipModal, payslipModal.FileDetail, ApplicationConstants.Pdf);

            // Converting html context for pdf conversion.
            var html = await GetPayslipHtmlString(payslipModal.PdfTemplatePath, payslipModal, true);
            var Email = payslipModal.Employee.Email.Replace("@", "_").Replace(".", "_");

            HtmlToPdfConvertorModal htmlToPdfConvertorModal = new HtmlToPdfConvertorModal
            {
                HTML = html,
                ServiceName = LocalConstants.EmstumFileService,
                FileName = payslipModal.FileDetail.FileName + $".{ApplicationConstants.Pdf}",
                FolderPath = Path.Combine(_currentSession.CompanyCode, _fileLocationDetail.User, Email)
            };

            string url = $"{_microserviceUrlLogs.ConvertHtmlToPdf}";

            MicroserviceRequest microserviceRequest = new MicroserviceRequest
            {
                Url = url,
                CompanyCode = _currentSession.CompanyCode,
                Token = _currentSession.Authorization,
                Database = _requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString),
                Payload = JsonConvert.SerializeObject(htmlToPdfConvertorModal)
            };

            payslipModal.FileDetail.FilePath = await _requestMicroservice.PostRequest<string>(microserviceRequest);

            await Task.CompletedTask;
        }

        private async Task<string> GetPayslipHtmlString(string templatePath, PayslipGenerationModal payslipModal, bool isHeaderLogoRequired = false)
        {
            string html = string.Empty;
            bool isYTDRequired = false;
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

            // here add condition that it detail will shown or not
            string declarationHTML = String.Empty;
            //declarationHTML = GetDeclarationDetailHTML(employeeDeclaration);

            var grosComponent = payslipModal.SalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId == ComponentNames.GrossId);
            var grossIncome = Math.Round(grosComponent.FinalAmount);
            decimal totalYTDAmount = 0;
            string employeeContribution = string.Empty;
            decimal totalContribution = 0;
            string htmlFilePath = "";
            int templateId = 1;
            switch (templateId)
            {
                case 2:
                    if (_env.IsDevelopment())
                    {
                        htmlFilePath = Path.Combine(_env.ContentRootPath, "ApplicationFiles", "htmltemplates", "billing", "payslipTemplate1.html");
                        payslipModal.PdfTemplateHTML = await File.ReadAllTextAsync(htmlFilePath);
                    }

                    salaryDetailsHTML = BuildSlaryStructureForFirstTemplate(payslipModal, salaryDetail, ref totalYTDAmount, ref totalContribution);
                    break;
                case 3:
                    if (_env.IsDevelopment())
                    {
                        htmlFilePath = Path.Combine(_env.ContentRootPath, "ApplicationFiles", "htmltemplates", "billing", "payslipTemplate2.html");
                        payslipModal.PdfTemplateHTML = await File.ReadAllTextAsync(htmlFilePath);
                    }

                    salaryDetailsHTML = BuildEmployeeEarningForSecondTemplate(payslipModal, salaryDetail);
                    employeeContribution = BuildEmployeeDeductionForSecondTemplate(payslipModal, ref totalContribution);
                    break;
                case 4:
                    if (_env.IsDevelopment())
                    {
                        htmlFilePath = Path.Combine(_env.ContentRootPath, "ApplicationFiles", "htmltemplates", "billing", "payslipTemplate3.html");
                        payslipModal.PdfTemplateHTML = await File.ReadAllTextAsync(htmlFilePath);
                    }

                    salaryDetailsHTML = BuildSlaryStructureForThirdTemplate(payslipModal, salaryDetail, ref totalYTDAmount, ref totalContribution);
                    break;
                case 5:
                    if (_env.IsDevelopment())
                    {
                        htmlFilePath = Path.Combine(_env.ContentRootPath, "ApplicationFiles", "htmltemplates", "billing", "payslipTemplate4.html");
                        payslipModal.PdfTemplateHTML = await File.ReadAllTextAsync(htmlFilePath);
                    }

                    if (isYTDRequired)
                        salaryDetailsHTML = AddEarningComponentsWithYTD(payslipModal, salaryDetail, ref totalYTDAmount);
                    else
                        salaryDetailsHTML = AddEarningComponentsWithoutYTD(payslipModal, salaryDetail);

                    salaryDetailsHTML = AddArrearComponent(payslipModal, salaryDetailsHTML);
                    salaryDetailsHTML = AddBonusComponent(payslipModal, salaryDetailsHTML);
                    employeeContribution = AddEmployeePfComponent(payslipModal, employeeContribution, ref totalContribution);
                    employeeContribution = AddEmployeeESI(payslipModal, employeeContribution, ref totalContribution);
                    break;
                default:
                    if (_env.IsDevelopment())
                    {
                        htmlFilePath = Path.Combine(_env.ContentRootPath, "ApplicationFiles", "htmltemplates", "billing", "payslipTemplate4.html");
                        payslipModal.PdfTemplateHTML = await File.ReadAllTextAsync(htmlFilePath);
                    }

                    if (isYTDRequired)
                        salaryDetailsHTML = AddEarningComponentsWithYTD(payslipModal, salaryDetail, ref totalYTDAmount);
                    else
                        salaryDetailsHTML = AddEarningComponentsWithoutYTD(payslipModal, salaryDetail);

                    salaryDetailsHTML = AddArrearComponent(payslipModal, salaryDetailsHTML, isYTDRequired);
                    salaryDetailsHTML = AddBonusComponent(payslipModal, salaryDetailsHTML, isYTDRequired);
                    employeeContribution = AddEmployeePfComponent(payslipModal, employeeContribution, ref totalContribution);
                    employeeContribution = AddEmployeeESI(payslipModal, employeeContribution, ref totalContribution);
                    break;
            }

            // var pTaxAmount = PTaxCalculation(payslipModal.Gross, payslipModal.PTaxSlabs);
            var pTaxAmount = Math.Round(payslipModal.SalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId == ComponentNames.ProfessionalTax).FinalAmount);
            var totalEarning = Math.Round(salaryDetail.Sum(x => x.FinalAmount) + payslipModal.SalaryDetail.ArrearAmount + payslipModal.SalaryDetail.BonusAmount);
            var totalActualEarning = Math.Round(salaryDetail.Sum(x => x.ActualAmount));
            var totalIncomeTax = payslipModal.TaxDetail.TaxDeducted >= pTaxAmount ? Math.Round(payslipModal.TaxDetail.TaxDeducted) - Math.Round(pTaxAmount) : 0;
            Dictionary<string, decimal> dedcutionsComponent = new Dictionary<string, decimal>
            {
                {"Professional Tax", pTaxAmount },
                {"Total Income Tax",  totalIncomeTax}
            };

            var advaceSalaryDetailTable = "";
            if (payslipModal.SalaryAdanceRepayments?.Any() == true)
            {
                dedcutionsComponent.Add("Advance", Math.Round(payslipModal.SalaryAdanceRepayments.Sum(x => x.ActualAmount)));
                advaceSalaryDetailTable = GenerateAdvanceSalaryDetail(payslipModal.SalaryAdanceRepayments);
            }

            if (payslipModal.OtherDeductionAndReimbursementRepayments?.Any() == true)
            {
                dedcutionsComponent.Add("Other Deduction", Math.Round(payslipModal.OtherDeductionAndReimbursementRepayments.Sum(x => x.ActualDeductionAmount)));
            }

            var deductionComponent = GetTaxAndDeductions(dedcutionsComponent);
            var totalDeduction = Math.Round(dedcutionsComponent.Sum(x => x.Value));
            var netSalary = totalEarning > 0 ? Math.Round(totalEarning - (totalContribution + totalDeduction)) : 0;
            if (netSalary < 0)
                throw HiringBellException.ThrowBadRequest($"Your salary is in -ve i.e {netSalary}");

            var netSalaryInWord = NumberToWords(netSalary);
            var designation = payslipModal.EmployeeRoles.Find(x => x.RoleId == payslipModal.Employee.DesignationId).RoleName;

            var doj = _timezoneConverter.ToTimeZoneDateTime(payslipModal.Employee.CreatedOn, _currentSession.TimeZone);
            var ActualPayableDays = await GetActualPayableDay(doj, payslipModal.Month, payslipModal.Year);
            var TotalWorkingDays = ActualPayableDays - payslipModal.PayrollMonthlyDetail.LOP;

            var LossOfPayDays = payslipModal.PayrollMonthlyDetail.LOP;
            string employeeCode = _commonService.GetEmployeeCode(payslipModal.Employee.EmployeeUid, _currentSession.CurrentUserDetail.EmployeeCodePrefix, _currentSession.CurrentUserDetail.EmployeeCodeLength);

            string advanceSalary = "";
            string totalSalaryRow = "";
            if (payslipModal.SalaryAdvanceRequest != null && payslipModal.SalaryAdvanceRequest.ApprovedAmount > 0)
            {
                var totalSalary = Math.Round(netSalary + payslipModal.SalaryAdvanceRequest.ApprovedAmount);

                advanceSalary = AddAdvanceSalary(payslipModal.SalaryAdvanceRequest.ApprovedAmount);
                totalSalaryRow = GetTotalSalaryRow(totalSalary);
                netSalaryInWord = NumberToWords(totalSalary);
            }

            html = payslipModal.PdfTemplateHTML.Replace("[[CompanyFirstAddress]]", payslipModal.Company.FirstAddress).
                Replace("[[CompanySecondAddress]]", payslipModal.Company.SecondAddress).
                Replace("[[CompanyThirdAddress]]", payslipModal.Company.ThirdAddress).
                Replace("[[CompanyFourthAddress]]", payslipModal.Company.ForthAddress).
                Replace("[[CompanyName]]", payslipModal.Company.CompanyName).
                Replace("[[EmployeeName]]", payslipModal.Employee.FirstName + " " + payslipModal.Employee.LastName).
                Replace("[[EmployeeNo]]", employeeCode).
                Replace("[[AdanceSalary]]", advanceSalary).
                Replace("[[JoiningDate]]", doj.ToString("dd MMM, yyyy")).
                Replace("[[PayDate]]", payslipModal.PayrollMonthlyDetail.PaymentRunDate.ToString("dd MMM, yyyy")).
                Replace("[[Department]]", string.IsNullOrEmpty(payslipModal.Employee.Department) ? "--" : payslipModal.Employee.Department).
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
                Replace("[[TotalEarnings]]", totalEarning.ToString()).
                Replace("[[TotalIncomeTax]]", (payslipModal.TaxDetail.TaxDeducted >= pTaxAmount ? Math.Round(payslipModal.TaxDetail.TaxDeducted) - Math.Round(pTaxAmount) : 0).ToString()).
                //Replace("[[TotalDeduction]]", totalDeduction.ToString()). //=> This is general template
                Replace("[[TotalDeduction]]", (totalDeduction + totalContribution).ToString()). // => This is only for Template4
                Replace("[[TotalContribution]]", totalContribution.ToString()).
                Replace("[[NetSalaryInWords]]", netSalaryInWord).
                Replace("[[PTax]]", pTaxAmount.ToString()).
                Replace("[[NetSalaryPayable]]", netSalary.ToString()).
                Replace("[[GrossIncome]]", grossIncome.ToString()).
                Replace("[[TotalActualEarnings]]", totalActualEarning.ToString()).
                Replace("[[TotalYTD]]", totalYTDAmount.ToString()).
                Replace("[[EmployeeDeclaration]]", declarationHTML)
               .Replace("[[TaxAndDeduction]]", deductionComponent)
               .Replace("[[CompanyLegalName]]", payslipModal.Company.CompanyName)
               .Replace("[[TotalSalary]]", totalSalaryRow)
               .Replace("[[AdvanceSalaryDetail]]", advaceSalaryDetailTable)
               .Replace("[[ESINumber]]", payslipModal.Employee.ESISerialNumber)
               .Replace("[[Location]]", payslipModal.Company.City);

            if (!string.IsNullOrEmpty(payslipModal.HeaderLogoPath) && isHeaderLogoRequired)
                html = await AddCompanyLogo(payslipModal, html);

            return html;
        }

        private string BuildSlaryStructureForThirdTemplate(PayslipGenerationModal payslipModal, List<CalculatedSalaryBreakupDetail> salaryDetail, ref decimal totalYTDAmount, ref decimal totalContribution)
        {
            var textinfo = CultureInfo.CurrentCulture.TextInfo;
            string salaryDetailsHTML = "";

            var YTDSalaryBreakup = payslipModal.AnnualSalaryBreakup.FindAll(x => x.IsActive && !x.IsPreviouEmployer && x.IsPayrollExecutedForThisMonth
                                                                                && payslipModal.SalaryDetail.PresentMonthDate.Subtract(x.PresentMonthDate).Days >= 0);
            var deductions = new List<CalculatedSalaryBreakupDetail>();

            var employeeESI = payslipModal.SalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId == LocalConstants.ESI);
            if (employeeESI != null && employeeESI.IsIncludeInPayslip)
            {
                totalContribution += Math.Round(employeeESI.FinalAmount);
                deductions.Add(new CalculatedSalaryBreakupDetail
                {
                    ComponentName = "Employee ESI",
                    FinalAmount = Math.Round(employeeESI.FinalAmount)
                });
            }

            var employeePF = payslipModal.SalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId == LocalConstants.EPF);
            if (employeePF != null && employeePF.IsIncludeInPayslip)
            {
                totalContribution += Math.Round(employeePF.FinalAmount);
                deductions.Add(new CalculatedSalaryBreakupDetail
                {
                    ComponentName = "Employee PF",
                    ComponentId = LocalConstants.EPF,
                    FinalAmount = Math.Round(employeePF.FinalAmount)
                });
            }

            var ptaxAmount = payslipModal.SalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId == ComponentNames.ProfessionalTax).FinalAmount;
            deductions.Add(new CalculatedSalaryBreakupDetail
            {
                ComponentName = "Professional Tax",
                ComponentId = ComponentNames.ProfessionalTax,
                FinalAmount = Math.Round(ptaxAmount)
            });

            var tds = payslipModal.TaxDetail.TaxDeducted >= ptaxAmount ? Math.Round(payslipModal.TaxDetail.TaxDeducted) - Math.Round(ptaxAmount) : 0;
            deductions.Add(new CalculatedSalaryBreakupDetail
            {
                ComponentName = "Income Tax",
                FinalAmount = tds
            });

            if (payslipModal.SalaryDetail.ArrearAmount != decimal.Zero)
            {
                salaryDetail.Add(new CalculatedSalaryBreakupDetail
                {
                    ComponentName = "Arrear Amount",
                    FinalAmount = Math.Round(payslipModal.SalaryDetail.ArrearAmount)
                });
            }

            if (payslipModal.SalaryDetail.BonusAmount != decimal.Zero)
            {
                salaryDetail.Add(new CalculatedSalaryBreakupDetail
                {
                    ComponentName = "Bonus Amount",
                    FinalAmount = Math.Round(payslipModal.SalaryDetail.BonusAmount)
                });
            }

            for (int i = 0; i < GetComponentMaxLength(salaryDetail, deductions); i++)
            {
                decimal YTDAmount = 0;
                decimal deductionYTDAmount = 0;

                if (i < salaryDetail.Count)
                {
                    var ytdComponent = YTDSalaryBreakup.SelectMany(x => x.SalaryBreakupDetails).ToList().FindAll(x => x.ComponentId == salaryDetail[i].ComponentId);
                    if (ytdComponent != null)
                        YTDAmount = Math.Round(ytdComponent.Sum(x => x.FinalAmount));
                }

                if (i < deductions.Count)
                {
                    var YTDdeduction = YTDSalaryBreakup.SelectMany(x => x.SalaryBreakupDetails).ToList().FindAll(x => x.ComponentId == deductions[i].ComponentId);
                    if (YTDdeduction != null)
                        deductionYTDAmount = Math.Round(YTDdeduction.Sum(x => x.FinalAmount));

                    if (deductions[i].ComponentName == "Income Tax")
                        deductionYTDAmount = Math.Round(payslipModal.TaxDetails.Sum(x => x.TaxDeducted));
                }


                salaryDetailsHTML += "<tr>";
                salaryDetailsHTML += $"<td style=\"padding: 10px;\">{(i < salaryDetail.Count ? textinfo.ToTitleCase(salaryDetail[i].ComponentName.ToLower()) : "")}</td>";
                salaryDetailsHTML += $"<td style=\"padding: 10px; text - align: right;\">{(i < salaryDetail.Count ? "₹ " + Math.Round(salaryDetail[i].FinalAmount) : "")}</td>";
                salaryDetailsHTML += $"<td style=\"padding: 10px; text-align: right; border-right: 1px solid #ddd;\" >{(i < salaryDetail.Count ? "₹ " + YTDAmount : "")} </ td > ";
                salaryDetailsHTML += $"<td style=\"padding: 10px; border-left: 1px solid #ddd;\">{(i < deductions.Count ? textinfo.ToTitleCase(deductions[i].ComponentName.ToLower()) : "")}</td>";
                salaryDetailsHTML += $"<td style=\"padding: 10px; text - align: right;\">{(i < deductions.Count ? "₹ " + Math.Round(deductions[i].FinalAmount) : "")}</td>";
                salaryDetailsHTML += $"<td style=\"padding: 10px; text-align: right;\" >{(i < deductions.Count ? "₹ " + deductionYTDAmount : "")} </ td > ";
                salaryDetailsHTML += "</tr>";
                totalYTDAmount += YTDAmount;
            }

            decimal arrearAmount = Math.Round(YTDSalaryBreakup.Sum(x => x.ArrearAmount));
            totalYTDAmount += arrearAmount;

            return salaryDetailsHTML;
        }

        private string BuildEmployeeEarningForSecondTemplate(PayslipGenerationModal payslipModal, List<CalculatedSalaryBreakupDetail> salaryDetail)
        {
            var textinfo = CultureInfo.CurrentCulture.TextInfo;
            string salaryDetailsHTML = "";

            if (payslipModal.SalaryDetail.ArrearAmount != decimal.Zero)
            {
                salaryDetail.Add(new CalculatedSalaryBreakupDetail
                {
                    ComponentName = "Arrear Amount",
                    FinalAmount = Math.Round(payslipModal.SalaryDetail.ArrearAmount)
                });
            }

            if (payslipModal.SalaryDetail.BonusAmount != decimal.Zero)
            {
                salaryDetail.Add(new CalculatedSalaryBreakupDetail
                {
                    ComponentName = "Bonus Amount",
                    FinalAmount = Math.Round(payslipModal.SalaryDetail.BonusAmount)
                });
            }

            for (int i = 0; i < salaryDetail.Count; i++)
            {
                salaryDetailsHTML += "<tr>";
                salaryDetailsHTML += $"<td style=\"padding: 8px;\">{textinfo.ToTitleCase(salaryDetail[i].ComponentName.ToLower())}</td>";
                salaryDetailsHTML += $"<td style=\"padding: 8px;\">₹{Math.Round(salaryDetail[i].FinalAmount)}</td>";
                salaryDetailsHTML += "</tr>";
            }

            return salaryDetailsHTML;
        }

        private string BuildEmployeeDeductionForSecondTemplate(PayslipGenerationModal payslipModal, ref decimal totalContribution)
        {
            var textinfo = CultureInfo.CurrentCulture.TextInfo;
            string deductionDetailHtml = "";
            var deductions = new List<CalculatedSalaryBreakupDetail>();

            var employeeESI = payslipModal.SalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId == LocalConstants.ESI);
            if (employeeESI != null && employeeESI.IsIncludeInPayslip)
            {
                totalContribution += Math.Round(employeeESI.FinalAmount);
                deductions.Add(new CalculatedSalaryBreakupDetail
                {
                    ComponentName = "Employee ESI",
                    FinalAmount = Math.Round(employeeESI.FinalAmount)
                });
            }

            var employeePF = payslipModal.SalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId == LocalConstants.EPF);
            if (employeePF != null && employeePF.IsIncludeInPayslip)
            {
                totalContribution += Math.Round(employeePF.FinalAmount);
                deductions.Add(new CalculatedSalaryBreakupDetail
                {
                    ComponentName = "Employee PF",
                    FinalAmount = Math.Round(employeePF.FinalAmount)
                });
            }

            var ptaxAmount = payslipModal.SalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId == ComponentNames.ProfessionalTax).FinalAmount;
            deductions.Add(new CalculatedSalaryBreakupDetail
            {
                ComponentName = "Professional Tax",
                FinalAmount = Math.Round(ptaxAmount)
            });

            var tds = payslipModal.TaxDetail.TaxDeducted >= ptaxAmount ? Math.Round(payslipModal.TaxDetail.TaxDeducted) - Math.Round(ptaxAmount) : 0;
            deductions.Add(new CalculatedSalaryBreakupDetail
            {
                ComponentName = "Income Tax",
                FinalAmount = tds
            });

            for (int i = 0; i < deductions.Count; i++)
            {
                deductionDetailHtml += "<tr>";
                deductionDetailHtml += $"<td style=\"padding: 8px;\">{textinfo.ToTitleCase(deductions[i].ComponentName.ToLower())}</td>";
                deductionDetailHtml += $"<td style=\"padding: 8px;\">₹{Math.Round(deductions[i].FinalAmount)}</td>";

                deductionDetailHtml += "</tr>";
            }

            return deductionDetailHtml;
        }

        private string BuildSlaryStructureForFirstTemplate(PayslipGenerationModal payslipModal, List<CalculatedSalaryBreakupDetail> salaryDetail, ref decimal totalYTDAmount, ref decimal totalContribution)
        {
            var YTDSalaryBreakup = payslipModal.AnnualSalaryBreakup.FindAll(x => x.IsActive && !x.IsPreviouEmployer && x.IsPayrollExecutedForThisMonth
                                                                                && payslipModal.SalaryDetail.PresentMonthDate.Subtract(x.PresentMonthDate).Days >= 0);
            var deductions = new List<CalculatedSalaryBreakupDetail>();
            totalContribution = AddEmployeeContributionAndDeduction(payslipModal, totalContribution, deductions);

            if (payslipModal.SalaryDetail.ArrearAmount != decimal.Zero)
            {
                salaryDetail.Add(new CalculatedSalaryBreakupDetail
                {
                    ComponentName = "Arrear Amount",
                    FinalAmount = Math.Round(payslipModal.SalaryDetail.ArrearAmount)
                });
            }

            if (payslipModal.SalaryDetail.BonusAmount != decimal.Zero)
            {
                salaryDetail.Add(new CalculatedSalaryBreakupDetail
                {
                    ComponentName = "Bonus Amount",
                    FinalAmount = Math.Round(payslipModal.SalaryDetail.BonusAmount)
                });
            }

            string salaryDetailsHTML = BuildEarningAndDeduction(payslipModal, salaryDetail, ref totalYTDAmount, YTDSalaryBreakup, deductions);

            decimal arrearAmount = Math.Round(YTDSalaryBreakup.Sum(x => x.ArrearAmount));
            totalYTDAmount += arrearAmount;

            return salaryDetailsHTML;
        }

        private string BuildEarningAndDeduction(PayslipGenerationModal payslipModal, List<CalculatedSalaryBreakupDetail> salaryDetail, ref decimal totalYTDAmount, List<AnnualSalaryBreakup> YTDSalaryBreakup, List<CalculatedSalaryBreakupDetail> deductions)
        {
            string salaryDetailsHTML = "";
            var textinfo = CultureInfo.CurrentCulture.TextInfo;

            for (int i = 0; i < GetComponentMaxLength(salaryDetail, deductions); i++)
            {
                decimal YTDAmount = 0;
                decimal deductionYTDAmount = 0;

                if (i < salaryDetail.Count)
                {
                    var ytdComponent = YTDSalaryBreakup.SelectMany(x => x.SalaryBreakupDetails).ToList().FindAll(x => x.ComponentId == salaryDetail[i].ComponentId);
                    if (ytdComponent != null)
                        YTDAmount = Math.Round(ytdComponent.Sum(x => x.FinalAmount));
                }

                if (i < deductions.Count)
                {
                    var YTDdeduction = YTDSalaryBreakup.SelectMany(x => x.SalaryBreakupDetails).ToList().FindAll(x => x.ComponentId == deductions[i].ComponentId);
                    if (YTDdeduction != null)
                        deductionYTDAmount = Math.Round(YTDdeduction.Sum(x => x.FinalAmount));

                    if (deductions[i].ComponentName == "Income Tax")
                        deductionYTDAmount = Math.Round(payslipModal.TaxDetails.Sum(x => x.TaxDeducted));
                }

                salaryDetailsHTML += "<tr>";
                salaryDetailsHTML += $"<td style=\"padding: 10px; border: 1px solid #ddd;\">{(i < salaryDetail.Count ? textinfo.ToTitleCase(salaryDetail[i].ComponentName.ToLower()) : "")}</td>";
                salaryDetailsHTML += $"<td style=\"padding: 10px; text - align: right; border: 1px solid #ddd;\">{(i < salaryDetail.Count ? "₹ " + Math.Round(salaryDetail[i].FinalAmount) : "")}</td>";
                salaryDetailsHTML += $"<td style=\"padding: 10px; text-align: right; border: 1px solid #ddd;\" >{(i < salaryDetail.Count ? "₹ " + YTDAmount : "")} </ td > ";
                salaryDetailsHTML += $"<td style=\"padding: 10px; border: 1px solid #ddd;\">{(i < deductions.Count ? textinfo.ToTitleCase(deductions[i].ComponentName.ToLower()) : "")}</td>";
                salaryDetailsHTML += $"<td style=\"padding: 10px; text - align: right; border: 1px solid #ddd;\">{(i < deductions.Count ? "₹ " + Math.Round(deductions[i].FinalAmount) : "")}</td>";
                salaryDetailsHTML += $"<td style=\"padding: 10px; text-align: right; border: 1px solid #ddd;\" >{(i < deductions.Count ? "₹ " + deductionYTDAmount : "")} </ td > ";
                salaryDetailsHTML += "</tr>";
                totalYTDAmount += YTDAmount;
            }

            return salaryDetailsHTML;
        }

        private static decimal AddEmployeeContributionAndDeduction(PayslipGenerationModal payslipModal, decimal totalContribution, List<CalculatedSalaryBreakupDetail> deductions)
        {
            var employeeESI = payslipModal.SalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId == LocalConstants.ESI);
            if (employeeESI != null && employeeESI.IsIncludeInPayslip)
            {
                totalContribution += Math.Round(employeeESI.FinalAmount);
                deductions.Add(new CalculatedSalaryBreakupDetail
                {
                    ComponentName = "Employee ESI",
                    FinalAmount = Math.Round(employeeESI.FinalAmount)
                });
            }

            var employeePF = payslipModal.SalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId == LocalConstants.EPF);
            if (employeePF != null && employeePF.IsIncludeInPayslip)
            {
                totalContribution += Math.Round(employeePF.FinalAmount);
                deductions.Add(new CalculatedSalaryBreakupDetail
                {
                    ComponentName = "Employee PF",
                    ComponentId = LocalConstants.EPF,
                    FinalAmount = Math.Round(employeePF.FinalAmount)
                });
            }

            var ptaxAmount = payslipModal.SalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId == ComponentNames.ProfessionalTax).FinalAmount;
            deductions.Add(new CalculatedSalaryBreakupDetail
            {
                ComponentName = "Professional Tax",
                ComponentId = ComponentNames.ProfessionalTax,
                FinalAmount = Math.Round(ptaxAmount)
            });

            var tds = payslipModal.TaxDetail.TaxDeducted >= ptaxAmount ? Math.Round(payslipModal.TaxDetail.TaxDeducted) - Math.Round(ptaxAmount) : 0;
            deductions.Add(new CalculatedSalaryBreakupDetail
            {
                ComponentName = "Income Tax",
                FinalAmount = tds
            });
            return totalContribution;
        }

        private int GetComponentMaxLength(List<CalculatedSalaryBreakupDetail> earnings, List<CalculatedSalaryBreakupDetail> deduction)
        {
            return Math.Max(earnings.Count, deduction.Count);
        }

        private async Task<int> GetActualPayableDay(DateTime doj, int month, int year)
        {
            int actualDaysPayable = DateTime.DaysInMonth(year, month);
            if (doj.Month == month && doj.Year == year)
                actualDaysPayable = actualDaysPayable - doj.Day + 1;

            return await Task.FromResult(actualDaysPayable);
        }

        private string AddEarningComponentsWithYTD(PayslipGenerationModal payslipModal, List<CalculatedSalaryBreakupDetail> salaryDetail, ref decimal totalYTDAmount)
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
                    YTDAmount += Math.Round(x.SalaryBreakupDetails.Find(i => i.ComponentId == item.ComponentId).FinalAmount);
                });
                salaryDetailsHTML += "<tr>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px;\">" + textinfo.ToTitleCase(item.ComponentName.ToLower()) + "</td>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\">" + Math.Round(item.ActualAmount) + "</td>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\">" + Math.Round(item.FinalAmount) + "</td>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\">" + YTDAmount + "</td>";
                salaryDetailsHTML += "</tr>";
                totalYTDAmount += YTDAmount;
            }

            decimal arrearAmount = Math.Round(YTDSalaryBreakup.Sum(x => x.ArrearAmount));
            totalYTDAmount += arrearAmount;

            return salaryDetailsHTML;
        }

        private string AddEarningComponentsWithoutYTD(PayslipGenerationModal payslipModal, List<CalculatedSalaryBreakupDetail> salaryDetail)
        {
            var textinfo = CultureInfo.CurrentCulture.TextInfo;
            string salaryDetailsHTML = "";

            foreach (var item in salaryDetail)
            {
                salaryDetailsHTML += "<tr>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px;\" width=\"60%\">" + textinfo.ToTitleCase(item.ComponentName.ToLower()) + "</td>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\" width=\"19%\">" + Math.Round(item.ActualAmount) + "</td>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\" width=\"19%\">" + Math.Round(item.FinalAmount) + "</td>";
                salaryDetailsHTML += "</tr>";
            }

            return salaryDetailsHTML;
        }

        private string AddEmployeeESI(PayslipGenerationModal payslipModal, string employeeContribution, ref decimal totalContribution)
        {
            var employeeESI = payslipModal.SalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId == LocalConstants.ESI);
            if (employeeESI != null && employeeESI.IsIncludeInPayslip)
            {
                totalContribution += Math.Round(employeeESI.FinalAmount);
                employeeContribution += "<tr>";
                employeeContribution += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px;\">" + "Employee ESI" + "</td>";
                employeeContribution += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\">" + Math.Round(employeeESI.FinalAmount) + "</td>";
                employeeContribution += "</tr>";
            }

            return employeeContribution;
        }

        private string AddEmployeePfComponent(PayslipGenerationModal payslipModal, string employeeContribution, ref decimal totalContribution)
        {
            var employeePF = payslipModal.SalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId == LocalConstants.EPF);
            if (employeePF != null && employeePF.IsIncludeInPayslip)
            {
                totalContribution += Math.Round(employeePF.FinalAmount);
                employeeContribution += "<tr>";
                employeeContribution += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px;\">" + "Employee PF" + "</td>";
                employeeContribution += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\">" + Math.Round(employeePF.FinalAmount) + "</td>";
                employeeContribution += "</tr>";
            }

            return employeeContribution;
        }

        private string AddBonusComponent(PayslipGenerationModal payslipModal, string salaryDetailsHTML, bool isYTDRequired = true)
        {
            if (payslipModal.SalaryDetail.BonusAmount != decimal.Zero)
            {
                salaryDetailsHTML += "<tr>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px;\">" + "Bonus Amount" + "</td>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\">" + "--" + "</td>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\">" + Math.Round(payslipModal.SalaryDetail.BonusAmount) + "</td>";
                if (isYTDRequired)
                    salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\">" + "--" + "</td>";

                salaryDetailsHTML += "</tr>";
            }

            return salaryDetailsHTML;
        }

        private string AddArrearComponent(PayslipGenerationModal payslipModal, string salaryDetailsHTML, bool isYTDRequired = true)
        {
            if (payslipModal.SalaryDetail.ArrearAmount != decimal.Zero)
            {
                salaryDetailsHTML += "<tr>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px;\">" + "Arrear Amount" + "</td>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\">" + "--" + "</td>";
                salaryDetailsHTML += "<td class=\"box-cell\" style=\"border: 0; font-size: 12px; text-align: right;\">" + Math.Round(payslipModal.SalaryDetail.ArrearAmount) + "</td>";
                if (isYTDRequired)
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

        private async Task PrepareRequestForPayslipGeneration(PayslipGenerationModal payslipGenerationModal)
        {
            DataSet ds = _db.FetchDataSet(Procedures.Payslip_Detail, new
            {
                payslipGenerationModal.EmployeeId,
                payslipGenerationModal.Month,
                payslipGenerationModal.Year,
                FileRole = ApplicationConstants.CompanyPrimaryLogo
            });

            if (ds == null || ds.Tables.Count != 11)
                throw new HiringBellException("Fail to get payslip detail. Please contact to admin.");

            if (ds.Tables[0].Rows.Count != 1)
                throw new HiringBellException("Fail to get company detail. Please contact to admin.");

            payslipGenerationModal.Company = Converter.ToType<Organization>(ds.Tables[0]);
            if (ds.Tables[1].Rows.Count != 1)
                throw new HiringBellException("Fail to get employee detail. Please contact to admin.");

            payslipGenerationModal.Employee = Converter.ToType<Employee>(ds.Tables[1]);
            if (ds.Tables[2].Rows.Count != 1)
                throw new HiringBellException("Fail to get employee salary detail. Please contact to admin.");

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

            payslipGenerationModal.TaxDetails = JsonConvert.DeserializeObject<List<TaxDetails>>(SalaryDetail.TaxDetail);
            payslipGenerationModal.TaxDetail = payslipGenerationModal.TaxDetails.Find(x => x.Year == payslipGenerationModal.Year && x.Month == payslipGenerationModal.Month);
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
                payslipGenerationModal.HeaderLogoPath = Path.Combine(_fileLocationDetail.RootPath, file.FilePath, file.FileName);
            else
                payslipGenerationModal.HeaderLogoPath = "https://www.emstum.com/assets/images/logo.png";

            payslipGenerationModal.leaveRequestNotifications = Converter.ToList<LeaveRequestNotification>(ds.Tables[7]);
            payslipGenerationModal.SalaryAdanceRepayments = Converter.ToList<SalaryAdanceRepayment>(ds.Tables[8]);
            payslipGenerationModal.SalaryAdvanceRequest = Converter.ToType<SalaryAdvanceRequest>(ds.Tables[9]);
            payslipGenerationModal.OtherDeductionAndReimbursementRepayments = Converter.ToList<OtherDeductionAndReimbursementRepayment>(ds.Tables[10]);

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

        private string GetDeclarationDetailHTML(EmployeeDeclaration employeeDeclaration)
        {
            string declarationHTML = string.Empty;
            if (employeeDeclaration.EmployeeCurrentRegime == 1)
            {
                if (employeeDeclaration.TaxSavingAlloance.FindAll(x => x.DeclaredValue > 0).Count == 0)
                    employeeDeclaration.TaxSavingAlloance = new List<SalaryComponents>();

                decimal hraAmount = 0;
                var hraComponent = employeeDeclaration.SalaryComponentItems.Find(x => x.ComponentId == "HRA" && x.DeclaredValue > 0);
                if (hraComponent != null)
                {
                    employeeDeclaration.TaxSavingAlloance.Add(hraComponent);
                    hraAmount = employeeDeclaration.HRADeatils[0].HRAAmount;
                };
                var totalAllowTaxExemptAmount = ComponentTotalAmount(employeeDeclaration.TaxSavingAlloance) + hraAmount;
                if (totalAllowTaxExemptAmount > 0)
                {
                    declarationHTML += "<table style=\"margin-top: 20px;\" width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\">";
                    declarationHTML += "<thead>";
                    declarationHTML += "<tr>";
                    declarationHTML += "<th colspan = \"4\" style = \"padding-top:15px; padding-bottom: 10px; border-bottom: 1px solid #222; text-align: left;\">" + "Less: Allowance Tax Exemptions" + "</th>";
                    declarationHTML += "</tr>";
                    declarationHTML += "<tr>";
                    declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span style=\"font-size:12px;\">" + "SECTION" + "</span>";
                    declarationHTML += "</th>";
                    declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span style=\"font-size:12px;\">" + "ALLOWANCE" + " </span>";
                    declarationHTML += "</th>";
                    declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span style=\"font-size:12px;\">" + "GROSS AMOUNT" + " </span>";
                    declarationHTML += "</th>";
                    declarationHTML += "<th style=\"text-align: rigt; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span style=\"font-size:12px;\">" + "DEDUCTABLE AMOUNT" + " </span>";
                    declarationHTML += "</th>";
                    declarationHTML += "</tr>";
                    declarationHTML += "</thead>";
                    declarationHTML += "<tbody>";
                    employeeDeclaration.TaxSavingAlloance.ForEach(x =>
                    {
                        if (x.DeclaredValue > 0)
                        {
                            declarationHTML += "<tr>";
                            declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                            declarationHTML += "<span class=\"text-muted\">" + x.Section + "</span>";
                            declarationHTML += "</td>";
                            declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                            declarationHTML += "<span class=\"text-muted\">" + x.ComponentId + " (" + x.ComponentFullName + ")" + "</span>";
                            declarationHTML += "</td>";
                            declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                            declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", x.DeclaredValue) + "</span>";
                            declarationHTML += "</td>";
                            declarationHTML += "<td style=\"text-align: rigt; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                            declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", x.DeclaredValue) + "</span>";
                            declarationHTML += "</td>";
                            declarationHTML += "</tr>";
                        }
                    });
                    declarationHTML += "<tr>";
                    declarationHTML += "<th colspan = \"3\" style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span class=\"text-muted\">" + "Total" + "</span>";
                    declarationHTML += "</th>";
                    declarationHTML += "<th style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", totalAllowTaxExemptAmount) + "</span>";
                    declarationHTML += "</th>";
                    declarationHTML += "</tr>";
                    declarationHTML += "</tbody>";
                    declarationHTML += "</table>";
                }

                decimal sec16TaxExemptAmount = employeeDeclaration.Section16TaxExemption.Sum(x => x.DeclaredValue);
                if (sec16TaxExemptAmount > 0)
                {
                    declarationHTML += "<table style=\"margin-top: 20px;\" width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\">";
                    declarationHTML += "<thead>";
                    declarationHTML += "<tr>";
                    declarationHTML += "<th colspan = \"3\" style = \"padding-top:15px; padding-bottom: 10px; border-bottom: 1px solid #222; text-align: left;\">" + "Less: Section 16 Tax Exemptions" + "</th>";
                    declarationHTML += "</tr>";
                    declarationHTML += "</thead>";
                    declarationHTML += "<tbody>";
                    employeeDeclaration.Section16TaxExemption.ForEach(x =>
                    {
                        declarationHTML += "<tr>";
                        declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                        declarationHTML += "<span class=\"text-muted\">" + x.Section + "</span>";
                        declarationHTML += "</td>";
                        declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                        declarationHTML += "<span class=\"text-muted\">" + x.ComponentId + " (" + x.ComponentFullName + ")" + "</span>";
                        declarationHTML += "</td>";
                        declarationHTML += "<td style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                        declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", x.DeclaredValue) + "</span>";
                        declarationHTML += "</td>";
                        declarationHTML += "</tr>";
                    });
                    declarationHTML += "<tr>";
                    declarationHTML += "<th colspan = \"2\"  style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span class=\"text-muted\">" + "Total" + "</span>";
                    declarationHTML += "</th>";
                    declarationHTML += "<th style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", sec16TaxExemptAmount) + "</span>";
                    declarationHTML += "</th>";
                    declarationHTML += "</tr>";
                    declarationHTML += "</tbody>";
                    declarationHTML += "</table>";
                }

                if (employeeDeclaration.SalaryDetail.GrossIncome - totalAllowTaxExemptAmount > 0)
                {
                    declarationHTML += "<table width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\">";
                    declarationHTML += "<tbody>";
                    declarationHTML += "<tr>";
                    declarationHTML += "<th style=\"text-align: left; padding-top: 5px; padding-bottom: 5px;\">";
                    declarationHTML += "<span style=\"font-size: 12px;\">" + "Taxable Amount under Head Salaries" + " </span>";
                    declarationHTML += "</th>";
                    declarationHTML += "<th style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; \">";
                    declarationHTML += "<span style=\"font-size: 12px;\">" + String.Format("{0:0.00}", employeeDeclaration.SalaryDetail.GrossIncome - totalAllowTaxExemptAmount - sec16TaxExemptAmount) + "</span>";
                    declarationHTML += "</th>";
                    declarationHTML += "</tr>";
                    declarationHTML += "</tbody>";
                    declarationHTML += "</table>";

                    declarationHTML += "<table width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\">";
                    declarationHTML += "<tbody>";
                    declarationHTML += "<tr>";
                    declarationHTML += "<th style=\"text-align: left;  padding-top: 5px; padding-bottom: 5px;\">";
                    declarationHTML += "<span style=\"font-size: 12px;\">" + "Total Gross from all Heads" + " </span>";
                    declarationHTML += "</th>";
                    declarationHTML += "<th style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px;\">";
                    declarationHTML += "<span style=\"font-size: 12px;\">" + String.Format("{0:0.00}", employeeDeclaration.SalaryDetail.GrossIncome - totalAllowTaxExemptAmount - sec16TaxExemptAmount) + "</span>";
                    declarationHTML += "</th>";
                    declarationHTML += "</tr>";
                    declarationHTML += "</tbody>";
                    declarationHTML += "</table>";
                }

                decimal totalSection80CExempAmount = 0;
                decimal totalOtherExemptAmount = 0;
                employeeDeclaration.Declarations.ForEach(x =>
                {
                    if (x.DeclarationName == ApplicationConstants.OneAndHalfLakhsExemptions)
                        totalSection80CExempAmount = x.TotalAmountDeclared;
                    else if (x.DeclarationName == ApplicationConstants.OtherDeclarationName)
                        totalOtherExemptAmount = x.TotalAmountDeclared;
                });

                if (totalSection80CExempAmount > 0)
                {
                    declarationHTML += "<table style=\"margin-top: 20px;\" width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\">";
                    declarationHTML += "<thead>";
                    declarationHTML += "<tr>";
                    declarationHTML += "<th colspan = \"4\" style = \"padding-top:15px; padding-bottom: 10px; border-bottom: 1px solid #222; text-align: left;\">" + "Less: 1.5 Lac Tax Exemption (Section 80C + Others)" + "</th>";
                    declarationHTML += "</tr>";
                    declarationHTML += "<tr>";
                    declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span style=\"font-size:12px;\">" + "SECTION" + "</span>";
                    declarationHTML += "</th>";
                    declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span style=\"font-size:12px;\">" + "ALLOWANCE" + " </span>";
                    declarationHTML += "</th>";
                    declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span style=\"font-size:12px;\">" + "DECLARED AMOUNT" + " </span>";
                    declarationHTML += "</th>";
                    declarationHTML += "<th style=\"text-align: rigt; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span style=\"font-size:12px;\">" + "DEDUCTABLE AMOUNT" + " </span>";
                    declarationHTML += "</th>";
                    declarationHTML += "</tr>";
                    declarationHTML += "</thead>";
                    declarationHTML += "<tbody>";
                    employeeDeclaration.ExemptionDeclaration.ForEach(x =>
                    {
                        if (x.DeclaredValue > 0)
                        {
                            declarationHTML += "<tr>";
                            declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                            declarationHTML += "<span class=\"text-muted\">" + x.Section + "</span>";
                            declarationHTML += "</td>";
                            declarationHTML += "<td  style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                            declarationHTML += "<span class=\"text-muted\">" + x.ComponentId + " (" + x.ComponentFullName + ")" + "</span>";
                            declarationHTML += "</td>";
                            declarationHTML += "<td  style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                            declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", x.DeclaredValue) + "</span>";
                            declarationHTML += "</td>";
                            declarationHTML += "<td style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                            declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", x.DeclaredValue) + "</span>";
                            declarationHTML += "</td>";
                            declarationHTML += "</tr>";
                        }
                    });
                    declarationHTML += "<tr>";
                    declarationHTML += "<th colspan = \"3\" style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span class=\"text-muted\">" + "Total" + "</span>";
                    declarationHTML += "</th>";
                    declarationHTML += "<th style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", totalSection80CExempAmount) + "</span>";
                    declarationHTML += "</th>";
                    declarationHTML += "</tr>";
                    declarationHTML += "</tbody>";
                    declarationHTML += "</table>";
                }

                if (totalOtherExemptAmount > 0)
                {
                    declarationHTML += "<table style=\"margin-top: 20px;\" width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\">";
                    declarationHTML += "<thead>";
                    declarationHTML += "<tr>";
                    declarationHTML += "<th colspan = \"4\" style = \"padding-top:15px; padding-bottom: 10px; border-bottom: 1px solid #222; text-align: left;\">" + "Less: Other Tax Exemption" + "</th>";
                    declarationHTML += "</tr>";
                    declarationHTML += "<tr>";
                    declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span style=\"font-size:12px;\">" + "SECTION" + "</span>";
                    declarationHTML += "</th>";
                    declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span style=\"font-size:12px;\">" + "ALLOWANCE" + " </span>";
                    declarationHTML += "</th>";
                    declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span style=\"font-size:12px;\">" + "DECLARED AMOUNT" + " </span>";
                    declarationHTML += "</th>";
                    declarationHTML += "<th style=\"text-align: end; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span style=\"font-size:12px;\">" + "DEDUCTABLE AMOUNT" + " </span>";
                    declarationHTML += "</th>";
                    declarationHTML += "</tr>";
                    declarationHTML += "</thead>";
                    declarationHTML += "<tbody>";
                    employeeDeclaration.OtherDeclaration.ForEach(x =>
                    {
                        if (x.DeclaredValue > 0)
                        {
                            declarationHTML += "<tr>";
                            declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                            declarationHTML += "<span class=\"text-muted\">" + x.Section + "</span>";
                            declarationHTML += "</td>";
                            declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                            declarationHTML += "<span class=\"text-muted\">" + x.ComponentId + " (" + x.ComponentFullName + ")" + "</span>";
                            declarationHTML += "</td>";
                            declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                            declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", x.DeclaredValue) + "</span>";
                            declarationHTML += "</td>";
                            declarationHTML += "<td style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                            declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", x.DeclaredValue) + "</span>";
                            declarationHTML += "</td>";
                            declarationHTML += "</tr>";
                        }
                    });
                    declarationHTML += "<tr>";
                    declarationHTML += "<th colspan = \"3\" style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span class=\"text-muted\">" + "Total" + "</span>";
                    declarationHTML += "</th>";
                    declarationHTML += "<th style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", totalOtherExemptAmount) + "</span>";
                    declarationHTML += "</th>";
                    declarationHTML += "</tr>";
                    declarationHTML += "</tbody>";
                    declarationHTML += "</table>";
                }

                declarationHTML += "<table style=\"margin-top: 20px;\" width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\">";
                declarationHTML += "<tbody>";
                declarationHTML += "<tr>";
                declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                declarationHTML += "<span style=\"font-size:12px;\">" + "HRA Applied" + " </span>";
                declarationHTML += "</th>";
                declarationHTML += "<th style=\"text-align: right; border-bottom: 1px solid #d9d9d9;\">";
                declarationHTML += "<span style=\"font-size:12px;\">" + "AMOUNT DECLARED" + " </span>";
                declarationHTML += "</th>";
                declarationHTML += "</tr>";
                declarationHTML += "<tr>";
                declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                declarationHTML += "<span style=\"font-size:12px;\">" + "Actual HRA [Per Month]" + " </span>";
                declarationHTML += "</td>";
                declarationHTML += "<td style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                declarationHTML += "<span style=\"font-size:12px;\">" + String.Format("{0:0.00}", employeeDeclaration.HRADeatils[0].HRAAmount) + "</span>";
                declarationHTML += "</td>";
                declarationHTML += "</tr>";
                declarationHTML += "<tr>";
                declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                declarationHTML += "<span class=\"text-muted\">" + "Total" + "</span>";
                declarationHTML += "</th>";
                declarationHTML += "<th style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", (employeeDeclaration.HRADeatils[0].HRAAmount * 12)) + "</span>";
                declarationHTML += "</th>";
                declarationHTML += "</tr>";
                declarationHTML += "</tbody>";
                declarationHTML += "</table>";

                decimal totalTaxableAmount = employeeDeclaration.SalaryDetail.GrossIncome - totalAllowTaxExemptAmount - sec16TaxExemptAmount - totalOtherExemptAmount - totalSection80CExempAmount - (employeeDeclaration.HRADeatils[0].HRAAmount * 12);
                declarationHTML += "<table style=\"margin-top: 20px;\" width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\">";
                declarationHTML += "<tbody>";
                declarationHTML += "<tr>";
                declarationHTML += "<th style=\"text-align: left; padding-top: 5px; padding-bottom: 5px; \">";
                declarationHTML += "<span style=\"font-size: 12px;\">" + "Total Taxable Amount" + " </span>";
                declarationHTML += "</th>";
                declarationHTML += "<th style=\"text-align: right; padding-top: 5px; padding-bottom: 5px; \">";
                declarationHTML += "<span style=\"font-size: 12px;\">" + String.Format("{0:0.00}", totalTaxableAmount) + " </span>";
                declarationHTML += "</th>";
                declarationHTML += "</tr>";
                declarationHTML += "</tbody>";
                declarationHTML += "</table>";

                declarationHTML += "<table width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\">";
                declarationHTML += "<tbody>";
                declarationHTML += "<tr>";
                declarationHTML += "<th style=\"text-align: left; padding-top: 5px; padding-bottom: 5px; \">";
                declarationHTML += "<span style=\"font-size: 12px;\">" + "Net taxable income is" + " </span>";
                declarationHTML += "</th>";
                declarationHTML += "<th style=\"text-align: right; padding-top: 5px; padding-bottom: 5px; \">";
                declarationHTML += "<span style=\"font-size: 12px;\">" + String.Format("{0:0.00}", totalTaxableAmount) + " </span>";
                declarationHTML += "</th>";
                declarationHTML += "</tr>";
                declarationHTML += "</tbody>";
                declarationHTML += "</table>";

                if (employeeDeclaration.IncomeTaxSlab.Count > 0 || employeeDeclaration.NewRegimIncomeTaxSlab.Count > 0)
                {
                    declarationHTML += "<h5 style=\"font-weight: bold; color: #222; padding-bottom: 0; margin-bottom: 0;\">" + "Tax Calculation" + "</h5>";
                    declarationHTML += "<table width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\">";
                    declarationHTML += "<thead>";
                    declarationHTML += "<tr>";
                    declarationHTML += "<th style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span class=\"text-muted\">" + "TAXABLE INCOME SLAB" + "</span>";
                    declarationHTML += "</th>";
                    declarationHTML += "<th style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                    declarationHTML += "<span class=\"text-muted\">" + "TAX AMOUNT" + "</span>";
                    declarationHTML += "</th>";
                    declarationHTML += "</tr>";
                    declarationHTML += "</thead>";
                    declarationHTML += "<tbody>";
                    if (employeeDeclaration.EmployeeCurrentRegime == 1)
                    {
                        foreach (var item in employeeDeclaration.IncomeTaxSlab.OrderByDescending(x => x.Key))
                        {
                            declarationHTML += "<tr>";
                            declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                            declarationHTML += "<span class=\"text-muted\">" + item.Value.Description + "</span>";
                            declarationHTML += "</td>";
                            declarationHTML += "<td style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                            declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", item.Value.Value) + "</span>";
                            declarationHTML += "</td>";
                            declarationHTML += "</tr>";
                        }
                    }
                    else
                    {
                        foreach (var item in employeeDeclaration.NewRegimIncomeTaxSlab.OrderByDescending(x => x.Key))
                        {
                            declarationHTML += "<tr>";
                            declarationHTML += "<td style=\"text-align: left; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                            declarationHTML += "<span class=\"text-muted\">" + item.Value.Description + "</span>";
                            declarationHTML += "</td>";
                            declarationHTML += "<td style=\"text-align: right; padding-left: 15px; padding-top: 5px; padding-bottom: 5px; border-bottom: 1px solid #d9d9d9;\">";
                            declarationHTML += "<span class=\"text-muted\">" + String.Format("{0:0.00}", item.Value.Value) + "</span>";
                            declarationHTML += "</td>";
                            declarationHTML += "</tr>";
                        }
                    };
                    declarationHTML += "</tbody>";
                    declarationHTML += "</table>";
                }
            }
            return declarationHTML;
        }

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

        public async Task<byte[]> GenerateBulkPayslipService(PayslipGenerationModal payslipGenerationModal)
        {
            var pdfPaths = new List<string>();

            foreach (var employeeId in payslipGenerationModal.EmployeeIds)
            {
                try
                {
                    var fileDetail = await GeneratePayslip(new PayslipGenerationModal
                    {
                        EmployeeId = employeeId,
                        Year = payslipGenerationModal.Year,
                        Month = payslipGenerationModal.Month
                    });

                    var pdfPath = $"{_microserviceUrlLogs.ResourceBaseUrl}{fileDetail.FilePath}/{fileDetail.FileName}.{ApplicationConstants.Pdf}";

                    if (!string.IsNullOrEmpty(pdfPath))
                        pdfPaths.Add(pdfPath);
                }
                catch (Exception ex)
                {
                    throw;
                }
            }

            if (!pdfPaths.Any())
                return null;

            return await GetZipFile(pdfPaths);
        }

        private async Task<byte[]> GetZipFile(List<string> pdfPaths)
        {
            var url = $"{_microserviceUrlLogs.ConvertZipFile}";

            var microserviceRequest = MicroserviceRequest.Builder(url);
            microserviceRequest
            .SetPayload(pdfPaths)
            .SetDbConfig(_requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString))
            .SetConnectionString(_currentSession.LocalConnectionString)
            .SetCompanyCode(_currentSession.CompanyCode)
            .SetToken(_currentSession.Authorization);

            return await _requestMicroservice.PostRequest<byte[]>(microserviceRequest);
        }

        private string AddAdvanceSalary(decimal advanceAmount)
        {
            var result = "<div class=\"advance-disbursed-section\">";
            result += "<div style=\"display: flex; justify-content: space-between; align-items: center;\" >";
            result += "<div class=\"dt\">Salary Advance Disbursed(This Period) :</div>";
            result += $"<div class=\"dt\">{advanceAmount.ToString("0.00")}</div>";
            result += "</div>";
            result += "<p style=\"font-size: 10px; margin: 5px 0 0 0; color: #555;\" > Note: This amount is paid out in addition to Net Pay or included in the total bank transfer.</p>";
            result += "</div>";

            return result;
        }

        private string GetTaxAndDeductions(Dictionary<string, decimal> components)
        {
            var tableRows = "";
            foreach (var component in components)
            {
                tableRows += "<tr>";
                tableRows += $"<td class=\"box-cell\" style=\"border: 0; font-size: 12px;\">{component.Key}</td>";
                tableRows += $"<td class=\"box-cell\" style=\"border: 0; text-align: right; font-size: 12px;\">{component.Value}</td>";
                tableRows += "</tr>";
            }

            return tableRows;
        }

        private string GenerateAdvanceSalaryDetail(List<SalaryAdanceRepayment> salaryAdanceRepayments)
        {
            var table = "<div style = \"padding-top: 20px;\">";
            table = "<table style =\"width: 100%; margin-top: 20px; border-collapse: collapse;\" >";
            table += "<tr>";
            table += "<th style = \"padding: 8px; font-size: 12px; text-align: center; border: 1px solid #ddd;\"> AMOUNT </th>";
            table += "<th style = \"padding: 8px; font-size: 12px; text-align: center; border: 1px solid #ddd;\"> TAKEN ON </th>";
            table += "<th style = \"padding: 8px; font-size: 12px; text-align: center; border: 1px solid #ddd;\"> INSTALLMENT </th>";
            table += "<th style = \"padding: 8px; font-size: 12px; text-align: center; border: 1px solid #ddd;\"> Current INSTALLMENT </th>";
            table += "<th style = \"padding: 8px; font-size: 12px; text-align: center; border: 1px solid #ddd;\"> PRINCIPAL DEDUCTED </th>";
            table += "<th style = \"padding: 8px; font-size: 12px; text-align: center; border: 1px solid #ddd;\"> CLOSING BALANCE </th>";
            table += "</tr>";
            foreach (var item in salaryAdanceRepayments)
            {
                var deductedAmount = Math.Round(item.InstallmentNumber * item.ActualAmount);
                var closingAmount = Math.Round(item.ApprovedAmount - deductedAmount);
                table += "<tr>";
                table += $"<td style = \"padding: 5px; font-size: 12px; text-align: center; border: 1px solid #ddd;\"> {Math.Round(item.ApprovedAmount)} </td>";
                table += $"<td style = \"padding: 5px; font-size: 12px; text-align: center; border: 1px solid #ddd;\"> {item.ApprovedDate.ToString("dd MMM, yyyy")} </td>";
                table += $"<td style = \"padding: 5px; font-size: 12px; text-align: center; border: 1px solid #ddd;\"> {item.InstallmentCount} </td>";
                table += $"<td style = \"padding: 5px; font-size: 12px; text-align: center; border: 1px solid #ddd;\"> {item.InstallmentNumber} </td>";
                table += $"<td style = \"padding: 5px; font-size: 12px; text-align: center; border: 1px solid #ddd;\"> {deductedAmount} </td>";
                table += $"<td style = \"padding: 5px; font-size: 12px; text-align: center; border: 1px solid #ddd;\"> {closingAmount} </td>";
                table += "</tr>";
            }
            table += "</table>";
            table += "</div>";

            return table;
        }

        private string GetTotalSalaryRow(decimal totalSalary)
        {
            var totalSalaryRow = "";
            totalSalaryRow += "<tr>";
            totalSalaryRow += $"<td class=\"box-cell\" style=\"border: 0; font-size: 12px;\"> Total Salary (Net Salary + Salary Advance) </td>";
            totalSalaryRow += $"<td class=\"box-cell\" style=\"border: 0; font-size: 12px;\"> {totalSalary} </td>";
            totalSalaryRow += "</tr>";

            return totalSalaryRow;
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

        private async Task<EmployeeDeclaration> GetEmployeeDeclaration(long employeeId)
        {
            string url = $"{_microserviceUrlLogs.GetEmployeeDeclarationDetailById}/{employeeId}";
            MicroserviceRequest microserviceRequest = new MicroserviceRequest
            {
                Url = url,
                CompanyCode = _currentSession.CompanyCode,
                Token = _currentSession.Authorization,
                Database = _requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString)
            };

            return await _requestMicroservice.GetRequest<EmployeeDeclaration>(microserviceRequest);
        }
    }
}