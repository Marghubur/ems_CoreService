using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EMailService.Modal;
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
        private readonly ApprovalEmailService _approvalEmailService;
        private readonly KafkaNotificationService _kafkaNotificationService;
        private readonly ITimezoneConverter _timezoneConverter;

        public LeaveRequestService(IDb db,
            ApprovalEmailService approvalEmailService,
            CurrentSession currentSession,
            KafkaNotificationService kafkaNotificationService,
            ITimezoneConverter timezoneConverter)
        {
            _db = db;
            _currentSession = currentSession;
            _approvalEmailService = approvalEmailService;
            _kafkaNotificationService = kafkaNotificationService;
            _timezoneConverter = timezoneConverter;
        }

        public async Task<List<LeaveRequestNotification>> ApprovalLeaveService(LeaveRequestDetail leaveRequestDetail, int filterId = ApplicationConstants.Only)
        {
            await UpdateLeaveDetail(leaveRequestDetail, ItemStatus.Approved);
            LeaveRequestNotification leaveRequestNotification = new LeaveRequestNotification
            {
                ReportingManagerId = _currentSession.CurrentUserDetail.UserId,
                EmployeeId = leaveRequestDetail.EmployeeId,
                FromDate = leaveRequestDetail.LeaveFromDay,
                ToDate = leaveRequestDetail.LeaveToDay,
                RequestStatusId = leaveRequestDetail.RequestStatusId,
                PageIndex = 1
            };
            return await GetLeaveRequestNotificationService(leaveRequestNotification);
        }

        public async Task<List<LeaveRequestNotification>> RejectLeaveService(LeaveRequestDetail leaveRequestDetail, int filterId = ApplicationConstants.Only)
        {
            await UpdateLeaveDetail(leaveRequestDetail, ItemStatus.Rejected);
            LeaveRequestNotification leaveRequestNotification = new LeaveRequestNotification
            {
                ReportingManagerId = _currentSession.CurrentUserDetail.UserId,
                EmployeeId = leaveRequestDetail.EmployeeId,
                FromDate = leaveRequestDetail.LeaveFromDay,
                ToDate = leaveRequestDetail.LeaveToDay,
                RequestStatusId = leaveRequestDetail.RequestStatusId,
                PageIndex = 1
            };
            return await GetLeaveRequestNotificationService(leaveRequestNotification);
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
            bool isSendEmailNotification = false;
            if (requestDetail.LeaveRequestNotificationId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid request. Please check your detail first.");

            (var leaveRequestDetail, LeavePlanType leavePlanType) = _db.Get<LeaveRequestDetail, LeavePlanType>("sp_employee_leave_request_GetById", new
            {
                requestDetail.LeaveRequestNotificationId
            });

            if (leaveRequestDetail == null)
                throw new HiringBellException("Unable to find leave detail. Please contact to admin.");

            var reporterDetails = JsonConvert.DeserializeObject<List<EmployeeWithRoles>>(leaveRequestDetail.Notify);
            var selectedReporter = reporterDetails.Find(x => x.EmployeeUid == _currentSession.CurrentUserDetail.UserId);
            if (selectedReporter == null)
                throw HiringBellException.ThrowBadRequest("Reporter detail not found. Please contact to admin");

            selectedReporter.Status = (int)status;
            if (ItemStatus.Rejected == status)
            {
                var totalLeaves = (decimal)requestDetail.LeaveToDay.Date.Subtract(requestDetail.LeaveFromDay.Date).TotalDays + 1;
                updateLeaveCountOnRejected(leaveRequestDetail, requestDetail.LeaveTypeId, totalLeaves);
            }

            int rejected = reporterDetails.Count(x => x.IsRequired == true && x.Status == (int)ItemStatus.Rejected);
            if (rejected > 0)
            {
                leaveRequestDetail.RequestStatusId = (int)ItemStatus.Rejected;
                isSendEmailNotification = true;
            }
            else
            {
                int requiredCounts = reporterDetails.Count(x => x.IsRequired == true);
                int approvedRequiredCount = reporterDetails.Count(x => x.IsRequired == true && x.Status == (int)ItemStatus.Approved);
                int totalApprovalCount = reporterDetails.Count(x => x.Status == (int)ItemStatus.Approved);

                if (requiredCounts == 0)
                {
                    leaveRequestDetail.RequestStatusId = (int)ItemStatus.Approved;
                    isSendEmailNotification = true;
                }
                else
                {
                    if (requiredCounts == approvedRequiredCount && totalApprovalCount >= leaveRequestDetail.NoOfApprovalsRequired)
                    {
                        leaveRequestDetail.RequestStatusId = (int)ItemStatus.Approved;
                        isSendEmailNotification = true;
                    }
                    else
                    {
                        leaveRequestDetail.RequestStatusId = (int)ItemStatus.Pending;
                    }
                }
            }
            string message = _db.Execute<LeaveRequestNotification>(Procedures.Leave_Notification_And_Request_InsUpdate, new
            {
                requestDetail.LeaveRequestNotificationId,
                leaveRequestDetail.LeaveRequestId,
                leaveRequestDetail.EmployeeId,
                leaveRequestDetail.LeaveDetail,
                leaveRequestDetail.Reason,
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
                ReporterDetail = JsonConvert.SerializeObject(reporterDetails),
                NumOfDays = 0,
                IsPending = false,
            }, true);

            if (!ApplicationConstants.IsExecuted(message))
                throw new HiringBellException("Unable to update leave status. Please contact to admin");

            if (isSendEmailNotification)
            {
                var leaveTemplateModel = new LeaveTemplateModel
                {
                    kafkaServiceName = KafkaServiceName.Leave,
                    RequestType = nameof(RequestType.Leave),
                    ActionType = status == ItemStatus.Approved ? nameof(ItemStatus.Approved) : nameof(ItemStatus.Rejected),
                    FromDate = _timezoneConverter.ToTimeZoneDateTime(leaveRequestDetail.LeaveFromDay, _currentSession.TimeZone),
                    ToDate = _timezoneConverter.ToTimeZoneDateTime(leaveRequestDetail.LeaveToDay, _currentSession.TimeZone),
                    Message = leaveRequestDetail.Reason,
                    ManagerName = _currentSession.CurrentUserDetail.FullName,
                    DeveloperName = leaveRequestDetail.FirstName + " " + leaveRequestDetail.LastName,
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

                var leaveRequestDetails = _db.GetList<LeaveRequestDetail>(Procedures.Employee_Leave_Level_Migration, new
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

            var result = _db.GetList<LeaveRequestNotification>(Procedures.Leave_Requests_By_Filter, new
            {
                leaveRequestNotification.ReportingManagerId,
                leaveRequestNotification.EmployeeId,
                leaveRequestNotification.FromDate,
                leaveRequestNotification.ToDate,
                leaveRequestNotification.RequestStatusId,
                leaveRequestNotification.PageIndex,
                PageSize = 10
            });
            return await Task.FromResult(result);
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
