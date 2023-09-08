using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using EMailService.Service;
using Microsoft.Extensions.Logging;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using Newtonsoft.Json;
using NUnit.Framework.Interfaces;
using OpenXmlPowerTools;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly FileLocationDetail _fileLocationDetail;

        public PayrollService(ITimezoneConverter timezoneConverter,
            IDb db,
            IDeclarationService declarationService,
            CurrentSession currentSession,
            IEMailManager eMailManager,
            FileLocationDetail fileLocationDetail,
            IBillService billService,
            ILogger<PayrollService> logger,
            ICompanyCalendar companyCalendar)
        {
            _db = db;
            _timezoneConverter = timezoneConverter;
            _currentSession = currentSession;
            _declarationService = declarationService;
            _eMailManager = eMailManager;
            _billService = billService;
            _fileLocationDetail = fileLocationDetail;
            _logger = logger;
            _companyCalendar = companyCalendar;
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
                leaveRequestNotificatios = Converter.ToList<LeaveRequestNotification>(resultSet.Tables[2])
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

        private decimal GetTotalAttendance(PayrollEmployeeData empPayroll, List<PayrollEmployeeData> payrollEmployeeData, DateTime payrollDate, Payroll payroll)
        {
            var attrDetail = payrollEmployeeData.Find(x => x.EmployeeId == empPayroll.EmployeeId );

            if (attrDetail == null)
                throw HiringBellException.ThrowBadRequest("Attendance detail not found while running payroll cycle.");

            List<AttendanceDetailJson> attendanceDetailJsons = JsonConvert.DeserializeObject<List<AttendanceDetailJson>>(attrDetail.AttendanceDetail);
            attendanceDetailJsons.AddRange(JsonConvert.DeserializeObject<List<AttendanceDetailJson>>(attrDetail.SecondMonthAttendanceDetail));
            attendanceDetailJsons = attendanceDetailJsons.FindAll(x => x.AttendanceDay.Date.Subtract(payrollDate.AddMonths(-1).Date).TotalDays > 0
                                   && x.AttendanceDay.Date.Subtract(payrollDate.Date).TotalDays <= 0);

            if (payroll.IsExcludeWeeklyOffs)
            {
                attendanceDetailJsons = attendanceDetailJsons.FindAll(x => x.PresentDayStatus != (int)ItemStatus.Rejected && x.PresentDayStatus != (int)ItemStatus.NotSubmitted
                            && x.PresentDayStatus != 3);
            }
            else if (payroll.IsExcludeHolidays)
            {
                attendanceDetailJsons = attendanceDetailJsons.FindAll(x => x.PresentDayStatus != (int)ItemStatus.Rejected && x.PresentDayStatus != (int)ItemStatus.NotSubmitted
                            && x.PresentDayStatus != 4);
            }
            else if (payroll.IsExcludeHolidays && payroll.IsExcludeWeeklyOffs)
            {
                attendanceDetailJsons = attendanceDetailJsons.FindAll(x => x.PresentDayStatus != (int)ItemStatus.Rejected && x.PresentDayStatus != (int)ItemStatus.NotSubmitted
                            && x.PresentDayStatus != 4 && x.PresentDayStatus != 3);
            }
            else
            {
                attendanceDetailJsons = attendanceDetailJsons.FindAll(x => x.PresentDayStatus != (int)ItemStatus.Rejected && x.PresentDayStatus != (int)ItemStatus.NotSubmitted);
            }
            decimal totalDays = attendanceDetailJsons.Count(x => x.SessionType == 1);
            totalDays = totalDays + (attendanceDetailJsons.Count(x => x.SessionType != 1) * 0.5m);
            return totalDays;
        }

        private async Task CalculateRunPayrollForEmployees(Payroll payroll, PayrollCommonData payrollCommonData)
        {
            _logger.LogInformation($"[CalculateRunPayrollForEmployees] method started");

            DateTime payrollDate = (DateTime)_currentSession.TimeZoneNow;
            int offsetindex = 0;
            decimal daysPresnet = 0;
            int totalDaysInMonth = 0;
            int pageSize = 15;
            while (true)
            {
                PayrollEmployeePageData payrollEmployeePageData = GetEmployeeDetail(payrollDate, offsetindex, pageSize);
                if (payrollEmployeePageData.payrollEmployeeData.Count == 0)
                    break;

                List<PayrollEmployeeData> payrollEmployeeData = payrollEmployeePageData.payrollEmployeeData;

                // run pay cycle by considering actual days in months
                if (payroll.PayCalculationId == 1)
                {
                    // totalDaysInMonth = DateTime.DaysInMonth(payrollCommonData.presentDate.Year, payrollDate.Month);
                    totalDaysInMonth = payrollDate.Date.Subtract(payrollDate.AddMonths(-1).Date).Days;
                }
                else // run pay cycle by considering only weekdays in month
                {
                    totalDaysInMonth = TimezoneConverter.GetNumberOfWeekdaysInMonth(payrollCommonData.presentDate.Year, payrollDate.Month);
                }

                bool IsTaxCalculationRequired = false;
                int daysUsedForDeduction = 0;

                payrollEmployeeData = CombineMonthlyRecord(payrollEmployeeData);

                foreach (PayrollEmployeeData empPayroll in payrollEmployeeData)
                {
                    try
                    {
                        daysUsedForDeduction = totalDaysInMonth;
                        DateTime doj = _timezoneConverter.ToTimeZoneDateTime(empPayroll.Doj, _currentSession.TimeZone);
                        if (doj.Month == payrollDate.Month && doj.Year == payrollDate.Year)
                        {
                            daysUsedForDeduction = totalDaysInMonth - doj.Day + 1;
                        }

                        daysPresnet = GetTotalAttendance(empPayroll, payrollEmployeeData, payrollDate, payroll);

                        var taxDetails = JsonConvert.DeserializeObject<List<TaxDetails>>(empPayroll.TaxDetail);
                        if (taxDetails == null)
                            throw HiringBellException.ThrowBadRequest("Invalid taxdetail found. Fail to run payroll.");

                        var presentData = taxDetails.Find(x => x.Month == payrollDate.Month);
                        if (presentData == null)
                            throw HiringBellException.ThrowBadRequest("Invalid taxdetail found. Fail to run payroll.");

                        var shiftDetail = payrollCommonData.shiftDetail.Find(x => x.WorkShiftId == empPayroll.WorkShiftId);
                        if (shiftDetail == null)
                            throw HiringBellException.ThrowBadRequest("Shift detail not found. Please contact to admin");

                        int holidays = 0;
                        int weekOff = 0;
                        if (payroll.IsExcludeHolidays)
                            holidays = _companyCalendar.GetHolidayBetweenTwoDates(payrollDate.AddMonths(-1), payrollDate).Result;

                        if (payroll.IsExcludeWeeklyOffs)
                            weekOff = _companyCalendar.CountWeekOffBetweenTwoDates(payrollDate.AddMonths(-1), payrollDate, shiftDetail).Result;

                        decimal shiftDuration = (shiftDetail.Duration / 60);
                        decimal hrsUsedForDeduction = (daysUsedForDeduction - holidays - weekOff) * shiftDuration;
                        decimal hrsPresnet = daysPresnet * shiftDuration;
                        if (!presentData.IsPayrollCompleted)
                        {
                            UpdateSalaryBreakup(payrollDate, hrsUsedForDeduction, hrsPresnet, empPayroll);
                            if (hrsPresnet != hrsUsedForDeduction)
                            {
                                var newAmount = (presentData.TaxDeducted / hrsUsedForDeduction) * hrsPresnet;
                                presentData.TaxPaid = newAmount;
                                presentData.TaxDeducted = newAmount;
                                presentData.IsPayrollCompleted = true;
                                IsTaxCalculationRequired = true;
                            }
                            else
                            {
                                presentData.TaxPaid = presentData.TaxDeducted;
                                presentData.IsPayrollCompleted = true;
                            }

                            empPayroll.TaxDetail = JsonConvert.SerializeObject(taxDetails);
                            await _declarationService.UpdateTaxDetailsService(empPayroll, payrollCommonData, IsTaxCalculationRequired);
                            IsTaxCalculationRequired = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    _logger.LogInformation($"[CalculateRunPayrollForEmployees] method: generating and sending payroll email");
                    Task task = Task.Run(async () => await SendPayrollGeneratedEmail(payrollCommonData.presentDate, empPayroll.EmployeeId));
                }

                offsetindex = offsetindex + pageSize;
            }

            _logger.LogInformation($"[CalculateRunPayrollForEmployees] method ended");
            await Task.CompletedTask;
        }

        private List<PayrollEmployeeData> CombineMonthlyRecord(List<PayrollEmployeeData> payrollEmployeeData)
        {
            List<PayrollEmployeeData> newPayrollEmployeeData = new List<PayrollEmployeeData>();
            foreach (var pay in payrollEmployeeData)
            {
                var record = newPayrollEmployeeData.Find(x => x.EmployeeId == pay.EmployeeId);
                if (record == null)
                    newPayrollEmployeeData.Add(pay);
                else
                    record.SecondMonthAttendanceDetail = pay.AttendanceDetail;
            }

            return newPayrollEmployeeData;
        }

        private static void UpdateSalaryBreakup(DateTime payrollDate, decimal hrsUsedForDeduction, decimal hrsPresnet, PayrollEmployeeData empPayroll)
        {
            var salaryBreakup = JsonConvert.DeserializeObject<List<AnnualSalaryBreakup>>(empPayroll.CompleteSalaryDetail);
            if (salaryBreakup == null)
                throw HiringBellException.ThrowBadRequest("Salary breakup not found. Fail to run payroll.");

            var presentMonthSalaryDetail = salaryBreakup.Find(x => x.MonthNumber == payrollDate.Month);
            if (presentMonthSalaryDetail != null)
            {
                foreach (var item in presentMonthSalaryDetail.SalaryBreakupDetails)
                {
                    item.FinalAmount = (item.FinalAmount / hrsUsedForDeduction) * hrsPresnet;
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

        public async Task RunPayrollCycle(int i)
        {
            _logger.LogInformation($"[RunPayrollCycle] method started");
            PayrollCommonData payrollCommonData = GetCommonPayrollData();
            var payroll = payrollCommonData.payroll;

            _currentSession.TimeZone = TZConvert.GetTimeZoneInfo(payroll.TimezoneName);
            payrollCommonData.timeZone = _currentSession.TimeZone;
            _currentSession.TimeZoneNow = _timezoneConverter.ToTimeZoneDateTime(DateTime.UtcNow, _currentSession.TimeZone);

            payrollCommonData.presentDate = _timezoneConverter.ToTimeZoneDateTime(DateTime.UtcNow, _currentSession.TimeZone).AddMonths(+i);
            _currentSession.TimeZoneNow = payrollCommonData.presentDate;
            payrollCommonData.utcPresentDate = DateTime.UtcNow.AddMonths(+i);

            if (DoesRunPayrollCycle(payroll.PayCycleDayOfMonth, payrollCommonData.presentDate))
            {
                switch (payroll.PayFrequency)
                {
                    case "monthly":
                        _logger.LogInformation($"[RunPayrollCycle] method: runnig monthly payroll");
                        await CalculateRunPayrollForEmployees(payroll, payrollCommonData);
                        break;
                    case "daily":
                        break;
                    case "hourly":
                        break;
                }
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

            var generatedfile = await _billService.GeneratePayslipService(payslipGenerationModal);

            _logger.LogInformation($"[SendPayrollGeneratedEmail] method: Payslip generated");
            var file = new FileDetail
            {
                FileName = generatedfile.FileDetail.FileName,
                FilePath = generatedfile.FileDetail.FilePath
            };
            StringBuilder builder = new StringBuilder();
            builder.Append("<div style=\"border-bottom:1px solid black; margin-top: 14px; margin-bottom:5px\">" + "" + "</div>");
            var logoPath = Path.Combine(_fileLocationDetail.RootPath, _fileLocationDetail.LogoPath, ApplicationConstants.HiringBellLogoSmall);
            if (File.Exists(logoPath))
            {
                builder.Append($"<div><img src=\"cid:{ApplicationConstants.LogoContentId}\" style=\"width: 10rem;margin-top: 1rem;\"></div>");
            }
            EmailSenderModal emailSenderModal = new EmailSenderModal
            {
                To = new List<string> { "istiyaq.mi9@gmail.com", "marghub12@gmail.com" },
                CC = new List<string>(),
                BCC = new List<string>(),
                FileDetails = new List<FileDetail> { file },
                Subject = "Monthly Payslip",
                Body = string.Concat($"Payslip of the month {presentDate}", builder.ToString()),
                Title = "Payslip"
            };

            _logger.LogInformation($"[SendPayrollGeneratedEmail] method: Sending email");
            await _eMailManager.SendMailAsync(emailSenderModal);
            _logger.LogInformation($"[SendPayrollGeneratedEmail] method ended");
        }
    }
}
