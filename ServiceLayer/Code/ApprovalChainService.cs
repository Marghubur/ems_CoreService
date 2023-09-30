using BottomhalfCore.Configuration;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ApplicationConstants;

namespace ServiceLayer.Code
{
    public class ApprovalChainService : IApprovalChainService
    {
        private readonly IDb _db;
        private readonly CurrentSession _currentSession;
        public ApprovalChainService(IDb db, CurrentSession currentSession)
        {
            _db = db;
            _currentSession = currentSession;
        }

        public async Task<ApprovalWorkFlowModal> GetApprovalChainService(FilterModel filterModel)
        {
            ApprovalWorkFlowModal approvalWorkFlowModal = null;
            return await Task.FromResult(approvalWorkFlowModal);
        }

        private string GetSelectQuery(ApprovalWorkFlowChain approvalWorkFlowModal)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("1=1 ");

            if (approvalWorkFlowModal.ApprovalWorkFlowId > 0)
                sb.Append($"and w.ApprovalWorkFlowId = {approvalWorkFlowModal.ApprovalWorkFlowId} ");

            if (approvalWorkFlowModal.ApprovalChainDetailId > 0)
                sb.Append($"and w.ApprovalChainDetailId = {approvalWorkFlowModal.ApprovalChainDetailId} ");

            if (!string.IsNullOrEmpty(approvalWorkFlowModal.Title))
                sb.Append($"and f.Title = '{approvalWorkFlowModal.Title}' ");

            return sb.ToString();
        }

        public async Task<string> InsertApprovalChainService(ApprovalWorkFlowChain approvalWorkFlowModal)
        {
            int approvalWorkFlowId = approvalWorkFlowModal.ApprovalWorkFlowId;
            ValidateApprovalWorkFlowDetail(approvalWorkFlowModal);

            var approvalWorkFlowModalExisting = _db.GetList<ApprovalWorkFlowChainFilter>(ConfigurationDetail.sp_approval_chain_detail_filter, new
            {
                SearchString = GetSelectQuery(approvalWorkFlowModal),
            });

            if (approvalWorkFlowModalExisting.Count > 0)
            {
                var firstRecord = approvalWorkFlowModalExisting.First();
                approvalWorkFlowModal.ApprovalWorkFlowId = firstRecord.ApprovalWorkFlowId;

                ApprovalChainDetail chainDetail = null;
                approvalWorkFlowModal.ApprovalChainDetails.ForEach(item =>
                {
                    chainDetail = approvalWorkFlowModalExisting.FirstOrDefault(x => x.ApprovalChainDetailId == item.ApprovalChainDetailId);

                    if (chainDetail != null)
                    {
                        chainDetail.AssignieId = item.AssignieId;
                        chainDetail.IsRequired = item.IsRequired;
                    }
                    else
                    {
                        approvalWorkFlowModalExisting.Add(new ApprovalWorkFlowChainFilter
                        {
                            ApprovalChainDetailId = 0,
                            ApprovalWorkFlowId = 0,
                            AssignieId = item.AssignieId,
                            IsRequired = item.IsRequired,
                            LastUpdatedOn = item.LastUpdatedOn,
                            ApprovalStatus = (int)ItemStatus.Pending
                        });
                    }
                });
            }
            else
            {
                foreach (var item in approvalWorkFlowModal.ApprovalChainDetails)
                {
                    approvalWorkFlowModalExisting.Add(new ApprovalWorkFlowChainFilter
                    {
                        ApprovalChainDetailId = 0,
                        ApprovalWorkFlowId = 0,
                        AssignieId = item.AssignieId,
                        IsRequired = item.IsRequired,
                        LastUpdatedOn = item.LastUpdatedOn,
                        ApprovalStatus = (int)ItemStatus.Pending
                    });
                }
            }

            var data = (from n in approvalWorkFlowModalExisting
                        select new
                        {
                            ApprovalChainDetailId = n.ApprovalChainDetailId > 0 ? n.ApprovalChainDetailId : 0,
                            ApprovalWorkFlowId = DbProcedure.getParentKey(n.ApprovalWorkFlowId),
                            AssignieId = n.AssignieId,
                            IsRequired = n.IsRequired,
                            LastUpdatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                            ApprovalStatus = n.ApprovalStatus
                        }
                    ).ToList<object>();

            var result = await _db.BatchInsetUpdate(ConfigurationDetail.sp_approval_work_flow_insupd,
                new
                {
                    approvalWorkFlowModal.ApprovalWorkFlowId,
                    approvalWorkFlowModal.Title,
                    approvalWorkFlowModal.TitleDescription,
                    approvalWorkFlowModal.Status,
                    approvalWorkFlowModal.IsAutoExpiredEnabled,
                    approvalWorkFlowModal.AutoExpireAfterDays,
                    approvalWorkFlowModal.IsSilentListner,
                    approvalWorkFlowModal.ListnerDetail,
                    approvalWorkFlowModal.NoOfApprovalLevel,
                    AdminId = _currentSession.CurrentUserDetail.UserId
                },
                DbProcedure.ApprovalChainDetail,
                data);
            if (string.IsNullOrEmpty(result))
                throw HiringBellException.ThrowBadRequest("Fail to insert/update record. Please contact to admin.");

            return "insert/updated successfully";
        }

