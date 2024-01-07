using ModalLayer.Modal;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface ITimesheetService
    {
        Task<TimesheetDetail> GetWeekTimesheetDataService(TimesheetDetail timesheetDetail);
        Task<TimesheetDetail> SubmitTimesheetService(TimesheetDetail timesheetDetail);
        Task<TimesheetDetail> SaveTimesheetService(TimesheetDetail timesheetDetail);
        Task<string> ExecuteActionOnTimesheetService(TimesheetDetail timesheetDetail);
        List<TimesheetDetail> GetPendingTimesheetByIdService(long employeeId, long clientId);
        List<DailyTimesheetDetail> GetEmployeeTimeSheetService(TimesheetDetail timesheetDetail);
        BillingDetail EditEmployeeBillDetailService(GenerateBillFileDetail fileDetail);
        Task RunWeeklyTimesheetCreation(DateTime TimesheetStartDate, DateTime? TimesheetEndDate);
        List<TimesheetDetail> GetTimesheetByFilterService(TimesheetDetail timesheetDetail);
    }
}
