
using EMailService.Modal.CronJobs;
using ServiceLayer.Code.Leaves;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Code.HostedServicesJobs
{
    public class YearEndLeaveProcessingJob: IYearEndLeaveProcessingJob
    {
        private readonly YearEndCalculation _yearEndCalculation;

        public YearEndLeaveProcessingJob(YearEndCalculation yearEndCalculation)
        {
            _yearEndCalculation = yearEndCalculation;
        }

        public async Task LoadDbConfiguration()
        {
            List<LeaveYearEnd> leaveYearEnds = new List<LeaveYearEnd>();
            leaveYearEnds.Add(new LeaveYearEnd
            {
                TimezoneName = "India Standard Time",
                ProcessingDateTime = DateTime.UtcNow.AddMonths(-1),
                Timezone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"),
                ConnectionString = "server=tracker.io;port=3308;database=bottomhalf;User Id=root;password=live@Bottomhalf_001;Connection Timeout=30;Connection Lifetime=0;Min Pool Size=0;Max Pool Size=100;Pooling=true;",
            });

            leaveYearEnds.ForEach(async x => await _yearEndCalculation.RunLeaveYearEndCycle(x));

            await Task.CompletedTask;
        }
    }
}
