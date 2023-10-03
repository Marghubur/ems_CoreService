using EMailService.Modal.Leaves;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using ModalLayer.Modal.Leaves;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface ILeaveCalculation
    {
        Task<LeaveCalculationModal> GetBalancedLeave(long EmployeeId, DateTime FromDate, DateTime ToDate);
        Task RunAccrualCycle(RunAccrualModel runAccrualModel);
        Task<List<CompanySetting>> StartAccrualCycle(RunAccrualModel runAccrualModel);
        Task<LeaveCalculationModal> CheckAndApplyForLeave(LeaveRequestModal leaveRequestModal, IFormFileCollection fileCollection, List<Files> fileDetail);
        Task<LeaveCalculationModal> GetLeaveDetailService(long EmployeeId);
        Task RunAccrualCycleByEmployee(long EmployeeId);
        Task<LeaveCalculationModal> PrepareCheckLeaveCriteria(LeaveRequestModal leaveRequestModal);
        LeaveRequestNotification GetApprovalChainDetail(long employeeId, out List<string> emails);
    }
}
