using EMailService.Modal;
using ModalLayer.Modal;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IAttendanceService
    {
        Task<AttendanceWithClientDetail> GetAttendanceByUserId(Attendance attendance);
        AttendanceWithClientDetail EnablePermission(AttendenceDetail attendenceDetail);
        Task<AttendanceJson> SubmitAttendanceService(Attendance attendance);
        Task<string> RaiseMissingAttendanceRequestService(ComplaintOrRequestWithEmail compalintOrRequest);
        Task<List<ComplaintOrRequest>> GetMissingAttendanceRequestService(FilterModel filter);
        Task<List<ComplaintOrRequest>> GetMissingAttendanceApprovalRequestService(FilterModel filter);
        List<AttendenceDetail> GetAllPendingAttendanceByUserIdService(long employeeId, int UserTypeId, long clientId);
        dynamic GetEmployeePerformanceService(AttendenceDetail attendanceDetails);
        Task<List<ComplaintOrRequest>> ApproveRaisedAttendanceRequestService(List<ComplaintOrRequest> complaintOrRequests);
        Task<List<ComplaintOrRequest>> RejectRaisedAttendanceRequestService(List<ComplaintOrRequest> complaintOrRequests);
        Task GenerateAttendanceService(AttendenceDetail attendenceDetail);
        Task<List<AttendanceJson>> AdjustAttendanceService(Attendance attendance);
        Task<List<LOPAdjustmentDetail>> GetLOPAdjustmentService(int month, int year);
        Task<AttendanceWithClientDetail> GetDailyAttendanceByUserIdService(WeekDates weekDates);
        Task<AttendanceConfig> LoadAttendanceConfigDataService(long EmployeeId);
        Task<List<DailyAttendance>> SaveDailyAttendanceService(List<DailyAttendance> attendances);
        Task<List<DailyAttendance>> SubmitDailyAttendanceService(List<DailyAttendance> attendances);
    }
}