        private void ValidateApprovalWorkFlowDetail(ApprovalWorkFlowChain approvalWorkFlowModal)
        {
            if (string.IsNullOrEmpty(approvalWorkFlowModal.Title))
                throw HiringBellException.ThrowBadRequest("Tite is null or empty");

            if (string.IsNullOrEmpty(approvalWorkFlowModal.TitleDescription))
                throw HiringBellException.ThrowBadRequest("Title description is null or empty");

            if (approvalWorkFlowModal.IsAutoExpiredEnabled)
            {
                if (approvalWorkFlowModal.AutoExpireAfterDays <= 0)
                    throw HiringBellException.ThrowBadRequest("Please add auto expire after days");
            }

            if (approvalWorkFlowModal.ApprovalChainDetails.Count > 0)
            {
                foreach (var item in approvalWorkFlowModal.ApprovalChainDetails)
                {
                    if (item.AssignieId <= 0)
                        throw HiringBellException.ThrowBadRequest("Please add assigne first");

                }
            }
            int approvalRequiredCount =  approvalWorkFlowModal.ApprovalChainDetails.Count(x => x.IsRequired);
            if (approvalRequiredCount > 0 && approvalWorkFlowModal.NoOfApprovalLevel == 0)
                throw HiringBellException.ThrowBadRequest("No of Approval level is less than equal to required level");

            if (approvalRequiredCount > 0 && approvalWorkFlowModal.NoOfApprovalLevel > approvalRequiredCount)
                throw HiringBellException.ThrowBadRequest("No of Approval level is greater than required level");
        }

        public async Task<List<ApprovalWorkFlowModal>> GetPageDateService(FilterModel filterModel)
        {
            if (string.IsNullOrEmpty(filterModel.SearchString))
                filterModel.SearchString = "1=1";

            var result = _db.GetList<ApprovalWorkFlowModal>(ConfigurationDetail.sp_approval_work_flow_filter, new
            {
                filterModel.SearchString,
                filterModel.SortBy,
                filterModel.PageSize,
                filterModel.PageIndex
            });

            return await Task.FromResult(result);
        }

        public async Task<dynamic> GetApprovalChainData(int ApprovalWorkFlowId)
        {
            string searchString = string.Empty;
            ApprovalWorkFlowChain approvalWorkFlowChain = null;

            (List<ApprovalWorkFlowChainFilter> approvalWorkFlow, List<EmployeeRole> employeeRole) = _db.GetList<ApprovalWorkFlowChainFilter, EmployeeRole>(ConfigurationDetail.sp_approval_chain_detail_by_id, new
            {
                ApprovalWorkFlowId
            });

            if (employeeRole.Count == 0)
                throw HiringBellException.ThrowBadRequest("Fail to get employee role.");

            if (approvalWorkFlow.Count > 0)
            {
                var firstRecord = approvalWorkFlow.First();
                approvalWorkFlowChain = new ApprovalWorkFlowChain
                {
                    ApprovalChainDetailId = firstRecord.ApprovalChainDetailId,
                    ApprovalWorkFlowId = firstRecord.ApprovalWorkFlowId,
                    Title = firstRecord.Title,
                    TitleDescription = firstRecord.TitleDescription,
                    Status = firstRecord.Status,
                    IsAutoExpiredEnabled = firstRecord.IsAutoExpiredEnabled,
                    AutoExpireAfterDays = firstRecord.AutoExpireAfterDays,
                    IsSilentListner = firstRecord.IsSilentListner,
                    ListnerDetail = firstRecord.ListnerDetail,
                    NoOfApprovalLevel = firstRecord.NoOfApprovalLevel,
                    ApprovalChainDetails = new List<ApprovalChainDetail>()
                };

                approvalWorkFlowChain.ApprovalChainDetails = (
                    from n in approvalWorkFlow
                    select new ApprovalChainDetail
                    {
                        ApprovalChainDetailId = n.ApprovalChainDetailId,
                        ApprovalWorkFlowId = n.ApprovalWorkFlowId,
                        AssignieId = n.AssignieId,
                        IsRequired = n.IsRequired,
                        LastUpdatedOn = n.LastUpdatedOn,
                        ApprovalStatus = n.ApprovalStatus
                    }
                 ).ToList<ApprovalChainDetail>();
            }

            employeeRole = employeeRole.FindAll(x => x.RoleId == 1 || x.RoleId == 2 || x.RoleId == 19 || x.RoleId == 3 || x.RoleId == 5);

            return await Task.FromResult(new { approvalWorkFlowChain, employeeRole });
        }

        public Task<string> DeleteApprovalChainService(int approvalChainDetailId)
        {
            if (approvalChainDetailId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid approval chain selected for delete");

            var result = _db.Execute<ApprovalChainDetail>(ConfigurationDetail.sp_approval_chain_detail_delete_byid, new { ApprovalChainDetailId = approvalChainDetailId }, true);
            if (string.IsNullOrEmpty(result))
                throw HiringBellException.ThrowBadRequest("Fail to delete chain level. Please contact to admin");

            return Task.FromResult(result);
        }
    }
}
