using Bot.CoreBottomHalf.CommonModal.API;
using Confluent.Kafka;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EMailService.Modal;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModalLayer;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : BaseController
    {
        private readonly IAttendanceService _attendanceService;
        private readonly ProducerConfig _producerConfig;
        private readonly List<KafkaServiceConfig> _kafkaServiceConfig;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(IAttendanceService attendanceService,
            ProducerConfig producerConfig,
            IOptions<List<KafkaServiceConfig>> options,
            ILogger<AttendanceController> logger)
        {
            _attendanceService = attendanceService;
            _producerConfig = producerConfig;
            _kafkaServiceConfig = options.Value;
            _logger = logger;
        }

        [HttpPost("GetAttendanceByUserId")]
        public async Task<ApiResponse> GetAttendanceByUserId(Attendance attendance)
        {
            try
            {
                var result = await _attendanceService.GetAttendanceByUserId(attendance);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, attendance);
            }
        }

        [HttpPost("SendEmailNotification")]
        [AllowAnonymous]
        public async Task<ApiResponse> SendEmailNotification(AttendanceRequestModal attendanceTemplateModel)
        {
            try
            {
                var config = _kafkaServiceConfig.Find(x => x.Topic == LocalConstants.SendEmail);
                if (config == null)
                {
                    throw new HiringBellException($"No configuration found for the kafka", "service name", LocalConstants.SendEmail, HttpStatusCode.InternalServerError);
                }

                var result = JsonConvert.SerializeObject(attendanceTemplateModel);
                _logger.LogInformation($"[Kafka] Starting kafka service to send mesage. Topic used: {config.Topic}, Service: {config.ServiceName}");
                using (var producer = new ProducerBuilder<Null, string>(_producerConfig).Build())
                {
                    _logger.LogInformation($"[Kafka] Sending mesage: {result}");
                    await producer.ProduceAsync(config.Topic, new Message<Null, string>
                    {
                        Value = result
                    });

                    producer.Flush(TimeSpan.FromSeconds(10));
                    _logger.LogInformation($"[Kafka] Messge send successfully");
                }

                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, attendanceTemplateModel);
            }
        }

        [HttpGet("BuildMonthBlankAttadanceData")]
        public IResponse<ApiResponse> BuildMonthBankAttadanceData()
        {
            return BuildResponse(null, HttpStatusCode.OK);
        }

        [HttpGet("GetPendingAttendanceById/{EmployeeId}/{UserTypeId}/{clientId}")]
        public IResponse<ApiResponse> GetPendingAttendanceById(long employeeId, int UserTypeId, long clientId)
        {
            try
            {
                var result = _attendanceService.GetAllPendingAttendanceByUserIdService(employeeId, UserTypeId, clientId);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { EmployeeId = employeeId, UserTypeId = UserTypeId, ClientId = clientId });
            }
        }

        [HttpPost("SubmitAttendance")]
        public async Task<ApiResponse> SubmitAttendance(Attendance attendance)
        {
            try
            {
                var result = await _attendanceService.SubmitAttendanceService(attendance);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, attendance);
            }
        }

        [HttpPost("GetMissingAttendanceRequest")]
        public async Task<ApiResponse> GetMissingAttendanceRequest(FilterModel filter)
        {
            try
            {
                var result = await _attendanceService.GetMissingAttendanceRequestService(filter);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filter);
            }
        }

        [HttpPost("GetMissingAttendanceApprovalRequest")]
        public async Task<ApiResponse> GetMissingAttendanceApprovalRequest(FilterModel filter)
        {
            try
            {
                var result = await _attendanceService.GetMissingAttendanceApprovalRequestService(filter);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filter);
            }
        }

        [HttpPost("RaiseMissingAttendanceRequest")]
        public async Task<ApiResponse> RaiseMissingAttendanceRequest(ComplaintOrRequestWithEmail compalintOrRequest)
        {
            try
            {
                var result = await _attendanceService.RaiseMissingAttendanceRequestService(compalintOrRequest);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, compalintOrRequest);
            }
        }

        [HttpPost("EnablePermission")]
        public IResponse<ApiResponse> EnablePermission(AttendenceDetail attendenceDetail)
        {
            try
            {
                var result = _attendanceService.EnablePermission(attendenceDetail);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, attendenceDetail);
            }
        }

        [HttpPost("GetEmployeePerformance")]
        public IResponse<ApiResponse> GetEmployeePerformance(AttendenceDetail attendenceDetail)
        {
            try
            {
                var result = _attendanceService.GetEmployeePerformanceService(attendenceDetail);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, attendenceDetail);
            }
        }

        [HttpPut("ApproveRaisedAttendanceRequest")]
        public async Task<ApiResponse> ApproveRaisedAttendanceRequest(List<ComplaintOrRequest> complaintOrRequests)
        {
            try
            {
                var result = await _attendanceService.ApproveRaisedAttendanceRequestService(complaintOrRequests);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, complaintOrRequests);
            }
        }

        [HttpPut("RejectRaisedAttendanceRequest")]
        public async Task<ApiResponse> RejectRaisedAttendanceRequestService(List<ComplaintOrRequest> complaintOrRequests)
        {
            try
            {
                var result = await _attendanceService.RejectRaisedAttendanceRequestService(complaintOrRequests);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, complaintOrRequests);
            }
        }

        [HttpPost("AdjustAttendance")]
        public async Task<ApiResponse> AdjustAttendance(Attendance attendance)
        {
            try
            {
                var result = await _attendanceService.AdjustAttendanceService(attendance);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, attendance);
            }
        }

        [HttpGet("GetLOPAdjustment/{month}/{year}")]
        public async Task<ApiResponse> GetLOPAdjustment([FromRoute] int month, [FromRoute] int year)
        {
            try
            {
                var result = await _attendanceService.GetLOPAdjustmentService(month, year);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { Month = month, Year = year });
            }
        }

        [HttpPost("GenerateAttendance")]
        public async Task<ApiResponse> GenerateAttendance(AttendenceDetail attendenceDetail)
        {
            try
            {
                await _attendanceService.GenerateAttendanceService(attendenceDetail);
                return BuildResponse(ApplicationConstants.Successfull, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, attendenceDetail);
            }
        }

        #region NEW-SERVICE

        [HttpPost("GetWeeklyAttendanceByUserId")]
        public async Task<ApiResponse> GetAttendanceByUserId(WeekDates weekDates)
        {
            try
            {
                var result = await _attendanceService.GetDailyAttendanceByUserIdService(weekDates);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, weekDates);
            }
        }

        [HttpGet("LoadAttendanceConfigData/{EmployeeId}")]
        public async Task<ApiResponse> LoadAttendanceConfigData(long EmployeeId)
        {
            try
            {
                var result = await _attendanceService.LoadAttendanceConfigDataService(EmployeeId);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, EmployeeId);
            }
        }

        [HttpPut("SubmitDailyAttendance")]
        public async Task<ApiResponse> SubmitDailyAttendance([FromBody] List<DailyAttendance> attendances)
        {
            try
            {
                var result = await _attendanceService.SubmitDailyAttendanceService(attendances);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, attendances);
            }
        }

        [HttpPut("SaveDailyAttendance")]
        public async Task<ApiResponse> SaveDailyAttendance([FromBody] List<DailyAttendance> attendances)
        {
            try
            {
                var result = await _attendanceService.SaveDailyAttendanceService(attendances);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, attendances);
            }
        }

        #endregion
    }
}