using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Authorize(Roles = Role.Admin)]
    [Route("api/[controller]")]
    [ApiController]
    public class ApprovalChainController : BaseController
    {
        private readonly IApprovalChainService _approvalChainService;
        public ApprovalChainController(IApprovalChainService approvalChainService)
        {
            _approvalChainService = approvalChainService;
        }

        [HttpGet("GetApprovalChain")]
        public async Task<ApiResponse> GetApprovalChain(FilterModel filterModel)
        {
            var result = await _approvalChainService.GetApprovalChainService(filterModel);
            return BuildResponse(result);
        }

        [HttpPost("InsertApprovalChain")]
        public async Task<ApiResponse> InsertApprovalChain(ApprovalWorkFlowChain approvalWorkFlowModal)
        {
            var result = await _approvalChainService.InsertApprovalChainService(approvalWorkFlowModal);
            return BuildResponse(result);
        }

        [HttpPost("GetPageDate")]
        public async Task<ApiResponse> GetPageDate(FilterModel filter)
        {
            var result = await _approvalChainService.GetPageDateService(filter);
            return BuildResponse(result);
        }

        [HttpGet("GetApprovalChainData/{ApprovalWorkFlowId}")]
        public async Task<ApiResponse> GetApprovalChainData(int ApprovalWorkFlowId)
        {
            var result = await _approvalChainService.GetApprovalChainData(ApprovalWorkFlowId);
            return BuildResponse(result);
        }

        [HttpDelete("DeleteApprovalChain/{ApprovalChainDetailId}")]
        public async Task<ApiResponse> DeleteApprovalChain([FromRoute] int ApprovalChainDetailId)
        {
            var result = await _approvalChainService.DeleteApprovalChainService(ApprovalChainDetailId);
            return BuildResponse(result);
        }
    }
}
