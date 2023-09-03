using BottomhalfCore.Flags;
using Education.MiddlewareServices.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using ModalLayer.Modal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolInMindServer.MiddlewareServices.Src
{
    public class RequestHandler : IRequesHandlerManager
    {
        private RequestDelegate _next;
        private CurrentSession currentSession;
        private IConfiguration configuration;
        private string TokenName;
        private List<string> NoCheck;
        public RequestHandler()
        {
        }
        public async Task HandleRequest(HttpContext context, RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            this.configuration = configuration;
            TokenName = configuration.GetValue<string>("Configuration:TokenName");
            NoCheck = configuration.GetSection("Configuration:NoCheck").Get<List<string>>();
            try
            {
                if (context.Request.Method == HttpMethods.Options)
                    await _next(context);
                else
                {
                    (Boolean IsValidToken, int StatusCode) = ValidateRequest(context);
                    if (!IsValidToken)
                    {
                        //HttpResponse response = null;
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("This is a test");
                        return;
                        ////var request = context.Request;
                        ////var originalBodyStream = context.Response.Body;
                        //using (var responseBody = new MemoryStream())
                        //{
                        //    //response = context.Response;
                        //    //response.Body = responseBody;
                        //    //string responseBodyContent = await ReadResponseBody(response);
                        //    await responseBody.CopyToAsync(context.Response.Body);
                        //}
                    }
                    else
                    {
                        context.Response.OnStarting(() =>
                        {
                            context.Response.Headers.Add(TokenName, currentSession.Authorization);
                            return Task.CompletedTask;
                        });
                        await _next(context);
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
            request.EnableBuffering();
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

        private async Task SafeLog(long responseMillis, int statusCode, string method, string path, string queryString, string requestBody, string responseBody)
        {
            //SystemUserInfo systemUserInfo = this.Instance.Get(this.currentSession.Authorization) as SystemUserInfo;
            await Task.Run(() =>
            {
                //string[] logData = new string[5];
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
            currentSession = context.RequestServices.GetService(typeof(CurrentSession)) as CurrentSession;
            currentSession.RequestPath = context.Request != null ? context.Request.Path.Value : "";
            currentSession.FileUploadFolderName = this.configuration.GetValue<string>("Configuration:FolderName");
            Parallel.ForEach(context.Request.Headers, header =>
            {
                if (header.Value.FirstOrDefault() != null)
                {
                    if (header.Key == TokenName)
                        currentSession.Authorization = header.Value.ToString().Trim();
                }
            });

            if (NoCheck.Where(x => x.ToLower() == currentSession.RequestPath.Replace(@"/api/", "").ToLower()).FirstOrDefault() == null)
            {
                if (!string.IsNullOrEmpty(currentSession.Authorization)) 
                {
                    EFlags flag = EFlags.InvalidToken;
                    StatusCode = (int)flag;
                    IsValidToken = false;
                    if (flag == EFlags.Success)
                    {
                        IsValidToken = true;
                        StatusCode = (int)EFlags.Success;
                    }
                }
                else
                {
                    StatusCode = (int)EFlags.TokenNotFound;
                    IsValidToken = false;
                }
            }
            else
            {
                IsValidToken = true;
                StatusCode = (int)EFlags.Success;
            }
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
