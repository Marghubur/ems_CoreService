using ModalLayer.Modal;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface ITimesheetRequestService
    {
        Task<RequestModel> ApprovalTimesheetService(int timesheetId, int filterId = 1);
        Task<RequestModel> RejectTimesheetService(int timesheetId, int filterId = 1);
        List<DailyTimesheetDetail> ReAssigneTimesheetService(List<DailyTimesheetDetail> dailyTimesheetDetails, int filterId = 1);
        Task<List<TimesheetDetail>> GetTimesheetRequestDataService(TimesheetDetail timesheetDetail);
    }
}
