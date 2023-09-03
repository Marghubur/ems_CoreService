using ModalLayer.Modal;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IApprovalChainService
    {
        Task<dynamic> GetApprovalChainData(int ApprovalWorkFlowId);
        Task<List<ApprovalWorkFlowModal>> GetPageDateService(FilterModel filterModel);
        Task<string> InsertApprovalChainService(ApprovalWorkFlowChain approvalWorkFlowModal);
        Task<ApprovalWorkFlowModal> GetApprovalChainService(FilterModel filterModel);
        Task<string> DeleteApprovalChainService(int approvalChainDetailId);
    }
}
