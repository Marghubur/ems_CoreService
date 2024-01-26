using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.Flags;
using Education.MiddlewareServices.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using ModalLayer.Modal;
using Newtonsoft.Json;
using OnlineDataBuilder.ContextHandler;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Education.MiddlewareServices.Src
{
    public class TransactionHandlerWithLog : IRequesHandlerManager
    {
        private RequestDelegate _next;
        public TransactionHandlerWithLog()
        {
        }
        public async Task HandleRequest(HttpContext context, RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            try
            {
                (Boolean IsValidToken, int StatusCode) = ValidateRequest(context);
                if (!IsValidToken)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    var finalResponseBody = context.Response.Body;
                    using (var responseBody = new MemoryStream())
                    {
                        context.Response.Body = responseBody;
                        string Message = GetStatusMessage(StatusCode);
                        IResponse<ApiResponse> apiResponse = new ApiResponse();
                        apiResponse.HttpStatusMessage = Message;
                        apiResponse.HttpStatusCode = HttpStatusCode.Unauthorized;
                        var ErrorMsgBuffer = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(apiResponse));
                        responseBody.Write(ErrorMsgBuffer, 0, ErrorMsgBuffer.Count());
                        finalResponseBody.Write(ErrorMsgBuffer, 0, ErrorMsgBuffer.Count());
                        await responseBody.CopyToAsync(finalResponseBody);
                    }
                }
                else
                {
                    var request = context.Request;
                    var requestTime = DateTime.UtcNow;
                    var requestBodyContent = await ReadRequestBody(request);
                    var originalBodyStream = context.Response.Body;
                    using (var responseBody = new MemoryStream())
                    {
                        var response = context.Response;
                        response.Body = responseBody;
                        await _next(context);

                        string responseBodyContent = null;
                        responseBodyContent = await ReadResponseBody(response);
                        await responseBody.CopyToAsync(originalBodyStream);

                        await SafeLog(response.StatusCode, request.Method, request.Path,
                            request.QueryString.ToString(), requestBodyContent, responseBodyContent);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<string> ReadRequestBody(HttpRequest request)
        {
            HttpRequestRewindExtensions.EnableBuffering(request);
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            var bodyAsText = Encoding.UTF8.GetString(buffer);
            request.Body.Seek(0, SeekOrigin.Begin);

            return bodyAsText;
        }

        private async Task<string> ReadResponseBody(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var bodyAsText = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            return bodyAsText;
        }

        private async Task SafeLog(int statusCode, string method, string path, string queryString, string requestBody, string responseBody)
        {
            //SystemUserInfo systemUserInfo = this.Instance.Get(this.currentSession.Authorization) as SystemUserInfo;
            await Task.Run(() =>
            {
                string[] logData = new string[5];
                //RequestResponseModal requestResponseModal = new RequestResponseModal
                //{
                //    ResponseMillis = responseMillis,
                //    StatusCode = statusCode,
                //    Method = method,
                //    Path = path,
                //    QueryString = queryString,
                //    RequestBody = requestBody
                //};
                //logData[0] = JsonConvert.SerializeObject(requestResponseModal);
                //logData[1] = responseBody;
                //logData[2] = systemUserInfo != null ? systemUserInfo.UserId.ToString() : "-1";
                //logData[3] = systemUserInfo != null ? systemUserInfo.Facility_Id.ToString() : "-1";
                //logData[4] = systemUserInfo != null ? systemUserInfo.CompanyId.ToString() : "-1";

                //var controllerClass = new ControllerClass(new System.Data.DataSet());
                //var oCommonBusiness = new CommonBusiness(controllerClass.InputData);

                //oCommonBusiness.SubmitTERMPointMobileAppTransLogs(logData);
            });
        }
        public (bool, int) ValidateRequest(HttpContext context)
        {
            Boolean IsValidToken = false;
            int StatusCode = default(int);
            CurrentSession currentSession = null;
            object currentObject = context.RequestServices.GetService(typeof(CurrentSession));
            if (currentObject != null && currentObject is CurrentSession) currentSession = currentObject as CurrentSession;
            currentSession.RequestPath = context.Request != null ? context.Request.Path.Value : "";
            Parallel.ForEach(context.Request.Headers, header =>
            {
                if (header.Value.FirstOrDefault() != null)
                {
                    switch (header.Key)
                    {
                        case "Authorization":
                            if (header.Value.ToString().IndexOf("JWT") == 0)
                                currentSession.Authorization = header.Value.ToString().Replace("JWT", "").Trim();
                            break;
                    }
                }
            });

            ////if (!this.Instance.IsNoAuthController(currentSession.RequestPath.Replace(@"/api/", "")))
            ////{
            //    if (!string.IsNullOrEmpty(currentSession.Authorization))
            //    {
            //        (IsValidToken, StatusCode) = this.Instance.ValidateToken(currentSession.Authorization);
            //    }
            //    else
            //    {
            //        StatusCode = (int)TokenResponseCode.TokenNotFound;
            //        IsValidToken = false;
            //    }
            ////}
            //else 
            IsValidToken = true;
            return (IsValidToken, StatusCode);
        }

        private string GetStatusMessage(int StatusCode)
        {
            string Message = default(string);
            switch (StatusCode)
            {
                case (int)EFlags.InvalidToken:
                    Message = "Requested token is invalid.";
                    break;
                case (int)EFlags.Success:
                    Message = "Successful token value.";
                    break;
                case (int)EFlags.TokenExpired:
                    Message = "Requested token is expired.";
                    break;
                case (int)EFlags.TokenNotFound:
                    Message = "Requested token is not found.";
                    break;
            }
            return Message;
        }
    }
}
