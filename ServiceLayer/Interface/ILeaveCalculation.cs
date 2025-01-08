using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.Leave;
using EMailService.Modal.Leaves;
using Microsoft.AspNetCore.Http;
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
        Task StartAccrualCycle(RunAccrualModel runAccrualModel, CompanySetting companySetting);
        Task<LeaveCalculationModal> CheckAndApplyForLeave(LeaveRequestModal leaveRequestModal, IFormFileCollection fileCollection, List<Files> fileDetail);
        Task<LeaveCalculationModal> GetLeaveDetailService(long EmployeeId);
        Task RunAccrualCycleByEmployee(long EmployeeId);
        Task<LeaveCalculationModal> PrepareCheckLeaveCriteria(LeaveRequestModal leaveRequestModal);
        LeaveRequestNotification GetApprovalChainDetail(LeaveRequestModal leaveRequestModal, out List<string> emails);
        Task StartAccrualCycleWithDefaultSetting(RunAccrualModel runAccrualModel);
    }
}
