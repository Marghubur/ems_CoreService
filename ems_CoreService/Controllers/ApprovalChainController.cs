using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
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
            try
            {
                var result = await _approvalChainService.GetApprovalChainService(filterModel);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }

        [HttpPost("InsertApprovalChain")]
        public async Task<ApiResponse> InsertApprovalChain(ApprovalWorkFlowChain approvalWorkFlowModal)
        {
            try
            {
                var result = await _approvalChainService.InsertApprovalChainService(approvalWorkFlowModal);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, approvalWorkFlowModal);
            }
        }

        [HttpPost("GetPageDate")]
        public async Task<ApiResponse> GetPageDate(FilterModel filter)
        {
            try
            {
                var result = await _approvalChainService.GetPageDateService(filter);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filter);
            }
        }

        [HttpGet("GetApprovalChainData/{ApprovalWorkFlowId}")]
        public async Task<ApiResponse> GetApprovalChainData(int ApprovalWorkFlowId)
        {
            try
            {
                var result = await _approvalChainService.GetApprovalChainData(ApprovalWorkFlowId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, ApprovalWorkFlowId);
            }
        }

        [HttpDelete("DeleteApprovalChain/{ApprovalChainDetailId}")]
        public async Task<ApiResponse> DeleteApprovalChain([FromRoute] int ApprovalChainDetailId)
        {
            try
            {
                var result = await _approvalChainService.DeleteApprovalChainService(ApprovalChainDetailId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, ApprovalChainDetailId);
            }
        }
    }
}