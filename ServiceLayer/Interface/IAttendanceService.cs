using ModalLayer.Modal;
using ModalLayer.Modal.Leaves;
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
        Task GenerateAttendanceService();
    }
}
