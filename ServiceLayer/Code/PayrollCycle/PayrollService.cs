using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EMailService.Modal.Payroll;
using EMailService.Service;
using Microsoft.Extensions.Logging;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using ModalLayer.Modal.Leaves;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace ServiceLayer.Code.PayrollCycle
{
    public class PayrollService : IPayrollService
    {
        private readonly IDb _db;
        private readonly ITimezoneConverter _timezoneConverter;
        private readonly CurrentSession _currentSession;
        private readonly IDeclarationService _declarationService;
        private readonly IEMailManager _eMailManager;
        private readonly IBillService _billService;
        private readonly ILogger<PayrollService> _logger;
        private readonly ICompanyCalendar _companyCalendar;
        private readonly KafkaNotificationService _kafkaNotificationService;

        public PayrollService(ITimezoneConverter timezoneConverter,
            IDb db,
            IDeclarationService declarationService,
            CurrentSession currentSession,
            IEMailManager eMailManager,
            IBillService billService,
            ILogger<PayrollService> logger,
            ICompanyCalendar companyCalendar,
            KafkaNotificationService kafkaNotificationService)
        {
            _db = db;
            _timezoneConverter = timezoneConverter;
            _currentSession = currentSession;
            _declarationService = declarationService;
            _eMailManager = eMailManager;
            _billService = billService;
            _logger = logger;
            _companyCalendar = companyCalendar;
            _kafkaNotificationService = kafkaNotificationService;
        }

        private PayrollEmployeePageData GetEmployeeDetail(DateTime presentDate, int offsetindex, int pageSize)
        {
            var resultSet = _db.FetchDataSet("sp_employee_payroll_get_by_page", new
            {
                ForYear = presentDate.Year,
                ForMonth = presentDate.Month,
                OffsetIndex = offsetindex,
                PageSize = pageSize
            }, false);

            if (resultSet == null || resultSet.Tables.Count != 3)
                throw HiringBellException.ThrowBadRequest($"[GetEmployeeDetail]: Employee data not found for date: {presentDate} of offSet: {offsetindex}");

            PayrollEmployeePageData payrollEmployeePageData = new PayrollEmployeePageData
            {
                payrollEmployeeData = Converter.ToList<PayrollEmployeeData>(resultSet.Tables[0]),
                leaveRequestDetail = Converter.ToType<LeaveRequestDetail>(resultSet.Tables[2])
            };

            if (payrollEmployeePageData.payrollEmployeeData == null)
                throw HiringBellException.ThrowBadRequest("Employee payroll data not found");

            List<EmployeeDeclaration> employeeDeclarations = Converter.ToList<EmployeeDeclaration>(resultSet.Tables[1]);

            Parallel.ForEach(payrollEmployeePageData.payrollEmployeeData, x =>
            {
                var context = employeeDeclarations.Find(i => i.EmployeeId == x.EmployeeId);
                if (context != null)
                    x.employeeDeclaration = context;
                else
                    x.employeeDeclaration = null;
            });

            return payrollEmployeePageData;
        }

        private decimal TotalLeaveInPresentMonthCycle(LeaveRequestDetail leaveRequestDetail, DateTime payrollDate)
        {
            var lastMonthDate = payrollDate.AddMonths(-1).AddDays(1);
            if (!string.IsNullOrEmpty(leaveRequestDetail.LeaveDetail) && leaveRequestDetail.LeaveDetail != "[]")
            {
                var leaves = JsonConvert.DeserializeObject<List<CompleteLeaveDetail>>(leaveRequestDetail.LeaveDetail);
                var workingMonthLeaves = leaves.Where(x => x.LeaveFromDay.Month == payrollDate.Month ||
                                            x.LeaveFromDay.Month == lastMonthDate.Month).ToList();

                var allLeaveDates = workingMonthLeaves.SelectMany(x => x.LeaveDates).ToList();
                var finalLeaveDates = (from i in allLeaveDates
                                       where i >= lastMonthDate && i <= payrollDate
                                       select i).ToList();

                return finalLeaveDates.Count;
            }

            return 0;
        }

        private List<AttendanceJson> GetTotalAttendance(long employeeId, List<PayrollEmployeeData> payrollEmployeeData)
        {
            var attrDetail = payrollEmployeeData.Find(x => x.EmployeeId == employeeId);

            List<AttendanceJson> attendanceDetailJsons = new List<AttendanceJson>();
            if (attrDetail == null)
                throw HiringBellException.ThrowBadRequest("Employee attendance detail not found. Please contact to admin.");

            if (!string.IsNullOrEmpty(attrDetail.AttendanceDetail))
            {
                attendanceDetailJsons = JsonConvert.DeserializeObject<List<AttendanceJson>>(attrDetail.AttendanceDetail);
                if (attendanceDetailJsons == null)
                    throw HiringBellException.ThrowBadRequest("Attendance detail not found while running payroll cycle.");
            }

            attendanceDetailJsons = attendanceDetailJsons.FindAll(x =>
                x.PresentDayStatus == (int)AttendanceEnum.Approved);

            //decimal totalDays = attendanceDetailJsons.Count(x => x.SessionType == (int)SessionType.FullDay);
            //totalDays = totalDays + (attendanceDetailJsons.Count(x => x.SessionType != (int)SessionType.FullDay) * 0.5m);
            return attendanceDetailJsons;
        }

        private async Task CalculateRunPayrollForEmployees(Payroll payroll, PayrollCommonData payrollCommonData, bool reRunFlag)
        {
            _logger.LogInformation($"[CalculateRunPayrollForEmployees] method started");

            List<string> missingDetail = new List<string>();
            DateTime payrollDate = new DateTime(payrollCommonData.utcPresentDate.Year, payrollCommonData.utcPresentDate.Month, payroll.PayCycleDayOfMonth);

            var payrollDateTimeZone = _timezoneConverter.ToTimeZoneDateTime(payrollDate, _currentSession.TimeZone);
            int totalDaysInMonth = DateTime.DaysInMonth(payrollDateTimeZone.Year, payrollDateTimeZone.Month);

            if (payrollCommonData.presentDate.Subtract(payrollDate).TotalDays < 0)
            {
                throw HiringBellException.ThrowBadRequest($"Payroll run date is defined: {payrollDate.ToString("dd MMMM, yyyy")}. You can run after defined date.");
            }

            int offsetindex = 0;
            int pageSize = 15;
            List<FileDetail> fileDetails = new List<FileDetail>();
            PayrollMonthlyDetail payrollMonthlyDetail = new PayrollMonthlyDetail();
            while (true)
            {
                PayrollEmployeePageData payrollEmployeePageData = GetEmployeeDetail(payrollDate, offsetindex, pageSize);
                // payrollDate = _timezoneConverter.ToSpecificTimezoneDateTime(payrollCommonData.timeZone, payrollDate);

                if (payrollEmployeePageData.payrollEmployeeData.Count == 0)
                    break;

                List<PayrollEmployeeData> payrollEmployeeData = payrollEmployeePageData.payrollEmployeeData;

                // run pay cycle by considering actual days in months if value = 1
                // else run pay cycle by considering only weekdays in month value = 0
                if (payroll.PayCalculationId != 1)
                {
                    totalDaysInMonth = TimezoneConverter.GetNumberOfWeekdaysInMonth(payrollCommonData.presentDate.Year, payrollDate.Month);
                }

                bool IsTaxCalculationRequired = false;

                foreach (PayrollEmployeeData empPayroll in payrollEmployeeData)
                {
                    try
                    {
                        var taxDetails = JsonConvert.DeserializeObject<List<TaxDetails>>(empPayroll.TaxDetail);
                        if (taxDetails == null)
                            throw HiringBellException.ThrowBadRequest("TaxDtail not prepaired for the current employee. Fail to run payroll.");

                        var presentData = taxDetails.Find(x => x.Month == payrollDate.Month);
                        if (presentData == null)
                            throw HiringBellException.ThrowBadRequest("Invalid taxdetail found. Fail to run payroll.");

                        if (!presentData.IsPayrollCompleted || reRunFlag)
                        {
                            var shiftDetail = payrollCommonData.shiftDetail.Find(x => x.WorkShiftId == empPayroll.WorkShiftId);
                            if (shiftDetail == null)
                                throw HiringBellException.ThrowBadRequest("Shift detail not found. Please contact to admin");

                            PayrollCalculationModal payrollCalculationModal = new PayrollCalculationModal
                            {
                                shiftDetail = shiftDetail,
                                payrollDate = payrollDate,
                                isExcludeHolidays = payroll.IsExcludeHolidays,
                                payCalculationId = payroll.PayCalculationId,
                                employeeId = empPayroll.EmployeeId,
                                payrollEmployeeDatas = payrollEmployeeData,
                                totalDaysInMonth = totalDaysInMonth
                            };

                            (decimal actualMinutesWorked, decimal expectedMinuteInMonth) = await GetMonthlyActualandExpectedMinute(payrollCalculationModal);

                            UpdateSalaryBreakup(payrollDate, expectedMinuteInMonth, actualMinutesWorked, empPayroll, payrollMonthlyDetail);
                            if (actualMinutesWorked != expectedMinuteInMonth)
                            {
                                var newAmount = (presentData.TaxDeducted / expectedMinuteInMonth) * actualMinutesWorked;
                                presentData.TaxPaid = newAmount;
                                presentData.TaxDeducted = newAmount;
                                IsTaxCalculationRequired = true;
                            }
                            else
                            {
                                presentData.TaxPaid = presentData.TaxDeducted;
                            }

                            payrollMonthlyDetail.TotalPayableToEmployees -= presentData.TaxPaid;
                            payrollMonthlyDetail.TotalDeduction = presentData.TaxPaid;

                            presentData.IsPayrollCompleted = true;
                            empPayroll.TaxDetail = JsonConvert.SerializeObject(taxDetails);
                            await _declarationService.UpdateTaxDetailsService(empPayroll, payrollCommonData, IsTaxCalculationRequired);
                            IsTaxCalculationRequired = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        missingDetail.Add(string.Concat(empPayroll.EmployeeName, "  [", empPayroll.EmployeeId, "]"));
                    }

                    _logger.LogInformation($"[CalculateRunPayrollForEmployees] method: generating and sending payroll email");
                    Task task = Task.Run(async () => await SendPayrollGeneratedEmail(payrollCommonData.presentDate, empPayroll.EmployeeId));
                }

                offsetindex = offsetindex + pageSize;
            }

            _logger.LogInformation($"[CalculateRunPayrollForEmployees] method ended");
            // _ = Task.Run(() => SendEmail(missingDetail, payrollDate.ToString("MMMM")));
            PayrollTemplateModel payrollTemplateModel = new PayrollTemplateModel
            {
                CompanyName = _currentSession.CurrentUserDetail.CompanyName,
                ToAddress = new List<string> { "istiyaq.mi9@gmail.com", "marghub12@gmail.com" },
                kafkaServiceName = KafkaServiceName.Payroll,
                Body = BuildPayrollTemplateBody(missingDetail),
                LocalConnectionString = _currentSession.LocalConnectionString
            };

            _ = Task.Run(() => _kafkaNotificationService.SendEmailNotification(payrollTemplateModel));
            await UpdatePayrollMonthlyDetail(payrollMonthlyDetail, payrollDate);
        }

        private async Task<Tuple<decimal, decimal>> GetMonthlyActualandExpectedMinute(PayrollCalculationModal payrollCalculationModal)
        {
            List<AttendanceJson> attendance = GetTotalAttendance(payrollCalculationModal.employeeId, payrollCalculationModal.payrollEmployeeDatas);
            decimal actualMinutesWorked = attendance.Select(x => x.TotalMinutes).Aggregate((x, y) => x + y);
            int weekOff = CalculateWeekOffs(attendance, payrollCalculationModal.shiftDetail);

            decimal holidays = await _companyCalendar.GetHolidayCountInMonth(payrollCalculationModal.payrollDate.Month, payrollCalculationModal.payrollDate.Year);
            decimal expectedMinuteInMonth = 0M;
            if (payrollCalculationModal.payCalculationId == 1)
            {
                actualMinutesWorked = actualMinutesWorked + (weekOff * payrollCalculationModal.shiftDetail.Duration);
                expectedMinuteInMonth = payrollCalculationModal.totalDaysInMonth * payrollCalculationModal.shiftDetail.Duration;
            }
            else
            {
                expectedMinuteInMonth = (payrollCalculationModal.totalDaysInMonth - weekOff) * payrollCalculationModal.shiftDetail.Duration;
            }

            if (payrollCalculationModal.isExcludeHolidays)
                expectedMinuteInMonth = (payrollCalculationModal.totalDaysInMonth - holidays) * payrollCalculationModal.shiftDetail.Duration;
            else
                actualMinutesWorked = actualMinutesWorked + (holidays * payrollCalculationModal.shiftDetail.Duration);

            return Tuple.Create(actualMinutesWorked, expectedMinuteInMonth);
        }

        private async Task UpdatePayrollMonthlyDetail(PayrollMonthlyDetail payrollMonthlyDetail, DateTime payrollDate)
        {
            payrollMonthlyDetail.ForYear = payrollDate.Year;
            payrollMonthlyDetail.ForMonth = payrollDate.Month;
            payrollMonthlyDetail.PayrollStatus = 16;
            payrollMonthlyDetail.Reason = string.Empty;
            payrollMonthlyDetail.PaymentRunDate = payrollDate;
            payrollMonthlyDetail.ExecutedBy = _currentSession.CurrentUserDetail.UserId;
            payrollMonthlyDetail.ExecutedOn = DateTime.Now;
            payrollMonthlyDetail.CompanyId = _currentSession.CurrentUserDetail.CompanyId;

            await _db.ExecuteAsync("sp_payroll_monthly_detail_ins", new
            {
                payrollMonthlyDetail.PayrollMonthlyDetailId,
                payrollMonthlyDetail.ForYear,
                payrollMonthlyDetail.ForMonth,
                payrollMonthlyDetail.TotalPayableToEmployees,
                payrollMonthlyDetail.TotalPFByEmployer,
                payrollMonthlyDetail.TotalProfessionalTax,
                payrollMonthlyDetail.TotalDeduction,
                payrollMonthlyDetail.PayrollStatus,
                payrollMonthlyDetail.Reason,
                payrollMonthlyDetail.PaymentRunDate,
                payrollMonthlyDetail.ProofOfDocumentPath,
                payrollMonthlyDetail.ExecutedBy,
                payrollMonthlyDetail.ExecutedOn,
                payrollMonthlyDetail.CompanyId
            });
        }

        private string BuildPayrollTemplateBody(List<string> missingDetail)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"<div style=\"margin-bottom: 25px;\">Payroll of the month {DateTime.Now} completed</div>");

            string status = "<b style=\"color: green\">Ran successfully</b>";
            if (missingDetail.Count > 0 && missingDetail.Count <= 20)
            {
                foreach (var detail in missingDetail)
                {
                    builder.Append("<div>" + detail + "</div>");
                }

                status = "<b style=\"color: red\">Partially successfull</b>";
            }
            else if (missingDetail.Count > 20)
            {
                builder.Append("<div style=\"color: red;\"><b>Alert!!!</b>  " +
                    "payroll cycle failed, more than 20 employee payroll cycle raise exception. " +
                    "For detail please check the log files. Total failed count: " + missingDetail.Count + "</div>");

                status = "<b style=\"color: red\">Many failed</b>";
            }

            builder.AppendLine($"<div style=\"margin-top 50px;\">Status: {status}</div>");
            return builder.ToString();
        }

        private static void UpdateSalaryBreakup(DateTime payrollDate, decimal minuteUsedForDeduction, decimal minutePresnet,
            PayrollEmployeeData empPayroll, PayrollMonthlyDetail payrollMonthlyDetail)
        {
            var salaryBreakup = JsonConvert.DeserializeObject<List<AnnualSalaryBreakup>>(empPayroll.CompleteSalaryDetail);
            if (salaryBreakup == null)
                throw HiringBellException.ThrowBadRequest("Salary breakup not found. Fail to run payroll.");

            var presentMonthSalaryDetail = salaryBreakup.Find(x => x.MonthNumber == payrollDate.Month);
            if (presentMonthSalaryDetail != null)
            {
                foreach (var item in presentMonthSalaryDetail.SalaryBreakupDetails)
                {
                    item.FinalAmount = (item.FinalAmount / minuteUsedForDeduction) * minutePresnet;
                    switch (item.ComponentId)
                    {
                        case ComponentNames.ProfessionalTax:
                            payrollMonthlyDetail.TotalProfessionalTax += item.FinalAmount;
                            break;
                        case ComponentNames.EmployerPF:
                            payrollMonthlyDetail.TotalPFByEmployer += item.FinalAmount;
                            break;
                        case ComponentNames.CTCId:
                            break;
                        case ComponentNames.GrossId:
                            break;
                        default:
                            payrollMonthlyDetail.TotalPayableToEmployees += item.FinalAmount;
                            break;
                    }
                }

                presentMonthSalaryDetail.IsPayrollExecutedForThisMonth = true;
            }

            empPayroll.CompleteSalaryDetail = JsonConvert.SerializeObject(salaryBreakup);
        }


        private PayrollCommonData GetCommonPayrollData()
        {
            PayrollCommonData payrollCommonData = new PayrollCommonData();
            var result = _db.FetchDataSet("sp_payroll_cycle_setting_get_all");
            if (result.Tables.Count != 6)
                throw HiringBellException.ThrowBadRequest($"[GetCommonPayrollData]: Fail to get payroll cycle data to run it. Please contact to admin");

            if (result.Tables[0].Rows.Count != 1)
                throw HiringBellException.ThrowBadRequest($"[GetCommonPayrollData]: Payroll cycle and company setting detail not found. Please contact to admin");

            if (result.Tables[1].Rows.Count == 0)
                throw HiringBellException.ThrowBadRequest($"[GetCommonPayrollData]: Salary component not found. Please contact to admin");

            if (result.Tables[2].Rows.Count == 0)
                throw HiringBellException.ThrowBadRequest($"[GetCommonPayrollData]: Surcharge slab detail not found. Please contact to admin");

            if (result.Tables[3].Rows.Count == 0)
                throw HiringBellException.ThrowBadRequest($"[GetCommonPayrollData]: Professional tax detail not found. Please contact to admin");

            if (result.Tables[4].Rows.Count == 0)
                throw HiringBellException.ThrowBadRequest($"[GetCommonPayrollData]: Salary group detail not found. Please contact to admin");

            if (result.Tables[5].Rows.Count == 0)
                throw HiringBellException.ThrowBadRequest($"[GetCommonPayrollData]: Shift detail not found. Please contact to admin");

            payrollCommonData.payroll = Converter.ToType<Payroll>(result.Tables[0]);
            payrollCommonData.salaryComponents = Converter.ToList<SalaryComponents>(result.Tables[1]);
            payrollCommonData.surchargeSlabs = Converter.ToList<SurChargeSlab>(result.Tables[2]);
            payrollCommonData.ptaxSlab = Converter.ToList<PTaxSlab>(result.Tables[3]);
            payrollCommonData.salaryGroups = Converter.ToList<SalaryGroup>(result.Tables[4]);
            payrollCommonData.shiftDetail = Converter.ToList<ShiftDetail>(result.Tables[5]);

            return payrollCommonData;
        }

        private bool DoesRunPayrollCycle(int payrollDate, DateTime presentDate)
        {
            bool flag = false;

            if (payrollDate == presentDate.Day)
            {
                flag = true;
            }
            else
            {
                int totalDays = DateTime.DaysInMonth(presentDate.Year, presentDate.Month);
                if (totalDays <= payrollDate)
                    flag = true;
            }

            return flag;
        }

        public async Task RunPayrollCycle(int i, bool reRunFlag = false)
        {
            _logger.LogInformation($"[RunPayrollCycle] method started");
            PayrollCommonData payrollCommonData = GetCommonPayrollData();
            var payroll = payrollCommonData.payroll;

            _currentSession.TimeZone = TZConvert.GetTimeZoneInfo(payroll.TimezoneName);
            payrollCommonData.timeZone = _currentSession.TimeZone;

            DateTime firstDayOfYear = new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            payrollCommonData.utcPresentDate = firstDayOfYear.AddMonths(+i);
            payrollCommonData.presentDate = _timezoneConverter.ToTimeZoneDateTime(DateTime.UtcNow, _currentSession.TimeZone);

            int totalDaysInMonth = DateTime.DaysInMonth(payrollCommonData.utcPresentDate.Year, payrollCommonData.utcPresentDate.Month);
            if (payroll.PayCycleDayOfMonth > totalDaysInMonth)
                payroll.PayCycleDayOfMonth = totalDaysInMonth;

            switch (payroll.PayFrequency)
            {
                case "monthly":
                    _logger.LogInformation($"[RunPayrollCycle] method: runnig monthly payroll");
                    await CalculateRunPayrollForEmployees(payroll, payrollCommonData, reRunFlag);
                    break;
                case "daily":
                    break;
                case "hourly":
                    break;
            }

            _logger.LogInformation($"[RunPayrollCycle] method ended");
            await Task.CompletedTask;
        }

        private async Task SendPayrollGeneratedEmail(DateTime presentDate, long empId)
        {
            _logger.LogInformation($"[SendPayrollGeneratedEmail] method started");
            PayslipGenerationModal payslipGenerationModal = new PayslipGenerationModal
            {
                Year = presentDate.Year,
                Month = presentDate.Month,
                EmployeeId = empId
            };

            _logger.LogInformation($"[SendPayrollGeneratedEmail] method: Generating payslip");

            await _billService.GeneratePayslipService(payslipGenerationModal);

            _logger.LogInformation($"[SendPayrollGeneratedEmail] method ended");
            await Task.CompletedTask;
        }

        private int CalculateWeekOffs(List<AttendanceJson> attendanceJsons, ShiftDetail shiftDetail)
        {
            int weekOffCount = 0;
            if (attendanceJsons.Count > 0)
            {
                attendanceJsons.ForEach(x =>
                {
                    switch (x.AttendanceDay.DayOfWeek)
                    {
                        case DayOfWeek.Sunday:
                            if (!shiftDetail.IsSun)
                                weekOffCount++;
                            break;
                        case DayOfWeek.Monday:
                            if (!shiftDetail.IsMon)
                                weekOffCount++;
                            break;
                        case DayOfWeek.Tuesday:
                            if (!shiftDetail.IsTue)
                                weekOffCount++;
                            break;
                        case DayOfWeek.Wednesday:
                            if (!shiftDetail.IsWed)
                                weekOffCount++;
                            break;
                        case DayOfWeek.Thursday:
                            if (!shiftDetail.IsThu)
                                weekOffCount++;
                            break;
                        case DayOfWeek.Friday:
                            if (!shiftDetail.IsFri)
                                weekOffCount++;
                            break;
                        case DayOfWeek.Saturday:
                            if (!shiftDetail.IsSat)
                                weekOffCount++;
                            break;
                    }
                });
            }
            return weekOffCount;
        }

        private async Task SendEmail(List<string> missingDetail, string presentDate)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"<div style=\"margin-bottom: 25px;\">Payroll of the month {presentDate} completed</div>");

            string status = "<b style=\"color: green\">Ran successfully</b>";
            if (missingDetail.Count > 0 && missingDetail.Count <= 20)
            {
                foreach (var detail in missingDetail)
                {
                    builder.Append("<div>" + detail + "</div>");
                }

                status = "<b style=\"color: red\">Partially successfull</b>";
            }
            else if (missingDetail.Count > 20)
            {
                builder.Append("<div style=\"color: red;\"><b>Alert!!!</b>  " +
                    "payroll cycle failed, more than 20 employee payroll cycle raise exception. " +
                    "For detail please check the log files. Total failed count: " + missingDetail.Count + "</div>");

                status = "<b style=\"color: red\">Many failed</b>";
            }

            builder.AppendLine($"<div style=\"margin-top 50px;\">Status: {status}</div>");

            EmailSenderModal emailSenderModal = new EmailSenderModal
            {
                To = new List<string> { "istiyaq.mi9@gmail.com", "marghub12@gmail.com" },
                CC = new List<string>(),
                BCC = new List<string>(),
                FileDetails = new List<FileDetail>(),
                Subject = "Monthly Payslip | " + presentDate,
                Body = builder.ToString(),
                Title = "Payslip"
            };

            _logger.LogInformation($"[SendPayrollGeneratedEmail] method: Sending email");
            await _eMailManager.SendMailAsync(emailSenderModal);
        }
    }
}
