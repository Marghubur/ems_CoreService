using EMailService.Modal.Jobs;
using EMailService.Modal.Leaves;
using ModalLayer.Modal.Accounts;
using ServiceLayer.Interface;
using System.Threading.Tasks;

namespace ServiceLayer.Code.HostedServiceJobs
{
    public class LeaveAccrualJob : ILeaveAccrualJob
    {
        private readonly ILeaveCalculation _leaveCalculation;

        public LeaveAccrualJob(ILeaveCalculation leaveCalculation)
        {
            _leaveCalculation = leaveCalculation;
        }

        public async Task LeaveAccrualAsync(CompanySetting companySetting, LeaveAccrualKafkaModel leaveAccrualKafkaModel)
        {
            RunAccrualModel runAccrualModel = new RunAccrualModel
            {
                RunTillMonthOfPresnetYear = false,
                EmployeeId = 0,
                IsSingleRun = false
            };

            if (leaveAccrualKafkaModel.GenerateLeaveAccrualTillMonth)
            {
                runAccrualModel.RunTillMonthOfPresnetYear = leaveAccrualKafkaModel.GenerateLeaveAccrualTillMonth;
            }

            await _leaveCalculation.StartAccrualCycle(runAccrualModel, companySetting);
        }
    }
}
