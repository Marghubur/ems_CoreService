using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.Configuration;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ApplicationConstants;

namespace ServiceLayer.Code
{
    public class ApprovalChainService(IDb _db,
                                      CurrentSession _currentSession) : IApprovalChainService
    {
        public async Task<ApprovalWorkFlowModal> GetApprovalChainService(FilterModel filterModel)
        {
            ApprovalWorkFlowModal approvalWorkFlowModal = null;
            return await Task.FromResult(approvalWorkFlowModal);
        }

        public async Task<string> InsertApprovalChainService(ApprovalWorkFlowChain approvalWorkFlowModal)
        {
            int approvalWorkFlowId = approvalWorkFlowModal.ApprovalWorkFlowId;
            ValidateApprovalWorkFlowDetail(approvalWorkFlowModal);

            var resultData = await GetApprovalChainData(approvalWorkFlowModal.ApprovalWorkFlowId);
            var existingApprovalWorkflow = resultData.approvalWorkFlowChain;
            if (existingApprovalWorkflow == null)
            {
                existingApprovalWorkflow = new ApprovalWorkFlowChain
                {
                    Title = approvalWorkFlowModal.Title,
                    TitleDescription = approvalWorkFlowModal.TitleDescription,
                    ApprovalChainDetails = new List<ApprovalChainDetail>()
                };
            }
            else
            {
                existingApprovalWorkflow.Title = approvalWorkFlowModal.Title;
                existingApprovalWorkflow.TitleDescription = approvalWorkFlowModal.TitleDescription;
                existingApprovalWorkflow.ApprovalChainDetails ??= new List<ApprovalChainDetail>();
            }

            if (approvalWorkFlowModal.ApprovalChainDetails.Any())
            {
                approvalWorkFlowModal.ApprovalChainDetails.ForEach(x =>
                {
                    var chainDetail = existingApprovalWorkflow.ApprovalChainDetails.Find(i => i.ApprovalChainDetailId == x.ApprovalChainDetailId && i.ApprovalChainDetailId > 0);
                    if (chainDetail != null)
                    {
                        chainDetail.AssignieId = x.AssignieId;
                        chainDetail.IsRequired = x.IsRequired;
                        chainDetail.AutoActionType = x.AutoActionType;
                        chainDetail.AutoActionDays = x.AutoActionDays;
                    }
                    else
                    {
                        existingApprovalWorkflow.ApprovalChainDetails.Add(new ApprovalWorkFlowChainFilter
                        {
                            ApprovalChainDetailId = 0,
                            ApprovalWorkFlowId = 0,
                            AssignieId = x.AssignieId,
                            IsRequired = x.IsRequired,
                            LastUpdatedOn = DateTime.UtcNow,
                            ApprovalStatus = (int)ItemStatus.Pending,
                            AutoActionDays = x.AutoActionDays,
                            AutoActionType = x.AutoActionType,
                        });
                    }
                });
            }

            var data = (from n in existingApprovalWorkflow.ApprovalChainDetails
                        select new
                        {
                            ApprovalChainDetailId = n.ApprovalChainDetailId > 0 ? n.ApprovalChainDetailId : 0,
                            ApprovalWorkFlowId = DbProcedure.getParentKey(n.ApprovalWorkFlowId),
                            n.AssignieId,
                            n.IsRequired,
                            n.AutoActionDays,
                            n.AutoActionType,
                            LastUpdatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                            n.ApprovalStatus
                        }
                    ).ToList<object>();

            var result = await _db.BatchInsetUpdate(ConfigurationDetail.sp_approval_work_flow_insupd,
                new
                {
                    approvalWorkFlowModal.ApprovalWorkFlowId,
                    approvalWorkFlowModal.Title,
                    approvalWorkFlowModal.TitleDescription,
                    approvalWorkFlowModal.Status,
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


            if (approvalWorkFlowModal.ApprovalChainDetails.Any())
            {
                foreach (var item in approvalWorkFlowModal.ApprovalChainDetails)
                {
                    if (item.AssignieId <= 0)
                        throw HiringBellException.ThrowBadRequest("Please add assigne first");

                }
            }
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

        public async Task<(ApprovalWorkFlowChain approvalWorkFlowChain, List<EmployeeRole> employeeRole)> GetApprovalChainData(int ApprovalWorkFlowId)
        {
            string searchString = string.Empty;
            ApprovalWorkFlowChain approvalWorkFlowChain = null;

            (List<ApprovalWorkFlowChainFilter> approvalWorkFlow, List<EmployeeRole> employeeRole) = _db.GetList<ApprovalWorkFlowChainFilter, EmployeeRole>(ConfigurationDetail.sp_approval_chain_detail_by_id, new
            {
                ApprovalWorkFlowId,
                CompanyId = _currentSession.CurrentUserDetail.CompanyId
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
                    ApprovalChainDetails = new List<ApprovalChainDetail>()
                };

                approvalWorkFlowChain.ApprovalChainDetails = (
                    from n in approvalWorkFlow
                    where n.ApprovalChainDetailId > 0
                    select new ApprovalChainDetail
                    {
                        ApprovalChainDetailId = n.ApprovalChainDetailId,
                        ApprovalWorkFlowId = n.ApprovalWorkFlowId,
                        AssignieId = n.AssignieId,
                        IsRequired = n.IsRequired,
                        LastUpdatedOn = n.LastUpdatedOn,
                        ApprovalStatus = n.ApprovalStatus,
                        AutoActionDays = n.AutoActionDays,
                        AutoActionType = n.AutoActionType
                    }
                 ).ToList<ApprovalChainDetail>();
            }

            employeeRole = employeeRole.FindAll(x => !x.IsDepartment && x.IsActive);

            return await Task.FromResult((approvalWorkFlowChain, employeeRole));
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