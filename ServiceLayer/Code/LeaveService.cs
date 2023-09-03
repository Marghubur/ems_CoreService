using BottomhalfCore.DatabaseLayer.Common.Code;
using Microsoft.AspNetCore.Http;
using ModalLayer;
using ModalLayer.Modal;
using ModalLayer.Modal.Leaves;
using Newtonsoft.Json;
using ServiceLayer.Code.SendEmail;
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
        private readonly LeaveEmailService _leaveEmailService;
        private readonly ILeaveRequestService _leaveRequestService;

        public LeaveService(IDb db,
            CurrentSession currentSession,
            ICommonService commonService,
            LeaveEmailService leaveEmailService,
            ILeaveCalculation leaveCalculation,
            ILeaveRequestService leaveRequestService)
        {
            _db = db;
            _currentSession = currentSession;
            _commonService = commonService;
            _leaveCalculation = leaveCalculation;
            _leaveEmailService = leaveEmailService;
            _leaveRequestService = leaveRequestService;
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
                leavePlans = _db.GetList<LeavePlan>("sp_leave_plans_get");
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
            var value = _db.Execute<LeavePlan>("sp_leave_plan_insupd", leavePlan, true);
            if (string.IsNullOrEmpty(value))
                throw new HiringBellException("Unable to add or update leave plan");

            leavePlans = _db.GetList<LeavePlan>("sp_leave_plans_get");
            return leavePlans;
        }

        public List<LeavePlanType> AddLeavePlanTypeService(LeavePlanType leavePlanType)
        {
            List<LeavePlanType> leavePlanTypes = default(List<LeavePlanType>);
            BuildConfigurationDetailObject(leavePlanType);
            LeavePlanTypeValidation(leavePlanType);

            string result = _db.Execute<LeavePlanType>("sp_leave_plans_type_insupd", new
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
                leavePlanTypes = _db.GetList<LeavePlanType>("sp_leave_plans_type_get");

            return leavePlanTypes;
        }

        public List<LeavePlan> GetLeavePlansService(FilterModel filterModel)
        {
            List<LeavePlan> leavePlans = _db.GetList<LeavePlan>("sp_leave_plans_get");
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
            LeavePlanType record = _db.Get<LeavePlanType>("sp_leave_plans_type_getbyId", new { LeavePlanTypeId = leavePlanTypeId });

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
            string result = _db.Execute<LeaveDetail>("sp_leave_detail_InsUpdate", leaveDetail, true);
            return result;
        }

        public LeavePlanConfiguration GetLeaveTypeDetailByIdService(int leavePlanTypeId)
        {
            LeavePlanType leavePlanType = _db.Get<LeavePlanType>("sp_leave_plans_type_getbyId", new { LeavePlanTypeId = leavePlanTypeId });
            if (leavePlanType == null)
                throw new HiringBellException("Invalid plan id supplied");

            LeavePlanConfiguration leavePlanConfiguration = JsonConvert.DeserializeObject<LeavePlanConfiguration>(leavePlanType.PlanConfigurationDetail);
            if (leavePlanConfiguration == null)
                leavePlanConfiguration = new LeavePlanConfiguration();

            return leavePlanConfiguration;
        }

        public List<LeavePlanType> GetLeaveTypeFilterService()
        {
            List<LeavePlanType> leavePlanTypes = _db.GetList<LeavePlanType>("sp_leave_plans_type_get");
            return leavePlanTypes;
        }

        public async Task<LeavePlan> LeavePlanUpdateTypes(int leavePlanId, List<LeavePlanType> leavePlanTypes)
        {
            if (leavePlanId <= 0)
                throw new HiringBellException("Invalid leave plan id.");

            LeavePlan leavePlan = _db.Get<LeavePlan>("sp_leave_plans_getbyId", new { LeavePlanId = leavePlanId });
            if (leavePlan == null)
                throw new HiringBellException("Invalid leave plan selected.");
            foreach (LeavePlanType leavePlanType in leavePlanTypes)
            {
                leavePlanType.PlanConfigurationDetail = "";
                leavePlanType.LeavePlanId = leavePlanId;
            }
            leavePlan.AssociatedPlanTypes = JsonConvert.SerializeObject(leavePlanTypes);

            var result = await _db.ExecuteAsync("sp_leave_plan_insupd", leavePlan, true);
            if (result.rowsEffected != 1 || string.IsNullOrEmpty(result.statusMessage))
                throw new HiringBellException("Unable to add leave type. Please contact to admin.");
            return leavePlan;
        }

        public List<LeavePlan> SetDefaultPlanService(int LeavePlanId, LeavePlan leavePlan)
        {
            List<LeavePlan> leavePlans = null;
            if (leavePlan.LeavePlanId <= 0)
                throw new HiringBellException("Invalid leave plan selected.");

            var value = _db.Execute<LeavePlan>("sp_leave_plan_set_default", new
            {
                leavePlan.LeavePlanId,
                leavePlan.IsDefaultPlan
            }, true);
            if (string.IsNullOrEmpty(value))
                throw new HiringBellException("Unable to add or update leave plan");

            leavePlans = _db.GetList<LeavePlan>("sp_leave_plans_get");
            return leavePlans;
        }

        public string LeaveRquestManagerActionService(LeaveRequestNotification notification, ItemStatus status)
        {
            string message = string.Empty;
            var requestNotification = _db.Get<LeaveRequestNotification>("sp_leave_request_notification_get_byId", new
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
                    message = _db.Execute<LeaveRequestNotification>("sp_leave_request_notification_InsUpdate", new
                    {
                        requestNotification.LeaveRequestNotificationId,
                        requestNotification.LeaveRequestId,
                        requestNotification.UserMessage,
                        requestNotification.EmployeeId,
                        requestNotification.AssigneeId,
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

        }

        public async Task<dynamic> ApplyLeaveService(LeaveRequestModal leaveRequestModal, IFormFileCollection fileCollection, List<Files> fileDetail)
        {
            this.ValidateRequestModal(leaveRequestModal);
            var leaveCalculationModal = await _leaveCalculation.CheckAndApplyForLeave(leaveRequestModal, fileCollection, fileDetail);

            if (!leaveCalculationModal.IsEmailNotificationPasued)
            {
                Task task = Task.Run(async () => await _leaveEmailService.LeaveRequestSendEmail(leaveCalculationModal, leaveRequestModal.Reason));
            }

            if (leaveCalculationModal.IsLeaveAutoApproval)
            {
                LeaveRequestDetail leaveRequestDetail = new LeaveRequestDetail
                {
                    LeaveFromDay = leaveRequestModal.LeaveFromDay,
                    LeaveToDay = leaveRequestModal.LeaveToDay,
                    EmployeeId = leaveRequestModal.EmployeeId,
                    Reason = leaveRequestModal.Reason,
                    LeaveType = leaveRequestModal.LeaveTypeId,
                    LeaveRequestNotificationId = 0
                };

                await _leaveRequestService.ApprovalLeaveService(leaveRequestDetail);
            }

            return new
            {
                LeaveTypeBriefs = leaveCalculationModal.leaveTypeBriefs,
                EmployeeLeaveDetail = leaveCalculationModal.leaveRequestDetail,
                Employee = leaveCalculationModal.employee
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
            this.ValidateRequestModal(leaveRequestModal);
            var leaveCalculationModal = await GetLatestLeaveDetail(leaveRequestModal.EmployeeId);

            //if (!string.IsNullOrEmpty(leaveCalculationModal.leaveRequestDetail.LeaveDetail))
            //    this.UpdateLeavePlanDetail(leaveCalculationModal);
            var companyHoliday = _db.GetList<Calendar>("sp_company_calendar_get_by_company", new { CompanyId = _currentSession.CurrentUserDetail.CompanyId });
            return new
            {
                LeaveTypeBriefs = leaveCalculationModal.leaveTypeBriefs,
                EmployeeLeaveDetail = leaveCalculationModal.leaveRequestDetail,
                Employee = leaveCalculationModal.employee,
                CompanyHoliday = companyHoliday,
                ShiftDetail = leaveCalculationModal.shiftDetail
            };
        }

        public DataSet GetLeaveAttachmentService(string FileIds)
        {
            if (string.IsNullOrEmpty(FileIds) || FileIds == "[]")
                throw HiringBellException.ThrowBadRequest("File ids are null or empty");

            var result = _db.FetchDataSet("sp_user_files_get_byids_json", new { UserFileId = FileIds });
            return result;
        }

        public DataSet GetLeaveAttachByMangerService(LeaveRequestNotification leaveRequestNotification)
        {
            if (leaveRequestNotification.LeaveRequestNotificationId < 0)
                throw HiringBellException.ThrowBadRequest("Invalid leave request selected");

            (LeaveRequestDetail leaveRequestDetail, LeavePlanType leavePlanType) = _db.GetMulti<LeaveRequestDetail, LeavePlanType>("sp_employee_leave_request_GetById", new { LeaveRequestNotificationId = leaveRequestNotification.LeaveRequestNotificationId });
            var completeLeave = JsonConvert.DeserializeObject<List<CompleteLeaveDetail>>(leaveRequestDetail.LeaveDetail);
            var slectedleave = completeLeave.Find(x => x.RecordId == leaveRequestNotification.RecordId);
            if (slectedleave != null && !string.IsNullOrEmpty(slectedleave.FileIds) && slectedleave.FileIds != "[]")
            {
                var fileids = slectedleave.FileIds;
                return this.GetLeaveAttachmentService(fileids);
            }
            return null;
        }
    }
}
