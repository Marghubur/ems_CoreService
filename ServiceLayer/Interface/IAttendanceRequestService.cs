using ModalLayer.Modal;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IAttendanceRequestService
    {
        RequestModel FetchPendingRequestService(long employeeId, ItemStatus itemStatus = ItemStatus.Pending);
        RequestModel GetManagerAndUnAssignedRequestService(long employeeId);
        Task<dynamic> ApproveAttendanceService(DailyAttendance dailyAttendance, int filterId = ApplicationConstants.Only);
        Task<dynamic> RejectAttendanceService(DailyAttendance dailyAttendance, int filterId = ApplicationConstants.Only);
        RequestModel GetRequestPageData(long employeeId, int filterId);
        List<Attendance> ReAssigneAttendanceService(AttendenceDetail attendanceDetail);
        Task<dynamic> GetAttendenceRequestDataServive(Attendance attendance);
    }
}
