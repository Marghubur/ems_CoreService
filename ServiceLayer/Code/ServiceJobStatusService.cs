using BottomhalfCore.DatabaseLayer.Common.Code;
using EMailService.Modal;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class ServiceJobStatusService(IDb _db) : IServiceJobStatusService
    {
        public async Task<ServiceJobStatus> GetServiceJobStatusService(int serviceJobStatusId)
        {
            if (serviceJobStatusId == 0)
                throw HiringBellException.ThrowBadRequest("Invalid service job status id");

            var serviceJobStatus = _db.Get<ServiceJobStatus>(Procedures.SERVICE_JOB_STATUS_GET_BY_ID, new { ServiceJobStatusId = serviceJobStatusId });
            if (serviceJobStatus == null)
                throw HiringBellException.ThrowBadRequest("Fail to get existing service job detail");

            return await Task.FromResult(serviceJobStatus);
        }

        public async Task<int> AddServiceJobStatusService(string serviceName)
        {
            ServiceJobStatus serviceJobStatus = new ServiceJobStatus
            {
                ServiceJobStatusId = 0,
                JobStartedOn = DateTime.UtcNow,
                JobStatus = (int)ItemStatus.Pending,
                ServiceName = serviceName,
                JobEndedOn = null,
                ServiceLog = "[]"
            };

            return await InsertUpdateServiceJobStatu(serviceJobStatus);
        }

        public async Task<int> UpdateServiceJobStatusService(int serviceJobStatusId)
        {
            ServiceJobStatus existingervice = await GetServiceJobStatusService(serviceJobStatusId);

            existingervice.JobEndedOn = DateTime.UtcNow;
            existingervice.JobStatus = (int)ItemStatus.Approved;

            return await InsertUpdateServiceJobStatu(existingervice);
        }

        private async Task<int> InsertUpdateServiceJobStatu(ServiceJobStatus serviceJobStatus)
        {
            var result = await _db.ExecuteAsync(Procedures.SERVICE_JOB_STATUS_INS_UPD, new
            {
                serviceJobStatus.ServiceJobStatusId,
                serviceJobStatus.ServiceName,
                serviceJobStatus.JobStartedOn,
                serviceJobStatus.JobEndedOn,
                serviceJobStatus.JobStatus,
                serviceJobStatus.ServiceLog
            }, true);
            if (string.IsNullOrEmpty(result.statusMessage))
                throw HiringBellException.ThrowBadRequest("Fail to insert/update service job detail");

            return Convert.ToInt32(result.statusMessage);
        }

    }
}
