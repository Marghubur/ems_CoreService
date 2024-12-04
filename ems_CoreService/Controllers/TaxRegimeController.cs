using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
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
            try
            {
                var result = _taxRegimeService.AddUpdateTaxRegimeDescService(taxRegimeDesc);
                return BuildResponse(result);

            }
            catch (Exception ex)
            {
                throw Throw(ex, taxRegimeDesc);
            }
        }

        [HttpGet("GetAllRegime")]
        public IResponse<ApiResponse> GetAllRegime()
        {
            try
            {
                var result = _taxRegimeService.GetAllRegimeService();
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        [HttpPost("AddUpdateTaxRegime")]
        public async Task<ApiResponse> AddUpdateTaxRegime(List<TaxRegime> taxRegimes)
        {
            try
            {
                var result = await _taxRegimeService.AddUpdateTaxRegimeService(taxRegimes);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, taxRegimes);
            }
        }

        [HttpPost("AddUpdateAgeGroup")]
        public IResponse<ApiResponse> AddUpdateAgeGroup(TaxAgeGroup taxAgeGroup)
        {
            try
            {
                var result = _taxRegimeService.AddUpdateAgeGroupService(taxAgeGroup);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, taxAgeGroup);
            }
        }

        [HttpDelete("DeleteTaxRegime/{TaxregimeId}")]
        public IResponse<ApiResponse> DeleteTaxRegime(int TaxregimeId)
        {
            try
            {
                var result = _taxRegimeService.DeleteTaxRegimeService(TaxregimeId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, TaxregimeId);
            }
        }

        [HttpPost("AddUpdatePTaxSlab")]
        public async Task<ApiResponse> AddUpdatePTaxSlab(List<PTaxSlab> pTaxSlabs)
        {
            try
            {
                var result = await _taxRegimeService.AddUpdatePTaxSlabService(pTaxSlabs);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [HttpDelete("DeletePTaxSlab/{PtaxSlabId}")]
        public IResponse<ApiResponse> DeletePTaxSlab([FromRoute] int PtaxSlabId)
        {
            try
            {
                var result = _taxRegimeService.DeletePTaxSlabService(PtaxSlabId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, PtaxSlabId);
            }
        }

        [HttpGet("GetPTaxSlabByCompId/{CompanyId}")]
        public IResponse<ApiResponse> GetPTaxSlabByCompId([FromRoute] int CompanyId)
        {
            try
            {
                var result = _taxRegimeService.GetPTaxSlabByCompIdService(CompanyId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, CompanyId);
            }
        }

        [HttpGet("GetAllSurchargeSlab")]
        public IResponse<ApiResponse> GetAllSurchargeSlab()
        {
            try
            {
                var result = _taxRegimeService.GetAllSurchargeService();
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [HttpPost("AddUpdateSurchargeSlab")]
        public async Task<ApiResponse> AddUpdateSurchargeSlab(List<SurChargeSlab> surChargeSlabs)
        {
            try
            {
                var result = await _taxRegimeService.AddUpdateSurchargeService(surChargeSlabs);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, surChargeSlabs);
            }
        }

        [HttpDelete("DeleteSurchargeSlab/{SurchargeSlabId}")]
        public IResponse<ApiResponse> DeleteSurchargeSlab([FromRoute] long SurchargeSlabId)
        {
            try
            {
                var result = _taxRegimeService.DeleteSurchargeSlabService(SurchargeSlabId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, SurchargeSlabId);
            }
        }

        [Authorize(Roles = Role.Admin)]
        [HttpPost("UploadEmployeeExcel")]
        public async Task<ApiResponse> UploadEmployeeExcel([FromForm] IFormFile file)
        {
            try
            {
                await _taxRegimeService.ReadProfessionalTaxDataService(file);
                return BuildResponse("file found");
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }
    }
}