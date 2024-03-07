using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using Newtonsoft.Json;
using System;
using System.Net;

namespace OnlineDataBuilder.Controllers
{
    [Authorize]
    public abstract class BaseController : ControllerBase
    {
        protected ApiResponse apiResponse;
        protected string responseMessage = string.Empty;
        public BaseController()
        {
            apiResponse = new ApiResponse();
        }

        [NonAction]
        public HiringBellException Throw(Exception ex, dynamic request = null)
        {
            string msg = JsonConvert.SerializeObject(new
            {
                ex.Message,
                RequestObject = request
            });

            return new HiringBellException(msg, ex);
        }
        
        public ApiResponse BuildResponse(dynamic Data, HttpStatusCode httpStatusCode = HttpStatusCode.OK, string Resion = null, string Token = null)
        {
            apiResponse.AuthenticationToken = Token;
            apiResponse.HttpStatusMessage = Resion;
            apiResponse.HttpStatusCode = httpStatusCode;
            apiResponse.ResponseBody = Data;
            return apiResponse;
        }


        public ApiResponse GenerateResponse(HttpStatusCode httpStatusCode, dynamic Data = null)
        {
            apiResponse.HttpStatusCode = httpStatusCode;
            apiResponse.ResponseBody = Data;
            return apiResponse;
        }
    }
}