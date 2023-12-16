using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EMailService.Modal;
using ems_CoreService.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ModalLayer;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using ModalLayer.Modal.Leaves;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class LeaveService : ILeaveService
    {
        private readonly IDb _db;
        private readonly CurrentSession _currentSession;
        private readonly ICommonService _commonService;
        private readonly ILeaveCalculation _leaveCalculation;
        private readonly KafkaNotificationService _kafkaNotificationService;
        private readonly ILogger<LeaveService> _logger;
        private readonly ITimezoneConverter _timezoneConverter;

        public LeaveService(IDb db,
            CurrentSession currentSession,
            ICommonService commonService,
            ILeaveCalculation leaveCalculation,
            KafkaNotificationService kafkaNotificationService,
            ILogger<LeaveService> logger,
            ITimezoneConverter timezoneConverter)
        {
            _db = db;
            _currentSession = currentSession;
            _commonService = commonService;
            _leaveCalculation = leaveCalculation;
            _kafkaNotificationService = kafkaNotificationService;
            _logger = logger;
            _timezoneConverter = timezoneConverter;
        }

        public List<LeavePlan> AddLeavePlansService(LeavePlan leavePlan)
        {
            List<LeavePlan> leavePlans = null;
            if (string.IsNullOrEmpty(leavePlan.PlanName))
                throw HiringBellException.ThrowBadRequest("Leave plan name is null or empty");

            if (leavePlan.PlanStartCalendarDate == null)
                throw HiringBellException.ThrowBadRequest("Leave plan start calendar date is null or empty");

            if (leavePlan.LeavePlanId > 0)
            {
                leavePlans = _db.GetList<LeavePlan>(Procedures.Leave_Plans_Get);
                if (leavePlans.Count <= 0)
                    throw new HiringBellException("Invalid leave plan.");

                var result = leavePlans.Find(x => x.LeavePlanId == leavePlan.LeavePlanId);
                if (result == null)
                    throw new HiringBellException("Invalid leave plan.");

                result.PlanName = leavePlan.PlanName;
                result.PlanDescription = leavePlan.PlanDescription;
                result.IsShowLeavePolicy = leavePlan.IsShowLeavePolicy;
                result.IsUploadedCustomLeavePolicy = leavePlan.IsUploadedCustomLeavePolicy;
                result.PlanStartCalendarDate = leavePlan.PlanStartCalendarDate;
                result.IsDefaultPlan = leavePlan.IsDefaultPlan;
                leavePlan = result;
            }
            else
            {
                leavePlan.CompanyId = _currentSession.CurrentUserDetail.CompanyId;
                leavePlan.AssociatedPlanTypes = "[]";
            }
            var value = _db.Execute<LeavePlan>(Procedures.Leave_Plan_Insupd, leavePlan, true);
            if (string.IsNullOrEmpty(value))
                throw new HiringBellException("Unable to add or update leave plan");

            leavePlans = _db.GetList<LeavePlan>(Procedures.Leave_Plans_Get);
            return leavePlans;
        }

        public List<LeavePlanType> AddLeavePlanTypeService(LeavePlanType leavePlanType)
        {
            List<LeavePlanType> leavePlanTypes = default(List<LeavePlanType>);
            BuildConfigurationDetailObject(leavePlanType);
            LeavePlanTypeValidation(leavePlanType);

            string result = _db.Execute<LeavePlanType>(Procedures.Leave_Plans_Type_Insupd, new
            {
                leavePlanType.IsPaidLeave,
                leavePlanType.MaxLeaveLimit,
                leavePlanType.IsSickLeave,
                leavePlanType.IsStatutoryLeave,
                leavePlanType.LeavePlanTypeId,
                leavePlanType.ShowDescription,
                leavePlanType.LeavePlanCode,
                leavePlanType.PlanName,
                leavePlanType.PlanDescription,
                leavePlanType.IsMale,
                leavePlanType.IsMarried,
                leavePlanType.IsRestrictOnGender,
                leavePlanType.IsRestrictOnMaritalStatus,
                Reasons = leavePlanType.Reasons,
                PlanConfigurationDetail = leavePlanType.PlanConfigurationDetail,
                AdminId = _currentSession.CurrentUserDetail.UserId
            }, true);

            if (ApplicationConstants.IsExecuted(result))
                leavePlanTypes = _db.GetList<LeavePlanType>(Procedures.Leave_Plans_Type_Get);

            return leavePlanTypes;
        }

        public List<LeavePlan> GetLeavePlansService(FilterModel filterModel)
        {
            List<LeavePlan> leavePlans = _db.GetList<LeavePlan>(Procedures.Leave_Plans_Get);
            if (leavePlans == null)
                throw new HiringBellException("Leave plans not found.");

            leavePlans.ForEach(item =>
            {
                if (!string.IsNullOrEmpty(item.AssociatedPlanTypes))
                {
                    var planTypes = JsonConvert.DeserializeObject<List<LeavePlanType>>(item.AssociatedPlanTypes);
                    if (planTypes != null)
                    {
                        Parallel.ForEach(planTypes, type =>
                        {
                            type.LeavePlanId = item.LeavePlanId;
                        });
                    }

                    item.AssociatedPlanTypes = JsonConvert.SerializeObject(planTypes);
                }
            });

            return leavePlans;
        }

        private void BuildConfigurationDetailObject(LeavePlanType leavePlanType)
        {
            LeavePlanConfiguration leavePlanConfiguration = new LeavePlanConfiguration();
            if (!_commonService.IsEmptyJson(leavePlanType.PlanConfigurationDetail))
                leavePlanConfiguration = JsonConvert.DeserializeObject<LeavePlanConfiguration>(leavePlanType.PlanConfigurationDetail);

            if (leavePlanConfiguration.leaveDetail == null)
                leavePlanConfiguration.leaveDetail = new LeaveDetail();

            if (leavePlanConfiguration.leaveAccrual == null)
                leavePlanConfiguration.leaveAccrual = new LeaveAccrual();

            if (leavePlanConfiguration.leaveApplyDetail == null)
                leavePlanConfiguration.leaveApplyDetail = new LeaveApplyDetail();

            if (leavePlanConfiguration.leaveEndYearProcessing == null)
                leavePlanConfiguration.leaveEndYearProcessing = new LeaveEndYearProcessing();

            if (leavePlanConfiguration.leaveHolidaysAndWeekoff == null)
                leavePlanConfiguration.leaveHolidaysAndWeekoff = new LeaveHolidaysAndWeekoff();

            if (leavePlanConfiguration.leavePlanRestriction == null)
                leavePlanConfiguration.leavePlanRestriction = new LeavePlanRestriction();

            if (leavePlanConfiguration.leaveApproval == null)
                leavePlanConfiguration.leaveApproval = new LeaveApproval();

            leavePlanType.PlanConfigurationDetail = JsonConvert.SerializeObject(leavePlanConfiguration);
        }

        public List<LeavePlanType> UpdateLeavePlanTypeService(int leavePlanTypeId, LeavePlanType leavePlanType)
        {
            if (leavePlanType.LeavePlanTypeId <= 0)
                throw new HiringBellException("Leave plan type id not found. Please add one plan first.");

            LeavePlanTypeValidation(leavePlanType);
            LeavePlanType record = _db.Get<LeavePlanType>(Procedures.Leave_Plans_Type_GetbyId, new { LeavePlanTypeId = leavePlanTypeId });

            if (record == null || record.LeavePlanTypeId != leavePlanTypeId)
                throw new HiringBellException("Trying to udpate invalid leave plan type");

            record.IsPaidLeave = leavePlanType.IsPaidLeave;
            record.AvailableLeave = leavePlanType.AvailableLeave;
            record.IsSickLeave = leavePlanType.IsSickLeave;
            record.MaxLeaveLimit = leavePlanType.MaxLeaveLimit;
            record.LeavePlanCode = leavePlanType.LeavePlanCode;
            record.AdminId = _currentSession.CurrentUserDetail.UserId;
            record.IsMale = leavePlanType.IsMale;
            record.IsMarried = leavePlanType.IsMarried;
            record.IsRestrictOnGender = leavePlanType.IsRestrictOnGender;
            record.IsRestrictOnMaritalStatus = leavePlanType.IsRestrictOnMaritalStatus;
            record.IsStatutoryLeave = leavePlanType.IsStatutoryLeave;
            record.PlanDescription = leavePlanType.PlanDescription;

            return this.AddLeavePlanTypeService(record);
        }

        private void LeavePlanTypeValidation(LeavePlanType leavePlanType)
        {
            if (string.IsNullOrEmpty(leavePlanType.PlanDescription))
                throw HiringBellException.ThrowBadRequest("Leave plan type description is null or empty");

            if (string.IsNullOrEmpty(leavePlanType.PlanName))
                throw HiringBellException.ThrowBadRequest("Leave plan type name is null or empty");

            if (string.IsNullOrEmpty(leavePlanType.LeavePlanCode))
                throw HiringBellException.ThrowBadRequest("Leave plan type code is null or empty");

            if (!leavePlanType.IsPaidLeave)
            {
                if (leavePlanType.IsSickLeave)
                    throw HiringBellException.ThrowBadRequest("Sick leave must be disable");

                if (leavePlanType.IsStatutoryLeave)
                    throw HiringBellException.ThrowBadRequest("Statutory leave must be disable");
            }

            if (leavePlanType.IsRestrictOnGender)
            {
                if (leavePlanType.IsMale == null)
                    throw HiringBellException.ThrowBadRequest("Please select gender first");
            }

            if (leavePlanType.IsRestrictOnMaritalStatus)
            {
                if (leavePlanType.IsMarried == null)
                    throw HiringBellException.ThrowBadRequest("Please select restrict to employees");
            }
        }

        public string AddUpdateLeaveQuotaService(LeaveDetail leaveDetail)
        {
            string result = _db.Execute<LeaveDetail>(Procedures.Leave_Detail_InsUpdate, leaveDetail, true);
            return result;
        }

        public LeavePlanConfiguration GetLeaveTypeDetailByIdService(int leavePlanTypeId)
        {
            LeavePlanType leavePlanType = _db.Get<LeavePlanType>(Procedures.Leave_Plans_Type_GetbyId, new { LeavePlanTypeId = leavePlanTypeId });
            if (leavePlanType == null)
                throw new HiringBellException("Invalid plan id supplied");

            LeavePlanConfiguration leavePlanConfiguration = JsonConvert.DeserializeObject<LeavePlanConfiguration>(leavePlanType.PlanConfigurationDetail);
            if (leavePlanConfiguration == null)
                leavePlanConfiguration = new LeavePlanConfiguration();

            return leavePlanConfiguration;
        }

        public List<LeavePlanType> GetLeaveTypeFilterService()
        {
            List<LeavePlanType> leavePlanTypes = _db.GetList<LeavePlanType>(Procedures.Leave_Plans_Type_Get);
            return leavePlanTypes;
        }

        public async Task<LeavePlan> LeavePlanUpdateTypes(int leavePlanId, List<int> LeavePlanTypeId)
        {
            if (leavePlanId <= 0)
                throw new HiringBellException("Invalid leave plan id.");

            if (LeavePlanTypeId.Count == 0)
                throw new HiringBellException("Select at least one plan");

            LeavePlanTypeId.ForEach(x =>
            {
                if (x == 0)
                    throw new HiringBellException("Invalid leave plan type selected");
            });

            (List<LeavePlanTypeBrief> leavePlanTypes, List<LeavePlan> leavePlans) = _db.GetList<LeavePlanTypeBrief, LeavePlan>(Procedures.Leave_Plan_And_Type_Get_By_Ids_Json, new
            {
                LeavePlanId = leavePlanId,
                LeavePlanTypeId = JsonConvert.SerializeObject(LeavePlanTypeId)
            });
            LeavePlan leavePlan = leavePlans[0];
            if (leavePlan == null)
                throw new HiringBellException("Invalid leave plan selected.");


            foreach (LeavePlanTypeBrief leavePlanType in leavePlanTypes)
            {
                leavePlanType.LeavePlanId = leavePlanId;
            }

            leavePlan.AssociatedPlanTypes = JsonConvert.SerializeObject(leavePlanTypes);

            var result = await _db.ExecuteAsync(Procedures.Leave_Plan_Insupd, leavePlan, true);
            if (result.rowsEffected != 1 || string.IsNullOrEmpty(result.statusMessage))
                throw new HiringBellException("Unable to add leave type. Please contact to admin.");
            return leavePlan;
        }

        public List<LeavePlan> SetDefaultPlanService(int LeavePlanId, LeavePlan leavePlan)
        {
            List<LeavePlan> leavePlans = null;
            if (leavePlan.LeavePlanId <= 0)
                throw new HiringBellException("Invalid leave plan selected.");

            var value = _db.Execute<LeavePlan>(Procedures.Leave_Plan_Insupd, new
            {
                leavePlan.LeavePlanId,
                leavePlan.IsDefaultPlan
            }, true);
            if (string.IsNullOrEmpty(value))
                throw new HiringBellException("Unable to add or update leave plan");

            leavePlans = _db.GetList<LeavePlan>(Procedures.Leave_Plans_Get);
            return leavePlans;
        }

        public string LeaveRquestManagerActionService(LeaveRequestNotification notification, ItemStatus status)
        {
            string message = string.Empty;
            var requestNotification = _db.Get<LeaveRequestNotification>(Procedures.Leave_Request_Notification_Get_ById, new
            {
                notification.LeaveRequestNotificationId
            });

            if (requestNotification != null)
            {
                List<CompleteLeaveDetail> completeLeaveDetail = JsonConvert
                  .DeserializeObject<List<CompleteLeaveDetail>>(requestNotification.LeaveDetail);

                if (completeLeaveDetail != null)
                {
                    var singleLeaveDetail = completeLeaveDetail.Find(x =>
                        requestNotification.FromDate.Subtract(x.LeaveFromDay).TotalDays == 0 &&
                        requestNotification.ToDate.Subtract(x.LeaveToDay).TotalDays == 0
                    );

                    if (singleLeaveDetail != null)
                    {
                        singleLeaveDetail.LeaveStatus = (int)status;
                        singleLeaveDetail.RespondedBy = _currentSession.CurrentUserDetail.UserId;
                        requestNotification.LeaveDetail = JsonConvert.SerializeObject(
                            (from n in completeLeaveDetail
                             select new
                             {
                                 Reason = n.Reason,
                                 Session = n.Session,
                                 AssignTo = n.AssignTo,
                                 LeaveType = n.LeaveTypeId,
                                 NumOfDays = n.NumOfDays,
                                 ProjectId = n.ProjectId,
                                 UpdatedOn = n.UpdatedOn,
                                 EmployeeId = n.EmployeeId,
                                 LeaveToDay = n.LeaveToDay,
                                 LeaveStatus = n.LeaveStatus,
                                 RequestedOn = n.RequestedOn,
                                 RespondedBy = n.RespondedBy,
                                 EmployeeName = n.EmployeeName,
                                 LeaveFromDay = n.LeaveFromDay
                             })
                            );
                    }
                    else
                    {
                        throw new HiringBellException("Error");
                    }
                }
                else
                {
                    throw new HiringBellException("Error");
                }

                if (requestNotification != null)
                {
                    requestNotification.LastReactedOn = DateTime.UtcNow;
                    requestNotification.RequestStatusId = notification.RequestStatusId;
                    message = _db.Execute<LeaveRequestNotification>(Procedures.Leave_Request_Notification_InsUpdate, new
                    {
                        requestNotification.LeaveRequestNotificationId,
                        requestNotification.LeaveRequestId,
                        requestNotification.UserMessage,
                        requestNotification.EmployeeId,
                        requestNotification.ReportingManagerId,
                        requestNotification.ProjectId,
                        requestNotification.ProjectName,
                        requestNotification.FromDate,
                        requestNotification.ToDate,
                        requestNotification.NumOfDays,
                        requestNotification.RequestStatusId,
                        requestNotification.LeaveTypeId,
                        requestNotification.FeedBackMessage,
                        requestNotification.LastReactedOn,
                        requestNotification.LeaveDetail
                    }, true);
                }
            }
            return message;
        }

        private void ValidateRequestModal(LeaveRequestModal leaveRequestModal)
        {
            if (leaveRequestModal == null)
                throw new HiringBellException("Invalid request detail sumitted.");

            if (leaveRequestModal.EmployeeId <= 0)
                throw new HiringBellException("Invalid Employee Id submitted.");

            if (leaveRequestModal.LeaveFromDay == null || leaveRequestModal.LeaveToDay == null)
                throw new HiringBellException("Invalid From and To date passed.");

            if (DateTime.UtcNow.Subtract(leaveRequestModal.LeaveFromDay).TotalDays > 0)
                throw new HiringBellException("You don't take any action on past date leave");
        }

        public async Task<dynamic> ApplyLeaveService(LeaveRequestModal leaveRequestModal, IFormFileCollection fileCollection, List<Files> fileDetail)
        {
            this.ValidateRequestModal(leaveRequestModal);
            var leaveCalculationModal = await _leaveCalculation.CheckAndApplyForLeave(leaveRequestModal, fileCollection, fileDetail);

            LeaveTemplateModel leaveTemplateModel = null;
            if (!leaveCalculationModal.IsEmailNotificationPasued)
            {
                leaveTemplateModel = new LeaveTemplateModel
                {
                    kafkaServiceName = KafkaServiceName.Leave,
                    RequestType = nameof(RequestType.Leave),
                    ActionType = nameof(ItemStatus.Submitted),
                    FromDate = _timezoneConverter.ToTimeZoneDateTime(leaveRequestModal.LeaveFromDay, _currentSession.TimeZone),
                    ToDate = _timezoneConverter.ToTimeZoneDateTime(leaveRequestModal.LeaveToDay, _currentSession.TimeZone),
                    Message = leaveRequestModal.Reason,
                    ManagerName = _currentSession.CurrentUserDetail.ManagerName,
                    DeveloperName = _currentSession.CurrentUserDetail.FullName,
                    CompanyName = _currentSession.CurrentUserDetail.CompanyName,
                    DayCount = (int)leaveRequestModal.LeaveToDay.Subtract(leaveRequestModal.LeaveFromDay).TotalDays + 1,
                };
            }

            if (leaveCalculationModal.IsLeaveAutoApproval)
            {
                leaveTemplateModel = new LeaveTemplateModel
                {
                    kafkaServiceName = KafkaServiceName.Leave,
                    RequestType = nameof(RequestType.Leave),
                    ActionType = "Auto Approved",
                    FromDate = _timezoneConverter.ToTimeZoneDateTime(leaveRequestModal.LeaveFromDay, _currentSession.TimeZone),
                    ToDate = _timezoneConverter.ToTimeZoneDateTime(leaveRequestModal.LeaveToDay, _currentSession.TimeZone),
                    ManagerName = _currentSession.CurrentUserDetail.ManagerName,
                    DeveloperName = _currentSession.CurrentUserDetail.FullName,
                    CompanyName = _currentSession.CurrentUserDetail.CompanyName,
                    DayCount = (int)leaveRequestModal.LeaveToDay.Subtract(leaveRequestModal.LeaveFromDay).TotalDays + 1,
                };
            }
            leaveTemplateModel.ToAddress = new List<string>();
            leaveCalculationModal.ReporterEmail.ForEach(x =>
            {
                leaveTemplateModel.ToAddress.Add(x);
            });

            _logger.LogInformation($"Call to kafka: {leaveCalculationModal.ReporterEmail.ToString()}");
            await _kafkaNotificationService.SendEmailNotification(leaveTemplateModel);
            var companyHoliday = _db.GetList<Calendar>(Procedures.Company_Calendar_Get_By_Company, new { CompanyId = _currentSession.CurrentUserDetail.CompanyId });
            var monthlyLeaveData = new Dictionary<string, decimal>();
            for (int i = 1; i <= 12; i++)
            {
                var leaveCurrentMonth = leaveCalculationModal.lastAppliedLeave.FindAll(x => x.FromDate.Month == i && x.ToDate.Month == i);
                var leaveCurrentAndNextMonth = leaveCalculationModal.lastAppliedLeave.Find(x => x.FromDate.Month == i && x.ToDate.Month == i + 1);
                var leavePrevAndCurrentMonth = leaveCalculationModal.lastAppliedLeave.Find(x => x.FromDate.Month == i - 1 && x.ToDate.Month == i);

                string monthName = new DateTime(2023, i, 1).ToString("MMM");
                decimal totalDays = 0M;
                if (leaveCurrentMonth != null && leaveCurrentMonth.Count > 0)
                    totalDays += leaveCurrentMonth.Sum(x => x.NumOfDays);

                if (leaveCurrentAndNextMonth != null)
                {
                    var lastDateOfMonth = new DateTime(2023, i, 1).AddMonths(1).AddDays(-1);
                    totalDays += (decimal)lastDateOfMonth.Subtract(leaveCurrentAndNextMonth.FromDate).TotalDays + 1;
                }

                if (leavePrevAndCurrentMonth != null)
                    totalDays += (decimal)leavePrevAndCurrentMonth.FromDate.Subtract(new DateTime(2023, i, 1)).TotalDays + 1;

                monthlyLeaveData.Add(monthName, totalDays);

            }

            return new
            {
                LeaveTypeBriefs = leaveCalculationModal.leaveTypeBriefs,
                EmployeeLeaveDetail = leaveCalculationModal.leaveRequestDetail,
                Employee = leaveCalculationModal.employee,
                LeaveNotificationDetail = leaveCalculationModal.lastAppliedLeave,
                MonthlyLeaveData = monthlyLeaveData
            };
        }

        public async Task RunAccrualByEmployeeService(long EmployeeId)
        {
            await _leaveCalculation.RunAccrualCycleByEmployee(EmployeeId);
        }

        private async Task<LeaveCalculationModal> GetLatestLeaveDetail(long employeeId)
        {
            if (employeeId < 0)
                throw new HiringBellException("Invalid employee id.");

            LeaveCalculationModal leaveCalculationModal = await _leaveCalculation.GetLeaveDetailService(employeeId);
            if (leaveCalculationModal == null)
                throw new HiringBellException("Unable to calculate leave balance detail. Please contact to admin.");

            return leaveCalculationModal;
        }

        public async Task<dynamic> GetEmployeeLeaveDetail(LeaveRequestModal leaveRequestModal)
        {
            if (leaveRequestModal.EmployeeId <= 0)
                throw new HiringBellException("Invalid Employee Id submitted.");

            var leaveCalculationModal = await GetLatestLeaveDetail(leaveRequestModal.EmployeeId);

            //if (!string.IsNullOrEmpty(leaveCalculationModal.leaveRequestDetail.LeaveDetail))
            //    this.UpdateLeavePlanDetail(leaveCalculationModal);
            var companyHoliday = _db.GetList<Calendar>(Procedures.Company_Calendar_Get_By_Company, new { CompanyId = _currentSession.CurrentUserDetail.CompanyId });
            var monthlyLeaveData = new Dictionary<string, decimal>();
            for (int i = 1; i <= 12; i++)
            {
                var leaveCurrentMonth = leaveCalculationModal.lastAppliedLeave.FindAll(x => x.FromDate.Month == i && x.ToDate.Month == i);
                var leaveCurrentAndNextMonth = leaveCalculationModal.lastAppliedLeave.Find(x => x.FromDate.Month == i && x.ToDate.Month == i + 1);
                var leavePrevAndCurrentMonth = leaveCalculationModal.lastAppliedLeave.Find(x => x.FromDate.Month == i - 1 && x.ToDate.Month == i);

                string monthName = new DateTime(2023, i, 1).ToString("MMM");
                decimal totalDays = 0M;
                if (leaveCurrentMonth != null && leaveCurrentMonth.Count > 0)
                    totalDays += leaveCurrentMonth.Sum(x => x.NumOfDays);

                if (leaveCurrentAndNextMonth != null)
                {
                    var lastDateOfMonth = new DateTime(2023, i, 1).AddMonths(1).AddDays(-1);
                    totalDays += (decimal)lastDateOfMonth.Subtract(leaveCurrentAndNextMonth.FromDate).TotalDays + 1;
                }

                if (leavePrevAndCurrentMonth != null)
                    totalDays += (decimal)leavePrevAndCurrentMonth.FromDate.Subtract(new DateTime(2023, i, 1)).TotalDays + 1;

                monthlyLeaveData.Add(monthName, totalDays);

            }

            return new
            {
                LeaveTypeBriefs = leaveCalculationModal.leaveTypeBriefs,
                Employee = leaveCalculationModal.employee,
                CompanyHoliday = companyHoliday,
                ShiftDetail = leaveCalculationModal.shiftDetail,
                LeaveNotificationDetail = leaveCalculationModal.lastAppliedLeave.OrderByDescending(x => x.CreatedOn).ToList(),
                MonthlyLeaveData = monthlyLeaveData
            };
        }

        public DataSet GetLeaveAttachmentService(string FileIds)
        {
            if (string.IsNullOrEmpty(FileIds) || FileIds == "[]")
                throw HiringBellException.ThrowBadRequest("File ids are null or empty");

            var result = _db.FetchDataSet(Procedures.User_Files_Get_Byids_Json, new { UserFileId = FileIds });
            return result;
        }

        public DataSet GetLeaveAttachByMangerService(LeaveRequestNotification leaveRequestNotification)
        {
            if (leaveRequestNotification.LeaveRequestNotificationId < 0)
                throw HiringBellException.ThrowBadRequest("Invalid leave request selected");

            (LeaveRequestDetail leaveRequestDetail, LeavePlanType leavePlanType) = _db.GetMulti<LeaveRequestDetail, LeavePlanType>(Procedures.Employee_Leave_Request_GetById, new { LeaveRequestNotificationId = leaveRequestNotification.LeaveRequestNotificationId });
            var completeLeave = JsonConvert.DeserializeObject<List<CompleteLeaveDetail>>(leaveRequestDetail.LeaveDetail);
            var slectedleave = completeLeave.Find(x => x.RecordId == leaveRequestNotification.RecordId);
            if (slectedleave != null && !string.IsNullOrEmpty(slectedleave.FileIds) && slectedleave.FileIds != "[]")
            {
                var fileids = slectedleave.FileIds;
                return this.GetLeaveAttachmentService(fileids);
            }
            return null;
        }

        public Leave GetLeaveDetailByEmpIdService(long employeeId)
        {
            if (employeeId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid employee selected");

            var result = _db.Get<Leave>(Procedures.Employee_Leave_Request_By_Empid, new { EmployeeId = employeeId });
            if (result == null)
                throw HiringBellException.ThrowBadRequest("Leave detail not found. Please contact to admin");

            if (string.IsNullOrEmpty(result.LeaveQuotaDetail) || result.LeaveQuotaDetail == "[]")
                throw HiringBellException.ThrowBadRequest("Leave quota detail not found. Please contact to admin");

            return result;
        }

        public async Task<string> AdjustLOPAsLeaveService(LOPAdjustmentDetail lOPAdjustmentDetail)
        {
            validateLOPAdjustmentDetail(lOPAdjustmentDetail);
            List<int> leaveDetails = new List<int>();
            string result = string.Empty;

            var leaveCalculationModal = LoadCalculationData(lOPAdjustmentDetail.EmployeeId);
            var leaveTypeBriefs = JsonConvert.DeserializeObject<List<LeaveTypeBrief>>(leaveCalculationModal.leaveRequestDetail.LeaveQuotaDetail);
            var availableLeave = leaveTypeBriefs.Find(x => x.LeavePlanTypeId == lOPAdjustmentDetail.LeaveTypeId);

            if (lOPAdjustmentDetail.LOPAdjusment > availableLeave.AvailableLeaves)
                throw HiringBellException.ThrowBadRequest("LOP adjustment is greater than leave quota");

            if (leaveCalculationModal.leaveRequestDetail.LeaveDetail != null && leaveCalculationModal.leaveRequestDetail.LeaveDetail != "[]")
                leaveDetails = JsonConvert.DeserializeObject<List<int>>(leaveCalculationModal.leaveRequestDetail.LeaveDetail);

            lOPAdjustmentDetail.BlockedDates.ForEach(x =>
            {
                result = _db.Execute<LeaveRequestNotification>(Procedures.Leave_Request_Notification_InsUpdate, new
                {
                    LeaveRequestNotificationId = 0,
                    LeaveRequestId = leaveCalculationModal.leaveRequestDetail.LeaveRequestId,
                    UserMessage = lOPAdjustmentDetail.Comment,
                    lOPAdjustmentDetail.EmployeeId,
                    ReportingManagerId = leaveCalculationModal.employee.ReportingManagerId,
                    ProjectId = 0,
                    ProjectName = string.Empty,
                    FromDate = x,
                    ToDate = x,
                    NumOfDays = 1M,
                    RequestStatusId = (int)ItemStatus.Approved,
                    NoOfApprovalsRequired = 0,
                    ReporterDetail = ApplicationConstants.EmptyJsonArray,
                    FileIds = ApplicationConstants.EmptyJsonArray,
                    FeedBack = ApplicationConstants.EmptyJsonArray,
                    LeaveTypeName = lOPAdjustmentDetail.LeavePlanName,
                    AutoActionAfterDays = 0,
                    IsAutoApprovedEnabled = false,
                    leaveCalculationModal.LeaveTypeId,
                    AdminId = _currentSession.CurrentUserDetail.UserId
                }, true);

                if (string.IsNullOrEmpty(result))
                    throw new HiringBellException("fail to insert or update leave notification detail");
                leaveDetails.Add(int.Parse(result));

                var leaveTemplateModel = new LeaveTemplateModel
                {
                    kafkaServiceName = KafkaServiceName.Leave,
                    RequestType = nameof(RequestType.Leave),
                    ActionType = nameof(ItemStatus.Approved),
                    FromDate = _timezoneConverter.ToTimeZoneDateTime(x, _currentSession.TimeZone),
                    ToDate = _timezoneConverter.ToTimeZoneDateTime(x, _currentSession.TimeZone),
                    Message = lOPAdjustmentDetail.Comment,
                    ManagerName = _currentSession.CurrentUserDetail.FullName,
                    DeveloperName = leaveCalculationModal.employee.FirstName + " " + leaveCalculationModal.employee.LastName,
                    CompanyName = _currentSession.CurrentUserDetail.CompanyName,
                    DayCount = 1,
                    ToAddress = new List<string> { leaveCalculationModal.employee.Email }
                };

                _ = Task.Run(() => _kafkaNotificationService.SendEmailNotification(leaveTemplateModel));
            });

            availableLeave.AvailableLeaves = availableLeave.AvailableLeaves - lOPAdjustmentDetail.BlockedDates.Count;
            leaveCalculationModal.leaveTypeBriefs = leaveTypeBriefs;

            result = _db.Execute<LeaveRequestDetail>(Procedures.Employee_Leave_Request_InsUpdate, new
            {
                leaveCalculationModal.leaveRequestDetail.LeaveRequestId,
                lOPAdjustmentDetail.EmployeeId,
                LeaveDetail = JsonConvert.SerializeObject(leaveDetails),
                Year = leaveCalculationModal.leaveRequestDetail.Year,
                IsPending = false,
                AvailableLeaves = 0,
                TotalLeaveApplied = 0,
                TotalApprovedLeave = 0,
                TotalLeaveQuota = leaveCalculationModal.leaveRequestDetail.TotalLeaveQuota,
                LeaveQuotaDetail = JsonConvert.SerializeObject(leaveTypeBriefs)
            }, true);

            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("fail to insert or update leave detail");

            return await Task.FromResult("Successfully");
        }

        private void validateLOPAdjustmentDetail(LOPAdjustmentDetail lOPAdjustmentDetail)
        {
            if (lOPAdjustmentDetail.BlockedDates.Count > lOPAdjustmentDetail.LOPAdjusment)
                lOPAdjustmentDetail.BlockedDates = lOPAdjustmentDetail.BlockedDates.Take(lOPAdjustmentDetail.LOPAdjusment).ToList();

            if (lOPAdjustmentDetail.EmployeeId <= 0)
                throw HiringBellException.ThrowBadRequest("Employee id noyt found.");

            if (lOPAdjustmentDetail.LeaveTypeId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid leave type selected");

            if (lOPAdjustmentDetail.ActualLOP < lOPAdjustmentDetail.LOPAdjusment)
                throw HiringBellException.ThrowBadRequest("LOP adjustment is greater tha actual LOP");

            if (lOPAdjustmentDetail.BlockedDates == null || lOPAdjustmentDetail.BlockedDates.Count == 0)
                throw HiringBellException.ThrowBadRequest("Blocked dates are not found");

            if (lOPAdjustmentDetail.BlockedDates.Count != lOPAdjustmentDetail.LOPAdjusment)
                throw HiringBellException.ThrowBadRequest("Blocked dates are more than lop adjustment days");
        }

        private LeaveCalculationModal LoadCalculationData(long EmployeeId)
        {
            LeaveCalculationModal leaveCalculationModal = new LeaveCalculationModal();
            var ds = _db.FetchDataSet(Procedures.Leave_Plan_Calculation_Get, new
            {
                EmployeeId,
                _currentSession.CurrentUserDetail.ReportingManagerId,
                IsActive = 1,
                Year = DateTime.UtcNow.Year
            }, false);

            if (ds != null && ds.Tables.Count == 8)
            {
                if (ds.Tables[0].Rows.Count == 0 || ds.Tables[1].Rows.Count == 0)
                    throw new HiringBellException("Fail to get employee related details. Please contact to admin.");

                leaveCalculationModal.employee = Converter.ToType<Employee>(ds.Tables[0]);
                if (string.IsNullOrEmpty(leaveCalculationModal.employee.LeaveTypeBriefJson))
                    throw HiringBellException.ThrowBadRequest("Unable to get employee leave detail. Please contact to admin.");

                leaveCalculationModal.leaveTypeBriefs = JsonConvert.DeserializeObject<List<LeaveTypeBrief>>(leaveCalculationModal.employee.LeaveTypeBriefJson);
                if (leaveCalculationModal.leaveTypeBriefs.Count == 0)
                    throw HiringBellException.ThrowBadRequest("Unable to get employee leave detail. Please contact to admin.");

                leaveCalculationModal.shiftDetail = Converter.ToType<ShiftDetail>(ds.Tables[5]);
                leaveCalculationModal.leavePlanTypes = Converter.ToList<LeavePlanType>(ds.Tables[1]);
                leaveCalculationModal.leaveRequestDetail = Converter.ToType<LeaveRequestDetail>(ds.Tables[2]);
                leaveCalculationModal.lastAppliedLeave = Converter.ToList<LeaveRequestNotification>(ds.Tables[6]);
                if (leaveCalculationModal.lastAppliedLeave.Count > 0)
                    leaveCalculationModal.lastAppliedLeave = leaveCalculationModal.lastAppliedLeave.Where(x => x.RequestStatusId != (int)ItemStatus.Rejected).ToList();

                if (!string.IsNullOrEmpty(leaveCalculationModal.leaveRequestDetail.LeaveQuotaDetail))
                    leaveCalculationModal.leaveRequestDetail.EmployeeLeaveQuotaDetail = JsonConvert
                        .DeserializeObject<List<EmployeeLeaveQuota>>(leaveCalculationModal.leaveRequestDetail.LeaveQuotaDetail);
                else
                    leaveCalculationModal.leaveRequestDetail.EmployeeLeaveQuotaDetail = new List<EmployeeLeaveQuota>();

                leaveCalculationModal.companySetting = Converter.ToType<CompanySetting>(ds.Tables[3]);
                leaveCalculationModal.leavePlan = Converter.ToType<LeavePlan>(ds.Tables[4]);
                leaveCalculationModal.projectMemberDetail = Converter.ToList<ProjectMemberDetail>(ds.Tables[7]);
            }
            else
                throw new HiringBellException("Employee does not exist. Please contact to admin.");

            return leaveCalculationModal;
        }
    }
}
