using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;

namespace OnlineDataBuilder.Controllers
{
    [Authorize]
    [Route("api/[Controller]")]
    [ApiController]
    public class LiveUrlController : BaseController
    {
        private readonly ILiveUrlService _liveUrlService;

        public LiveUrlController(ILiveUrlService liveUrlService)
        {
            _liveUrlService = liveUrlService;
        }

        [HttpPost("loadpagedata")]
        public IResponse<ApiResponse> LoadPageData(FilterModel filterModel)
        {
            try
            {
            var Result = _liveUrlService.LoadPageData(filterModel);
            BuildResponse(Result, System.Net.HttpStatusCode.OK);
            return apiResponse;
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }

        [HttpPost("saveliveUrl")]
        public IResponse<ApiResponse> SaveLiveUrl(LiveUrlModal liveUrlModal)
        {
            try
            {
            var Result = _liveUrlService.SaveUrlService(liveUrlModal);
            BuildResponse(Result, System.Net.HttpStatusCode.OK);
            return apiResponse;
            }
            catch (Exception ex)
            {
                throw Throw(ex, liveUrlModal);
            }
        }

        [AllowAnonymous]
        [HttpPost("bottomhalfliveurl/{getRoute}")]
        public IResponse<ApiResponse> GetLiveData(string getRoute, [FromBody] List<string> data)
        {
            return null;
        }
    }
}