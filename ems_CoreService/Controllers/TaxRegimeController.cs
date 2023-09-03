using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using OnlineDataBuilder.ContextHandler;
using ServiceLayer.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaxRegimeController : BaseController
    {
        private readonly ITaxRegimeService _taxRegimeService;

        public TaxRegimeController(ITaxRegimeService taxRegimeService)
        {
            _taxRegimeService = taxRegimeService;
        }

        [HttpPost("AddUpdateTaxRegimeDesc")]
        public IResponse<ApiResponse> AddUpdateTaxRegimeDesc(TaxRegimeDesc taxRegimeDesc)
        {
            var result = _taxRegimeService.AddUpdateTaxRegimeDescService(taxRegimeDesc);
            return BuildResponse(result);
        }

        [HttpGet("GetAllRegime")]
        public IResponse<ApiResponse> GetAllRegime()
        {
            var result = _taxRegimeService.GetAllRegimeService();
            return BuildResponse(result);
        }

        [HttpPost("AddUpdateTaxRegime")]
        public async Task<ApiResponse> AddUpdateTaxRegime(List<TaxRegime> taxRegimes)
        {
            var result = await _taxRegimeService.AddUpdateTaxRegimeService(taxRegimes);
            return BuildResponse(result);
        }

        [HttpPost("AddUpdateAgeGroup")]
        public IResponse<ApiResponse> AddUpdateAgeGroup(TaxAgeGroup taxAgeGroup)
        {
            var result = _taxRegimeService.AddUpdateAgeGroupService(taxAgeGroup);
            return BuildResponse(result);
        }

        [HttpDelete("DeleteTaxRegime/{TaxregimeId}")]
        public IResponse<ApiResponse> DeleteTaxRegime(int TaxregimeId)
        {
            var result = _taxRegimeService.DeleteTaxRegimeService(TaxregimeId);
            return BuildResponse(result);
        }

        [HttpPost("AddUpdatePTaxSlab")]
        public async Task<ApiResponse> AddUpdatePTaxSlab(List<PTaxSlab> pTaxSlabs)
        {
            var result = await _taxRegimeService.AddUpdatePTaxSlabService(pTaxSlabs);
            return BuildResponse(result);
        }

        [HttpDelete("DeletePTaxSlab/{PtaxSlabId}")]
        public IResponse<ApiResponse> DeletePTaxSlab([FromRoute] int PtaxSlabId)
        {
            var result = _taxRegimeService.DeletePTaxSlabService(PtaxSlabId);
            return BuildResponse(result);
        }

        [HttpGet("GetPTaxSlabByCompId/{CompanyId}")]
        public IResponse<ApiResponse> GetPTaxSlabByCompId([FromRoute] int CompanyId)
        {
            var result = _taxRegimeService.GetPTaxSlabByCompIdService(CompanyId);
            return BuildResponse(result);
        }
        [HttpGet("GetAllSurchargeSlab")]
        public IResponse<ApiResponse> GetAllSurchargeSlab()
        {
            var result = _taxRegimeService.GetAllSurchargeService();
            return BuildResponse(result);
        }

        [HttpPost("AddUpdateSurchargeSlab")]
        public async Task<ApiResponse> AddUpdateSurchargeSlab(List<SurChargeSlab> surChargeSlabs)
        {
            var result = await _taxRegimeService.AddUpdateSurchargeService(surChargeSlabs);
            return BuildResponse(result);
        }
        [HttpDelete("DeleteSurchargeSlab/{SurchargeSlabId}")]
        public IResponse<ApiResponse> DeleteSurchargeSlab([FromRoute] long SurchargeSlabId)
        {
            var result = _taxRegimeService.DeleteSurchargeSlabService(SurchargeSlabId);
            return BuildResponse(result);
        }
    }
}
