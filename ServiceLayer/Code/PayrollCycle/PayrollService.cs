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

        private PayrollEmployeePageData GetEmployeeDetail(DateTime presentDate, int offsetindex, int pageSize)
        {
            int previousMonth = presentDate.Month;
            int previousYear = presentDate.Year;

            if (presentDate.Month > 1 && presentDate.Month <= 12)
            {
                previousMonth -= 1;
            }
            else if (presentDate.Month == 1)
            {
                previousMonth = 12;
                previousYear = presentDate.Year - 1;
            }

            var resultSet = _db.FetchDataSet(Procedures.EMPLOYEE_PAYROLL_GET_BY_PAGE, new
            {
                ForYear = presentDate.Year,
                ForMonth = presentDate.Month,
                PreviousYear = previousYear,
                PreviousMonth = previousMonth,
                OffsetIndex = offsetindex,
                PageSize = pageSize,
                _currentSession.CurrentUserDetail.CompanyId
            }, false);

            if (resultSet == null || resultSet.Tables.Count != 5)
                throw HiringBellException.ThrowBadRequest($"[GetEmployeeDetail]: Employee data not found for date: {presentDate} of offSet: {offsetindex}");

            PayrollEmployeePageData payrollEmployeePageData = new PayrollEmployeePageData
            {
                payrollEmployeeData = Converter.ToList<PayrollEmployeeData>(resultSet.Tables[0]),
                leaveRequestDetails = Converter.ToList<LeaveRequestDetail>(resultSet.Tables[2]),
                hikeBonusSalaryAdhoc = Converter.ToList<HikeBonusSalaryAdhoc>(resultSet.Tables[3]),
                joinedAfterPayrollEmployees = resultSet.Tables[4] == null ? [] : Converter.ToList<JoinedAfterPayrollEmployees>(resultSet.Tables[4])
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

        private int GetPresentMonthLOP(List<LeaveRequestDetail> leaveRequestDetail, DateTime toDate)
        {
            int days = toDate.Day - 1;
            DateTime fromDate = toDate.AddDays(-days);

            var leaves = leaveRequestDetail.Where(x => x.LeaveFromDay >= fromDate && x.LeaveToDay <= toDate).ToList();

            double leavesCount = 0;
            foreach (var leave in leaveRequestDetail)
            {
                if (leave.LeaveToDay <= toDate)
                {
                    leavesCount += leave.LeaveToDay.Subtract(leave.LeaveFromDay).TotalDays;
                }
                else
                {
                    leavesCount += toDate.Subtract(leave.LeaveFromDay).TotalDays;
                }
            }

            return Convert.ToInt32(leavesCount);
        }

        private int GetPreviousMonthLOP(List<LeaveRequestDetail> leaveRequestDetail, DateTime fromDate)
        {
            fromDate = fromDate.AddMonths(-1);

            int days = DateTime.DaysInMonth(fromDate.Year, fromDate.Month);
            days = days - fromDate.Day;

            DateTime toDate = fromDate.AddDays(days);

            var leaves = leaveRequestDetail.Where(x => x.LeaveFromDay > fromDate && x.LeaveToDay <= toDate).ToList();

            double leavesCount = 0;
            foreach (var leave in leaves)
            {
                if (leave.LeaveFromDay > fromDate)
                {
                    leavesCount += leave.LeaveToDay.Subtract(leave.LeaveFromDay).TotalDays;
                }
                else
                {
                    leavesCount += leave.LeaveToDay.Subtract(fromDate).TotalDays;
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

        private async Task GetPayrollWorkingDetail(PayrollCalculationModal payrollCalculationModal, int payrollRunDay, DateTime doj)
        {
            var payrollDetail = await GetCurrentMonthWorkingDetail(payrollCalculationModal, payrollRunDay, doj);
            var previousPayrollDetail = await GetPreviousMonthWorkingDetail(payrollCalculationModal, payrollRunDay, doj);
            var previousMonth = payrollCalculationModal.payrollDate.AddMonths(-1);
            payrollCalculationModal.daysInPreviousMonth = DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month);
            payrollCalculationModal.presentActualMins = payrollDetail.ApprovedAttendanceMinutes;

            if (payrollCalculationModal.isExcludeHolidays)
            {
                payrollCalculationModal.presentActualMins = payrollDetail.ApprovedAttendanceMinutes
                                                            -
                                                            (payrollDetail.Holidays * payrollCalculationModal.shiftDetail.Duration);

                payrollCalculationModal.daysInPreviousMonth -= previousPayrollDetail.Holidays;
                payrollCalculationModal.totalDaysInPresentMonth -= payrollDetail.Holidays;
            }

            if (payrollCalculationModal.isExcludingWeekends)
            {
                payrollCalculationModal.presentActualMins = payrollDetail.ApprovedAttendanceMinutes
                                                            -
                                                            (payrollDetail.WeekOffs * payrollCalculationModal.shiftDetail.Duration);

                payrollCalculationModal.daysInPreviousMonth -= previousPayrollDetail.WeekOffs;
                payrollCalculationModal.totalDaysInPresentMonth -= payrollDetail.WeekOffs;
            }
            else
            {
                payrollCalculationModal.presentActualMins += payrollDetail.WeekOffs * payrollCalculationModal.shiftDetail.Duration;
            }

            payrollCalculationModal.presentActualMins -= (decimal)payrollDetail.LOPAttendanceMiutes;
            payrollCalculationModal.presentActualMins -= (decimal)payrollDetail.LOPLeaveMinutes;

            payrollCalculationModal.presentMinsNeeded = (payrollCalculationModal.totalDaysInPresentMonth * payrollCalculationModal.shiftDetail.Duration);

            payrollCalculationModal.prevLOPMins = previousPayrollDetail.LOPLeaveMinutes + previousPayrollDetail.LOPAttendanceMiutes;
            payrollCalculationModal.prevMinsNeeded = (payrollCalculationModal.daysInPreviousMonth * payrollCalculationModal.shiftDetail.Duration);
        }

        private async Task<PayrollWorkingDetail> GetCurrentMonthWorkingDetail(PayrollCalculationModal payrollCalculationModal, int payrollRunDay, DateTime doj)
        {
            List<AttendanceJson> attendanceDetail = new List<AttendanceJson>();
            var attrDetail = payrollCalculationModal.payrollEmployeeData.Find(x => x.EmployeeId == payrollCalculationModal.employeeId
                                                                                && x.ForMonth == payrollCalculationModal.payrollDate.Month);
            if (attrDetail == null || string.IsNullOrEmpty(attrDetail.AttendanceDetail))
                throw HiringBellException.ThrowBadRequest("Employee attendance detail not found. Please contact to admin.");

            attendanceDetail = JsonConvert.DeserializeObject<List<AttendanceJson>>(attrDetail.AttendanceDetail);

            if (attendanceDetail == null)
                throw HiringBellException.ThrowBadRequest("Attendance detail not found while running payroll cycle.");

            if (payrollCalculationModal.payrollDate.Month == doj.Month && payrollCalculationModal.payrollDate.Year == doj.Year)
                attendanceDetail = attendanceDetail.FindAll(x => x.AttendanceDay.Day >= doj.Day);

            // Update date's to be approved after payroll run date
            Parallel.ForEach(attendanceDetail.FindAll(x => x.AttendanceDay.Day > payrollRunDay), x =>
            {
                x.PresentDayStatus = (int)AttendanceEnum.Approved;
            });
            PayrollWorkingDetail payrollWorkingDetail = new PayrollWorkingDetail
            {
                ApprovedAttendanceMinutes = GetApprovedMinutes(attendanceDetail),
                LOPAttendanceMiutes = GetLOPAttendanceMinutes(attendanceDetail),
                WeekOffs = CalculateWeekOffs(attendanceDetail, payrollCalculationModal.shiftDetail),
                Holidays = await _companyCalendar.GetHolidayCountInMonth(
                                                payrollCalculationModal.payrollDate.Month,
                                                payrollCalculationModal.payrollDate.Year
                                           ),
                LOPLeaveMinutes = GetPresentMonthLOP(payrollCalculationModal.userLeaveRequests, payrollCalculationModal.payrollDate)
            };

            return payrollWorkingDetail;
        }

        private async Task<PayrollWorkingDetail> GetPreviousMonthWorkingDetail(PayrollCalculationModal payrollCalculationModal, int payrollRunDay, DateTime doj)
        {
            List<AttendanceJson> attendanceDetail = new List<AttendanceJson>();
            PayrollWorkingDetail payrollWorkingDetail = null;
            DateTime payrollDate = payrollCalculationModal.payrollDate.AddMonths(-1);
            var attrDetail = payrollCalculationModal.payrollEmployeeData
                                .Find(x => x.EmployeeId == payrollCalculationModal.employeeId && x.ForMonth == payrollDate.Month);

            if (attrDetail == null || string.IsNullOrEmpty(attrDetail.AttendanceDetail))
                return payrollWorkingDetail = new PayrollWorkingDetail();

            attendanceDetail = JsonConvert.DeserializeObject<List<AttendanceJson>>(attrDetail.AttendanceDetail);
            if (payrollDate.Month == doj.Month && payrollDate.Year == doj.Year)
                attendanceDetail = attendanceDetail.FindAll(x => x.AttendanceDay.Day >= doj.Day);

            if (attendanceDetail == null)
                throw HiringBellException.ThrowBadRequest("Attendance detail not found while running payroll cycle.");

            attendanceDetail = attendanceDetail.FindAll(x => x.AttendanceDay.Day > payrollRunDay);

            payrollWorkingDetail = new PayrollWorkingDetail
            {
                ApprovedAttendanceMinutes = GetApprovedMinutes(attendanceDetail),
                LOPAttendanceMiutes = GetLOPAttendanceMinutes(attendanceDetail),
                WeekOffs = CalculateWeekOffs(attendanceDetail, payrollCalculationModal.shiftDetail),
                Holidays = await _companyCalendar.GetHolidayCountInMonth(payrollDate.Month, payrollDate.Year),
                LOPLeaveMinutes = GetPreviousMonthLOP(payrollCalculationModal.userLeaveRequests, payrollCalculationModal.payrollDate)
            };

            return payrollWorkingDetail;
        }

        private int GetApprovedMinutes(List<AttendanceJson> attendanceDetail, bool withWeekends = true)
        {
            List<AttendanceJson> attendances = default(List<AttendanceJson>);
            int approvedMinute = 0;
            if (withWeekends)
            {
                attendances = attendanceDetail.FindAll(x =>
                x.PresentDayStatus == (int)AttendanceEnum.Approved ||
                x.PresentDayStatus == (int)AttendanceEnum.WeekOff);
            }
            else
            {
                attendances = attendanceDetail.FindAll(x => x.PresentDayStatus == (int)AttendanceEnum.Approved);
            }

            if (attendances != null && attendances.Count > 0)
                approvedMinute = attendances.Select(x => x.TotalMinutes).Aggregate((x, y) => x + y);

            return approvedMinute;
        }

        private int GetLOPAttendanceMinutes(List<AttendanceJson> attendanceDetail)
        {
            int attendanceLopMinute = 0;
            var attendances = attendanceDetail.FindAll(x =>
                x.PresentDayStatus != (int)AttendanceEnum.Approved &&
                x.PresentDayStatus != (int)AttendanceEnum.WeekOff);

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

            int totalDaysInMonth = DateTime.DaysInMonth(payrollCommonData.presentDate.Year, payrollCommonData.presentDate.Month);

            int offsetindex = 0;
            int pageSize = 15;
            List<FileDetail> fileDetails = new List<FileDetail>();
            PayrollMonthlyDetail payrollMonthlyDetail = null;
            int payrollRunDay = payrollCommonData.payroll.PayCycleDayOfMonth;
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
                            TaxDetails presentData = new TaxDetails();
                            List<TaxDetails> taxDetails = ValidateTaxDetail(payrollCommonData, empPayroll, presentData);

                            if (!presentData.IsPayrollCompleted || reRunFlag)
                            {
                                PayrollCalculationModal payrollCalculationModal = new PayrollCalculationModal
                                {
                                    payrollEmployeeData = payrollEmployeeData,
                                    userLeaveRequests = payrollEmployeePageData.leaveRequestDetails
                                                    .Where(x => x.EmployeeId == empPayroll.EmployeeId).ToList(),
                                    totalDaysInPresentMonth = totalDaysInMonth,
                                    employeeId = empPayroll.EmployeeId,
                                    shiftDetail = GetEmployeeShiftDetail(payrollCommonData, empPayroll.WorkShiftId)
                                };

                                await GetCalculationModal(payrollCommonData, payrollCalculationModal, payrollRunDay, empPayroll.Doj);

                                payrollMonthlyDetail = await GetUpdatedBreakup(payrollCommonData.utcPresentDate,
                                                                                payrollCalculationModal,
                                                                                empPayroll,
                                                                                payrollEmployeePageData.hikeBonusSalaryAdhoc
                                                                                );

                                if (payrollCalculationModal.presentActualMins != payrollCalculationModal.presentMinsNeeded)
                                {
                                    var taxAmount = (presentData.TaxDeducted / payrollCalculationModal.presentMinsNeeded)
                                        * payrollCalculationModal.presentActualMins;
                                    presentData.TaxPaid = taxAmount;
                                    presentData.TaxDeducted = taxAmount;
                                    IsTaxCalculationRequired = true;
                                }
                                else
                                {
                                    presentData.TaxPaid = presentData.TaxDeducted;
                                }

                                payrollMonthlyDetail.PayableToEmployee -= presentData.TaxPaid;
                                payrollMonthlyDetail.PayableToEmployee += payrollCalculationModal.ArrearAmount;

                                var pTax = GetProfessionalTaxAmount(payrollCommonData.ptaxSlab, payrollCommonData.company, empPayroll.CTC);

                                payrollMonthlyDetail.PayableToEmployee -= pTax;
                                payrollMonthlyDetail.GrossTotal = payrollMonthlyDetail.PayableToEmployee + amount;

                                payrollMonthlyDetail.TotalDeduction = presentData.TaxPaid;

                                var data = taxDetails.Find(x => x.Month == presentData.Month && x.Year == presentData.Year);
                                data.TaxPaid = presentData.TaxPaid;
                                data.TaxDeducted = presentData.TaxDeducted;
                                data.IsPayrollCompleted = true;
                                empPayroll.TaxDetail = JsonConvert.SerializeObject(taxDetails);

                                if (payrollEmployeePageData.joinedAfterPayrollEmployees.Count > 0 &&
                                    payrollEmployeePageData.joinedAfterPayrollEmployees.FirstOrDefault(x => x.EmployeeUid == empPayroll.EmployeeId) != null)
                                {
                                    await UpdateEmployeeArrearAmount(empPayroll.EmployeeId, payrollMonthlyDetail.PayableToEmployee, 
                                                                    payrollCommonData.presentDate, empPayroll.TaxDetail);
                                }
                                else
                                {
                                    await _declarationService.UpdateTaxDetailsService(empPayroll,
                                        payrollMonthlyDetail,
                                        payrollCommonData.utcPresentDate,
                                        IsTaxCalculationRequired
                                    );
                                }

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

        private ShiftDetail GetEmployeeShiftDetail(PayrollCommonData payrollCommonData, int workShiftId)
        {
            var shiftDetail = payrollCommonData.shiftDetail.Find(x => x.WorkShiftId == workShiftId);
            return shiftDetail == null
                ? throw HiringBellException.ThrowBadRequest("Shift detail not found. Please contact to admin")
                : shiftDetail;
        }

        private async Task UpdateEmployeeArrearAmount(long employeeId, decimal arrearAmount, 
                                                    DateTime presentDate, string taxDetail)
        {
            if (arrearAmount != decimal.Zero)
            {
                var result = await _db.ExecuteAsync(Procedures.HIKE_BONUS_SALARY_ADHOC_TAXDETAIL_INS_UPDATE,
                new
                {
                    SalaryAdhocId = 0,
                    EmployeeId = employeeId,
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
                    ForYear = presentDate.Year,
                    ForMonth = presentDate.Month,
                    ProgressState = (int)ItemStatus.Approved,
                    TaxDetail = taxDetail
                }, true);

                if (string.IsNullOrEmpty(result.statusMessage))
                    throw HiringBellException.ThrowBadRequest("Fail to insert arrear amount");
            }

            await Task.CompletedTask;
        }

        private async Task<decimal> GetPreviousMonthArrearAmount(List<HikeBonusSalaryAdhoc> hikeBonusSalaryAdhocs, long employeeId, DateTime payrollDate)
        {
            decimal arrearAmount = 0;
            if (hikeBonusSalaryAdhocs.Count > 0)
            {
                DateTime previousMonthDate = payrollDate.AddMonths(-1);

                var prevMonthArrearDetail = hikeBonusSalaryAdhocs.Find(x => x.EmployeeId == employeeId && x.ForMonth == previousMonthDate.Month
                                            && x.ForYear == previousMonthDate.Year && x.IsArrear);
                if (prevMonthArrearDetail != null)
                    arrearAmount = prevMonthArrearDetail.Amount;
            }

            return await Task.FromResult(arrearAmount);
        }

        private async Task<decimal> GetPreviousMonthLOPAmount(PayrollCalculationModal payrollCalculationModal, decimal grossAmount)
        {
            decimal amount = 0;

            if (payrollCalculationModal.prevLOPMins > 0)
            {
                amount = (grossAmount / (payrollCalculationModal.daysInPreviousMonth * payrollCalculationModal.shiftDetail.Duration))
                            *
                         payrollCalculationModal.prevLOPMins;
            }

            return await Task.FromResult(amount);
        }

        private async Task GetCalculationModal(PayrollCommonData payrollCommonData,
                                                PayrollCalculationModal payrollCalculationModal, int payrollRunDay,
                                                DateTime doj)
        {
            payrollCalculationModal.payrollDate = payrollCommonData.presentDate;
            payrollCalculationModal.isExcludeHolidays = payrollCommonData.payroll.IsExcludeHolidays;
            payrollCalculationModal.payCalculationId = payrollCommonData.payroll.PayCalculationId;
            payrollCalculationModal.isExcludingWeekends = payrollCommonData.payroll.IsExcludeWeeklyOffs;

            // (decimal actualMins, decimal expectedMins) = await GetMinutesForPayroll(payrollCalculationModal, payrollRunDay);

            await GetPayrollWorkingDetail(payrollCalculationModal, payrollRunDay, doj);
        }

        private List<TaxDetails> ValidateTaxDetail(PayrollCommonData payrollCommonData, PayrollEmployeeData empPayroll, TaxDetails presentData)
        {
            List<TaxDetails> taxDetails = JsonConvert.DeserializeObject<List<TaxDetails>>(empPayroll.TaxDetail);
            if (taxDetails == null)
                throw HiringBellException.ThrowBadRequest("TaxDetail not prepaired for the current employee. Fail to run payroll.");

            var currentpresentData = taxDetails.Find(x => x.Month == payrollCommonData.presentDate.Month);
            if (currentpresentData == null)
                throw HiringBellException.ThrowBadRequest("Invalid taxdetail found. Fail to run payroll.");

            presentData.TaxPaid = currentpresentData.TaxPaid;
            presentData.TaxDeducted = currentpresentData.TaxDeducted;
            presentData.Year = currentpresentData.Year;
            presentData.TaxDeducted = presentData.TaxDeducted;
            presentData.EmployeeId = currentpresentData.EmployeeId;
            presentData.IsPayrollCompleted = currentpresentData.IsPayrollCompleted;
            presentData.Month = currentpresentData.Month;
            presentData.Year = currentpresentData.Year;

            return taxDetails;
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

        private async Task<Tuple<decimal, decimal>> GetMinutesForPayroll(PayrollCalculationModal payrollCalculationModal, int payrollRunDay)
        {
            List<AttendanceJson> attendance = GetTotalAttendance(payrollCalculationModal.employeeId, payrollCalculationModal.payrollEmployeeData, payrollRunDay);
            decimal actualMinutesWorked = 0;

            if (attendance.Count > 0)
            {
                actualMinutesWorked = attendance.Select(x => x.TotalMinutes).Aggregate((x, y) => x + y);
            }

            var leaveDays = GetPresentMonthLOP(payrollCalculationModal.userLeaveRequests, payrollCalculationModal.payrollDate);

            (actualMinutesWorked, decimal expectedMinutes) = await GetMinutesWorkForPayroll(
                                                                        payrollCalculationModal,
                                                                        attendance,
                                                                        actualMinutesWorked
                                                                   );

            actualMinutesWorked = actualMinutesWorked - (leaveDays * payrollCalculationModal.shiftDetail.Duration);

            return Tuple.Create(actualMinutesWorked, expectedMinutes);
        }

        private async Task<Tuple<decimal, decimal>> GetMinutesWorkForPayroll(PayrollCalculationModal payrollCalculationModal,
            List<AttendanceJson> attendance, decimal actualMinutesWorked)
        {
            // get total week off days in current month
            int weekOff = CalculateWeekOffs(attendance, payrollCalculationModal.shiftDetail);

            // get total holidays in current month
            decimal holidays = await _companyCalendar.GetHolidayCountInMonth(payrollCalculationModal.payrollDate.Month, payrollCalculationModal.payrollDate.Year);

            decimal expectedMinuteInMonth = 0M;
            decimal modifiedActualMinutesWorked = actualMinutesWorked;

            // payrollCalculationModal.payCalculationId == 1 -> todays days in month including weekoffs e.g. March = 31 days
            if (payrollCalculationModal.payCalculationId == 1)
            {
                modifiedActualMinutesWorked = modifiedActualMinutesWorked + (weekOff * payrollCalculationModal.shiftDetail.Duration);
                expectedMinuteInMonth = payrollCalculationModal.totalDaysInPresentMonth * payrollCalculationModal.shiftDetail.Duration;
            }
            else
            {
                expectedMinuteInMonth = (payrollCalculationModal.totalDaysInPresentMonth - weekOff) * payrollCalculationModal.shiftDetail.Duration;
            }

            if (payrollCalculationModal.isExcludeHolidays)
            {
                modifiedActualMinutesWorked = modifiedActualMinutesWorked - (holidays * payrollCalculationModal.shiftDetail.Duration);
                expectedMinuteInMonth = expectedMinuteInMonth - (holidays * payrollCalculationModal.shiftDetail.Duration);
            }
            else
            {
                modifiedActualMinutesWorked = modifiedActualMinutesWorked + (holidays * payrollCalculationModal.shiftDetail.Duration);
                expectedMinuteInMonth = expectedMinuteInMonth + (holidays * payrollCalculationModal.shiftDetail.Duration);
            }

            return Tuple.Create(modifiedActualMinutesWorked, expectedMinuteInMonth);
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

        private async Task<PayrollMonthlyDetail> GetUpdatedBreakup(DateTime payrollDate, PayrollCalculationModal payrollCalculationModal, PayrollEmployeeData empPayroll,
                                                                    List<HikeBonusSalaryAdhoc> hikeBonusSalaryAdhocs)
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
                    //item.ActualAmount = item.FinalAmount;
                    item.FinalAmount = (item.ActualAmount / payrollCalculationModal.presentMinsNeeded) * payrollCalculationModal.presentActualMins;
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
                var grossAmount = presentMonthSalaryDetail.SalaryBreakupDetails.Find(x => x.ComponentId == ComponentNames.GrossId).ActualAmount;
                presentMonthSalaryDetail.ArrearAmount = -1 * await GetPreviousMonthLOPAmount(payrollCalculationModal, grossAmount);
                presentMonthSalaryDetail.ArrearAmount += await GetPreviousMonthArrearAmount(hikeBonusSalaryAdhocs, empPayroll.EmployeeId, payrollDate);
                payrollCalculationModal.ArrearAmount = presentMonthSalaryDetail.ArrearAmount;
            }

            empPayroll.CompleteSalaryDetail = JsonConvert.SerializeObject(salaryBreakup);
            return await Task.FromResult(payrollMonthlyDetail);
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

            payrollCommonData.utcPresentDate = runDate;
            payrollCommonData.presentDate = _timezoneConverter.ToTimeZoneDateTime(runDate, _currentSession.TimeZone);

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
