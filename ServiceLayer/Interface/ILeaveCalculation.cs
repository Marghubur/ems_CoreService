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
        Task RunAccrualCycle(bool runTillMonthOfPresentYear = false);
        Task<List<CompanySetting>> StartAccrualCycle(bool runTillMonthOfPresentYear = false);
        Task<LeaveCalculationModal> CheckAndApplyForLeave(LeaveRequestModal leaveRequestModal, IFormFileCollection fileCollection, List<Files> fileDetail);
        Task<LeaveCalculationModal> GetLeaveDetailService(long EmployeeId);
        Task RunAccrualCycleByEmployee(long EmployeeId);
    }
}
