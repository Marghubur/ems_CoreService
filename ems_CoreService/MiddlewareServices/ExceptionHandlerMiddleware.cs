using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.API;
using Bot.CoreBottomHalf.CommonModal.Enums;
using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using Bot.CoreBottomHalf.CommonModal.Kafka;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using Newtonsoft.Json;
using Org.BouncyCastle.Utilities.Net;
using ServiceLayer.Code;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
                        await SendExceptionEmailService(ex.Message, currentSession, kafkaNotificationService);
                    });
                }

                await HandleHiringBellExceptionMessageAsync(context, ex.Message, ex.InnerException, ex.StackTrace);
            }
            catch (Exception ex)
            {
                if (currentSession.Environment == DefinedEnvironments.Production)
                {
                    _ = Task.Run(async () =>
                    {
                        await SendExceptionEmailService(ex.Message, currentSession, kafkaNotificationService);
                    });
                }

                if (applicationConfiguration.IsLoggingEnabled)
                    await HandleExceptionWriteToFile(context, ex, applicationConfiguration);
                else
                    await HandleHiringBellExceptionMessageAsync(context, ex.Message, ex.InnerException, ex.StackTrace, HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<Task> HandleHiringBellExceptionMessageAsync(HttpContext context,
            string message, Exception innerException, string stackTrace, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
        {
            context.Response.ContentType = ApplicationConstants.ApplicationJson;
            int statusCode = (int)httpStatusCode;

            string innerMessage = string.Empty;
            if (innerException != null)
            {
                innerMessage = innerException!.Message;
                stackTrace = innerException.StackTrace;
            }

            var result = JsonConvert.SerializeObject(new ApiResponse
            {
                AuthenticationToken = string.Empty,
                HttpStatusCode = httpStatusCode,
                HttpStatusMessage = message,
                ResponseBody = new { message, InnerMessage = innerMessage, stackTrace }
            });

            context.Response.ContentType = ApplicationConstants.ApplicationJson;
            context.Response.StatusCode = statusCode;
            return await Task.FromResult(context.Response.WriteAsync(result));
        }

        private static async Task<Task> HandleExceptionWriteToFile(HttpContext context, HiringBellException e, ApplicationConfiguration applicationConfiguration)
        {
            context.Response.ContentType = ApplicationConstants.ApplicationJson;
            int statusCode = (int)e.HttpStatusCode;
            var result = new ApiResponse
            {
                AuthenticationToken = string.Empty,
                HttpStatusCode = e.HttpStatusCode,
                HttpStatusMessage = e.UserMessage
            };

            context.Response.ContentType = ApplicationConstants.ApplicationJson;
            context.Response.StatusCode = statusCode;
            await Task.Run(() =>
            {
                var path = Path.Combine(applicationConfiguration.LoggingFilePath, DateTime.Now.ToString("dd_MM_yyyy") + ".txt");
                result.ResponseBody = new { e.UserMessage, InnerMessage = e.InnerException?.Message, e.StackTrace };
                File.AppendAllTextAsync(path, JsonConvert.SerializeObject(result));
            });

            result.ResponseBody = new { e.UserMessage, InnerMessage = e.InnerException?.Message };
            return await Task.FromResult(context.Response.WriteAsync(JsonConvert.SerializeObject(result)));
        }

        private static async Task<Task> HandleExceptionWriteToFile(HttpContext context, Exception e, ApplicationConfiguration applicationConfiguration)
        {
            context.Response.ContentType = ApplicationConstants.ApplicationJson;
            int statusCode = (int)HttpStatusCode.InternalServerError;
            var result = new ApiResponse
            {
                AuthenticationToken = string.Empty,
                HttpStatusCode = HttpStatusCode.InternalServerError,
                HttpStatusMessage = e.Message
            };

            context.Response.ContentType = ApplicationConstants.ApplicationJson;
            context.Response.StatusCode = statusCode;
            await Task.Run(() =>
            {
                var path = Path.Combine(applicationConfiguration.LoggingFilePath, DateTime.Now.ToString("dd_MM_yyyy") + ".txt");
                result.ResponseBody = new { e.Message, InnerMessage = e.InnerException?.Message, e.StackTrace };
                File.AppendAllTextAsync(path, JsonConvert.SerializeObject(result));
            });

            result.ResponseBody = new { e.Message, InnerMessage = e.InnerException?.Message };
            return await Task.FromResult(context.Response.WriteAsync(JsonConvert.SerializeObject(result)));
        }

        private async Task SendExceptionEmailService(string message, CurrentSession currentSession, KafkaNotificationService kafkaNotificationService)
        {
            KafkaPayload kafkaPayload = new KafkaPayload
            {
                Body = message,
                LocalConnectionString = string.Empty, // currentSession.LocalConnectionString,
                kafkaServiceName = KafkaServiceName.UnhandledException,
                UtcTimestamp = DateTime.Now,
                ToAddress = new List<string> { "marghub12@gmail.com", "istiyaq.mi9@gmail.com" }
            };
            await kafkaNotificationService.SendEmailNotification(kafkaPayload);
            await Task.CompletedTask;
        }
    }
}
