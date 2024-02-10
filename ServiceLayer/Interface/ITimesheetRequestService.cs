using ModalLayer.Modal;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface ITimesheetRequestService
    {
        Task<List<TimesheetDetail>> ApprovalTimesheetService(int timesheetId, TimesheetDetail timesheetDetail, int filterId = 1);
        Task<List<TimesheetDetail>> RejectTimesheetService(int timesheetId, TimesheetDetail timesheetDetail, int filterId = 1);
        List<DailyTimesheetDetail> ReAssigneTimesheetService(List<DailyTimesheetDetail> dailyTimesheetDetails, int filterId = 1);
        Task<List<TimesheetDetail>> GetTimesheetRequestDataService(TimesheetDetail timesheetDetail);
        Task<List<TimesheetDetail>> ReOpenTimesheetRequestService(int timesheetId, TimesheetDetail timesheetDetail, int filterId = 1);
    }
}
