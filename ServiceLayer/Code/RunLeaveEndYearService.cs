using ModalLayer.Modal.Accounts;
using ServiceLayer.Code.Leaves;
using ServiceLayer.Interface;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class RunLeaveEndYearService : IRunLeaveEndYearService
    {
        private readonly YearEndCalculation _yearEndCalculation;
        
        public RunLeaveEndYearService(YearEndCalculation yearEndCalculation)
        {
            _yearEndCalculation = yearEndCalculation;
        }

        public async Task RunYearEndLeaveProcessingAsync(CompanySetting companySetting)
        {
            await _yearEndCalculation.RunLeaveYearEndCycle(null);
        }
    }
}