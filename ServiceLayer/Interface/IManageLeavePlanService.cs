using ModalLayer.Modal;
using ModalLayer.Modal.Leaves;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IManageLeavePlanService
    {
        LeavePlanConfiguration UpdateLeaveAccrual(int leavePlanTypeId, LeaveAccrual leaveAccrual);
        LeavePlanConfiguration UpdateLeaveDetail(int leavePlanTypeId, int leavePlanId, LeaveDetail leaveDetail);
        LeavePlanConfiguration UpdateLeaveAccrualService(int leavePlanTypeId, int leavePlanId, LeaveAccrual leaveAccrual);
        LeavePlanConfiguration UpdateApplyForLeaveService(int leavePlanTypeId, int leavePlanId, LeaveApplyDetail leaveApplyDetail);
        LeavePlanConfiguration UpdateLeaveRestrictionService(int leavePlanTypeId, int leavePlanId, LeavePlanRestriction leavePlanRestriction);
        LeavePlanConfiguration UpdateHolidayNWeekOffPlanService(int leavePlanTypeId, int leavePlanId, LeaveHolidaysAndWeekoff leaveHolidaysAndWeekoff);        
        LeavePlanConfiguration UpdateYearEndProcessingService(int leavePlanTypeId, int leavePlanId, LeaveEndYearProcessing leaveEndYearProcessing);
        LeavePlanConfiguration UpdateLeaveApprovalService(int leavePlanTypeId, int leavePlanId, LeaveApproval leaveApproval);        
        dynamic GetLeaveConfigurationDetail(int leavePlanTypeId);
        Task<string> AddUpdateEmpLeavePlanService(int leavePlanId, List<Employee> employees);
        List<EmpLeavePlanMapping> GetEmpMappingByLeavePlanIdService(int leavePlanId);
        LeavePlanConfiguration UpdateLeaveFromManagement(int leavePlanTypeId, int leavePlanId, ManagementLeave managementLeave);
    }
}
