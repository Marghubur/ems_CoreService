using ModalLayer.Modal;
using System.Collections.Generic;
using System.Threading.Tasks;
using DailyAttendance = ModalLayer.Modal.DailyAttendance;

namespace ServiceLayer.Interface
{
    public interface IAttendanceRequestService
    {
        RequestModel FetchPendingRequestService(long employeeId, ItemStatus itemStatus = ItemStatus.Pending);
        RequestModel GetManagerAndUnAssignedRequestService(long employeeId);
        Task<dynamic> ApproveAttendanceService(List<DailyAttendance> dailyAttendances, int filterId = ApplicationConstants.Only);
        Task<dynamic> RejectAttendanceService(List<DailyAttendance> dailyAttendances, int filterId = ApplicationConstants.Only);
        RequestModel GetRequestPageData(long employeeId, int filterId);
        List<Attendance> ReAssigneAttendanceService(AttendenceDetail attendanceDetail);
        Task<dynamic> GetAttendanceRequestDataService(Attendance attendance);
    }
}
