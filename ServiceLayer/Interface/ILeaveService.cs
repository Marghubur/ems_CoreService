using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using ModalLayer.Modal.Leaves;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface ILeaveService
    {
        List<LeavePlan> GetLeavePlansService(FilterModel filterModel);
        List<LeavePlanType> AddLeavePlanTypeService(LeavePlanType leavePlanType);
        List<LeavePlan> AddLeavePlansService(LeavePlan leavePlanType);
        Task<LeavePlan> LeavePlanUpdateTypes(int leavePlanId, List<int> LeavePlanTypeId);
        List<LeavePlanType> UpdateLeavePlanTypeService(int leavePlanTypeId, LeavePlanType leavePlanType);
        string AddUpdateLeaveQuotaService(LeaveDetail leaveDetail);
        LeavePlanConfiguration GetLeaveTypeDetailByIdService(int leavePlanTypeId);
        List<LeavePlanType> GetLeaveTypeFilterService();
        List<LeavePlan> SetDefaultPlanService(int leavePlanId, LeavePlan leavePlan);
        string LeaveRquestManagerActionService(LeaveRequestNotification approvalRequest, ItemStatus status);
        Task<dynamic> ApplyLeaveService(LeaveRequestModal leaveRequestModal, IFormFileCollection FileCollection, List<Files> fileDetail);
        Task<dynamic> GetEmployeeLeaveDetail(LeaveRequestModal leaveRequestModal);
        Task RunAccrualByEmployeeService(long EmployeeId);
        DataSet GetLeaveAttachmentService(string FileIds);
        DataSet GetLeaveAttachByMangerService(LeaveRequestNotification leaveRequestNotification);
        Leave GetLeaveDetailByEmpIdService(long EmployeeId);
    }
}
