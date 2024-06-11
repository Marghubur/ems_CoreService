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
using Newtonsoft.Json;
using ServiceLayer.Code.PayrollCycle.Interface;
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

        private PayrollEmployeePageData GetEmployeeDetail(DateTime localTimePresentDate, int paryRollrunDay, int offsetindex, int pageSize)
        {
            DateTime FromDate = localTimePresentDate.AddMonths(-1);
            DateTime previousMonthFirstDate = FromDate.AddDays(-1 * (localTimePresentDate.Day - 1));
            FromDate = previousMonthFirstDate.AddDays(paryRollrunDay - 1);

            // logically it should go 1 less day but in procedure we are filtering between thats why here ToDate will be last day of month
            DateTime ToDate = previousMonthFirstDate.AddMonths(2).AddDays(-1);

            var resultSet = _db.FetchDataSet("sp_employee_payroll_get_bet_dates", new
            {
                FromDate,
                ToDate,
                OffsetIndex = offsetindex,
                PageSize = pageSize,
                _currentSession.CurrentUserDetail.CompanyId
            }, false);

            if (resultSet == null || resultSet.Tables.Count != 6)
                throw HiringBellException.ThrowBadRequest($"[GetEmployeeDetail]: Employee data not found for date: {localTimePresentDate} of offSet: {offsetindex}");

            PayrollEmployeePageData payrollEmployeePageData = new PayrollEmployeePageData
            {
                employeeData = Converter.ToList<EmployeePayrollData>(resultSet.Tables[0], _currentSession.TimeZone),
                leaveRequestDetails = Converter.ToList<LeaveRequestNotification>(resultSet.Tables[2], _currentSession.TimeZone),
                hikeBonusSalaryAdhoc = Converter.ToList<HikeBonusSalaryAdhoc>(resultSet.Tables[3], _currentSession.TimeZone),
                joinedAfterPayrollEmployees = resultSet.Tables[4] == null ? [] : Converter.ToList<JoinedAfterPayrollEmployees>(resultSet.Tables[4], _currentSession.TimeZone),
                dailyAttendances = Converter.ToList<DailyAttendance>(resultSet.Tables[5], _currentSession.TimeZone),
            };

            if (payrollEmployeePageData.employeeData == null)
                throw HiringBellException.ThrowBadRequest("Employee payroll data not found");

            List<EmployeeDeclaration> employeeDeclarations = Converter.ToList<EmployeeDeclaration>(resultSet.Tables[1]);

            Parallel.ForEach(payrollEmployeePageData.employeeData, x =>
            {
                var context = employeeDeclarations.Find(i => i.EmployeeId == x.EmployeeId);
                if (context != null)
                    x.employeeDeclaration = context;
                else
                    x.employeeDeclaration = null;
            });

            return payrollEmployeePageData;
        }

        private int GetPresentMonthLOP(List<LeaveRequestNotification> leaveRequestDetail, DateTime toDate, out decimal paidLeave)
        {
            int days = toDate.Day - 1;
            DateTime fromDate = toDate.AddDays(-days);

            var leaves = leaveRequestDetail.Where(x => x.FromDate >= fromDate && x.ToDate <= toDate).ToList();

            double leavesCount = 0;
            paidLeave = 0;
            foreach (var leave in leaves)
            {
                if (!leave.IsPaidLeave)
                {
                    if (leave.ToDate <= toDate)
                        leavesCount += leave.ToDate.Subtract(leave.FromDate).TotalDays + 1;
                    else
                        leavesCount += toDate.Subtract(leave.FromDate).TotalDays + 1;
                }
                else
                {
                    paidLeave += leave.NumOfDays;
                }
            }

            return Convert.ToInt32(leavesCount);
        }

        private int GetPreviousMonthLOP(List<LeaveRequestNotification> leaveRequestDetail, DateTime fromDate, out decimal paidLeave)
        {
            //fromDate = fromDate.AddMonths(-1);

            int days = DateTime.DaysInMonth(fromDate.Year, fromDate.Month);
            days = days - fromDate.Day;

            DateTime toDate = fromDate.AddDays(days);

            var leaves = leaveRequestDetail.Where(x => x.FromDate > fromDate && x.ToDate <= toDate).ToList();

            double leavesCount = 0;
            paidLeave = 0;
            foreach (var leave in leaves)
            {
                if (!leave.IsPaidLeave)
                {
                    if (leave.FromDate > fromDate)
                        leavesCount += leave.ToDate.Subtract(leave.FromDate).TotalDays;
                    else
                        leavesCount += leave.ToDate.Subtract(fromDate).TotalDays;
                }
                else
                {
                    paidLeave += leave.NumOfDays;
                }
            }

            return Convert.ToInt32(leavesCount);
        }

        private List<AttendanceJson> GetTotalAttendance(long employeeId, List<PayrollEmployeeData> payrollEmployeeData, int payrollRunDay)
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

            var consideredAttendance = attendanceDetailJsons.FindAll(x => x.AttendanceDay.Day > payrollRunDay);

            consideredAttendance.ForEach(x => x.PresentDayStatus = (int)AttendanceEnum.Approved);
            attendanceDetailJsons = attendanceDetailJsons.FindAll(x =>
                x.PresentDayStatus == (int)AttendanceEnum.Approved ||
                x.PresentDayStatus == (int)AttendanceEnum.WeekOff);

            return attendanceDetailJsons;
        }

        private async Task GetAttendanceLeaveLopDetail(PayrollCalculationModal payrollModel)
        {
            // get attendance, lop and leave of present month
            var payrollDetail = await GetCurrentMonthWorkingDetail(payrollModel);

            // get attendance, lop and leave of previous month
            var previousPayrollDetail = await GetPreviousMonthWorkingDetail(payrollModel);
            payrollModel.MinutesInPresentMonth = payrollDetail.ApprovedAttendanceMinutes;

            // check and if exclude holiday is given then remove it from daily attendance part
            CheckExcludeHolidays(payrollModel, payrollDetail, previousPayrollDetail);

            // check and if exclude weekends is given then remove it from daily attendance part
            CheckExcludeWeekends(payrollModel, payrollDetail, previousPayrollDetail);

            // payrollCalculationModal.presentActualMins -= (decimal)payrollDetail.LOPAttendanceMiutes;
            payrollModel.MinutesInPresentMonth -= (decimal)payrollDetail.LOPLeaveMinutes * payrollModel.ShiftDetail.Duration;

            payrollModel.MinutesNeededInPresentMonth = (payrollModel.DaysInPresentMonth * payrollModel.ShiftDetail.Duration);

            payrollModel.PreviousMonthLOPMinutes = (previousPayrollDetail.LOPLeaveMinutes * payrollModel.ShiftDetail.Duration) + previousPayrollDetail.LOPAttendanceMiutes;

            if (previousPayrollDetail.ApprovedAttendanceMinutes != 0 && previousPayrollDetail.LOPAttendanceMiutes != 0)
            {
                var previousMonth = payrollModel.PayrollDate.AddMonths(-1);
                payrollModel.DaysInPreviousMonth = DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month);
                payrollModel.MinutesNeededInPreviousMonth = (payrollModel.DaysInPreviousMonth * payrollModel.ShiftDetail.Duration);
            }
            else
            {
                payrollModel.DaysInPreviousMonth = 0;
                payrollModel.MinutesNeededInPreviousMonth = 0;
            }
        }

        private static void CheckExcludeWeekends(PayrollCalculationModal payrollCalculationModal, PayrollWorkingDetail payrollDetail, PayrollWorkingDetail previousPayrollDetail)
        {
            if (payrollCalculationModal.IsWeekendsExcluded)
            {
                payrollCalculationModal.MinutesInPresentMonth = payrollDetail.ApprovedAttendanceMinutes
                                                            -
                                                            (payrollDetail.WeekOffs * payrollCalculationModal.ShiftDetail.Duration);

                payrollCalculationModal.DaysInPreviousMonth -= previousPayrollDetail.WeekOffs;
                payrollCalculationModal.DaysInPresentMonth -= payrollDetail.WeekOffs;
            }
            else
            {
                payrollCalculationModal.MinutesInPresentMonth += payrollDetail.WeekOffs * payrollCalculationModal.ShiftDetail.Duration;
            }
        }

        private static void CheckExcludeHolidays(PayrollCalculationModal payrollCalculationModal, PayrollWorkingDetail payrollDetail, PayrollWorkingDetail previousPayrollDetail)
        {
            if (payrollCalculationModal.IsHolidaysExcluded)
            {
                payrollCalculationModal.MinutesInPresentMonth = payrollDetail.ApprovedAttendanceMinutes
                                                            -
                                                            (payrollDetail.Holidays * payrollCalculationModal.ShiftDetail.Duration);

                payrollCalculationModal.DaysInPreviousMonth -= previousPayrollDetail.Holidays;
                payrollCalculationModal.DaysInPresentMonth -= payrollDetail.Holidays;
            }
        }

        private async Task<PayrollWorkingDetail> GetCurrentMonthWorkingDetail(PayrollCalculationModal payroll)
        {
            var attendance = payroll.DailyAttendances.Where(x => x.AttendanceDate.Month == payroll.PayrollDate.Month).ToList();
            if (payroll.PayrollDate.Month == payroll.Doj.Month && payroll.PayrollDate.Year == payroll.Doj.Year)
            {
                attendance = attendance.FindAll(x => x.AttendanceDate.Day >= payroll.Doj.Day);
                payroll.DaysInPresentMonth -= (payroll.Doj.Day - 1);
            }

            // Update date's to be approved after payroll run date
            Parallel.ForEach(attendance.FindAll(x => x.AttendanceDate.Day > payroll.PayrollRunDay), x =>
            {
                var isWeekend = CheckWeekend(payroll.ShiftDetail, x.AttendanceDate);
                if (isWeekend)
                    x.AttendanceStatus = (int)AttendanceEnum.WeekOff;
                else
                    x.AttendanceStatus = (int)AttendanceEnum.Approved;
            });

            decimal paidLeave = 0;
            PayrollWorkingDetail payrollWorkingDetail = new PayrollWorkingDetail
            {
                ApprovedAttendanceMinutes = GetApprovedMinutes(attendance, payroll.IsWeekendsExcluded),
                LOPAttendanceMiutes = GetLOPAttendanceMinutes(attendance),
                WeekOffs = CalculateWeekOffs(attendance, payroll.ShiftDetail),
                Holidays = await _companyCalendar.GetHolidayCountInMonth(
                                                payroll.PayrollDate.Month,
                                                payroll.PayrollDate.Year
                                           ),
                LOPLeaveMinutes = GetPresentMonthLOP(payroll.UserLeaveRequests, payroll.PayrollDate, out paidLeave)
            };
            payrollWorkingDetail.ApprovedAttendanceMinutes += paidLeave * payroll.ShiftDetail.Duration;
            return payrollWorkingDetail;
        }

        private async Task<PayrollWorkingDetail> GetPreviousMonthWorkingDetail(PayrollCalculationModal payroll)
        {
            DateTime payrollDate = payroll.PayrollDate.AddMonths(-1);
            var attendance = payroll.DailyAttendances.Where(x => x.AttendanceDate.Month == payrollDate.Month).ToList();
            if (payrollDate.Month == payroll.Doj.Month && payrollDate.Year == payroll.Doj.Year)
                attendance = attendance.FindAll(x => x.AttendanceDate.Day >= payroll.Doj.Day);

            if (attendance.Count == 0)
                return new PayrollWorkingDetail();

            decimal paidLeave = 0;
            return new PayrollWorkingDetail
            {
                ApprovedAttendanceMinutes = GetApprovedMinutes(attendance, payroll.IsWeekendsExcluded),
                LOPAttendanceMiutes = GetLOPAttendanceMinutes(attendance),
                WeekOffs = CalculateWeekOffs(attendance, payroll.ShiftDetail),
                Holidays = await _companyCalendar.GetHolidayCountInMonth(payrollDate.Month, payrollDate.Year),
                LOPLeaveMinutes = GetPreviousMonthLOP(payroll.UserLeaveRequests, payroll.PayrollDate, out paidLeave)
            };
        }

        private int GetApprovedMinutes(List<DailyAttendance> attendanceDetail, bool withWeekends = true)
        {
            List<DailyAttendance> attendances = default(List<DailyAttendance>);
            int approvedMinute = 0;
            if (withWeekends)
            {
                attendances = attendanceDetail.FindAll(x =>
                x.AttendanceStatus == (int)AttendanceEnum.Approved ||
                x.AttendanceStatus == (int)AttendanceEnum.WeekOff);
            }
            else
            {
                attendances = attendanceDetail.FindAll(x => x.AttendanceStatus == (int)AttendanceEnum.Approved);
            }

            if (attendances != null && attendances.Count > 0)
                approvedMinute = attendances.Select(x => x.TotalMinutes).Aggregate((x, y) => x + y);

            return approvedMinute;
        }

        private int GetLOPAttendanceMinutes(List<DailyAttendance> attendanceDetail)
        {
            int attendanceLopMinute = 0;
            var attendances = attendanceDetail.FindAll(x =>
                x.AttendanceStatus != (int)AttendanceEnum.Approved &&
                x.AttendanceStatus != (int)AttendanceEnum.WeekOff);

            if (attendances != null && attendances.Count > 0)
                attendanceLopMinute = attendances.Select(x => x.TotalMinutes).Aggregate((x, y) => x + y);

            return attendanceLopMinute;
        }

        //private int GetWeekOffsMinutes(List<AttendanceJson> attendanceDetail, ShiftDetail shiftDetail)
        //{
        //    int weekOff = CalculateWeekOffs(attendanceDetail, shiftDetail);
        //    return weekOff * shiftDetail.Duration;
        //}

        //private async Task<int> GetHolidayMinutes(int month, int year, ShiftDetail shiftDetail)
        //{
        //    decimal holidays = await _companyCalendar.GetHolidayCountInMonth(month, year);
        //    return Convert.ToInt32(holidays * shiftDetail.Duration);
        //}

        private async Task CalculateRunPayrollForEmployees(PayrollCommonData payrollCommonData, bool reRunFlag)
        {
            _logger.LogInformation($"[CalculateRunPayrollForEmployees] method started");

            Payroll payroll = payrollCommonData.payroll;
            List<string> missingDetail = new List<string>();

            int totalDaysInMonth = DateTime.DaysInMonth(payrollCommonData.localTimePresentDate.Year, payrollCommonData.localTimePresentDate.Month);

            int offsetindex = 0;
            int pageSize = 15;
            List<FileDetail> fileDetails = new List<FileDetail>();
            int payrollRunDay = payrollCommonData.payroll.PayCycleDayOfMonth;

            while (true)
            {
                PayrollEmployeePageData payrollEmployeePageData = GetEmployeeDetail(payrollCommonData.localTimePresentDate, payrollRunDay, offsetindex, pageSize);

                if (payrollEmployeePageData.employeeData.Count == 0)
                    break;

                // run pay cycle by considering actual days in months if payroll.PayCalculationId = 1
                // else run pay cycle by considering only weekdays in month if payroll.PayCalculationId = 0
                if (payroll.PayCalculationId != 1)
                {
                    totalDaysInMonth = TimezoneConverter.GetNumberOfWeekdaysInMonth(payrollCommonData.localTimePresentDate.Year, payrollCommonData.localTimePresentDate.Month);
                }

                foreach (EmployeePayrollData currentEmployee in payrollEmployeePageData.employeeData)
                {
                    try
                    {
                        bool salaryHoldFlag = true;
                        (bool salaryHoldFlag, decimal amount) = GetAdhocComponentValue(empPayroll.EmployeeId, payrollEmployeePageData.hikeBonusSalaryAdhoc);
                        if (salaryHoldFlag)
                        {
                            // Get and check tax detail
                            PayrollCalculationModal payrollModel = ValidateTaxDetail(payrollCommonData, currentEmployee);

                            if (!payrollModel.PresentTaxDetail.IsPayrollCompleted || reRunFlag)
                            {
                                PrepareAttendaceCalculationModel(payrollEmployeePageData, payrollCommonData, payrollModel);
                                payrollModel.DaysInPresentMonth = totalDaysInMonth;
                                payrollModel.PayrollRunDay = payrollRunDay;


                                // Calculate present month and previous month attendance, lop and leaves
                                await GetAttendanceLeaveLopDetail(payrollModel);

                                // Update salary break
                                PayrollMonthlyDetail payrollMonthlyDetail = await GetUpdatedBreakup(payrollModel, payrollEmployeePageData);

                                // Update current employee taxdetail
                                bool IsTaxCalculationRequired = UpdateTaxDetail(payrollModel);

                                // update payable detail if any
                                UpdatePayableDetail(payrollCommonData, payrollModel, payrollMonthlyDetail);


                                if (CheckArrearUpdate(payrollEmployeePageData, payrollModel))
                                {
                                    await UpdateEmployeeArrearAmount(payrollMonthlyDetail.PayableToEmployee, payrollModel);
                                }
                                else
                                {
                                    await _declarationService.UpdateTaxDetailsService(
                                        currentEmployee,
                                        payrollMonthlyDetail,
                                        payrollCommonData.localTimePresentDate,
                                        IsTaxCalculationRequired
                                    );
                                }
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
                        missingDetail.Add(string.Concat(currentEmployee.EmployeeName, "  [", currentEmployee.EmployeeId, "]"));
                    }

                    _logger.LogInformation($"[CalculateRunPayrollForEmployees] method: generating and sending payroll email");
                    Task task = Task.Run(async () => await SendPayrollGeneratedEmail(payrollCommonData.localTimePresentDate, currentEmployee.EmployeeId));
                }

                offsetindex = offsetindex + pageSize;
            }

            _logger.LogInformation($"[CalculateRunPayrollForEmployees] method ended");
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

        private bool CheckArrearUpdate(PayrollEmployeePageData payrollEmployeePageData, PayrollCalculationModal payrollModel)
        {
            return payrollEmployeePageData.joinedAfterPayrollEmployees.Count > 0 &&
            payrollEmployeePageData.joinedAfterPayrollEmployees.FirstOrDefault(x => x.EmployeeUid == payrollModel.CurrentEmployee.EmployeeId) != null;
        }

        private void UpdatePayableDetail(PayrollCommonData payrollCommonData, PayrollCalculationModal payrollModel, PayrollMonthlyDetail payrollMonthlyDetail)
        {
            EmployeePayrollData currentEmployee = payrollModel.CurrentEmployee;
            payrollMonthlyDetail.PayableToEmployee -= payrollModel.PresentTaxDetail.TaxPaid;
            payrollMonthlyDetail.PayableToEmployee += payrollModel.ArrearAmount + payrollModel.BonusAmount;

            payrollMonthlyDetail.ProfessionalTax = GetProfessionalTaxAmount(payrollCommonData.ptaxSlab, payrollCommonData.company, currentEmployee.CTC);

            payrollMonthlyDetail.PayableToEmployee -= payrollMonthlyDetail.ProfessionalTax;
            payrollMonthlyDetail.GrossTotal = payrollMonthlyDetail.PayableToEmployee; //+ amount;

            payrollMonthlyDetail.TotalDeduction = payrollModel.PresentTaxDetail.TaxPaid;
        }

        private string UpdateTaxDetailJson(List<TaxDetails> taxDetails, TaxDetails presentData)
        {
            var taxDetail = taxDetails.Find(x => x.Month == presentData.Month && x.Year == presentData.Year);
            taxDetail.TaxPaid = presentData.TaxPaid;
            taxDetail.TaxDeducted = presentData.TaxDeducted;
            taxDetail.IsPayrollCompleted = true;
            return JsonConvert.SerializeObject(taxDetails);
        }

        private bool UpdateTaxDetail(PayrollCalculationModal payrollModel)
        {
            TaxDetails presentTaxDetail = payrollModel.PresentTaxDetail;
            bool IsTaxCalculationRequired = false;
            if (payrollModel.MinutesInPresentMonth != payrollModel.MinutesNeededInPresentMonth)
            {
                var taxAmount = (presentTaxDetail.TaxDeducted / payrollModel.MinutesNeededInPresentMonth)
                    * payrollModel.MinutesInPresentMonth;
                presentTaxDetail.TaxPaid = taxAmount;
                presentTaxDetail.TaxDeducted = taxAmount;
                IsTaxCalculationRequired = true;
            }
            else
            {
                presentTaxDetail.TaxPaid = presentTaxDetail.TaxDeducted;
            }

            payrollModel.CurrentEmployee.TaxDetail = UpdateTaxDetailJson(payrollModel.TaxDetails, payrollModel.PresentTaxDetail);

            return IsTaxCalculationRequired;
        }

        private void PrepareAttendaceCalculationModel(PayrollEmployeePageData pageData, PayrollCommonData commonData, PayrollCalculationModal payrollModel)
        {
            EmployeePayrollData currentEmployee = payrollModel.CurrentEmployee;

            payrollModel.LocalTimePresentDate = commonData.localTimePresentDate;
            payrollModel.CurrentEmployee = currentEmployee;
            payrollModel.DailyAttendances = pageData.dailyAttendances.Where(x => x.EmployeeId == currentEmployee.EmployeeId).ToList();
            payrollModel.UserLeaveRequests = pageData.leaveRequestDetails.Where(x => x.EmployeeId == currentEmployee.EmployeeId).ToList();
            payrollModel.ShiftDetail = GetEmployeeShiftDetail(commonData, currentEmployee.WorkShiftId);
            payrollModel.PayrollDate = commonData.localTimePresentDate;
            payrollModel.IsHolidaysExcluded = commonData.payroll.IsExcludeHolidays;
            payrollModel.Doj = currentEmployee.Doj;
            payrollModel.IsWeekendsExcluded = commonData.payroll.IsExcludeWeeklyOffs;
        }

        private ShiftDetail GetEmployeeShiftDetail(PayrollCommonData payrollCommonData, int workShiftId)
        {
            var shiftDetail = payrollCommonData.shiftDetail.Find(x => x.WorkShiftId == workShiftId);
            return shiftDetail == null
                ? throw HiringBellException.ThrowBadRequest("Shift detail not found. Please contact to admin")
                : shiftDetail;
        }

        private async Task UpdateEmployeeArrearAmount(decimal arrearAmount, PayrollCalculationModal payrollModel)
        {
            if (arrearAmount != decimal.Zero)
            {
                DateTime utcTime = _timezoneConverter.ToUtcTime(payrollModel.LocalTimePresentDate);

                var result = await _db.ExecuteAsync(Procedures.HIKE_BONUS_SALARY_ADHOC_TAXDETAIL_INS_UPDATE,
                new
                {
                    SalaryAdhocId = 0,
                    payrollModel.CurrentEmployee.EmployeeId,
                    ProcessStepId = 0,
                    FinancialYear = _currentSession.FinancialStartYear,
                    _currentSession.CurrentUserDetail.OrganizationId,
                    _currentSession.CurrentUserDetail.CompanyId,
                    IsPaidByCompany = false,
                    IsPaidByEmployee = false,
                    IsFine = false,
                    IsHikeInSalary = false,
                    IsBonus = false,
                    IsReimbursment = false,
                    IsSalaryOnHold = false,
                    IsArrear = true,
                    IsOvertime = false,
                    IsCompOff = false,
                    OTCalculatedOn = "",
                    AmountInPercentage = 0,
                    Amount = arrearAmount,
                    IsActive = true,
                    PaymentActionType = "",
                    Comments = "",
                    Status = (int)ItemStatus.Approved,
                    ForYear = utcTime.Year,
                    ForMonth = utcTime.Month,
                    ProgressState = (int)ItemStatus.Approved,
                    payrollModel.CurrentEmployee.TaxDetail
                }, true);

                if (string.IsNullOrEmpty(result.statusMessage))
                    throw HiringBellException.ThrowBadRequest("Fail to insert arrear amount");
            }

            await Task.CompletedTask;
        }

        private async Task<decimal> GetPreviousMonthArrearAmount(List<HikeBonusSalaryAdhoc> hikeBonusSalaryAdhocs, PayrollCalculationModal payroll)
        {
            decimal arrearAmount = 0;
            if (hikeBonusSalaryAdhocs.Count > 0)
            {
                DateTime previousMonthDate = payroll.LocalTimePresentDate.AddMonths(-1);

                var prevMonthArrearDetail = hikeBonusSalaryAdhocs.Find(x => x.EmployeeId == payroll.CurrentEmployee.EmployeeId
                                                && x.ForMonth == previousMonthDate.Month
                                                && x.ForYear == previousMonthDate.Year
                                                && x.IsArrear
                                            );

                if (prevMonthArrearDetail != null)
                    arrearAmount = prevMonthArrearDetail.Amount;
            }

            return await Task.FromResult(arrearAmount);
        }

        private async Task<decimal> GetPreviousMonthLOPAmount(PayrollCalculationModal payrollCalculationModal, decimal grossAmount)
        {
            decimal amount = 0;

            if (payrollCalculationModal.PreviousMonthLOPMinutes > 0)
            {
                amount = (grossAmount / (payrollCalculationModal.DaysInPreviousMonth * payrollCalculationModal.ShiftDetail.Duration))
                            *
                         payrollCalculationModal.PreviousMonthLOPMinutes;
            }

            return await Task.FromResult(amount);
        }

        private async Task<decimal> GetBonusAmount(List<HikeBonusSalaryAdhoc> hikeBonusSalaryAdhocs, PayrollCalculationModal payroll)
        {
            decimal amount = 0;
            var bonusDetail = hikeBonusSalaryAdhocs.FirstOrDefault(x => x.IsBonus && x.EmployeeId == payroll.CurrentEmployee.EmployeeId
                                                                        && x.ForMonth == payroll.LocalTimePresentDate.Month
                                                                        && x.ForYear == payroll.LocalTimePresentDate.Year
                                                                   );

            if (bonusDetail != null)
                amount = bonusDetail.Amount;

            return await Task.FromResult(amount);
        }

        private PayrollCalculationModal ValidateTaxDetail(PayrollCommonData payrollCommonData, EmployeePayrollData empPayroll)
        {
            PayrollCalculationModal payrollModel = new PayrollCalculationModal();
            payrollModel.CurrentEmployee = empPayroll;

            payrollModel.TaxDetails = JsonConvert.DeserializeObject<List<TaxDetails>>(empPayroll.TaxDetail);
            if (payrollModel.TaxDetails == null)
                throw HiringBellException.ThrowBadRequest("TaxDetail not prepaired for the current employee. Fail to run payroll.");

            var presentTaxDetail = payrollModel.TaxDetails.Find(x => x.Month == payrollCommonData.localTimePresentDate.Month);
            if (presentTaxDetail == null)
                throw HiringBellException.ThrowBadRequest("Invalid taxdetail found. Fail to run payroll.");

            TaxDetails taxDetails = new TaxDetails();
            taxDetails.TaxPaid = presentTaxDetail.TaxPaid;
            taxDetails.TaxDeducted = presentTaxDetail.TaxDeducted;
            taxDetails.Year = presentTaxDetail.Year;
            taxDetails.TaxDeducted = taxDetails.TaxDeducted;
            taxDetails.EmployeeId = presentTaxDetail.EmployeeId;
            taxDetails.IsPayrollCompleted = presentTaxDetail.IsPayrollCompleted;
            taxDetails.Month = presentTaxDetail.Month;
            taxDetails.Year = presentTaxDetail.Year;

            payrollModel.PresentTaxDetail = taxDetails;
            return payrollModel;
        }

        private (bool, decimal) GetAdhocComponentValue(long EmployeeId, List<HikeBonusSalaryAdhoc> hikeBonusSalaryAdhoc)
        {
            bool flag = true;
            decimal amount = 0;

            var presnetEmployees = hikeBonusSalaryAdhoc.Where(x => x.EmployeeId == EmployeeId).ToList();
            var holdRecord = presnetEmployees.Find(x => x.EmployeeId == EmployeeId && x.IsSalaryOnHold);

            if (holdRecord != null)
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

        //private async Task<Tuple<decimal, decimal>> GetMinutesForPayroll(PayrollCalculationModal payrollCalculationModal, int payrollRunDay)
        //{
        //    List<AttendanceJson> attendance = GetTotalAttendance(payrollCalculationModal.employeeId, payrollCalculationModal.payrollEmployeeData, payrollRunDay);
        //    decimal actualMinutesWorked = 0;

        //    if (attendance.Count > 0)
        //    {
        //        actualMinutesWorked = attendance.Select(x => x.TotalMinutes).Aggregate((x, y) => x + y);
        //    }

        //    var leaveDays = GetPresentMonthLOP(payrollCalculationModal.userLeaveRequests, payrollCalculationModal.payrollDate);

        //    (actualMinutesWorked, decimal expectedMinutes) = await GetMinutesWorkForPayroll(
        //                                                                payrollCalculationModal,
        //                                                                attendance,
        //                                                                actualMinutesWorked
        //                                                           );

        //    actualMinutesWorked = actualMinutesWorked - (leaveDays * payrollCalculationModal.shiftDetail.Duration);

        //    return Tuple.Create(actualMinutesWorked, expectedMinutes);
        //}

        //private async Task<Tuple<decimal, decimal>> GetMinutesWorkForPayroll(PayrollCalculationModal payrollCalculationModal,
        //    List<AttendanceJson> attendance, decimal actualMinutesWorked)
        //{
        //    // get total week off days in current month
        //    int weekOff = CalculateWeekOffs(attendance, payrollCalculationModal.shiftDetail);

        //    // get total holidays in current month
        //    decimal holidays = await _companyCalendar.GetHolidayCountInMonth(payrollCalculationModal.payrollDate.Month, payrollCalculationModal.payrollDate.Year);

        //    decimal expectedMinuteInMonth = 0M;
        //    decimal modifiedActualMinutesWorked = actualMinutesWorked;

        //    // payrollCalculationModal.payCalculationId == 1 -> todays days in month including weekoffs e.g. March = 31 days
        //    if (payrollCalculationModal.payCalculationId == 1)
        //    {
        //        modifiedActualMinutesWorked = modifiedActualMinutesWorked + (weekOff * payrollCalculationModal.shiftDetail.Duration);
        //        expectedMinuteInMonth = payrollCalculationModal.totalDaysInPresentMonth * payrollCalculationModal.shiftDetail.Duration;
        //    }
        //    else
        //    {
        //        expectedMinuteInMonth = (payrollCalculationModal.totalDaysInPresentMonth - weekOff) * payrollCalculationModal.shiftDetail.Duration;
        //    }

        //    if (payrollCalculationModal.isExcludeHolidays)
        //    {
        //        modifiedActualMinutesWorked = modifiedActualMinutesWorked - (holidays * payrollCalculationModal.shiftDetail.Duration);
        //        expectedMinuteInMonth = expectedMinuteInMonth - (holidays * payrollCalculationModal.shiftDetail.Duration);
        //    }
        //    else
        //    {
        //        modifiedActualMinutesWorked = modifiedActualMinutesWorked + (holidays * payrollCalculationModal.shiftDetail.Duration);
        //        expectedMinuteInMonth = expectedMinuteInMonth + (holidays * payrollCalculationModal.shiftDetail.Duration);
        //    }

        //    return Tuple.Create(modifiedActualMinutesWorked, expectedMinuteInMonth);
        //}

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

        private async Task<PayrollMonthlyDetail> GetUpdatedBreakup(PayrollCalculationModal payroll, PayrollEmployeePageData payrollEmployeePageData)
        {
            List<HikeBonusSalaryAdhoc> hikeBonusSalaryAdhocs = payrollEmployeePageData.hikeBonusSalaryAdhoc;

            PayrollMonthlyDetail payrollMonthlyDetail = new PayrollMonthlyDetail();
            var salaryBreakup = JsonConvert.DeserializeObject<List<AnnualSalaryBreakup>>(payroll.CurrentEmployee.CompleteSalaryDetail);
            if (salaryBreakup == null)
                throw HiringBellException.ThrowBadRequest("Salary breakup not found. Fail to run payroll.");

            AnnualSalaryBreakup presentMonthSalaryDetail = salaryBreakup.Find(x => x.MonthNumber == payroll.LocalTimePresentDate.Month);
            if (presentMonthSalaryDetail == null)
                throw HiringBellException.ThrowBadRequest($"{payroll.LocalTimePresentDate.Month.ToString("MMMM")} month salary detail not found");

            foreach (var item in presentMonthSalaryDetail.SalaryBreakupDetails)
            {
                item.FinalAmount = (item.ActualAmount / payroll.MinutesNeededInPresentMonth) * payroll.MinutesInPresentMonth;
                switch (item.ComponentId)
                {
                    case ComponentNames.CTCId:
                    case ComponentNames.GrossId:
                    case ComponentNames.EmployeePF:
                    case ComponentNames.EmployerPF:
                        break;
                    default:
                        payrollMonthlyDetail.PayableToEmployee += item.FinalAmount;
                        break;
                }
            }

            CalculatePFAmount(presentMonthSalaryDetail, payrollMonthlyDetail);

            presentMonthSalaryDetail.IsPayrollExecutedForThisMonth = true;
            var grossAmount = presentMonthSalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId == ComponentNames.GrossId).FinalAmount;

            presentMonthSalaryDetail.ArrearAmount = -1 * await GetPreviousMonthLOPAmount(payroll, grossAmount);
            presentMonthSalaryDetail.ArrearAmount += await GetPreviousMonthArrearAmount(hikeBonusSalaryAdhocs, payroll);
            presentMonthSalaryDetail.BonusAmount = await GetBonusAmount(hikeBonusSalaryAdhocs, payroll);

            payroll.ArrearAmount = presentMonthSalaryDetail.ArrearAmount;
            payroll.BonusAmount = presentMonthSalaryDetail.BonusAmount;
            payroll.CurrentEmployee.CompleteSalaryDetail = JsonConvert.SerializeObject(salaryBreakup);

            return await Task.FromResult(payrollMonthlyDetail);
        }

        private void CalculatePFAmount(AnnualSalaryBreakup presentMonthSalaryDetail, PayrollMonthlyDetail payrollMonthlyDetail)
        {
            var basicAmount = presentMonthSalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId == ComponentNames.Basic).FinalAmount;
            var employeePFComponent = presentMonthSalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId == ComponentNames.EmployeePF);

            var formula = GetConvertedFormula(employeePFComponent.Formula, basicAmount);
            employeePFComponent.FinalAmount = GetConvertedAmount(formula);

            payrollMonthlyDetail.PFByEmployee = employeePFComponent.FinalAmount;
            payrollMonthlyDetail.PayableToEmployee += employeePFComponent.FinalAmount;

            var employerPFComponent = presentMonthSalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId == ComponentNames.EmployerPF);

            formula = GetConvertedFormula(employerPFComponent.Formula, basicAmount);
            employerPFComponent.FinalAmount = GetConvertedAmount(formula);

            payrollMonthlyDetail.PFByEmployer = employerPFComponent.FinalAmount;
            payrollMonthlyDetail.PayableToEmployee += employerPFComponent.FinalAmount;
        }

        private string GetConvertedFormula(string userFormula, decimal basicAmount)
        {
            string formula = userFormula;

            if (!userFormula.Contains("basic", StringComparison.OrdinalIgnoreCase))
                formula = userFormula;

            var elems = formula.Split(" ");
            if (elems != null && elems.Length > 0)
            {
                if (elems[0].Contains("%"))
                {
                    elems[0] = elems[0].Replace("%", "");
                    if (int.TryParse(elems[0].Trim(), out int value))
                    {
                        formula = $"{value}%{basicAmount}";
                    }
                }
                else
                {
                    if (int.TryParse(elems[0].Trim(), out int value))
                    {
                        formula = value.ToString();
                    }
                }
            }

            return formula;
        }

        private decimal GetConvertedAmount(string formula)
        {
            var expPart = formula.Split("%");
            if (expPart.Length != 2)
                return decimal.Parse(formula);

            var percentage = decimal.Parse(expPart[0]);
            var number = decimal.Parse(expPart[1]);
            return (percentage * number) / 100;
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

        public async Task RunPayrollCycle(DateTime runDate, bool reRunFlag = false)
        {
            _logger.LogInformation($"[RunPayrollCycle] method started");

            PayrollCommonData payrollCommonData = GetCommonPayrollData();

            if (payrollCommonData.payroll.FinancialYear != runDate.Year && payrollCommonData.payroll.FinancialYear + 1 != runDate.Year)
            {
                throw HiringBellException.ThrowBadRequest("Invalid year or month. Current financial year is: " + payrollCommonData.payroll.FinancialYear);
            }

            _currentSession.TimeZone = TZConvert.GetTimeZoneInfo(payrollCommonData.payroll.TimezoneName);
            payrollCommonData.timeZone = _currentSession.TimeZone;

            /* if (runDate.Day == 1)
                runDate = runDate.AddDays(payrollCommonData.payroll.PayCycleDayOfMonth - 1);*/

            payrollCommonData.utcTimePresentDate = _timezoneConverter.ToUtcTime(runDate);
            payrollCommonData.localTimePresentDate = _timezoneConverter.ToTimeZoneDateTime(payrollCommonData.utcTimePresentDate, _currentSession.TimeZone);

            switch (payrollCommonData.payroll.PayFrequency)
            {
                case LocalConstants.MonthlyPayFrequency:
                    await CalculateRunPayrollForEmployees(payrollCommonData, reRunFlag);
                    break;
                case LocalConstants.DailyPayFrequency:
                    break;
                case LocalConstants.HourlyPayFrequency:
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

        private int CalculateWeekOffs(List<DailyAttendance> attendanceJsons, ShiftDetail shiftDetail)
        {
            int weekOffCount = 0;
            if (attendanceJsons.Count > 0)
            {
                attendanceJsons.ForEach(x =>
                {
                    switch (x.AttendanceDate.DayOfWeek)
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

        private bool CheckWeekend(ShiftDetail shiftDetail, DateTime date)
        {
            bool flag = false;
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    flag = !shiftDetail.IsMon;
                    break;
                case DayOfWeek.Tuesday:
                    flag = !shiftDetail.IsTue;
                    break;
                case DayOfWeek.Wednesday:
                    flag = !shiftDetail.IsWed;
                    break;
                case DayOfWeek.Thursday:
                    flag = !shiftDetail.IsThu;
                    break;
                case DayOfWeek.Friday:
                    flag = !shiftDetail.IsFri;
                    break;
                case DayOfWeek.Saturday:
                    flag = !shiftDetail.IsSat;
                    break;
                case DayOfWeek.Sunday:
                    flag = !shiftDetail.IsSun;
                    break;
            }

            return flag;
        }

        //private async Task SendEmail(List<string> missingDetail, string presentDate)
        //{
        //    StringBuilder builder = new StringBuilder();
        //    builder.AppendLine($"<div style=\"margin-bottom: 25px;\">Payroll of the month {presentDate} completed</div>");

        //    string status = "<b style=\"color: green\">Ran successfully</b>";
        //    if (missingDetail.Count > 0 && missingDetail.Count <= 20)
        //    {
        //        foreach (var detail in missingDetail)
        //        {
        //            builder.Append("<div>" + detail + "</div>");
        //        }

        //        status = "<b style=\"color: red\">Partially successfull</b>";
        //    }
        //    else if (missingDetail.Count > 20)
        //    {
        //        builder.Append("<div style=\"color: red;\"><b>Alert!!!</b>  " +
        //            "payroll cycle failed, more than 20 employee payroll cycle raise exception. " +
        //            "For detail please check the log files. Total failed count: " + missingDetail.Count + "</div>");

        //        status = "<b style=\"color: red\">Many failed</b>";
        //    }

        //    builder.AppendLine($"<div style=\"margin-top 50px;\">Status: {status}</div>");

        //    EmailSenderModal emailSenderModal = new EmailSenderModal
        //    {
        //        To = new List<string> { "istiyaq.mi9@gmail.com", "marghub12@gmail.com" },
        //        CC = new List<string>(),
        //        BCC = new List<string>(),
        //        FileDetails = new List<FileDetail>(),
        //        Subject = "Monthly Payslip | " + presentDate,
        //        Body = builder.ToString(),
        //        Title = "Payslip"
        //    };

        //    _logger.LogInformation($"[SendPayrollGeneratedEmail] method: Sending email");
        //    await _eMailManager.SendMailAsync(emailSenderModal);
        //}
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
