using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using ModalLayer.Modal.Leaves;
using Newtonsoft.Json;
using ServiceLayer.Code.ApprovalChain;
using ServiceLayer.Code.SendEmail;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace ServiceLayer.Code
{
    public class LeaveRequestService : ILeaveRequestService
    {
        private readonly IDb _db;
        private readonly CurrentSession _currentSession;
        private readonly IAttendanceRequestService _attendanceRequestService;
        private readonly ApprovalEmailService _approvalEmailService;
        private readonly WorkFlowChain _workFlowChain;
        private readonly KafkaNotificationService _kafkaNotificationService;

        public LeaveRequestService(IDb db,
            ApprovalEmailService approvalEmailService,
            CurrentSession currentSession,
            WorkFlowChain workFlowChain,
            IAttendanceRequestService attendanceRequestService,
            KafkaNotificationService kafkaNotificationService)
        {
            _db = db;
            _workFlowChain = workFlowChain;
            _currentSession = currentSession;
            _attendanceRequestService = attendanceRequestService;
            _approvalEmailService = approvalEmailService;
            _kafkaNotificationService = kafkaNotificationService;
        }

        public async Task<RequestModel> ApprovalLeaveService(LeaveRequestDetail leaveRequestDetail, int filterId = ApplicationConstants.Only)
        {
            await UpdateLeaveDetail(leaveRequestDetail, ItemStatus.Approved);
            return _attendanceRequestService.GetRequestPageData(_currentSession.CurrentUserDetail.UserId, filterId);
        }

        public async Task<RequestModel> RejectLeaveService(LeaveRequestDetail leaveRequestDetail, int filterId = ApplicationConstants.Only)
        {
            await UpdateLeaveDetail(leaveRequestDetail, ItemStatus.Rejected);
            return _attendanceRequestService.GetRequestPageData(_currentSession.CurrentUserDetail.UserId, filterId);
        }

        private void updateLeaveCountOnRejected(LeaveRequestDetail LeaveRequestDetail, int leaveTypeId, decimal leaveCount)
        {
            if (leaveTypeId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid leave type id");


            if (!string.IsNullOrEmpty(LeaveRequestDetail.LeaveQuotaDetail))
            {
                var records = JsonConvert.DeserializeObject<List<LeaveTypeBrief>>(LeaveRequestDetail.LeaveQuotaDetail);
                if (records.Count > 0)
                {
                    var item = records.Find(x => x.LeavePlanTypeId == leaveTypeId);
                    item.AvailableLeaves += leaveCount;
                    if (item.AvailableLeaves > item.TotalLeaveQuota)
                        item.AvailableLeaves = item.TotalLeaveQuota;

                    LeaveRequestDetail.LeaveQuotaDetail = JsonConvert.SerializeObject(records);
                }
            }
        }

        public async Task UpdateLeaveDetail(LeaveRequestDetail requestDetail, ItemStatus status)
        {
            if (requestDetail.LeaveRequestNotificationId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid request. Please check your detail first.");

            string message = string.Empty;
            (var leaveRequestDetail, LeavePlanType leavePlanType) = _db.Get<LeaveRequestDetail, LeavePlanType>("sp_employee_leave_request_GetById", new
            {
                LeaveRequestNotificationId = requestDetail.LeaveRequestNotificationId
            });

            if (leaveRequestDetail == null || string.IsNullOrEmpty(leaveRequestDetail.LeaveDetail))
                throw new HiringBellException("Unable to find leave detail. Please contact to admin.");

            List<CompleteLeaveDetail> completeLeaveDetail = JsonConvert
              .DeserializeObject<List<CompleteLeaveDetail>>(leaveRequestDetail.LeaveDetail);

            if (completeLeaveDetail == null)
                throw new HiringBellException("Unable to find applied leave detail. Please contact to admin");


            var pendingCount = completeLeaveDetail.Where(x => !x.RecordId.Equals(requestDetail.RecordId)).Count(i => i.LeaveStatus == (int)ItemStatus.Pending);

            var singleLeaveDetail = completeLeaveDetail.Find(x => x.RecordId.Equals(requestDetail.RecordId));
            if (singleLeaveDetail == null)
                throw new HiringBellException("Unable to find applied leave. Please contact to admin");


            long nextId = 0;
            leaveRequestDetail.RequestStatusId = (int)status;
            singleLeaveDetail.LeaveStatus = (int)status;
            if (ItemStatus.Rejected == status)
            {
                var totalLeaves = (decimal)requestDetail.LeaveToDay.Date.Subtract(requestDetail.LeaveFromDay.Date).TotalDays + 1;
                updateLeaveCountOnRejected(leaveRequestDetail, requestDetail.LeaveTypeId, totalLeaves);
            }
            else
            {
                nextId = _workFlowChain.GetNextRequestor(leavePlanType, singleLeaveDetail, leaveRequestDetail.AssigneeId);
                if (nextId > 0)
                {
                    leaveRequestDetail.AssigneeId = nextId;
                    leaveRequestDetail.RequestStatusId = (int)ItemStatus.Pending;
                    singleLeaveDetail.LeaveStatus = (int)ItemStatus.Pending;
                }
                else
                {
                    leaveRequestDetail.AssigneeId = 0;
                }
            }

            singleLeaveDetail.RespondedBy = _currentSession.CurrentUserDetail.UserId;
            leaveRequestDetail.LeaveDetail = JsonConvert.SerializeObject(completeLeaveDetail);

            message = _db.Execute<LeaveRequestNotification>("sp_leave_notification_and_request_InsUpdate", new
            {
                leaveRequestDetail.LeaveRequestId,
                leaveRequestDetail.EmployeeId,
                leaveRequestDetail.LeaveDetail,
                leaveRequestDetail.Reason,
                leaveRequestDetail.AssigneeId,
                leaveRequestDetail.ReportingManagerId,
                leaveRequestDetail.Year,
                leaveRequestDetail.LeaveFromDay,
                leaveRequestDetail.LeaveToDay,
                leaveRequestDetail.LeaveTypeId,
                leaveRequestDetail.RequestStatusId,
                leaveRequestDetail.AvailableLeaves,
                leaveRequestDetail.TotalLeaveApplied,
                leaveRequestDetail.TotalApprovedLeave,
                leaveRequestDetail.TotalLeaveQuota,
                leaveRequestDetail.LeaveQuotaDetail,
                NumOfDays = 0,
                requestDetail.LeaveRequestNotificationId,
                RecordId = requestDetail.RecordId,
                IsPending = pendingCount > 0 ? true : false
            }, true);
            if (string.IsNullOrEmpty(message))
                throw new HiringBellException("Unable to update leave status. Please contact to admin");

            leaveRequestDetail.LeaveFromDay = requestDetail.LeaveFromDay;
            leaveRequestDetail.LeaveToDay = requestDetail.LeaveToDay;
            leaveRequestDetail.Reason = requestDetail.Reason;
            leaveRequestDetail.LeaveType = requestDetail.LeaveType;

            if (nextId == 0)
            {
                var leaveTemplateModel = new LeaveTemplateModel
                {
                    kafkaServiceName = KafkaServiceName.Leave,
                    RequestType = nameof(RequestType.Leave),
                    ActionType = status == ItemStatus.Approved ? nameof(ItemStatus.Approved) : nameof(ItemStatus.Rejected),
                    FromDate = leaveRequestDetail.LeaveFromDay,
                    ToDate = leaveRequestDetail.LeaveToDay,
                    Message = leaveRequestDetail.Reason,
                    ManagerName = _currentSession.CurrentUserDetail.ManagerName,
                    DeveloperName = _currentSession.CurrentUserDetail.FullName,
                    CompanyName = _currentSession.CurrentUserDetail.CompanyName,
                    DayCount = (int)leaveRequestDetail.LeaveToDay.Subtract(leaveRequestDetail.LeaveFromDay).TotalDays + 1,
                    ToAddress = new List<string> { leaveRequestDetail.Email }
                };

                await _kafkaNotificationService.SendEmailNotification(leaveTemplateModel);
                //Task task = Task.Run(async () => await _approvalEmailService.LeaveApprovalStatusSendEmail(leaveRequestDetail, status));
            }

            await Task.CompletedTask;
        }

        public List<LeaveRequestNotification> ReAssigneToOtherManagerService(LeaveRequestNotification leaveRequestNotification, int filterId = ApplicationConstants.Only)
        {
            return null;
        }

        public async Task LeaveLeaveManagerMigration(List<CompanySetting> companySettings)
        {
            foreach (var setting in companySettings)
            {
                _currentSession.CurrentUserDetail.CompanyId = setting.CompanyId;
                _currentSession.TimeZone = TZConvert.GetTimeZoneInfo(setting.TimezoneName);

                var leaveRequestDetails = _db.GetList<LeaveRequestDetail>("sp_employee_leave_level_migration", new
                {
                    Year = DateTime.UtcNow.Year,
                    setting.CompanyId
                });
                foreach (var level in leaveRequestDetails)
                {
                    await ExecuteFlowChainCycle(level);
                }
            }
        }

        public async Task<List<LeaveRequestNotification>> GetLeaveRequestNotificationService(LeaveRequestNotification leaveRequestNotification)
        {
            if (leaveRequestNotification.ReportingManagerId == 0)
                throw new HiringBellException("Reporting manager not found. Please contact to admin");

            var result = _db.GetList<LeaveRequestNotification>("sp_leave_requests_by_filter", new
            {
                leaveRequestNotification.ReportingManagerId,
                leaveRequestNotification.EmployeeId,
                leaveRequestNotification.FromDate,
                leaveRequestNotification.ToDate,
                leaveRequestNotification.RequestStatusId
            });
            return result;
        }

        private async Task ExecuteFlowChainCycle(LeaveRequestDetail level)
        {
            double daysPending = 0;
            List<CompleteLeaveDetail> completeLeaveDetails = JsonConvert.DeserializeObject<List<CompleteLeaveDetail>>(level.LeaveDetail);
            if (completeLeaveDetails != null && completeLeaveDetails.Count > 0)
            {
                var requests = completeLeaveDetails.Where(x => x.LeaveStatus == (int)ItemStatus.Pending).ToList();
                foreach (var request in requests)
                {
                    int i = 0;
                    int count = request.RequestChain.Count;
                    var chainDetail = request.RequestChain.OrderBy(x => x.Level).ToList();
                    while (i < count)
                    {
                        var chain = chainDetail.ElementAt(i);

                        daysPending = DateTime.UtcNow.Subtract(chain.ReactedOn.AddDays(chain.ForwardAfterDays)).TotalDays;
                        if (chain.Status == (int)ItemStatus.Pending && daysPending > 0)
                        {
                            if (chain.IsRequired)
                            {
                                if (chain.Status == (int)ItemStatus.Approved)
                                {
                                    i++;
                                    // forward request to next level if any exists else approve the request.
                                    chain.Status = (int)ItemStatus.AutoPromoted;
                                    if (i < count)
                                    {
                                        chain = chainDetail[i];
                                        chain.Status = (int)ItemStatus.Pending;

                                        level.LeaveDetail = JsonConvert.SerializeObject(completeLeaveDetails);
                                        await UpdateLeaveNotification(level, request.RecordId, chain);
                                    }

                                    break;
                                }
                                else
                                {
                                    // entire workflow will get rejected
                                    chain.Status = (int)ItemStatus.Rejected;
                                    chain.FeedBack = "AUTO REJECTED, NO ACTION TAKEN FOR THE GIVEN PERIOD OF TIME.";
                                    level.LeaveDetail = JsonConvert.SerializeObject(completeLeaveDetails);

                                    // reject in employee leave request
                                    await UpdateLeaveDetail(level, ItemStatus.Rejected);
                                    break;
                                }
                            }
                            else
                            {
                                if (chain.Status == chain.ForwardWhenStatus)
                                {
                                    // forward request to next level if any exists else approve the request.
                                    i++;
                                    if (chain.Status == (int)ItemStatus.Pending)
                                        chain.Status = (int)ItemStatus.AutoPromoted;

                                    if (i < count)
                                    {
                                        chain = chainDetail[i];
                                        chain.Status = (int)ItemStatus.Pending;

                                        level.LeaveDetail = JsonConvert.SerializeObject(completeLeaveDetails);
                                        // notify to next manage for the request.
                                        await UpdateLeaveNotification(level, request.RecordId, chain);
                                        break;
                                    }
                                    else
                                    {
                                        chain.Status = (int)ItemStatus.Rejected;
                                        chain.FeedBack = "AUTO REJECTED, NO ACTION TAKEN FOR THE GIVEN PERIOD OF TIME.";

                                        level.LeaveDetail = JsonConvert.SerializeObject(completeLeaveDetails);
                                        // reject in employee leave request
                                        await UpdateLeaveDetail(level, ItemStatus.Rejected);
                                        break;
                                    }
                                }
                                else if (chain.Status == (int)ItemStatus.Approved)
                                {
                                    chain.FeedBack = "LEAVE APPROVED.";
                                    level.LeaveDetail = JsonConvert.SerializeObject(completeLeaveDetails);

                                    // approve in employee leave request
                                    await UpdateLeaveDetail(level, ItemStatus.Approved);
                                    break;
                                }
                                else if (chain.Status == (int)ItemStatus.Rejected)
                                {
                                    chain.FeedBack = "LEAVE REJECTED.";
                                    level.LeaveDetail = JsonConvert.SerializeObject(completeLeaveDetails);

                                    // reject in employee leave request
                                    await UpdateLeaveDetail(level, ItemStatus.Rejected);
                                    break;
                                }
                                else
                                {
                                    chain.FeedBack = "AUTO REJECTED, NO ACTION TAKEN FOR THE GIVEN PERIOD OF TIME.";
                                    level.LeaveDetail = JsonConvert.SerializeObject(completeLeaveDetails);

                                    // reject in employee leave request
                                    await UpdateLeaveDetail(level, ItemStatus.Rejected);
                                    break;
                                }
                            }
                        }

                        i++;
                    }
                }
            }
        }

        private async Task UpdateLeaveNotification(LeaveRequestDetail leaveRequestDetail, string RecordId, RequestChainModal chain)
        {
            // update employee_leave_request table and update leave_request_notification to next manager
            var result = _db.Execute<string>("sp_leave_request_and_notification_update_level", new
            {
                leaveRequestDetail.LeaveRequestId,
                leaveRequestDetail.LeaveDetail,
                RecordId,
                chain.ExecuterId
            }, false);

            if (string.IsNullOrEmpty(result))
                throw HiringBellException.ThrowBadRequest("Fail to update leave request and notification");
            else
            {
                var task = Task.Run(async () => await _approvalEmailService.ManagerApprovalMigrationEmail(leaveRequestDetail, chain.ExecuterEmail));
            }

            await Task.CompletedTask;
        }
    }
}
