using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.API;
using Bot.CoreBottomHalf.CommonModal.Enums;
using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using Bot.CoreBottomHalf.CommonModal.Kafka;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModalLayer.Modal;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using Org.BouncyCastle.Utilities.Net;
using ServiceLayer.Code;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SchoolInMindServer.MiddlewareServices
{
    public class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        public static bool LoggingFlag = false;
        public static string FileLocation;
        public ExceptionHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ApplicationConfiguration applicationConfiguration, KafkaNotificationService kafkaNotificationService, CurrentSession currentSession)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (HiringBellException ex)
            {
                if (currentSession.Environment == DefinedEnvironments.Production)
                {
                    _ = Task.Run(async () =>
                    {
                        await SendExceptionEmailService(ex.UserMessage, ex.RequestBody, ex, kafkaNotificationService);
                    });
                }

                await HandleHiringBellExceptionMessageAsync(context, ex.UserMessage, ex, applicationConfiguration);
            }
            catch (Exception ex)
            {
                if (currentSession.Environment == DefinedEnvironments.Production)
                {
                    _ = Task.Run(async () =>
                    {
                        await SendExceptionEmailService(string.Empty, string.Empty, ex, kafkaNotificationService);
                    });
                }

                await HandleExceptionMessageAsync(context, string.Empty, ex, applicationConfiguration);
            }
        }

        private static async Task<Task> HandleHiringBellExceptionMessageAsync(HttpContext context, string userMessage, HiringBellException ex, ApplicationConfiguration applicationConfiguration)
        {
            string result = await BuildEmstumErrorResponse(context, userMessage, ex);

            if (applicationConfiguration.IsLoggingEnabled)
            {
                await LogErrorToFile(result, applicationConfiguration);
            }

            context.Response.ContentType = ApplicationConstants.ApplicationJson;
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return await Task.FromResult(context.Response.WriteAsync(result));
        }

        private static async Task<string> BuildEmstumErrorResponse(HttpContext context, string userMessage, HiringBellException ex)
        {
            if (string.IsNullOrEmpty(ex.RequestBody))
            {
                ex.RequestBody = await GetRequestBodyAsync(context); // For POST, PUT, DELETE
            }

            return JsonConvert.SerializeObject(new ApiResponse
            {
                AuthenticationToken = string.Empty,
                HttpStatusCode = HttpStatusCode.BadRequest,
                HttpStatusMessage = userMessage,
                ResponseBody = new
                {
                    context.Request.Method,
                    Url = context.Request.Path,
                    context.Request.QueryString,
                    context.Request.RouteValues,
                    Request = ex.RequestBody,
                    ExceptionMessage = ex.Message
                }
            });
        }

        private static async Task<Task> HandleExceptionMessageAsync(HttpContext context, string userMessage, Exception ex, ApplicationConfiguration applicationConfiguration)
        {
            var result = await BuildErrorResponse(context, userMessage, ex);

            if (applicationConfiguration.IsLoggingEnabled)
            {
                await LogErrorToFile(result, applicationConfiguration);
            }

            context.Response.ContentType = ApplicationConstants.ApplicationJson;
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest; ;
            return await Task.FromResult(context.Response.WriteAsync(result));
        }

        private static async Task<string> BuildErrorResponse(HttpContext context, string userMessage, Exception ex)
        {
            context.Response.ContentType = ApplicationConstants.ApplicationJson;
            var body = await GetRequestBodyAsync(context); // For POST, PUT, DELETE

            return JsonConvert.SerializeObject(new ApiResponse
            {
                AuthenticationToken = string.Empty,
                HttpStatusCode = HttpStatusCode.BadRequest,
                HttpStatusMessage = ex.Message,
                ResponseBody = new
                {
                    context.Request.Method,
                    Url = context.Request.Path,
                    context.Request.QueryString,
                    context.Request.RouteValues,
                    Request = body
                }
            });
        }

        private static async Task<string> GetRequestBodyAsync(HttpContext context)
        {
            HttpRequest request = context.Request;
            string requestBody = string.Empty;

            if (request.HasFormContentType)
            {
                // Handle form data (e.g., multipart/form-data)
                var form = await context.Request.ReadFormAsync();
                requestBody = form.ToString();
            }
            else if (request.ContentType == "application/json")
            {
                // Handle JSON request bodies
                using (var reader = new StreamReader(request.Body, Encoding.UTF8)) // Specify UTF-8 encoding
                {
                    var body = await reader.ReadToEndAsync();
                    requestBody = body;
                }
            }
            else
            {
                // Handle other request body formats (optional)
                using (var reader = new StreamReader(request.Body, Encoding.UTF8)) // Specify UTF-8 encoding
                {
                    var body = await reader.ReadToEndAsync();
                    requestBody = body;
                }
            }

            return requestBody;
        }

        private static async Task LogErrorToFile(string requestMessage, ApplicationConfiguration applicationConfiguration)
        {
            await Task.Run(() =>
            {
                var path = Path.Combine(applicationConfiguration.LoggingFilePath, DateTime.Now.ToString("dd_MM_yyyy") + ".txt");
                File.AppendAllTextAsync(path, requestMessage);
            });
        }

        private async Task SendExceptionEmailService(string userMessage,
            string requestPayload,
            Exception ex,
            KafkaNotificationService kafkaNotificationService)
        {
            KafkaPayload kafkaPayload = new KafkaPayload
            {
                exceptionPayloadetail = new ExceptionPayloadetail
                {
                    UserMessage = userMessage,
                    RequestPayload = requestPayload,
                    StackTrace = ex.StackTrace,
                    SystemMessage = ex.Message
                },
                LocalConnectionString = string.Empty, // currentSession.LocalConnectionString,
                kafkaServiceName = KafkaServiceName.UnhandledException,
                UtcTimestamp = DateTime.Now,
                ToAddress = new List<string> { "marghub12@gmail.com", "istiyaq.mi9@gmail.com", "kumarvivek1502@gmail.com" }
            };
            await kafkaNotificationService.SendEmailNotification(kafkaPayload);
            await Task.CompletedTask;
        }
    }
}
