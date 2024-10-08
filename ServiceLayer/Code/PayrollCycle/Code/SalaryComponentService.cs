﻿using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using EMailService.Modal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using Newtonsoft.Json;
using ServiceLayer.Code.PayrollCycle.Interface;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Code.PayrollCycle.Code
{
    public class SalaryComponentService : ISalaryComponentService
    {
        private readonly IDb _db;
        private readonly CurrentSession _currentSession;
        private readonly IEvaluationPostfixExpression _postfixToInfixConversion;
        private readonly ITimezoneConverter _timezoneConverter;
        private readonly ILogger<DeclarationService> _logger;
        private readonly IUtilityService _utilityService;
        private readonly ICommonService _commonService;

        public SalaryComponentService(IDb db,
            CurrentSession currentSession,
            IEvaluationPostfixExpression postfixToInfixConversion,
            ITimezoneConverter timezoneConverter,
            IUtilityService utilityService,
            ILogger<DeclarationService> logger,
            ICommonService commonService)
        {
            _db = db;
            _currentSession = currentSession;
            _postfixToInfixConversion = postfixToInfixConversion;
            _timezoneConverter = timezoneConverter;
            _utilityService = utilityService;
            _logger = logger;
            _commonService = commonService;
        }

        public SalaryComponents GetSalaryComponentByIdService()
        {
            throw new NotImplementedException();
        }

        public List<SalaryComponents> GetSalaryComponentsDetailService()
        {
            List<SalaryComponents> salaryComponents = _db.GetList<SalaryComponents>(Procedures.Salary_Components_Get, false);
            return salaryComponents;
        }

        public List<SalaryGroup> GetSalaryGroupService(int CompanyId)
        {
            List<SalaryGroup> salaryComponents = _db.GetList<SalaryGroup>(Procedures.Salary_Group_GetbyCompanyId, new { CompanyId }, false);
            return salaryComponents;
        }

        public dynamic GetCustomSalryPageDataService(int CompanyId)
        {
            List<SalaryGroup> salaryGroups = GetSalaryGroupService(CompanyId);
            List<SalaryComponents> salaryComponents = GetSalaryComponentsDetailService();
            return new { SalaryComponents = salaryComponents, SalaryGroups = salaryGroups };
        }

        public SalaryGroup GetSalaryGroupsByIdService(int SalaryGroupId)
        {
            if (SalaryGroupId <= 0)
                throw new HiringBellException("Invalid SalaryGroupId");
            SalaryGroup salaryGroup = _db.Get<SalaryGroup>(Procedures.Salary_Group_GetById, new { SalaryGroupId });
            return salaryGroup;
        }

        public async Task<List<SalaryComponents>> UpdateSalaryComponentService(List<SalaryComponents> salaryComponents)
        {
            if (salaryComponents.Count > 0)
            {
                List<SalaryComponents> result = _db.GetList<SalaryComponents>(Procedures.Salary_Components_Get, false);
                Parallel.ForEach(result, x =>
                {
                    var item = salaryComponents.Find(i => i.ComponentId == x.ComponentId);
                    if (item != null)
                    {
                        x.IsActive = item.IsActive;
                        x.Formula = item.Formula;
                        x.CalculateInPercentage = item.CalculateInPercentage;
                    }
                });


                var itemOfRows = (from n in result
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
                                      n.UploadedFileIds,
                                      n.Formula,
                                      n.EmployeeContribution,
                                      n.EmployerContribution,
                                      n.IncludeInPayslip,
                                      n.IsOpted,
                                      n.IsActive,
                                      Admin = n.CreatedBy,
                                  }).ToList();

                await _db.BulkExecuteAsync(Procedures.Salary_Components_Insupd, itemOfRows, true);
            }

            return salaryComponents;
        }

        public async Task<List<SalaryComponents>> InsertUpdateSalaryComponentsByExcelService(IFormFileCollection files)
        {
            try
            {
                var uploadedHolidayData = await _utilityService.ReadExcelData<SalaryComponents>(files);
                var result = await UpdateHolidayData(uploadedHolidayData);
                return result;
            }
            catch
            {
                throw;
            }
        }

        private async Task<List<SalaryComponents>> UpdateHolidayData(List<SalaryComponents> salaryComponentsData)
        {
            List<SalaryComponents> result = _db.GetList<SalaryComponents>(Procedures.Salary_Components_Get, false);
            List<SalaryComponents> finalResult = new List<SalaryComponents>();
            foreach (SalaryComponents item in salaryComponentsData)
            {
                if (string.IsNullOrEmpty(item.ComponentId) || string.IsNullOrEmpty(item.ComponentFullName))
                    throw new HiringBellException("ComponentId or ComponentFullName is empty.");
            }

            var itemOfRows = (from n in salaryComponentsData
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
                                  n.AcceptedAmount,
                                  n.RejectedAmount,
                                  n.UploadedFileIds,
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
                                  AdminId = _currentSession.CurrentUserDetail.UserId,
                              }).ToList();

            int count = await _db.BulkExecuteAsync(Procedures.Salary_Components_Insupd, itemOfRows, true);
            if (count > 0)
            {
                if (result.Count > 0)
                {
                    finalResult = result;
                    foreach (var newComponents in salaryComponentsData)
                    {
                        var existing = finalResult.Find(x => x.ComponentId == newComponents.ComponentId);
                        if (existing != null)
                        {
                            existing.ComponentFullName = newComponents.ComponentFullName;
                            existing.AdHocId = newComponents.AdHocId;
                            existing.AdminId = newComponents.AdminId;
                            existing.ComponentId = newComponents.ComponentId;
                            existing.ComponentDescription = newComponents.ComponentDescription;
                            existing.CalculateInPercentage = newComponents.CalculateInPercentage;
                            existing.TaxExempt = newComponents.TaxExempt;
                            existing.ComponentTypeId = newComponents.ComponentTypeId;
                            existing.ComponentCatagoryId = newComponents.ComponentCatagoryId;
                            existing.PercentageValue = newComponents.PercentageValue;
                            existing.MaxLimit = newComponents.MaxLimit;
                            existing.DeclaredValue = newComponents.DeclaredValue;
                            existing.Formula = newComponents.Formula;
                            existing.EmployeeContribution = newComponents.EmployeeContribution;
                            existing.EmployerContribution = newComponents.EmployerContribution;
                            existing.IncludeInPayslip = newComponents.IncludeInPayslip;
                            existing.IsAdHoc = newComponents.IsAdHoc;
                            existing.Section = newComponents.Section;
                            existing.SectionMaxLimit = newComponents.SectionMaxLimit;
                            existing.IsAffectInGross = newComponents.IsAffectInGross;
                            existing.RequireDocs = newComponents.RequireDocs;
                            existing.IsOpted = newComponents.IsOpted;
                            existing.IsActive = newComponents.IsActive;
                        }
                        else
                            finalResult.Add(newComponents);
                    }
                }
                else
                {
                    finalResult = salaryComponentsData;
                }
            }
            else
            {
                finalResult = result;
            }

            return await Task.FromResult(finalResult);
        }

        public List<SalaryGroup> AddSalaryGroup(SalaryGroup salaryGroup)
        {
            ValidateSalaryGroup(salaryGroup);

            SalaryGroup salaryGrp = _db.Get<SalaryGroup>(Procedures.Salary_Group_Get_If_Exists, new
            {
                salaryGroup.CompanyId,
                salaryGroup.MinAmount,
                salaryGroup.MaxAmount
            });

            if (salaryGrp != null)
                throw new HiringBellException("Salary group limit already exist");

            List<SalaryComponents> initialSalaryComponents = _db.GetList<SalaryComponents>(Procedures.Salary_Group_Get_Initial_Components);

            if (salaryGrp == null)
            {
                salaryGrp = salaryGroup;
                salaryGrp.SalaryComponents = _commonService.GetStringifySalaryGroupData(initialSalaryComponents);

                salaryGrp.AdminId = _currentSession.CurrentUserDetail.AdminId;
            }
            else
                throw new HiringBellException("Salary Group already exist.");

            var result = _db.Execute<SalaryGroup>(Procedures.Salary_Group_Insupd, new
            {
                salaryGrp.SalaryGroupId,
                salaryGrp.CompanyId,
                salaryGrp.SalaryComponents,
                salaryGrp.GroupName,
                salaryGrp.GroupDescription,
                salaryGroup.MinAmount,
                salaryGrp.MaxAmount,
                AdminId = _currentSession.CurrentUserDetail.UserId
            }, true);
            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Fail to insert or update.");
            List<SalaryGroup> value = GetSalaryGroupService(salaryGroup.CompanyId);
            return value;
        }

        private void ValidateSalaryGroup(SalaryGroup salaryGroup)
        {
            if (salaryGroup.CompanyId < 0)
                throw new HiringBellException("Invalid data selected to create group. Please contact to admin.");

            if (string.IsNullOrEmpty(salaryGroup.GroupName))
                throw HiringBellException.ThrowBadRequest("Salary group name is null or empty");

            if (string.IsNullOrEmpty(salaryGroup.GroupDescription))
                throw HiringBellException.ThrowBadRequest("Salary group description is null or empty");

            if (salaryGroup.MinAmount < 0)
                throw HiringBellException.ThrowBadRequest("Salary group minimum amount is invalid");

            if (salaryGroup.MaxAmount < 0)
                throw HiringBellException.ThrowBadRequest("Salary group maximum amount is invalid");
        }

        public async Task<List<SalaryComponents>> AddUpdateRecurringComponents(SalaryStructure recurringComponent)
        {
            if (string.IsNullOrEmpty(recurringComponent.ComponentName))
                throw new HiringBellException("Invalid component name.");

            if (recurringComponent.ComponentTypeId <= 0)
                throw new HiringBellException("Invalid component type.");


            if (recurringComponent.ComponentCatagoryId <= 0)
                throw new HiringBellException("Invalid component type.");

            List<SalaryComponents> components = _db.GetList<SalaryComponents>(Procedures.Salary_Components_Get);
            var value = components.Find(x => x.ComponentId == recurringComponent.ComponentName);
            if (value == null)
                value = new SalaryComponents();

            value.ComponentId = recurringComponent.ComponentName;
            value.ComponentFullName = recurringComponent.ComponentFullName;
            value.ComponentDescription = recurringComponent.ComponentDescription;
            value.MaxLimit = recurringComponent.MaxLimit;
            value.DeclaredValue = recurringComponent.DeclaredValue;
            value.AcceptedAmount = recurringComponent.AcceptedAmount;
            value.RejectedAmount = recurringComponent.RejectedAmount;
            value.UploadedFileIds = recurringComponent.UploadedFileIds;
            value.TaxExempt = recurringComponent.TaxExempt;
            value.Section = recurringComponent.Section;
            value.ComponentTypeId = recurringComponent.ComponentTypeId;
            value.SectionMaxLimit = recurringComponent.SectionMaxLimit;
            value.ComponentCatagoryId = recurringComponent.ComponentCatagoryId;
            value.AdminId = _currentSession.CurrentUserDetail.AdminId;

            if (string.IsNullOrEmpty(value.UploadedFileIds))
                value.UploadedFileIds = "[]";

            var result = await _db.ExecuteAsync(Procedures.Salary_Components_Insupd, new
            {
                value.ComponentId,
                value.ComponentFullName,
                value.ComponentDescription,
                value.CalculateInPercentage,
                value.TaxExempt,
                value.ComponentTypeId,
                value.AcceptedAmount,
                value.RejectedAmount,
                value.UploadedFileIds,
                value.ComponentCatagoryId,
                value.PercentageValue,
                value.MaxLimit,
                value.DeclaredValue,
                value.Formula,
                value.EmployeeContribution,
                value.EmployerContribution,
                value.IncludeInPayslip,
                value.IsAdHoc,
                value.AdHocId,
                value.Section,
                value.SectionMaxLimit,
                value.IsAffectInGross,
                value.RequireDocs,
                value.IsOpted,
                value.IsActive,
                value.AdminId,
            }, true);

            if (string.IsNullOrEmpty(result.statusMessage))
                throw new HiringBellException("Fail insert salary component.");
            else
                await updateSalaryGroupByUdatingComponent(value);

            List<SalaryComponents> salaryComponents = GetSalaryComponentsDetailService();
            return salaryComponents;
        }

        private async Task updateSalaryGroupByUdatingComponent(SalaryComponents recurringComponent)
        {
            List<SalaryGroup> salaryGroups = _db.GetList<SalaryGroup>(Procedures.Salary_Group_GetAll, false);
            if (salaryGroups.Count > 0)
            {
                foreach (var item in salaryGroups)
                {
                    if (string.IsNullOrEmpty(item.SalaryComponents))
                        throw new HiringBellException("Salary component not found");

                    List<SalaryComponents> salaryComponents = JsonConvert.DeserializeObject<List<SalaryComponents>>(item.SalaryComponents);
                    var component = salaryComponents.Find(x => x.ComponentId == recurringComponent.ComponentId);
                    if (component != null)
                    {
                        component.ComponentId = recurringComponent.ComponentId;
                        component.ComponentCatagoryId = recurringComponent.ComponentCatagoryId;
                        component.ComponentTypeId = recurringComponent.ComponentTypeId;
                        component.ComponentFullName = recurringComponent.ComponentFullName;
                        component.MaxLimit = recurringComponent.MaxLimit;
                        component.ComponentDescription = recurringComponent.ComponentDescription;
                        component.TaxExempt = recurringComponent.TaxExempt;
                        component.Section = recurringComponent.Section;
                        component.SectionMaxLimit = recurringComponent.SectionMaxLimit;
                    }

                    item.SalaryComponents = JsonConvert.SerializeObject(salaryComponents);
                    var result = await _db.ExecuteAsync(Procedures.Salary_Group_Insupd, new
                    {
                        item.SalaryGroupId,
                        item.CompanyId,
                        item.SalaryComponents,
                        item.GroupName,
                        item.GroupDescription,
                        item.MinAmount,
                        item.MaxAmount,
                        AdminId = _currentSession.CurrentUserDetail.UserId
                    }, true);

                    if (string.IsNullOrEmpty(result.statusMessage))
                        throw HiringBellException.ThrowBadRequest("Fail to update salary group. Please contact to admin");
                }


                await Task.CompletedTask;
            }
        }

        public List<SalaryComponents> AddAdhocComponents(SalaryStructure adhocComponent)
        {
            if (string.IsNullOrEmpty(adhocComponent.ComponentName))
                throw new HiringBellException("Invalid AdHoc component name.");

            if (adhocComponent.AdHocId <= 0)
                throw new HiringBellException("Invalid AdHoc type component.");

            List<SalaryComponents> adhocComp = _db.GetList<SalaryComponents>(Procedures.Salary_Components_Get);
            var value = adhocComp.Find(x => x.ComponentId == adhocComponent.ComponentName);
            if (value != null)
                throw new HiringBellException("Component already exist.");

            value = new SalaryComponents();
            value.ComponentId = adhocComponent.ComponentName;
            value.ComponentFullName = adhocComponent.ComponentFullName;
            value.ComponentDescription = adhocComponent.ComponentDescription;
            value.UploadedFileIds = "[]";
            value.TaxExempt = adhocComponent.TaxExempt;
            value.Section = adhocComponent.Section;
            value.AdHocId = Convert.ToInt32(adhocComponent.AdHocId);
            value.SectionMaxLimit = adhocComponent.SectionMaxLimit;
            value.IsAdHoc = adhocComponent.IsAdHoc;
            value.AdminId = _currentSession.CurrentUserDetail.AdminId;

            var result = _db.Execute<SalaryComponents>(Procedures.Salary_Components_Insupd, new
            {
                value.ComponentId,
                value.ComponentFullName,
                value.ComponentDescription,
                value.CalculateInPercentage,
                value.TaxExempt,
                value.ComponentTypeId,
                value.AcceptedAmount,
                value.RejectedAmount,
                value.UploadedFileIds,
                value.ComponentCatagoryId,
                value.PercentageValue,
                value.MaxLimit,
                value.DeclaredValue,
                value.Formula,
                value.EmployeeContribution,
                value.EmployerContribution,
                value.IncludeInPayslip,
                value.IsAdHoc,
                value.AdHocId,
                value.Section,
                value.SectionMaxLimit,
                value.IsAffectInGross,
                value.RequireDocs,
                value.IsOpted,
                value.IsActive,
                value.AdminId,
            }, true);

            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Fail to add adhoc component.");

            return GetSalaryComponentsDetailService();
        }

        public List<SalaryComponents> AddDeductionComponents(SalaryStructure deductionComponent)
        {
            if (string.IsNullOrEmpty(deductionComponent.ComponentName))
                throw new HiringBellException("Invalid AdHoc component name.");

            if (deductionComponent.AdHocId <= 0)
                throw new HiringBellException("Invalid AdHoc type component.");

            List<SalaryComponents> adhocComp = _db.GetList<SalaryComponents>(Procedures.Salary_Components_Get);
            var value = adhocComp.Find(x => x.ComponentId == deductionComponent.ComponentName);
            if (value != null)
                throw new HiringBellException("Deduction Component already exist.");

            value = new SalaryComponents();
            value.ComponentId = deductionComponent.ComponentName;
            value.ComponentFullName = deductionComponent.ComponentFullName;
            value.ComponentDescription = deductionComponent.ComponentDescription;
            value.IsAffectInGross = deductionComponent.IsAffectInGross;
            value.AdHocId = Convert.ToInt32(deductionComponent.AdHocId);
            value.DeclaredValue = deductionComponent.DeclaredValue;
            value.UploadedFileIds = "[]";
            value.IsAdHoc = true;
            value.AdHocId = (int)AdhocType.Deduction;
            value.AdminId = _currentSession.CurrentUserDetail.AdminId;

            var result = _db.Execute<SalaryComponents>(Procedures.Salary_Components_Insupd, new
            {
                value.ComponentId,
                value.ComponentFullName,
                value.ComponentDescription,
                value.CalculateInPercentage,
                value.TaxExempt,
                value.ComponentTypeId,
                value.AcceptedAmount,
                value.RejectedAmount,
                value.UploadedFileIds,
                value.ComponentCatagoryId,
                value.PercentageValue,
                value.MaxLimit,
                value.DeclaredValue,
                value.Formula,
                value.EmployeeContribution,
                value.EmployerContribution,
                value.IncludeInPayslip,
                value.IsAdHoc,
                value.AdHocId,
                value.Section,
                value.SectionMaxLimit,
                value.IsAffectInGross,
                value.RequireDocs,
                value.IsOpted,
                value.IsActive,
                value.AdminId,
            }, true);

            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Fail to add deduction component.");

            return GetSalaryComponentsDetailService();
        }

        public List<SalaryComponents> AddBonusComponents(SalaryComponents bonusComponent)
        {
            if (string.IsNullOrEmpty(bonusComponent.ComponentId))
                throw new HiringBellException("Invalid component name.");

            List<SalaryComponents> bonuses = _db.GetList<SalaryComponents>(Procedures.Adhoc_Detail_Get);
            var value = bonuses.Find(x => x.ComponentId == bonusComponent.ComponentId);
            if (value != null)
                throw new HiringBellException("Bonus Component already exist.");

            value = new SalaryComponents();
            value.ComponentId = bonusComponent.ComponentId;
            value.ComponentFullName = bonusComponent.ComponentFullName;
            value.ComponentDescription = bonusComponent.ComponentDescription;
            value.DeclaredValue = bonusComponent.DeclaredValue;
            value.UploadedFileIds = "[]";
            value.IsAdHoc = true;
            value.AdHocId = (int)AdhocType.Bonus;
            value.AdminId = _currentSession.CurrentUserDetail.AdminId;

            var result = _db.Execute<SalaryComponents>(Procedures.Salary_Components_Insupd, new
            {
                value.ComponentId,
                value.ComponentFullName,
                value.ComponentDescription,
                value.CalculateInPercentage,
                value.TaxExempt,
                value.ComponentTypeId,
                value.AcceptedAmount,
                value.RejectedAmount,
                value.UploadedFileIds,
                value.ComponentCatagoryId,
                value.PercentageValue,
                value.MaxLimit,
                value.DeclaredValue,
                value.Formula,
                value.EmployeeContribution,
                value.EmployerContribution,
                value.IncludeInPayslip,
                value.IsAdHoc,
                value.AdHocId,
                value.Section,
                value.SectionMaxLimit,
                value.IsAffectInGross,
                value.RequireDocs,
                value.IsOpted,
                value.IsActive,
                value.AdminId,
            }, true);

            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Fail to add bonus component.");

            return GetSalaryComponentsDetailService();
        }

        public List<SalaryGroup> UpdateSalaryGroup(SalaryGroup salaryGroup)
        {
            List<SalaryGroup> salaryGroups = _db.GetList<SalaryGroup>(Procedures.Salary_Group_GetAll, false);
            salaryGroups = salaryGroups.Where(x => x.SalaryGroupId != salaryGroup.SalaryGroupId && x.CompanyId == _currentSession.CurrentUserDetail.CompanyId).ToList();
            foreach (SalaryGroup existSalaryGroup in salaryGroups)
            {
                if (salaryGroup.MinAmount < existSalaryGroup.MinAmount && salaryGroup.MinAmount > existSalaryGroup.MaxAmount || salaryGroup.MaxAmount > existSalaryGroup.MinAmount && salaryGroup.MaxAmount < existSalaryGroup.MaxAmount)
                    throw new HiringBellException("Salary group limit already exist");
            }
            SalaryGroup salaryGrp = _db.Get<SalaryGroup>(Procedures.Salary_Group_GetById, new { salaryGroup.SalaryGroupId });
            if (salaryGrp == null)
                throw new HiringBellException("Salary Group already exist.");
            else
            {
                if (string.IsNullOrEmpty(salaryGrp.SalaryComponents))
                    salaryGrp.SalaryComponents = "[]";

                salaryGrp.GroupName = salaryGroup.GroupName;
                salaryGrp.GroupDescription = salaryGroup.GroupDescription;
                salaryGrp.MinAmount = salaryGroup.MinAmount;
                salaryGrp.MaxAmount = salaryGroup.MaxAmount;
                salaryGrp.AdminId = _currentSession.CurrentUserDetail.AdminId;
            }

            var result = _db.Execute<SalaryGroup>(Procedures.Salary_Group_Insupd, salaryGrp, true);
            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Fail to insert or update.");
            List<SalaryGroup> value = GetSalaryGroupService(salaryGroup.CompanyId);
            return value;
        }

        public SalaryGroup RemoveAndUpdateSalaryGroupService(string componentId, int groupId)
        {
            SalaryGroup salaryGrp = _db.Get<SalaryGroup>(Procedures.Salary_Group_GetById, new { SalaryGroupId = groupId });
            if (salaryGrp == null)
                throw new HiringBellException("Salary Group already exist.");

            if (string.IsNullOrEmpty(salaryGrp.SalaryComponents))
                throw new HiringBellException("Salary Group already exist.", System.Net.HttpStatusCode.NotFound);

            var components = JsonConvert.DeserializeObject<List<SalaryComponents>>(salaryGrp.SalaryComponents);
            var component = components.FirstOrDefault(x => x.ComponentId == componentId);
            if (component != null)
            {
                if (components.Remove(component))
                {
                    salaryGrp.SalaryComponents = JsonConvert.SerializeObject(components);
                    var result = _db.Execute<SalaryGroup>(Procedures.Salary_Group_Insupd, salaryGrp, true);
                    if (string.IsNullOrEmpty(result))
                        throw new HiringBellException("Fail to insert or update.");
                }
                else
                {
                    throw new HiringBellException("Component does not exist in the group.", System.Net.HttpStatusCode.NotFound);
                }
            }
            else
            {
                components = components.Where(x => x.ComponentId != null && x.ComponentId != "").ToList();
                salaryGrp.SalaryComponents = JsonConvert.SerializeObject(components);
                var result = _db.Execute<SalaryGroup>(Procedures.Salary_Group_Insupd, salaryGrp, true);
                if (string.IsNullOrEmpty(result))
                    throw new HiringBellException("Fail to insert or update.");
            }

            return salaryGrp;
        }

        public List<SalaryComponents> UpdateSalaryGroupComponentService(SalaryGroup salaryGroup)
        {
            SalaryGroup salaryGrp = _db.Get<SalaryGroup>(Procedures.Salary_Group_GetById, new { salaryGroup.SalaryGroupId });
            if (salaryGrp == null)
                throw new HiringBellException("Salary Group already exist.");
            else
            {
                salaryGrp = salaryGroup;
                if (salaryGrp.GroupComponents == null)
                    salaryGrp.SalaryComponents = "[]";
                else
                    salaryGrp.SalaryComponents = JsonConvert.SerializeObject(salaryGroup.GroupComponents);
                salaryGrp.AdminId = _currentSession.CurrentUserDetail.AdminId;
            }

            var result = _db.Execute<SalaryGroup>(Procedures.Salary_Group_Insupd, salaryGrp, true);
            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Fail to insert or update.");
            List<SalaryComponents> value = GetSalaryGroupComponents(salaryGroup.SalaryGroupId, Convert.ToDecimal(salaryGroup.CTC));
            return value;
        }

        public List<SalaryComponents> GetSalaryGroupComponents(int salaryGroupId, decimal CTC)
        {
            SalaryGroup salaryGroup = _db.Get<SalaryGroup>(Procedures.Salary_Group_Get_By_Id_Or_Ctc,
                new { SalaryGroupId = salaryGroupId, CTC, _currentSession.CurrentUserDetail.CompanyId });
            if (salaryGroup == null)
            {
                salaryGroup = GetDefaultSalaryGroup();
                //throw new HiringBellException("Fail to calulate salar detail, salary group not defined for the current package.");
            }

            salaryGroup.GroupComponents = JsonConvert.DeserializeObject<List<SalaryComponents>>(salaryGroup.SalaryComponents);
            return salaryGroup.GroupComponents;
        }

        private SalaryGroup GetDefaultSalaryGroup()
        {
            var result = _db.Get<SalaryGroup>(Procedures.Salary_Group_GetById, new { SalaryGroupId = 1 });
            if (result == null)
                throw new HiringBellException("Default salry group not found");

            return result;
        }

        public List<SalaryComponents> GetSalaryGroupComponentsByCTC(long EmployeeId, decimal CTC)
        {
            SalaryGroup salaryGroup = _db.Get<SalaryGroup>(Procedures.Salary_Group_Get_By_Ctc, new { EmployeeId, CTC });
            if (salaryGroup == null)
            {
                salaryGroup = new SalaryGroup
                {
                    CTC = CTC,
                    GroupComponents = new List<SalaryComponents>()
                };
            }
            else
                salaryGroup.GroupComponents = JsonConvert.DeserializeObject<List<SalaryComponents>>(salaryGroup.SalaryComponents);

            return salaryGroup.GroupComponents;
        }

        private bool CompareFieldsValue(AnnualSalaryBreakup matchedSalaryBreakup, List<CalculatedSalaryBreakupDetail> completeSalaryBreakup)
        {
            bool flag = false;
            int i = 0;
            while (i < matchedSalaryBreakup.SalaryBreakupDetails.Count)
            {
                var item = matchedSalaryBreakup.SalaryBreakupDetails.ElementAt(i);
                var elem = completeSalaryBreakup.Find(x => x.ComponentId == item.ComponentId);
                if (elem == null)
                    break;

                if (item.FinalAmount != elem.FinalAmount)
                {
                    flag = true;
                    break;
                }

                i++;
            }

            return flag;
        }

        private void UpdateIfChangeFound(List<AnnualSalaryBreakup> annualSalaryBreakups, List<CalculatedSalaryBreakupDetail> salaryBreakup, int presentMonth, int PresentYear)
        {
            DateTime present = new DateTime(PresentYear, presentMonth, 1);
            if (_currentSession.TimeZone != null)
                present = _timezoneConverter.ToIstTime(present);

            AnnualSalaryBreakup matchedSalaryBreakups = annualSalaryBreakups.Where(x => x.PresentMonthDate.Subtract(present).TotalDays >= 0).FirstOrDefault();
            if (matchedSalaryBreakups == null)
                throw new HiringBellException("Invalid data found in salary detail. Please contact to admin.");
            else
                matchedSalaryBreakups.SalaryBreakupDetails = salaryBreakup;
        }

        private void ValidateCorrectnessOfSalaryDetail(List<CalculatedSalaryBreakupDetail> calculatedSalaryBreakupDetail)
        {
            // implement code to check the correctness of the modal on value level.
        }

        public string SalaryDetailService(long EmployeeId, List<CalculatedSalaryBreakupDetail> calculatedSalaryBreakupDetail, int PresentMonth, int PresentYear)
        {
            if (EmployeeId <= 0)
                throw new HiringBellException("Invalid EmployeeId");

            EmployeeSalaryDetail employeeSalaryDetail = _db.Get<EmployeeSalaryDetail>(Procedures.Employee_Salary_Detail_Get_By_Empid,
                new
                {
                    _currentSession.FinancialStartYear,
                    EmployeeId
                });

            if (employeeSalaryDetail == null)
                throw new HiringBellException("Fail to get salary detail. Please contact to admin.");

            List<AnnualSalaryBreakup> annualSalaryBreakups = JsonConvert.DeserializeObject<List<AnnualSalaryBreakup>>(employeeSalaryDetail.CompleteSalaryDetail);

            // implement code to check the correctness of the modal on value level.
            ValidateCorrectnessOfSalaryDetail(calculatedSalaryBreakupDetail);

            UpdateIfChangeFound(annualSalaryBreakups, calculatedSalaryBreakupDetail, PresentMonth, PresentYear);

            EmployeeSalaryDetail salaryBreakup = new EmployeeSalaryDetail
            {
                CompleteSalaryDetail = JsonConvert.SerializeObject(calculatedSalaryBreakupDetail),
                CTC = employeeSalaryDetail.CTC,
                EmployeeId = EmployeeId,
                GrossIncome = employeeSalaryDetail.GrossIncome,
                GroupId = employeeSalaryDetail.GroupId,
                NetSalary = employeeSalaryDetail.NetSalary,
                TaxDetail = employeeSalaryDetail.TaxDetail,
                NewSalaryDetail = employeeSalaryDetail.NewSalaryDetail
            };

            var result = _db.Execute<EmployeeSalaryDetail>(Procedures.Employee_Salary_Detail_InsUpd, salaryBreakup, true);

            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Unable to insert or update salary breakup");
            else
                result = "Inserted/Updated successfully";
            return result;
        }

        private decimal GetEmployeeContributionAmount(List<SalaryComponents> salaryComponents, decimal CTC)
        {
            decimal finalAmount = 0;
            var gratutity = salaryComponents.FirstOrDefault(x => x.ComponentId.ToUpper() == "GRA");
            if (gratutity != null && !string.IsNullOrEmpty(gratutity.Formula))
            {
                if (gratutity.Formula.Contains("[CTC]"))
                    gratutity.Formula = gratutity.Formula.Replace("[CTC]", Convert.ToDecimal(CTC).ToString());

                finalAmount += calculateExpressionUsingInfixDS(gratutity.Formula, gratutity.DeclaredValue);
            }

            var employeePF = salaryComponents.FirstOrDefault(x => x.ComponentId.ToUpper() == "EPER-PF");
            if (employeePF != null && !string.IsNullOrEmpty(employeePF.Formula))
            {
                if (employeePF.Formula.Contains("[CTC]"))
                    employeePF.Formula = employeePF.Formula.Replace("[CTC]", Convert.ToDecimal(CTC).ToString());

                finalAmount += calculateExpressionUsingInfixDS(employeePF.Formula, employeePF.DeclaredValue);
            }

            var employeeInsurance = salaryComponents.FirstOrDefault(x => x.ComponentId.ToUpper() == "ECI");
            if (employeeInsurance != null && !string.IsNullOrEmpty(employeeInsurance.Formula))
            {
                if (employeeInsurance.Formula.Contains("[CTC]"))
                    employeeInsurance.Formula = employeeInsurance.Formula.Replace("[CTC]", Convert.ToDecimal(CTC).ToString());

                finalAmount += calculateExpressionUsingInfixDS(employeeInsurance.Formula, employeeInsurance.DeclaredValue);
            }

            return finalAmount;
        }

        private decimal GetTaxExamptedAmount(List<SalaryComponents> salaryComponents)
        {
            _logger.LogInformation("Starting method: GetTaxExamptedAmount");

            decimal finalAmount = 0;
            var taxExamptedComponents = salaryComponents.FindAll(x => x.TaxExempt);
            if (taxExamptedComponents.Count > 0)
            {
                foreach (var item in taxExamptedComponents)
                {
                    finalAmount += calculateExpressionUsingInfixDS(item.Formula, item.DeclaredValue);
                }
            }

            _logger.LogInformation("Leaving method: GetTaxExamptedAmount");
            return finalAmount;
        }

        private decimal GetBaiscAmountValue(List<SalaryComponents> salaryComponents, decimal CTC)
        {
            _logger.LogInformation("Starting method: GetBaiscAmountValue");

            decimal finalAmount = 0;
            var basicComponent = salaryComponents.Find(x => x.ComponentId.ToUpper() == ComponentNames.Basic);
            if (basicComponent != null)
            {
                if (!string.IsNullOrEmpty(basicComponent.Formula))
                {
                    if (basicComponent.Formula.Contains(ComponentNames.CTCName))
                        basicComponent.Formula = basicComponent.Formula.Replace(ComponentNames.CTCName, Convert.ToDecimal(CTC).ToString());
                }

                finalAmount = calculateExpressionUsingInfixDS(basicComponent.Formula, basicComponent.DeclaredValue);
            }
            _logger.LogInformation("Leaving method: GetBaiscAmountValue");

            return finalAmount;
        }

        //private async Task<EmployeeCalculation> GetEmployeeSalaryDetail(long EmployeeId, decimal CTCAnnually)
        //{
        //    EmployeeCalculation employeeCalculation = new EmployeeCalculation();

        //    employeeCalculation.EmployeeId = EmployeeId;
        //    employeeCalculation.CTC = CTCAnnually;

        //    var ResultSet = _db.FetchDataSet("sp_salary_components_group_by_employeeid",
        //        new { employeeCalculation.EmployeeId });

        //    if (ResultSet == null || ResultSet.Tables.Count != 4)
        //        throw new HiringBellException("Unbale to get salary detail. Please contact to admin.");

        //    if (ResultSet.Tables[0].Rows.Count == 0)
        //        throw new HiringBellException($"Salary group not found for salary: [{CTCAnnually}]");

        //    if (ResultSet.Tables[1].Rows.Count == 0)
        //        throw new HiringBellException($"Salary detail not found for employee Id: [{EmployeeId}]");

        //    if (ResultSet.Tables[2].Rows.Count == 0)
        //        throw new HiringBellException($"Employee company setting is not defined. Please contact to admin.");

        //    employeeCalculation.salaryGroup = Converter.ToType<SalaryGroup>(ResultSet.Tables[0]);

        //    employeeCalculation.employeeSalaryDetail = Converter.ToType<EmployeeSalaryDetail>(ResultSet.Tables[1]);

        //    employeeCalculation.companySetting = Converter.ToType<CompanySetting>(ResultSet.Tables[2]);

        //    if (string.IsNullOrEmpty(employeeCalculation.salaryGroup.SalaryComponents))
        //        throw new HiringBellException($"Salary components not found for salary: [{CTCAnnually}]");

        //    return await Task.FromResult(employeeCalculation);
        //}

        public async Task<string> ResetSalaryBreakupService()
        {
            List<EmployeeSalaryDetail> employeeSalaryDetails = _db.GetList<EmployeeSalaryDetail>(Procedures.Employee_Salary_Detail_Get);
            employeeSalaryDetails.ForEach(async x =>
            {
                List<AnnualSalaryBreakup> annualSalaryBreakups = null;
                EmployeeCalculation employeeCalculation = new EmployeeCalculation
                {
                    EmployeeId = x.EmployeeId,
                    employee = new Employee { EmployeeUid = x.EmployeeId }
                };

                await GetEmployeeSalaryDetail(employeeCalculation);
                ValidateCurrentSalaryGroupNComponents(employeeCalculation);

                if (x.CTC <= 0)
                    annualSalaryBreakups = CreateSalaryBreakUpWithZeroCTC(x.EmployeeId, x.CTC);
                else
                    annualSalaryBreakups = CreateSalaryBreakupWithValue(employeeCalculation);

                var result = _db.Execute<EmployeeSalaryDetail>(Procedures.Employee_Salary_Detail_Upd_On_Payroll_Run, new
                {
                    _currentSession.FinancialStartYear,
                    x.EmployeeId,
                    x.TaxDetail,
                    CompleteSalaryDetail = JsonConvert.SerializeObject(annualSalaryBreakups)
                }, true);
                if (string.IsNullOrEmpty(result))
                    throw HiringBellException.ThrowBadRequest($"Fail to reset salary breakup of EmpoyeeId: {x.EmployeeId}");
            });

            return await Task.FromResult("Reset successfully");
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

        private void ReplaceFormulaToActual(EmployeeCalculation eCal)
        {
            DateTime startDate = new DateTime(eCal.companySetting.FinancialYear, eCal.companySetting.DeclarationStartMonth, 1, 0, 0, 0, DateTimeKind.Utc);
            eCal.PayrollStartDate = startDate;

            //decimal taxExamptedComponents = GetTaxExamptedAmount(eCal.salaryGroup.GroupComponents);
            //eCal.TaxableCTC = Convert.ToDecimal(eCal.CTC - taxExamptedComponents);
            eCal.TaxableCTC = eCal.CTC;
            decimal basicAmountValue = GetBaiscAmountValue(eCal.salaryGroup.GroupComponents, eCal.CTC);

            int i = 0;
            while (i < eCal.salaryGroup.GroupComponents.Count)
            {
                var item = eCal.salaryGroup.GroupComponents.ElementAt(i);
                if (!string.IsNullOrEmpty(item.Formula))
                {
                    if (item.Formula.Contains(ComponentNames.BasicName))
                        item.Formula = item.Formula.Replace(ComponentNames.BasicName, basicAmountValue.ToString());
                    else if (item.Formula.Contains(ComponentNames.CTCName))
                        item.Formula = item.Formula.Replace(ComponentNames.CTCName, Convert.ToDecimal(eCal.CTC).ToString());
                }

                i++;
            }
        }

        private List<CalculatedSalaryBreakupDetail> CalculateESIAmount(decimal grossAmount,
                                                                        List<CalculatedSalaryBreakupDetail> calculatedSalaryBreakupDetails,
                                                                        PfEsiSetting pfEsiSetting,
                                                                        List<SalaryComponents> salaryComponents)
        {
            var employerESIComp = salaryComponents.Find(x => x.ComponentId == LocalConstants.EESI);
            if (employerESIComp == null)
                throw HiringBellException.ThrowBadRequest("Employer ESI component not found.");

            decimal employerESI = (grossAmount * pfEsiSetting.EsiEmployerContribution) / 100;
            calculatedSalaryBreakupDetails.Add(new CalculatedSalaryBreakupDetail
            {
                ComponentId = employerESIComp.ComponentId,
                Formula = (employerESI * 12).ToString(),
                ComponentName = employerESIComp.ComponentFullName,
                ComponentTypeId = employerESIComp.ComponentTypeId,
                FinalAmount = 0,
                ActualAmount = employerESI,
                IsIncludeInPayslip = !pfEsiSetting.IsHideEsiEmployer
            });

            var employeeESIComp = salaryComponents.Find(x => x.ComponentId == LocalConstants.ESI);
            if (employeeESIComp == null)
                throw HiringBellException.ThrowBadRequest("Employer ESI component not found.");

            decimal employeeESI = (grossAmount * pfEsiSetting.EsiEmployeeContribution) / 100;
            calculatedSalaryBreakupDetails.Add(new CalculatedSalaryBreakupDetail
            {
                ComponentId = employeeESIComp.ComponentId,
                Formula = (employeeESI * 12).ToString(),
                ComponentName = employeeESIComp.ComponentFullName,
                ComponentTypeId = employeeESIComp.ComponentTypeId,
                FinalAmount = 0,
                ActualAmount = employeeESI,
                IsIncludeInPayslip = true
            });

            return calculatedSalaryBreakupDetails;
        }

        private List<CalculatedSalaryBreakupDetail> CalculatePFAmount(List<CalculatedSalaryBreakupDetail> calculatedSalaryBreakupDetails,
                                                                       EmployeeCalculation eCal,
                                                                       List<SalaryComponents> salaryComponents)
        {
            var employeePFComp = salaryComponents.Find(x => x.ComponentId == LocalConstants.EPF);
            if (employeePFComp == null)
                throw HiringBellException.ThrowBadRequest("Employee PF component not found.");

            var employerPFComp = salaryComponents.Find(x => x.ComponentId == LocalConstants.EEPF);
            if (employerPFComp == null)
                throw HiringBellException.ThrowBadRequest("Employer PF component not found.");

            decimal actualAmount = 0;
            if (!string.IsNullOrEmpty(eCal.employee.EmployeePF))
            {
                var convertedFormula = GetConvertedFormula(eCal, eCal.employee.EmployeePF);
                employeePFComp.Formula = eCal.employee.EmployeePF;
                actualAmount = calculateExpressionUsingInfixDS(convertedFormula, 0) / 12;
            }
            else
            {
                actualAmount = calculateExpressionUsingInfixDS(employeePFComp.Formula, 0);
            }

            calculatedSalaryBreakupDetails.Add(new CalculatedSalaryBreakupDetail
            {
                ComponentId = employeePFComp.ComponentId,
                Formula = employeePFComp.Formula,
                ComponentName = employeePFComp.ComponentFullName,
                ComponentTypeId = employeePFComp.ComponentTypeId,
                FinalAmount = 0,
                ActualAmount = actualAmount,
                IsIncludeInPayslip = true
            });

            if (!string.IsNullOrEmpty(eCal.employee.EmployerPF))
            {
                var convertedFormula = GetConvertedFormula(eCal, eCal.employee.EmployerPF);
                employerPFComp.Formula = eCal.employee.EmployerPF;
                var amount = calculateExpressionUsingInfixDS(convertedFormula, 0);
                actualAmount = amount / 12;
                employerPFComp.IncludeInPayslip = true;
            }
            //else if (eCal.pfEsiSetting != null && eCal.pfEsiSetting.PFEnable)
            //{
            //    employerPFComp.IncludeInPayslip = !eCal.pfEsiSetting.IsHidePfEmployer;
            //    var amount = calculateExpressionUsingInfixDS(employerPFComp.Formula, employerPFComp.DeclaredValue);
            //    actualAmount = amount / 12;

            //    //if (currentYearMonthFlag)
            //    //{
            //    //    int numberOfDays = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
            //    //    int daysWorked = numberOfDays - eCal.Doj.Day + 1;
            //    //    if (daysWorked <= 0)
            //    //    {
            //    //        actualAmount = 0;
            //    //    }
            //    //    else
            //    //    {
            //    //        actualAmount = actualAmount / numberOfDays * daysWorked;
            //    //    }
            //    //}
            //}
            else
            {
                var amount = calculateExpressionUsingInfixDS(employerPFComp.Formula, 0);
                actualAmount = amount / 12;
                employerPFComp.IncludeInPayslip = true;
            }

            calculatedSalaryBreakupDetails.Add(new CalculatedSalaryBreakupDetail
            {
                ComponentId = employerPFComp.ComponentId,
                Formula = employerPFComp.Formula,
                ComponentName = employerPFComp.ComponentFullName,
                ComponentTypeId = employerPFComp.ComponentTypeId,
                FinalAmount = 0,
                ActualAmount = actualAmount,
                IsIncludeInPayslip = employerPFComp.IncludeInPayslip
            });

            return calculatedSalaryBreakupDetails;
        }

        public List<AnnualSalaryBreakup> CreateSalaryBreakupWithValue(EmployeeCalculation eCal)
        {
            _logger.LogInformation("Starting method: CreateSalaryBreakupWithValue");

            ReplaceFormulaToActual(eCal);

            List<AnnualSalaryBreakup> annualSalaryBreakups = CreateFreshSalaryBreakUp(eCal);
            if (annualSalaryBreakups == null || annualSalaryBreakups.Count == 0)
                throw new HiringBellException("Unable to build salary detail. Please contact to admin.");

            _logger.LogInformation("Leaving method: CreateSalaryBreakupWithValue");

            return annualSalaryBreakups;
        }

        public List<AnnualSalaryBreakup> UpdateSalaryBreakUp(EmployeeCalculation eCal, EmployeeSalaryDetail salaryBreakup)
        {
            _logger.LogInformation("Starting method: UpdateSalaryBreakUp");

            List<AnnualSalaryBreakup> annualSalaryBreakup = JsonConvert.DeserializeObject<List<AnnualSalaryBreakup>>(salaryBreakup.CompleteSalaryDetail);

            ReplaceFormulaToActual(eCal);

            foreach (var salary in annualSalaryBreakup)
            {
                if (!salary.IsPayrollExecutedForThisMonth)
                    UpdateMonthSalaryCalculatedStructure(salary, eCal);
            }

            _logger.LogInformation("Leaving method: UpdateSalaryBreakUp");

            return annualSalaryBreakup;
        }

        private CalculatedSalaryBreakupDetail ResolvEMPPFForumulaAmount(EmployeeCalculation eCal)
        {
            SalaryComponents employerPFComponents = eCal.salaryGroup.GroupComponents.Where(x => x.ComponentId == ComponentNames.EmployerPF).FirstOrDefault();
            if (employerPFComponents.ComponentId == null)
                throw HiringBellException.ThrowBadRequest("EmployerPR component is not defined. Please add one.");

            decimal amount = calculateExpressionUsingInfixDS(employerPFComponents.Formula, employerPFComponents.DeclaredValue);

            CalculatedSalaryBreakupDetail calculatedSalaryBreakupDetail = new CalculatedSalaryBreakupDetail();
            calculatedSalaryBreakupDetail.ComponentId = employerPFComponents.ComponentId;
            calculatedSalaryBreakupDetail.Formula = employerPFComponents.Formula;
            calculatedSalaryBreakupDetail.ComponentName = employerPFComponents.ComponentFullName;
            calculatedSalaryBreakupDetail.ComponentTypeId = employerPFComponents.ComponentTypeId;
            calculatedSalaryBreakupDetail.FinalAmount = amount / 12;
            calculatedSalaryBreakupDetail.IsIncludeInPayslip = employerPFComponents.IncludeInPayslip;

            return calculatedSalaryBreakupDetail;
        }

        private string GetConvertedFormula(EmployeeCalculation eCal, string userFormula)
        {
            decimal basicAmountValue = 0;
            string formula = userFormula;

            if (userFormula.Contains("basic", StringComparison.OrdinalIgnoreCase))
            {
                basicAmountValue = GetBaiscAmountValue(eCal.salaryGroup.GroupComponents, eCal.CTC);
            }
            else
            {
                formula = (Convert.ToDouble(userFormula) * 12).ToString();
            }

            var elems = formula.Split(" ");
            if (elems != null && elems.Length > 0)
            {
                if (elems[0].Contains("%"))
                {
                    elems[0] = elems[0].Replace("%", "");
                    if (int.TryParse(elems[0].Trim(), out int value))
                    {
                        formula = $"{value}%{basicAmountValue}";
                    }
                }
                else
                {
                    if (int.TryParse(elems[0].Trim(), out int value))
                    {
                        formula = value.ToString();
                    }
                }
            }

            return formula;
        }

        private List<CalculatedSalaryBreakupDetail> ResolveFormula(EmployeeCalculation eCal, DateTime doj,
                                                                    DateTime startDate, decimal calculatedMontlyGross,
                                                                    bool currentYearMonthFlag)
        {
            _logger.LogInformation("Starting method: GetComponentsDetail");

            List<CalculatedSalaryBreakupDetail> calculatedSalaryBreakupDetails = new List<CalculatedSalaryBreakupDetail>();

            decimal amount = 0;
            decimal taxableComponentAmount = 0;
            List<SalaryComponents> taxableComponents = eCal.salaryGroup.GroupComponents
                                                        .Where(x => x.TaxExempt == false || x.ComponentId == LocalConstants.EPF)
                                                        .ToList();

            var autoComponent = taxableComponents.Find(x => x.Formula == ApplicationConstants.AutoCalculation);
            if (autoComponent == null)
            {
                var spaComponent = taxableComponents.Find(x => x.ComponentId == LocalConstants.SPA);
                if (spaComponent != null && string.IsNullOrEmpty(spaComponent.Formula))
                {
                    spaComponent.Formula = ApplicationConstants.AutoCalculation;
                }
            }
            var taxableSalaryComponents = taxableComponents.FindAll(x => x.ComponentId != LocalConstants.ESI
                                                                        && x.ComponentId != LocalConstants.EESI
                                                                        && x.ComponentId != LocalConstants.EEPF
                                                                        && x.ComponentId != LocalConstants.EPF);
            foreach (var item in taxableSalaryComponents)
            {
                CalculatedSalaryBreakupDetail calculatedSalaryBreakupDetail = new CalculatedSalaryBreakupDetail();

                if (!string.IsNullOrEmpty(item.ComponentId) && item.Formula != ApplicationConstants.AutoCalculation)
                {
                    amount = calculateExpressionUsingInfixDS(item.Formula, item.DeclaredValue);
                    amount = amount / 12;

                    if (currentYearMonthFlag)
                    {
                        int numberOfDays = DateTime.DaysInMonth(startDate.Year, startDate.Month);
                        int daysWorked = numberOfDays - doj.Day + 1;
                        if (daysWorked <= 0)
                            amount = 0;
                        else
                            amount = amount / numberOfDays * daysWorked;
                    }
                }
                else
                {
                    amount = 0;
                }

                calculatedSalaryBreakupDetail.ComponentId = item.ComponentId;
                calculatedSalaryBreakupDetail.Formula = item.Formula;
                calculatedSalaryBreakupDetail.ComponentName = item.ComponentFullName;
                calculatedSalaryBreakupDetail.ComponentTypeId = item.ComponentTypeId;
                calculatedSalaryBreakupDetail.FinalAmount = 0;
                calculatedSalaryBreakupDetail.ActualAmount = amount;
                calculatedSalaryBreakupDetail.IsIncludeInPayslip = item.IncludeInPayslip;
                taxableComponentAmount += calculatedSalaryBreakupDetail.ActualAmount;
                calculatedSalaryBreakupDetails.Add(calculatedSalaryBreakupDetail);
            }

            if (calculatedMontlyGross < taxableComponentAmount)
                throw HiringBellException.ThrowBadRequest("Invalid calculation. Gross amount must be greater than or equals to the sum of other components.");

            var component = calculatedSalaryBreakupDetails.Find(x => x.Formula == ApplicationConstants.AutoCalculation);
            if (component != null)
            {
                component.ActualAmount = calculatedMontlyGross - taxableComponentAmount;
            }

            // calculatedSalaryBreakupDetails.Add(ResolvEMPPFForumulaAmount(eCal));
            _logger.LogInformation("Ending method: GetComponentsDetail");
            if (calculatedMontlyGross <= eCal.pfEsiSetting.MaximumGrossForESI)
            {
                var esiComponents = taxableComponents.FindAll(x => x.ComponentId == LocalConstants.ESI
                                                                    || x.ComponentId == LocalConstants.EESI);
                CalculateESIAmount(calculatedMontlyGross, calculatedSalaryBreakupDetails, eCal.pfEsiSetting, esiComponents);
            }
            else
            {
                var pfComponents = taxableComponents.FindAll(x => x.ComponentId == LocalConstants.EEPF
                                                                    || x.ComponentId == LocalConstants.EPF);
                CalculatePFAmount(calculatedSalaryBreakupDetails, eCal, pfComponents);
            }

            return calculatedSalaryBreakupDetails;
        }

        private void UpdateMonthSalaryCalculatedStructure(AnnualSalaryBreakup annualSalaryBreakup, EmployeeCalculation eCal)
        {
            _logger.LogInformation("Starting method: GetComponentsDetail");

            List<CalculatedSalaryBreakupDetail> calculatedSalaryBreakupDetail = annualSalaryBreakup.SalaryBreakupDetails;

            decimal amount = 0;
            decimal monthlyGrossIncome = eCal.TaxableCTC / 12;
            DateTime currentDate = annualSalaryBreakup.PresentMonthDate;

            var taxableComponents = eCal.salaryGroup.GroupComponents.Where(x => x.TaxExempt == false);
            eCal.pfEsiSetting = _db.Get<PfEsiSetting>("sp_pf_esi_setting_get", new { _currentSession.CurrentUserDetail.CompanyId });
            if (eCal.pfEsiSetting == null)
                throw HiringBellException.ThrowBadRequest("PF and ESI setting is not found. Please contact contact to admin");

            foreach (var item in taxableComponents)
            {
                if (!string.IsNullOrEmpty(item.ComponentId) && item.Formula != ApplicationConstants.AutoCalculation)
                {
                    if (item.ComponentId == "EPER-PF")
                    {
                        if (eCal.pfEsiSetting.PFEnable)
                        {
                            item.IncludeInPayslip = !eCal.pfEsiSetting.IsHidePfEmployer;
                            amount = calculateExpressionUsingInfixDS(item.Formula, item.DeclaredValue);
                            amount = amount / 12;

                            if (_utilityService.CheckIsJoinedInCurrentFinancialYear(eCal.Doj, eCal.companySetting) && eCal.Doj.Month == currentDate.Month)
                            {
                                int numberOfDays = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
                                int daysWorked = numberOfDays - eCal.Doj.Day + 1;
                                if (daysWorked <= 0)
                                {
                                    amount = 0;
                                }
                                else
                                {
                                    amount = amount / numberOfDays * daysWorked;
                                }
                            }
                        }
                        else
                        {

                            item.IncludeInPayslip = false;
                        }
                    }
                    else if (item.ComponentId == "ECI")
                    {
                        if (eCal.pfEsiSetting.EsiEnable)
                        {
                            amount = eCal.pfEsiSetting.EsiEmployerContribution + eCal.pfEsiSetting.EsiEmployeeContribution;
                            item.IncludeInPayslip = eCal.pfEsiSetting.IsHideEsiEmployer;
                        }
                        else
                        {
                            item.IncludeInPayslip = false;
                        }
                    }
                    else
                    {
                        amount = calculateExpressionUsingInfixDS(item.Formula, item.DeclaredValue);
                        amount = amount / 12;

                        if (_utilityService.CheckIsJoinedInCurrentFinancialYear(eCal.Doj, eCal.companySetting) && eCal.Doj.Month == currentDate.Month)
                        {
                            int numberOfDays = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
                            int daysWorked = numberOfDays - eCal.Doj.Day + 1;
                            if (daysWorked <= 0)
                            {
                                amount = 0;
                            }
                            else
                            {
                                amount = amount / numberOfDays * daysWorked;
                            }
                        }
                    }
                }
                else
                {
                    amount = 0;
                }

                var component = calculatedSalaryBreakupDetail.Find(x => x.ComponentId == item.ComponentId);
                if (component != null)
                {
                    component.Formula = item.Formula;
                    component.ActualAmount = amount;
                    component.FinalAmount = 0;
                    component.IsIncludeInPayslip = item.IncludeInPayslip;
                }
            }

            var taxableComponentAmount = calculatedSalaryBreakupDetail.FindAll(x => x.ComponentId != nameof(ComponentNames.Gross) && x.ComponentId != nameof(ComponentNames.CTC)).Sum(x => x.ActualAmount);

            var autoComponent = calculatedSalaryBreakupDetail.Find(x => x.Formula == ApplicationConstants.AutoCalculation);
            if (autoComponent != null)
                autoComponent.ActualAmount = monthlyGrossIncome - taxableComponentAmount;

            if (monthlyGrossIncome < taxableComponentAmount)
                throw HiringBellException.ThrowBadRequest("Invalid calculation. Gross amount must be greater than or equals to the sum of other components.");

            var currentComponent = calculatedSalaryBreakupDetail.Find(x => x.ComponentId == nameof(ComponentNames.Gross));
            currentComponent.ActualAmount = monthlyGrossIncome;

            currentComponent = calculatedSalaryBreakupDetail.Find(x => x.ComponentId == nameof(ComponentNames.CTC));
            currentComponent.ActualAmount = eCal.CTC / 12;

            _logger.LogInformation("Endning method: GetComponentsDetail");
        }

        private decimal GetExpectedMonthSalary(DateTime doj, DateTime startDate, decimal monthlyGross)
        {
            if (doj.Year == startDate.Year && doj.Month == startDate.Month)
            {
                int numberOfDays = DateTime.DaysInMonth(startDate.Year, startDate.Month);
                var numberOfDaysInPresentMonth = numberOfDays - doj.Day + 1;

                if (numberOfDaysInPresentMonth <= 0)
                    return 0;

                monthlyGross = monthlyGross / numberOfDays * numberOfDaysInPresentMonth;
            }

            return monthlyGross;
        }

        private List<AnnualSalaryBreakup> CreateFreshSalaryBreakUp(EmployeeCalculation eCal)
        {
            _logger.LogInformation("Starting method: CreateFreshSalaryBreakUp");

            List<AnnualSalaryBreakup> annualSalaryBreakups = new List<AnnualSalaryBreakup>();
            DateTime doj = _timezoneConverter.ToTimeZoneDateTime(eCal.Doj, _currentSession.TimeZone);
            DateTime startDate = eCal.PayrollStartDate;

            int index = 0;
            bool IsJoinedInMiddleOfCalendar = false;

            decimal monthlyGrossIncome = eCal.TaxableCTC / 12;
            decimal calculatedMontlyGross = 0;

            bool currentYearMonthFlag = false;
            List<CalculatedSalaryBreakupDetail> calculatedSalaryBreakupDetails = null;
            eCal.pfEsiSetting = _db.Get<PfEsiSetting>(Procedures.Pf_Esi_Setting_Get, new { _currentSession.CurrentUserDetail.CompanyId });
            //if (eCal.pfEsiSetting == null)
            //    throw HiringBellException.ThrowBadRequest("PF and ESI setting is not found. Please contact contact to admin");

            while (index < 12)
            {
                calculatedMontlyGross = GetExpectedMonthSalary(doj, startDate, monthlyGrossIncome);
                List<CalculatedSalaryBreakupDetail> otherDetails = new List<CalculatedSalaryBreakupDetail>();

                IsJoinedInMiddleOfCalendar = false;

                // checking if joined in middle of calendar year. i.e Joined in Aug but cycle is from April, then previous month of Aug will be zeo initially.
                if (startDate.Subtract(doj).TotalDays < 0 && startDate.Month != doj.Month)
                {
                    IsJoinedInMiddleOfCalendar = true;
                }

                currentYearMonthFlag = false;
                if (_utilityService.CheckIsJoinedInCurrentFinancialYear(eCal.Doj, eCal.companySetting) && eCal.Doj.Month == startDate.Month)
                    currentYearMonthFlag = true;

                calculatedSalaryBreakupDetails = ResolveFormula(eCal, doj, startDate, calculatedMontlyGross, currentYearMonthFlag);

                var calculatedSalaryBreakupDetail = new CalculatedSalaryBreakupDetail
                {
                    ComponentId = nameof(ComponentNames.Gross),
                    Formula = null,
                    ComponentName = ComponentNames.Gross,
                    FinalAmount = 0,
                    ActualAmount = calculatedMontlyGross,
                    ComponentTypeId = 100,
                    IsIncludeInPayslip = true
                };

                otherDetails.Add(calculatedSalaryBreakupDetail);
                calculatedSalaryBreakupDetail = new CalculatedSalaryBreakupDetail
                {
                    ComponentId = nameof(ComponentNames.CTC),
                    Formula = null,
                    ComponentName = ComponentNames.CTC,
                    FinalAmount = 0,
                    ActualAmount = eCal.CTC / 12,
                    ComponentTypeId = 101,
                    IsIncludeInPayslip = true
                };

                otherDetails.Add(calculatedSalaryBreakupDetail);
                otherDetails.AddRange(calculatedSalaryBreakupDetails);


                annualSalaryBreakups.Add(new AnnualSalaryBreakup
                {
                    MonthName = startDate.ToString("MMM"),
                    IsPayrollExecutedForThisMonth = IsJoinedInMiddleOfCalendar,
                    MonthNumber = startDate.Month,
                    IsArrearMonth = eCal.companySetting.IsJoiningBarrierDayPassed && currentYearMonthFlag,
                    PresentMonthDate = startDate,
                    IsActive = !IsJoinedInMiddleOfCalendar,
                    StateName = eCal.employee.BaseLocation,
                    SalaryBreakupDetails = otherDetails
                });

                startDate = startDate.AddMonths(1);
                index++;
            }

            _logger.LogInformation("Leaving method: CreateFreshSalaryBreakUp");

            return annualSalaryBreakups;
        }

        private List<AnnualSalaryBreakup> CreateSalaryBreakUpWithZeroCTC(long EmployeeId, decimal CTCAnnually)
        {
            List<AnnualSalaryBreakup> annualSalaryBreakups = new List<AnnualSalaryBreakup>();


            List<CalculatedSalaryBreakupDetail> calculatedSalaryBreakupDetails = new List<CalculatedSalaryBreakupDetail>();
            List<SalaryComponents> salaryComponents = GetSalaryGroupComponentsByCTC(EmployeeId, CTCAnnually);

            CalculatedSalaryBreakupDetail calculatedSalaryBreakupDetail = null;
            foreach (var item in salaryComponents)
            {
                if (!string.IsNullOrEmpty(item.ComponentId))
                {
                    calculatedSalaryBreakupDetail = new CalculatedSalaryBreakupDetail();

                    calculatedSalaryBreakupDetail.ComponentId = item.ComponentId;
                    calculatedSalaryBreakupDetail.Formula = item.Formula;
                    calculatedSalaryBreakupDetail.ComponentName = item.ComponentFullName;
                    calculatedSalaryBreakupDetail.ComponentTypeId = item.ComponentTypeId;
                    calculatedSalaryBreakupDetail.FinalAmount = 0;
                    calculatedSalaryBreakupDetail.IsIncludeInPayslip = item.IncludeInPayslip;

                    calculatedSalaryBreakupDetails.Add(calculatedSalaryBreakupDetail);
                }
            }

            calculatedSalaryBreakupDetail = new CalculatedSalaryBreakupDetail
            {
                ComponentId = nameof(ComponentNames.Special),
                Formula = null,
                ComponentName = ComponentNames.Special,
                FinalAmount = 0,
                ComponentTypeId = 102,
                IsIncludeInPayslip = true
            };

            calculatedSalaryBreakupDetails.Add(calculatedSalaryBreakupDetail);

            calculatedSalaryBreakupDetail = new CalculatedSalaryBreakupDetail
            {
                ComponentId = nameof(ComponentNames.Gross),
                Formula = null,
                ComponentName = ComponentNames.Gross,
                FinalAmount = 0,
                ComponentTypeId = 100,
                IsIncludeInPayslip = true
            };

            calculatedSalaryBreakupDetails.Add(calculatedSalaryBreakupDetail);

            calculatedSalaryBreakupDetail = new CalculatedSalaryBreakupDetail
            {
                ComponentId = nameof(ComponentNames.CTC),
                Formula = null,
                ComponentName = ComponentNames.CTC,
                FinalAmount = 0,
                ComponentTypeId = 101,
                IsIncludeInPayslip = true
            };

            calculatedSalaryBreakupDetails.Add(calculatedSalaryBreakupDetail);

            return annualSalaryBreakups;
        }

        public dynamic GetSalaryBreakupByEmpIdService(long EmployeeId)
        {
            (EmployeeSalaryDetail completeSalaryBreakup, UserDetail userDetail) =
                _db.GetMulti<EmployeeSalaryDetail, UserDetail>(
                    Procedures.Employee_Salary_Detail_Get_By_Empid,
                    new
                    {
                        _currentSession.FinancialStartYear,
                        EmployeeId
                    });

            return new { completeSalaryBreakup, userDetail };
        }

        public SalaryGroup GetSalaryGroupByCTC(decimal CTC, long EmployeeId)
        {
            SalaryGroup salaryGroup = _db.Get<SalaryGroup>(Procedures.Salary_Group_Get_By_Ctc, new { CTC, EmployeeId });
            if (salaryGroup == null)
                throw new HiringBellException("Unable to get salary group. Please contact admin");
            return salaryGroup;
        }

        private decimal calculateExpressionUsingInfixDS(string expression, decimal declaredAmount)
        {
            _logger.LogInformation("Starting method: calculateExpressionUsingInfixDS");

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
            _logger.LogInformation("Leaving method: calculateExpressionUsingInfixDS");

            return _postfixToInfixConversion.evaluatePostfix(exp);
        }

        public List<SalaryComponents> GetBonusComponentsService()
        {
            List<SalaryComponents> result = _db.GetList<SalaryComponents>(Procedures.Salary_Components_Get);
            result = result.FindAll(x => x.IsAdHoc == true && x.AdHocId == (int)AdhocType.Bonus);
            return result;
        }

        public DataSet GetAllSalaryDetailService(FilterModel filterModel)
        {
            if (string.IsNullOrEmpty(filterModel.SearchString))
                filterModel.SearchString = $"1=1 and s.FinancialStartYear = {_currentSession.FinancialStartYear} and e.CompanyId = {_currentSession.CurrentUserDetail.CompanyId} and ";
            else
                filterModel.SearchString += $" and s.FinancialStartYear = {_currentSession.FinancialStartYear} and e.CompanyId = {_currentSession.CurrentUserDetail.CompanyId}";
            filterModel.CompanyId = _currentSession.CurrentUserDetail.CompanyId;

            var result = _db.FetchDataSet(Procedures.Employee_Salary_Detail_GetbyFilter, filterModel);
            result.Tables[0].TableName = "SalaryDetail";
            if (result.Tables[1].Rows.Count == 0)
                throw HiringBellException.ThrowBadRequest("Company setting not found. Please contact to admin.");

            result.Tables[1].TableName = "CompanySetting";
            return result;
        }

        public List<SalaryGroup> CloneSalaryGroupService(SalaryGroup salaryGroup)
        {
            if (salaryGroup.SalaryGroupId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid salary group selected");

            ValidateSalaryGroup(salaryGroup);

            SalaryGroup salaryGrp = _db.Get<SalaryGroup>(Procedures.Salary_Group_Get_If_Exists, new
            {
                salaryGroup.CompanyId,
                salaryGroup.MinAmount,
                salaryGroup.MaxAmount
            });

            if (salaryGrp != null)
                throw new HiringBellException("Salary group limit already exist");

            salaryGrp = _db.Get<SalaryGroup>(Procedures.Salary_Group_GetById, new { salaryGroup.SalaryGroupId });
            if (salaryGrp == null)
                throw new HiringBellException("Salary Group not exist.");
            else
            {
                if (string.IsNullOrEmpty(salaryGrp.SalaryComponents))
                    salaryGrp.SalaryComponents = "[]";

                salaryGrp.SalaryGroupId = 0;
                salaryGrp.GroupName = salaryGroup.GroupName;
                salaryGrp.GroupDescription = salaryGroup.GroupDescription;
                salaryGrp.MinAmount = salaryGroup.MinAmount;
                salaryGrp.MaxAmount = salaryGroup.MaxAmount;
                salaryGrp.AdminId = _currentSession.CurrentUserDetail.AdminId;
            }

            var result = _db.Execute<SalaryGroup>(Procedures.Salary_Group_Insupd, salaryGrp, true);
            if (string.IsNullOrEmpty(result))
                throw HiringBellException.ThrowBadRequest("Fail to insert or update.");

            List<SalaryGroup> value = GetSalaryGroupService(salaryGroup.CompanyId);
            return value;
        }

        public async Task GetEmployeeSalaryDetail(EmployeeCalculation employeeCalculation)
        {
            var ResultSet = _db.FetchDataSet(Procedures.Salary_Components_Group_By_Employeeid,
                new { employeeCalculation.EmployeeId });
            if (ResultSet == null || ResultSet.Tables.Count != 6)
                throw new HiringBellException("Unbale to get salary detail. Please contact to admin.");

            if (ResultSet.Tables[0].Rows.Count == 0)
                throw new HiringBellException($"Salary detail not found for employee Id: [{employeeCalculation.EmployeeId}]");

            if (ResultSet.Tables[1].Rows.Count == 0)
                throw new HiringBellException($"Employee company setting is not defined. Please contact to admin.");

            if (ResultSet.Tables[2].Rows.Count == 0)
                throw new Exception("Salary component are not defined, unable to perform calculation. Please contact to admin");

            if (ResultSet.Tables[3].Rows.Count == 0)
                throw new Exception("Surcharge data not found. Please contact to admin");

            if (ResultSet.Tables[4].Rows.Count == 0)
                throw new Exception("Professional tax data not found. Please contact to admin");

            if (ResultSet.Tables[5].Rows.Count == 0)
                throw new Exception("Company Shift data not found. Please contact to admin");

            employeeCalculation.salaryComponents = ResultSet.Tables[3].ToList<SalaryComponents>();
            employeeCalculation.employeeSalaryDetail = ResultSet.Tables[1].ToType<EmployeeSalaryDetail>();
            employeeCalculation.Doj = employeeCalculation.employeeSalaryDetail.DateOfJoining;
            employeeCalculation.CTC = employeeCalculation.employeeSalaryDetail.CTC;
            //employeeCalculation.salaryGroup = ResultSet.Tables[0].ToType<SalaryGroup>();
            employeeCalculation.surchargeSlabs = ResultSet.Tables[4].ToList<SurChargeSlab>();
            employeeCalculation.ptaxSlab = ResultSet.Tables[5].ToList<PTaxSlab>();
            employeeCalculation.shiftDetail = ResultSet.Tables[6].ToType<ShiftDetail>();

            //if (employeeCalculation.salaryGroup.SalaryGroupId == 1)
            //    employeeCalculation.employeeDeclaration.DefaultSlaryGroupMessage = $"Salary group for salary {employeeCalculation.CTC} not found. Default salary group for all calculation. For any query please contact to admin.";

            employeeCalculation.companySetting = ResultSet.Tables[2].ToType<CompanySetting>();
            employeeCalculation.PayrollStartDate = new DateTime(employeeCalculation.companySetting.FinancialYear,
                employeeCalculation.companySetting.DeclarationStartMonth, 1, 0, 0, 0, DateTimeKind.Utc);

            employeeCalculation.employeeSalaryDetail.FinancialStartYear = employeeCalculation.companySetting.FinancialYear;

            //if (string.IsNullOrEmpty(employeeCalculation.salaryGroup.SalaryComponents))
            //    throw new HiringBellException($"Salary components not found for salary: [{employeeCalculation.employeeSalaryDetail.CTC}]");

            //employeeCalculation.salaryGroup.GroupComponents = JsonConvert
            //    .DeserializeObject<List<SalaryComponents>>(employeeCalculation.salaryGroup.SalaryComponents);

            if (employeeCalculation.employeeDeclaration != null && employeeCalculation.employeeDeclaration.SalaryDetail != null)
                employeeCalculation.employeeDeclaration.SalaryDetail.CTC = employeeCalculation.CTC;

            var numOfYears = employeeCalculation.Doj.Year - employeeCalculation.companySetting.FinancialYear;
            if ((numOfYears == 0 || numOfYears == 1)
                &&
                employeeCalculation.Doj.Month >= employeeCalculation.companySetting.DeclarationStartMonth
                &&
                employeeCalculation.Doj.Month <= employeeCalculation.companySetting.DeclarationEndMonth)
                employeeCalculation.IsFirstYearDeclaration = true;
            else
                employeeCalculation.IsFirstYearDeclaration = false;

            await Task.CompletedTask;
        }

        public async Task<dynamic> GetSalaryGroupAndComponentService()
        {
            (List<SalaryGroup> salaryGroups, List<SalaryComponents> salaryComponents) = _db.GetList<SalaryGroup, SalaryComponents>(Procedures.SALARY_GROUP_AND_COMPONENTS_GET, new
            {
                _currentSession.CurrentUserDetail.CompanyId
            });

            if (salaryGroups.Count == 0)
                throw HiringBellException.ThrowBadRequest("Salary group not found. Please contact to admin");

            if (salaryComponents.Count == 0)
                throw HiringBellException.ThrowBadRequest("Salary components not found. Please contact to admin");

            var salaryGroup = salaryGroups[0];
            salaryGroup.GroupComponents = JsonConvert.DeserializeObject<List<SalaryComponents>>(salaryGroup.SalaryComponents);

            return await Task.FromResult(new { SalaryGroup = salaryGroup, SalaryComponents = salaryComponents });
        }
    }
}
