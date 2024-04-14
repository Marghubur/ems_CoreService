using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.HtmlTemplateModel;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using CoreBottomHalf.CommonModal.HtmlTemplateModel;
using EMailService.Modal;
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
            var resultSet = _db.FetchDataSet(Procedures.EMPLOYEE_PAYROLL_GET_BY_PAGE, new
            {
                ForYear = presentDate.Year,
                ForMonth = presentDate.Month,
                OffsetIndex = offsetindex,
                PageSize = pageSize,
                _currentSession.CurrentUserDetail.CompanyId
            }, false);

            if (resultSet == null || resultSet.Tables.Count != 4)
                throw HiringBellException.ThrowBadRequest($"[GetEmployeeDetail]: Employee data not found for date: {presentDate} of offSet: {offsetindex}");

            PayrollEmployeePageData payrollEmployeePageData = new PayrollEmployeePageData
            {
                payrollEmployeeData = Converter.ToList<PayrollEmployeeData>(resultSet.Tables[0]),
                leaveRequestDetail = Converter.ToType<LeaveRequestDetail>(resultSet.Tables[2]),
                hikeBonusSalaryAdhoc = Converter.ToList<HikeBonusSalaryAdhoc>(resultSet.Tables[3])
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
                x.PresentDayStatus == (int)AttendanceEnum.Approved ||
                x.PresentDayStatus == (int)AttendanceEnum.WeekOff);

            return attendanceDetailJsons;
        }

        private async Task CalculateRunPayrollForEmployees(Payroll payroll, PayrollCommonData payrollCommonData, bool reRunFlag)
        {
            _logger.LogInformation($"[CalculateRunPayrollForEmployees] method started");

            List<string> missingDetail = new List<string>();

            int totalDaysInMonth = DateTime.DaysInMonth(payrollCommonData.presentDate.Year, payrollCommonData.presentDate.Month);

            int offsetindex = 0;
            int pageSize = 15;
            List<FileDetail> fileDetails = new List<FileDetail>();
            PayrollMonthlyDetail payrollMonthlyDetail = null;

            while (true)
            {
                PayrollEmployeePageData payrollEmployeePageData = GetEmployeeDetail(payrollCommonData.presentDate, offsetindex, pageSize);

                if (payrollEmployeePageData.payrollEmployeeData.Count == 0)
                    break;

                List<PayrollEmployeeData> payrollEmployeeData = payrollEmployeePageData.payrollEmployeeData;

                // run pay cycle by considering actual days in months if payroll.PayCalculationId = 1
                // else run pay cycle by considering only weekdays in month if payroll.PayCalculationId = 0
                if (payroll.PayCalculationId != 1)
                {
                    totalDaysInMonth = TimezoneConverter.GetNumberOfWeekdaysInMonth(payrollCommonData.presentDate.Year, payrollCommonData.presentDate.Month);
                }

                bool IsTaxCalculationRequired = false;

                foreach (PayrollEmployeeData empPayroll in payrollEmployeeData)
                {
                    try
                    {
                        (bool flag, decimal amount) = GetAdhocComponentValue(empPayroll.EmployeeId, payrollEmployeePageData.hikeBonusSalaryAdhoc);
                        if (flag)
                        {
                            var taxDetails = JsonConvert.DeserializeObject<List<TaxDetails>>(empPayroll.TaxDetail);
                            if (taxDetails == null)
                                throw HiringBellException.ThrowBadRequest("TaxDtail not prepaired for the current employee. Fail to run payroll.");

                            var presentData = taxDetails.Find(x => x.Month == payrollCommonData.presentDate.Month);
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
                                    payrollDate = payrollCommonData.presentDate,
                                    isExcludeHolidays = payroll.IsExcludeHolidays,
                                    payCalculationId = payroll.PayCalculationId,
                                    employeeId = empPayroll.EmployeeId,
                                    payrollEmployeeDatas = payrollEmployeeData,
                                    totalDaysInMonth = totalDaysInMonth
                                };

                                (decimal actualMinutesWorked, decimal expectedMinuteInMonth) = await GetMonthlyMinutesConsideringWeekoffNHoliday(payrollCalculationModal);

                                payrollMonthlyDetail = GetUpdatedBreakup(payrollCommonData.utcPresentDate, expectedMinuteInMonth, actualMinutesWorked, empPayroll);
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

                                payrollMonthlyDetail.PayableToEmployee -= presentData.TaxPaid;
                                var pTax = GetProfessionalTaxAmount(payrollCommonData.ptaxSlab, payrollCommonData.company, empPayroll.CTC);

                                payrollMonthlyDetail.PayableToEmployee -= pTax;
                                payrollMonthlyDetail.GrossTotal = payrollMonthlyDetail.PayableToEmployee + amount;

                                payrollMonthlyDetail.TotalDeduction = presentData.TaxPaid;

                                presentData.IsPayrollCompleted = true;
                                empPayroll.TaxDetail = JsonConvert.SerializeObject(taxDetails);
                                await _declarationService.UpdateTaxDetailsService(empPayroll,
                                    payrollMonthlyDetail,
                                    payrollCommonData.utcPresentDate,
                                    IsTaxCalculationRequired
                                );

                                IsTaxCalculationRequired = false;
                            }
                        }
                        else
                        {
                            // salary is on hold for current employee.
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
                LocalConnectionString = _currentSession.LocalConnectionString,
                CompanyId = _currentSession.CurrentUserDetail.CompanyId
            };

            _ = Task.Run(() => _kafkaNotificationService.SendEmailNotification(payrollTemplateModel));
        }

        private (bool, decimal) GetAdhocComponentValue(long EmployeeId, List<HikeBonusSalaryAdhoc> hikeBonusSalaryAdhoc)
        {
            bool flag = true;
            decimal amount = 0;

            var presnetEmployees = hikeBonusSalaryAdhoc.Where(x => x.EmployeeId == EmployeeId).ToList();

            var record = presnetEmployees.Find(x => x.EmployeeId == EmployeeId &&
                    (
                        (x.ProcessStepId == (int)ProcessStep.NewJoineeExit && x.IsSalaryOnHold) ||
                        (x.ProcessStepId == (int)ProcessStep.SalaryArrear && x.IsSalaryOnHold)
                    )
                );
            if (record != null)
            {
                flag = false;
            }
            else
            {
                amount = presnetEmployees.Where(x => x.ProcessStepId != (int)ProcessStep.NewJoineeExit && x.ProcessStepId != (int)ProcessStep.SalaryArrear)
                    .Sum(i => i.Amount);
            }

            return (flag, amount);
        }

        private decimal GetProfessionalTaxAmount(List<PTaxSlab> ptaxs, Company company, decimal totalIncome)
        {
            var maxMinimumIncome = ptaxs.Where(i => i.StateName.ToUpper() == company.State.ToUpper()).Max(i => i.MinIncome);

            PTaxSlab ptax = null;
            if (totalIncome >= maxMinimumIncome)
                ptax = ptaxs.OrderByDescending(i => i.MinIncome).First();
            else
                ptax = ptaxs.Find(x => totalIncome >= x.MinIncome && totalIncome <= x.MaxIncome);

            if (ptax == null)
                return 0;

            return ptax.TaxAmount;
        }

        private async Task<Tuple<decimal, decimal>> GetMonthlyMinutesConsideringWeekoffNHoliday(PayrollCalculationModal payrollCalculationModal)
        {
            List<AttendanceJson> attendance = GetTotalAttendance(payrollCalculationModal.employeeId, payrollCalculationModal.payrollEmployeeDatas);
            decimal actualMinutesWorked = 0;
            if (attendance.Count > 0)
                actualMinutesWorked = attendance.Select(x => x.TotalMinutes).Aggregate((x, y) => x + y);

            // get total week off days in current month
            int weekOff = CalculateWeekOffs(attendance, payrollCalculationModal.shiftDetail);

            // get total holidays in current month
            decimal holidays = await _companyCalendar.GetHolidayCountInMonth(payrollCalculationModal.payrollDate.Month, payrollCalculationModal.payrollDate.Year);

            decimal expectedMinuteInMonth = 0M;
            // payrollCalculationModal.payCalculationId == 1 -> todays days in month including weekoffs
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
            {
                actualMinutesWorked = actualMinutesWorked - (holidays * payrollCalculationModal.shiftDetail.Duration);
                expectedMinuteInMonth = expectedMinuteInMonth - (holidays * payrollCalculationModal.shiftDetail.Duration);
            }
            else
            {
                actualMinutesWorked = actualMinutesWorked + (holidays * payrollCalculationModal.shiftDetail.Duration);
                expectedMinuteInMonth = expectedMinuteInMonth + (holidays * payrollCalculationModal.shiftDetail.Duration);
            }

            return Tuple.Create(actualMinutesWorked, expectedMinuteInMonth);
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

        private PayrollMonthlyDetail GetUpdatedBreakup(DateTime payrollDate, decimal minuteUsedForDeduction, decimal minutePresnet, PayrollEmployeeData empPayroll)
        {
            PayrollMonthlyDetail payrollMonthlyDetail = new PayrollMonthlyDetail();
            var salaryBreakup = JsonConvert.DeserializeObject<List<AnnualSalaryBreakup>>(empPayroll.CompleteSalaryDetail);
            if (salaryBreakup == null)
                throw HiringBellException.ThrowBadRequest("Salary breakup not found. Fail to run payroll.");

            var presentMonthSalaryDetail = salaryBreakup.Find(x => x.MonthNumber == payrollDate.Month);
            if (presentMonthSalaryDetail != null)
            {
                foreach (var item in presentMonthSalaryDetail.SalaryBreakupDetails)
                {
                    item.ActualAmount = item.FinalAmount;
                    item.FinalAmount = (item.FinalAmount / minuteUsedForDeduction) * minutePresnet;
                    switch (item.ComponentId)
                    {
                        case ComponentNames.EmployerPF:
                            payrollMonthlyDetail.PFByEmployer = item.FinalAmount;
                            payrollMonthlyDetail.PayableToEmployee += item.FinalAmount;
                            break;
                        case ComponentNames.EmployeePF:
                            payrollMonthlyDetail.PFByEmployee = item.FinalAmount;
                            payrollMonthlyDetail.PayableToEmployee += item.FinalAmount;
                            break;
                        case ComponentNames.CTCId:
                        case ComponentNames.GrossId:
                            break;
                        default:
                            payrollMonthlyDetail.PayableToEmployee += item.FinalAmount;
                            break;
                    }
                }

                presentMonthSalaryDetail.IsPayrollExecutedForThisMonth = true;
            }

            empPayroll.CompleteSalaryDetail = JsonConvert.SerializeObject(salaryBreakup);
            return payrollMonthlyDetail;
        }


        private PayrollCommonData GetCommonPayrollData()
        {
            PayrollCommonData payrollCommonData = new PayrollCommonData();
            var result = _db.FetchDataSet(Procedures.Payroll_Cycle_Setting_Get_All, new { _currentSession.CurrentUserDetail.CompanyId });
            if (result.Tables.Count != 7)
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

            if (result.Tables[6].Rows.Count == 0)
                throw HiringBellException.ThrowBadRequest($"[GetCommonPayrollData]: Company detail not found. Please contact to admin");

            payrollCommonData.payroll = Converter.ToType<Payroll>(result.Tables[0]);
            payrollCommonData.salaryComponents = Converter.ToList<SalaryComponents>(result.Tables[1]);
            payrollCommonData.surchargeSlabs = Converter.ToList<SurChargeSlab>(result.Tables[2]);
            payrollCommonData.ptaxSlab = Converter.ToList<PTaxSlab>(result.Tables[3]);
            payrollCommonData.salaryGroups = Converter.ToList<SalaryGroup>(result.Tables[4]);
            payrollCommonData.shiftDetail = Converter.ToList<ShiftDetail>(result.Tables[5]);
            payrollCommonData.company = Converter.ToType<Company>(result.Tables[6]);

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

        public async Task RunPayrollCycle(DateTime runDate, bool reRunFlag = false)
        {
            _logger.LogInformation($"[RunPayrollCycle] method started");
            PayrollCommonData payrollCommonData = GetCommonPayrollData();

            var payroll = payrollCommonData.payroll;

            if (payroll.FinancialYear != runDate.Year && payroll.FinancialYear + 1 != runDate.Year)
            {
                throw HiringBellException.ThrowBadRequest("Invalid year or month. Current financial year is: " + payroll.FinancialYear);
            }

            _currentSession.TimeZone = TZConvert.GetTimeZoneInfo(payroll.TimezoneName);
            payrollCommonData.timeZone = _currentSession.TimeZone;

            payrollCommonData.utcPresentDate = runDate;
            payrollCommonData.presentDate = _timezoneConverter.ToTimeZoneDateTime(runDate, _currentSession.TimeZone);

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

    enum ProcessStep
    {
        LeaveAttendance = 1,
        NewJoineeExit,
        BonusOverTime,
        ReimbursmentAdhoc,
        SalaryArrear,
        OverrideComponent
    }
}
