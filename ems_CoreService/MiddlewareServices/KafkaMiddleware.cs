using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Interface;
using EMailService.Modal.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModalLayer.Modal;
using ServiceLayer.Code;
using ServiceLayer.Code.Leaves;
using ServiceLayer.Interface;
using System;
using System.Threading.Tasks;

namespace SchoolInMindServer.MiddlewareServices
{
    public class KafkaManager
    {
        public async Task InitKafkaListner(IServiceProvider _serviceProvider)
        {
            try
            {
                AutoTriggerService autoTriggerService = new AutoTriggerService(
                    _serviceProvider.GetRequiredService<ILogger<AutoTriggerService>>(),
                    _serviceProvider.GetRequiredService<IOptions<MasterDatabase>>(),
                    _serviceProvider.GetRequiredService<IOptions<KafkaServiceConfigExtend>>(),
                    _serviceProvider.GetRequiredService<ITimezoneConverter>(),
                    _serviceProvider.GetRequiredService<IWeeklyTimesheetCreationJob>(),
                    _serviceProvider.GetRequiredService<ILeaveAccrualJob>(),
                    _serviceProvider.GetRequiredService<IDb>(),
                    _serviceProvider.GetRequiredService<YearEndCalculation>(),
                    _serviceProvider.GetRequiredService<IPayrollService>()
                );

                await autoTriggerService.ScheduledJobManager();
            }
            catch (HiringBellException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
