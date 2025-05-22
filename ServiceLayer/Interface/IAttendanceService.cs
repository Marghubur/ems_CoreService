using Bot.CoreBottomHalf.CommonModal.Leave;
using EMailService.Modal;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using System.Collections.Generic;
using System.Threading.Tasks;
using DailyAttendance = ModalLayer.Modal.DailyAttendance;

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
        Task<DailyAttendance> AdjustAttendanceService(Attendance attendance);
        Task<List<LOPAdjustmentDetail>> GetLOPAdjustmentService(int month, int year);
        Task<AttendanceWithClientDetail> GetDailyAttendanceByUserIdService(WeekDates weekDates);
        Task<dynamic> LoadAttendanceConfigDataService(long EmployeeId);
        Task<dynamic> SaveDailyAttendanceService(List<DailyAttendance> attendances);
        Task<dynamic> SubmitDailyAttendanceService(List<DailyAttendance> attendances);
        Task<Dictionary<long, List<DailyAttendance>>> GetAttendancePageService(FilterModel filterModel);
        Task<Dictionary<long, List<DailyAttendance>>> GetRecentWeeklyAttendanceService(FilterModel filterModel);
        Task GenerateMonthlyAttendance();
        Task<int> UploadMonthlyAttendanceExcelService(IFormFileCollection files);
        Task UploadDailyBiometricAttendanceExcelService(IFormFileCollection files);
        Task<List<DailyAttendance>> GetRecentDailyAttendanceService(FilterModel filterModel);
        Task<byte[]> DownloadAttendanceExcelWithDataService(int month, int year, bool isSingleMonth);
        Task<int> UploadSingleMonthAttendanceExcelService(IFormFileCollection files, int month, int year);
    }
}