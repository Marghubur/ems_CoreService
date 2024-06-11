using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using Bot.CoreBottomHalf.CommonModal.Enums;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using EMailService.Modal;
using ems_CoreService.Model;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using Newtonsoft.Json;
using ServiceLayer.Code.HttpRequest;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class SettingService : ISettingService
    {
        private readonly IDb _db;
        private readonly CurrentSession _currentSession;
        private readonly ICommonService _commonService;
        //private readonly IDeclarationService _declarationService;
        public SettingService(IDb db,
            CurrentSession currentSession,
            ICommonService commonService
            //IDeclarationService declarationService
            )
        {
            _db = db;
            _currentSession = currentSession;
            _commonService = commonService;
            //_declarationService = declarationService;
        }

        public string AddUpdateComponentService(SalaryComponents salaryComponents)
        {
            salaryComponents = _db.Get<SalaryComponents>("", null);
            return null;
        }

        public PfEsiSetting GetSalaryComponentService(int CompanyId)
        {
            PfEsiSetting pfEsiSettings = new PfEsiSetting();
            var value = _db.Get<PfEsiSetting>(Procedures.Pf_Esi_Setting_Get, new { CompanyId });
            if (value != null)
                pfEsiSettings = value;

            return pfEsiSettings;
        }

        public async Task<PfEsiSetting> PfEsiSetting(int CompanyId, PfEsiSetting pfesiSetting)
        {
            string value = string.Empty;
            var existing = _db.Get<PfEsiSetting>(Procedures.Pf_Esi_Setting_Get, new { CompanyId });
            if (existing != null)
            {
                existing.PFEnable = pfesiSetting.PFEnable;
                existing.IsPfAmountLimitStatutory = pfesiSetting.IsPfAmountLimitStatutory;
                existing.IsPfCalculateInPercentage = pfesiSetting.IsPfCalculateInPercentage;
                existing.IsAllowOverridingPf = pfesiSetting.IsAllowOverridingPf;
                existing.IsPfEmployerContribution = pfesiSetting.IsPfEmployerContribution;
                existing.IsHidePfEmployer = pfesiSetting.IsHidePfEmployer;
                existing.IsPayOtherCharges = pfesiSetting.IsPayOtherCharges;
                existing.IsAllowVPF = pfesiSetting.IsAllowVPF;
                existing.EsiEnable = pfesiSetting.EsiEnable;
                existing.IsAllowOverridingEsi = pfesiSetting.IsAllowOverridingEsi;
                existing.IsHideEsiEmployer = pfesiSetting.IsHideEsiEmployer;
                existing.IsEsiExcludeEmployerShare = pfesiSetting.IsEsiExcludeEmployerShare;
                existing.IsEsiExcludeEmployeeGratuity = pfesiSetting.IsEsiExcludeEmployeeGratuity;
                existing.IsEsiEmployerContributionOutside = pfesiSetting.IsEsiEmployerContributionOutside;
                existing.IsRestrictEsi = pfesiSetting.IsRestrictEsi;
                existing.IsIncludeBonusEsiEligibility = pfesiSetting.IsIncludeBonusEsiEligibility;
                existing.IsIncludeBonusEsiContribution = pfesiSetting.IsIncludeBonusEsiContribution;
                existing.IsEmployerPFLimitContribution = pfesiSetting.IsEmployerPFLimitContribution;
                existing.EmployerPFLimit = pfesiSetting.EmployerPFLimit;
                existing.MaximumGrossForESI = pfesiSetting.MaximumGrossForESI;
                existing.EsiEmployeeContribution = pfesiSetting.EsiEmployeeContribution;
                existing.EsiEmployerContribution = pfesiSetting.EsiEmployerContribution;
            }
            else
                existing = pfesiSetting;

            pfesiSetting.Admin = _currentSession.CurrentUserDetail.UserId;
            value = _db.Execute<PfEsiSetting>(Procedures.Pf_Esi_Setting_Insupd, existing, true);
            if (string.IsNullOrEmpty(value))
                throw new HiringBellException("Unable to update PF Setting.");
            else
                await UpdateEmployeeSalaryDetails(existing);

            return existing;
        }

        private async Task UpdateEmployeeSalaryDetails(PfEsiSetting pfEsiSetting)
        {
            var employeeSalaryDetail = _db.GetList<EmployeeSalaryDetail>(Procedures.Employee_Salary_Detail_Get);
            if (employeeSalaryDetail != null && employeeSalaryDetail.Count > 0)
            {
                employeeSalaryDetail.ForEach(x =>
                {
                    List<AnnualSalaryBreakup> annualSalaryBreakup = JsonConvert.DeserializeObject<List<AnnualSalaryBreakup>>(x.CompleteSalaryDetail);
                    if (annualSalaryBreakup != null && annualSalaryBreakup.Count > 0)
                    {
                        var remainingMonthSalaryBreakup = annualSalaryBreakup.FindAll(x => !x.IsPayrollExecutedForThisMonth);
                        if (remainingMonthSalaryBreakup != null && remainingMonthSalaryBreakup.Count > 0)
                        {
                            remainingMonthSalaryBreakup.ForEach(i =>
                            {
                                var employeePFComponent = i.SalaryBreakupDetails.Find(y => y.ComponentId == "EPER-PF");
                                if (employeePFComponent != null)
                                {
                                    if (pfEsiSetting.PFEnable)
                                    {
                                        employeePFComponent.IsIncludeInPayslip = !pfEsiSetting.IsHidePfEmployer;
                                    }
                                    else
                                    {
                                        employeePFComponent.IsIncludeInPayslip = false;
                                    }
                                }
                                var employeeECIComponent = i.SalaryBreakupDetails.Find(y => y.ComponentId == "ECI");
                                if (employeeECIComponent != null)
                                {
                                    if (pfEsiSetting.EsiEnable)
                                    {
                                        var amount = pfEsiSetting.EsiEmployerContribution + pfEsiSetting.EsiEmployeeContribution;
                                        employeeECIComponent.IsIncludeInPayslip = pfEsiSetting.IsHideEsiEmployer;
                                    }
                                    else
                                    {
                                        employeeECIComponent.IsIncludeInPayslip = false;
                                    }
                                }
                            });

                            x.CompleteSalaryDetail = JsonConvert.SerializeObject(annualSalaryBreakup);
                        }

                    }
                });
                var data = (from n in employeeSalaryDetail
                            select new
                            {
                                _currentSession.FinancialStartYear,
                                n.EmployeeId,
                                n.CompleteSalaryDetail,
                                n.TaxDetail,
                                n.CTC
                            }).ToList();

                var result = await _db.BulkExecuteAsync(Procedures.Employee_Salary_Detail_Upd_Salarydetail, data, true);
                if (result != employeeSalaryDetail.Count)
                    throw HiringBellException.ThrowBadRequest("Fail to update salary breakup");

                employeeSalaryDetail.ForEach(async x =>
                {
                    DataSet resultSet = _db.FetchDataSet(Procedures.Employee_Declaration_Get_ByEmployeeId, new
                    {
                        EmployeeId = x.EmployeeId,
                        UserTypeId = (int)UserType.Compnay
                    });

                    if ((resultSet == null || resultSet.Tables.Count == 0) && resultSet.Tables.Count != 2)
                        throw HiringBellException.ThrowBadRequest("Unable to get the detail");

                    EmployeeDeclaration employeeDeclaration = Converter.ToType<EmployeeDeclaration>(resultSet.Tables[0]);
                    if (employeeDeclaration == null)
                        throw new HiringBellException("Employee declaration detail not defined. Please contact to admin.");

                    // await _declarationService.CalculateSalaryDetail(x.EmployeeId, employeeDeclaration, true, true);
                    await RequestMicroservice.PostRequest(MicroserviceRequest.Builder("", null));
                });

            };
        }

        //private async Task updateComponentByUpdatingPfEsiSetting(PfEsiSetting pfesiSetting)
        //{
        //    var ds = _db.GetDataSet("sp_salary_group_and_components_get", new { CompanyId = pfesiSetting.CompanyId });

        //    if (!ds.IsValidDataSet(ds))
        //        throw HiringBellException.ThrowBadRequest("Invalid result got from salary and group table.");

        //    // var salaryComponents = Converter.ToList<SalaryComponents>(ds.Tables[1]);
        //    var groups = Converter.ToList<SalaryGroup>(ds.Tables[0]);

        //    foreach (var gp in groups)
        //    {
        //        var salaryComponents = JsonConvert.DeserializeObject<List<SalaryComponents>>(gp.SalaryComponents);

        //        var component = salaryComponents.Find(x => x.ComponentId == "EPER-PF");
        //        if (component == null)
        //            throw HiringBellException.ThrowBadRequest("Employer contribution toward PF component not found. Please contact to admin");

        //        component.DeclaredValue = pfesiSetting.EmployerPFLimit;
        //        component.EmployerContribution = pfesiSetting.EmployerPFLimit;
        //        component.Formula = pfesiSetting.EmployerPFLimit.ToString();
        //        component.IncludeInPayslip = pfesiSetting.IsHidePfEmployer;

        //        component = salaryComponents.Find(x => x.ComponentId == "ECI");
        //        if (component == null)
        //            throw HiringBellException.ThrowBadRequest("Employer contribution toward insurance component not found. Please contact to admin");

        //        component.DeclaredValue = pfesiSetting.EsiEmployerContribution + pfesiSetting.EsiEmployeeContribution;
        //        component.Formula = (pfesiSetting.EsiEmployerContribution + pfesiSetting.EsiEmployeeContribution).ToString();
        //        component.IncludeInPayslip = pfesiSetting.IsHideEsiEmployer;
        //        component.EmployerContribution = pfesiSetting.EsiEmployerContribution;
        //        component.EmployeeContribution = pfesiSetting.EsiEmployeeContribution;

        //        gp.SalaryComponents = _commonService.GetStringifySalaryGroupData(salaryComponents);
        //    }

        //    await _db.BulkExecuteAsync("sp_salary_group_insupd", groups, true);
        //    await Task.CompletedTask;
        //}

        public List<OrganizationDetail> GetOrganizationInfo()
        {
            List<OrganizationDetail> organizations = _db.GetList<OrganizationDetail>(Procedures.Organization_Setting_Get, false);
            return organizations;
        }

        public BankDetail GetOrganizationBankDetailInfoService(int OrganizationId)
        {
            BankDetail result = _db.Get<BankDetail>(Procedures.Bank_Accounts_Get_By_OrgId, new { OrganizationId });
            return result;
        }

        public Payroll GetPayrollSetting(int CompanyId)
        {
            var result = _db.Get<Payroll>(Procedures.Payroll_Cycle_Setting_GetById, new { CompanyId });
            return result;
        }

        public string InsertUpdatePayrollSetting(Payroll payroll)
        {
            ValidatePayrollSetting(payroll);

            var status = _db.Execute<Payroll>(Procedures.Payroll_Cycle_Setting_Intupd,
                new
                {
                    payroll.PayrollCycleSettingId,
                    payroll.CompanyId,
                    payroll.OrganizationId,
                    payroll.PayFrequency,
                    payroll.PayCycleMonth,
                    payroll.PayCycleDayOfMonth,
                    payroll.PayCalculationId,
                    payroll.IsExcludeWeeklyOffs,
                    payroll.IsExcludeHolidays,
                    AdminId = _currentSession.CurrentUserDetail.UserId,
                    DeclarationEndMonth = payroll.PayCycleMonth == 1 ? 12 : payroll.PayCycleMonth - 1
                },
                true
            );

            if (string.IsNullOrEmpty(status))
            {
                throw new HiringBellException("Fail to insert or update.");
            }

            return status;
        }

        private void ValidatePayrollSetting(Payroll payroll)
        {
            if (payroll.CompanyId <= 0)
                throw new HiringBellException("Compnay is mandatory. Please selecte your company first.");

            if (payroll.PayCycleMonth < 0)
                throw HiringBellException.ThrowBadRequest("Please select payroll month first");

            if (string.IsNullOrEmpty(payroll.PayFrequency))
                throw HiringBellException.ThrowBadRequest("Please select pay frequency first");

            if (payroll.PayCycleDayOfMonth < 0)
                throw HiringBellException.ThrowBadRequest("Please select pay cycle day of month first");

            if (payroll.PayCalculationId < 0)
                throw HiringBellException.ThrowBadRequest("Please select payment type first");
        }

        public string InsertUpdateSalaryStructure(List<SalaryStructure> salaryStructure)
        {
            var status = string.Empty;

            return status;
        }

        public async Task<string> UpdateGroupSalaryComponentDetailService(string componentId, int groupId, SalaryComponents component)
        {
            if (groupId <= 0)
                throw new HiringBellException("Invalid groupId");

            if (string.IsNullOrEmpty(componentId))
                throw new HiringBellException("Invalid component passed.");

            decimal formulavalue = 0;
            if (component.Formula != ApplicationConstants.AutoCalculation)
                formulavalue = calculateExpressionUsingInfixDS(component.Formula, 0);

            SalaryGroup salaryGroup = _db.Get<SalaryGroup>(Procedures.Salary_Group_GetById, new { SalaryGroupId = groupId });
            if (salaryGroup == null)
                throw new HiringBellException("Unable to get salary group. Please contact admin");

            salaryGroup.GroupComponents = JsonConvert.DeserializeObject<List<SalaryComponents>>(salaryGroup.SalaryComponents);
            var existingComponent = salaryGroup.GroupComponents.Find(x => x.ComponentId == component.ComponentId);
            if (existingComponent == null)
            {
                salaryGroup.GroupComponents.Add(component);
            }
            else
            {
                if (string.IsNullOrEmpty(component.Formula))
                    throw new HiringBellException("Given formula is not correct or unable to submit. Please try again or contact to admin");

                existingComponent.Formula = component.Formula;
                existingComponent.IncludeInPayslip = component.IncludeInPayslip;
            }

            salaryGroup.SalaryComponents = _commonService.GetStringifySalaryGroupData(salaryGroup.GroupComponents);
            var status = await _db.ExecuteAsync(Procedures.Salary_Group_Insupd, new
            {
                salaryGroup.SalaryGroupId,
                salaryGroup.CompanyId,
                salaryGroup.SalaryComponents,
                salaryGroup.GroupName,
                salaryGroup.GroupDescription,
                salaryGroup.MinAmount,
                salaryGroup.MaxAmount,
                AdminId = _currentSession.CurrentUserDetail.UserId
            }, true);

            if (!ApplicationConstants.IsExecuted(status.statusMessage))
                throw new HiringBellException("Fail to update the record.");

            await UpdateSalaryDetails(salaryGroup.SalaryGroupId, component);
            return status.statusMessage;
        }

        private async Task UpdateSalaryDetails(int salaryGroupId, SalaryComponents component)
        {
            var employeeSalaryDetail = _db.GetList<EmployeeSalaryDetail>(Procedures.Employee_Salary_Detail_Get_By_Groupid, new { SalaryGroupId = salaryGroupId });
            if (employeeSalaryDetail != null && employeeSalaryDetail.Count > 0)
            {
                employeeSalaryDetail.ForEach(x =>
                {
                    List<AnnualSalaryBreakup> annualSalaryBreakup = JsonConvert.DeserializeObject<List<AnnualSalaryBreakup>>(x.CompleteSalaryDetail);
                    if (annualSalaryBreakup != null && annualSalaryBreakup.Count > 0)
                    {
                        var remainingMonthSalaryBreakup = annualSalaryBreakup.FindAll(x => !x.IsPayrollExecutedForThisMonth);
                        if (remainingMonthSalaryBreakup != null && remainingMonthSalaryBreakup.Count > 0)
                        {
                            remainingMonthSalaryBreakup.ForEach(i =>
                            {
                                var salaryComponent = i.SalaryBreakupDetails.Find(y => y.ComponentId == component.ComponentId);
                                if (salaryComponent != null)
                                {
                                    salaryComponent.Formula = component.Formula;
                                    salaryComponent.IsIncludeInPayslip = component.IncludeInPayslip;
                                }
                            });

                            x.CompleteSalaryDetail = JsonConvert.SerializeObject(annualSalaryBreakup);
                        }

                    }
                });
                var data = (from n in employeeSalaryDetail
                            select new
                            {
                                _currentSession.FinancialStartYear,
                                n.EmployeeId,
                                n.CompleteSalaryDetail,
                                n.TaxDetail,
                                n.CTC
                            }).ToList();

                var result = await _db.BulkExecuteAsync(Procedures.Employee_Salary_Detail_Upd_Salarydetail, data, true);
                if (result != employeeSalaryDetail.Count)
                    throw HiringBellException.ThrowBadRequest("Fail to update salary breakup");

                employeeSalaryDetail.ForEach(async x =>
                {
                    DataSet resultSet = _db.FetchDataSet(Procedures.Employee_Declaration_Get_ByEmployeeId, new
                    {
                        EmployeeId = x.EmployeeId,
                        UserTypeId = (int)UserType.Compnay
                    });

                    if ((resultSet == null || resultSet.Tables.Count == 0) && resultSet.Tables.Count != 2)
                        throw HiringBellException.ThrowBadRequest("Unable to get the detail");

                    EmployeeDeclaration employeeDeclaration = Converter.ToType<EmployeeDeclaration>(resultSet.Tables[0]);
                    if (employeeDeclaration == null)
                        throw new HiringBellException("Employee declaration detail not defined. Please contact to admin.");

                    // await _declarationService.CalculateSalaryDetail(x.EmployeeId, employeeDeclaration, true, true);
                    await RequestMicroservice.PostRequest(MicroserviceRequest.Builder("", null));
                });

            };
        }

        private decimal calculateExpressionUsingInfixDS(string expression, decimal declaredAmount)
        {
            if (string.IsNullOrEmpty(expression))
                return declaredAmount;

            if (!expression.Contains("()"))
                expression = string.Format("({0})", expression);

            List<string> operatorStact = new List<string>();
            var expressionStact = new List<object>();
            int index = 0;
            var lastOp = "";
            var ch = "";

            while (index < expression.Length)
            {
                ch = expression[index].ToString();
                if (ch.Trim() == "")
                {
                    index++;
                    continue;
                }
                int number;
                if (!int.TryParse(ch.ToString(), out number))
                {
                    switch (ch)
                    {
                        case "+":
                        case "-":
                        case "/":
                        case "%":
                        case "*":
                            if (operatorStact.Count > 0)
                            {
                                lastOp = operatorStact[operatorStact.Count - 1];
                                if (lastOp == "+" || lastOp == "-" || lastOp == "/" || lastOp == "*" || lastOp == "%")
                                {
                                    lastOp = operatorStact[operatorStact.Count - 1];
                                    operatorStact.RemoveAt(operatorStact.Count - 1);
                                    expressionStact.Add(lastOp);
                                }
                            }
                            operatorStact.Add(ch);
                            break;
                        case ")":
                            while (true)
                            {
                                lastOp = operatorStact[operatorStact.Count - 1];
                                operatorStact.RemoveAt(operatorStact.Count - 1);
                                if (lastOp == "(")
                                {
                                    break;
                                }
                                expressionStact.Add(lastOp);
                            }
                            break;
                        case "(":
                            operatorStact.Add(ch);
                            break;
                    }
                }
                else
                {
                    decimal value = 0;
                    decimal fraction = 0;
                    bool isFractionFound = false;
                    while (true)
                    {
                        ch = expression[index].ToString();
                        if (ch == ".")
                        {
                            index++;
                            isFractionFound = true;
                            break;
                        }

                        if (ch.Trim() == "")
                        {
                            expressionStact.Add($"{value}.{fraction}");
                            break;
                        }

                        if (int.TryParse(ch.ToString(), out number))
                        {
                            if (!isFractionFound)
                                value = Convert.ToDecimal(value + ch);
                            else
                                fraction = Convert.ToDecimal(fraction + ch);
                            index++;
                        }
                        else
                        {
                            index--;
                            expressionStact.Add($"{value}.{fraction}");
                            break;
                        }
                    }
                }

                index++;
            }

            var exp = expressionStact.Aggregate((x, y) => x.ToString() + " " + y.ToString()).ToString();
            // return _postfixToInfixConversion.evaluatePostfix(exp);
            return declaredAmount;
        }

        public async Task<List<SalaryComponents>> ActivateCurrentComponentService(List<SalaryComponents> components)
        {
            List<SalaryComponents> salaryComponents = new List<SalaryComponents>();
            var salaryComponent = _db.GetList<SalaryComponents>(Procedures.Salary_Components_Get);
            if (salaryComponent != null)
            {
                SalaryComponents componentItem = null;
                Parallel.ForEach<SalaryComponents>(salaryComponent, x =>
                {
                    componentItem = components.Find(i => i.ComponentId == x.ComponentId);
                    if (componentItem != null)
                    {
                        x.IsOpted = componentItem.IsOpted;
                        x.ComponentCatagoryId = componentItem.ComponentCatagoryId;
                    }
                });

                var updateComponents = (from n in salaryComponent
                                        select new
                                        {
                                            n.ComponentId,
                                            n.ComponentFullName,
                                            n.ComponentDescription,
                                            n.CalculateInPercentage,
                                            n.TaxExempt,
                                            n.ComponentTypeId,
                                            n.ComponentCatagoryId,
                                            n.PercentageValue,
                                            n.MaxLimit,
                                            n.DeclaredValue,
                                            n.RejectedAmount,
                                            n.AcceptedAmount,
                                            UploadedFileIds = string.IsNullOrEmpty(n.UploadedFileIds) ? "[]" : n.UploadedFileIds,
                                            n.Formula,
                                            n.EmployeeContribution,
                                            n.EmployerContribution,
                                            n.IncludeInPayslip,
                                            n.IsAdHoc,
                                            n.AdHocId,
                                            n.Section,
                                            n.SectionMaxLimit,
                                            n.IsAffectInGross,
                                            n.RequireDocs,
                                            n.IsOpted,
                                            n.IsActive,
                                            CreatedOn = DateTime.UtcNow,
                                            n.UpdatedOn,
                                            n.CreatedBy,
                                            UpdatedBy = _currentSession.CurrentUserDetail.UserId
                                        }).ToList<object>();

                var status = await _db.BatchInsetUpdate<SalaryComponents>(updateComponents);
                if (string.IsNullOrEmpty(status))
                    throw new HiringBellException("Unable to update detail");
            }
            else
                throw new HiringBellException("Invalid component passed.");

            return salaryComponent;
        }

        public List<SalaryComponents> FetchComponentDetailByIdService(int componentTypeId)
        {
            if (componentTypeId < 0)
                throw new HiringBellException("Invalid component type passed.");

            List<SalaryComponents> salaryComponent = _db.GetList<SalaryComponents>(Procedures.Salary_Components_Get_Type, new { ComponentTypeId = componentTypeId });
            if (salaryComponent == null)
                throw new HiringBellException("Fail to retrieve component detail.");

            return salaryComponent;
        }

        public async Task<UserLayoutConfigurationJSON> LayoutConfigurationSettingService(UserLayoutConfigurationJSON userLayoutConfiguration)
        {
            var existingLayoutConfig = _db.Get<UserLayoutConfiguration>("sp_user_layout_configuration_get_by_empid", new
            {
                EmployeeId = _currentSession.CurrentUserDetail.UserId
            });
            var userLayoutConfig = new UserLayoutConfigurationJSON();
            if (existingLayoutConfig != null)
                userLayoutConfig = JsonConvert.DeserializeObject<UserLayoutConfigurationJSON>(existingLayoutConfig.SettingsJson);

            if (!string.IsNullOrEmpty(userLayoutConfiguration.NavbarColor))
                userLayoutConfig.NavbarColor = userLayoutConfiguration.NavbarColor;
            else
                userLayoutConfig.NavbarColor = "#ffffff";

            userLayoutConfig.IsMenuExpanded = userLayoutConfiguration.IsMenuExpanded;

            await _db.ExecuteAsync(Procedures.User_Layout_Configuration_Ins_Upt, new
            {
                EmployeeId = _currentSession.CurrentUserDetail.UserId,
                UserLayoutConfiguration = JsonConvert.SerializeObject(userLayoutConfig)
            });

            return userLayoutConfig;
        }

        public List<SalaryComponents> FetchActiveComponentService()
        {
            List<SalaryComponents> salaryComponent = _db.GetList<SalaryComponents>(Procedures.Salary_Components_Get);
            if (salaryComponent == null)
                throw new HiringBellException("Fail to retrieve component detail.");

            return salaryComponent;
        }

        public List<SalaryComponents> UpdateSalaryComponentDetailService(string componentId, SalaryComponents component)
        {
            List<SalaryComponents> salaryComponents = null;

            if (string.IsNullOrEmpty(componentId))
                throw new HiringBellException("Invalid component passed.");

            salaryComponents = _db.GetList<SalaryComponents>(Procedures.Salary_Components_Get_Type, new { ComponentTypeId = 0 });
            if (salaryComponents == null)
                throw new HiringBellException("Fail to retrieve component detail.");

            var salaryComponent = salaryComponents.Find(x => x.ComponentId == componentId);
            if (salaryComponent != null)
            {
                salaryComponent.CalculateInPercentage = component.CalculateInPercentage;
                salaryComponent.TaxExempt = component.TaxExempt;
                salaryComponent.IsActive = component.IsActive;
                salaryComponent.TaxExempt = component.TaxExempt;
                salaryComponent.RequireDocs = component.RequireDocs;
                salaryComponent.IncludeInPayslip = component.IncludeInPayslip;
                salaryComponent.AdminId = _currentSession.CurrentUserDetail.UserId;

                var status = _db.Execute<SalaryComponents>(Procedures.Salary_Components_Insupd, new
                {
                    salaryComponent.ComponentId,
                    salaryComponent.ComponentFullName,
                    salaryComponent.ComponentDescription,
                    salaryComponent.ComponentCatagoryId,
                    salaryComponent.CalculateInPercentage,
                    salaryComponent.TaxExempt,
                    salaryComponent.ComponentTypeId,
                    salaryComponent.PercentageValue,
                    salaryComponent.MaxLimit,
                    salaryComponent.DeclaredValue,
                    salaryComponent.AcceptedAmount,
                    salaryComponent.RejectedAmount,
                    salaryComponent.UploadedFileIds,
                    salaryComponent.Formula,
                    salaryComponent.EmployeeContribution,
                    salaryComponent.EmployerContribution,
                    salaryComponent.IncludeInPayslip,
                    salaryComponent.IsAdHoc,
                    salaryComponent.AdHocId,
                    salaryComponent.Section,
                    salaryComponent.SectionMaxLimit,
                    salaryComponent.IsAffectInGross,
                    salaryComponent.RequireDocs,
                    salaryComponent.IsOpted,
                    salaryComponent.IsActive,
                    salaryComponent.AdminId
                }, true);

                if (!ApplicationConstants.IsExecuted(status))
                    throw new HiringBellException("Fail to update the record.");
            }
            else
            {
                throw new HiringBellException("Invalid component passed.");
            }

            return salaryComponents;
        }
    }
}