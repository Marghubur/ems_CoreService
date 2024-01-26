using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using EMailService.Modal;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class ServiceRequestService : IServiceRequestService
    {
        private readonly IDb _db;
        private CurrentSession _currentSession;
        public ServiceRequestService(IDb db, CurrentSession currentSession)
        {
            _db = db;
            _currentSession = currentSession;
        }

        public async Task<List<ServiceRequest>> GetServiceRequestService(FilterModel filter)
        {
            List<ServiceRequest> ServiceRequests = _db.GetList<ServiceRequest>(Procedures.Service_Request_Filter, new
            {
                filter.SearchString,
                filter.SortBy,
                filter.PageIndex,
                filter.PageSize
            });

            return await Task.FromResult(ServiceRequests);
        }

        public async Task<List<ServiceRequest>> AddUpdateServiceRequestService(ServiceRequest serviceRequest)
        {
            validateServiceRequest(serviceRequest);
            var oldrequest = _db.Get<ServiceRequest>(Procedures.Service_Request_Sel_By_Id, new { ServiceRequestId = serviceRequest.ServiceRequestId });
            if (oldrequest == null)
                oldrequest = serviceRequest;
            else
            {
                oldrequest.RequestTitle = serviceRequest.RequestTitle;
                oldrequest.RequestDescription = serviceRequest.RequestDescription; 
                oldrequest.ServiceRequestId = serviceRequest.ServiceRequestId;
                switch (serviceRequest.RequestTypeId)
                {
                    case "BOOKING":
                        oldrequest.Duration = serviceRequest.Duration;
                        oldrequest.FromDate = serviceRequest.FromDate;
                        oldrequest.ToDate = serviceRequest.ToDate;
                        break;
                }
            }
            oldrequest.AdminId = _currentSession.CurrentUserDetail.UserId;
            oldrequest.CompanyId = _currentSession.CurrentUserDetail.CompanyId;
            var result = _db.Execute<string>(Procedures.Service_Request_Ins_Upd, oldrequest, true);
            if (string.IsNullOrEmpty(result))
                throw HiringBellException.ThrowBadRequest("Fail to insert/update service request");

            FilterModel filterModel = new FilterModel();
            return await this.GetServiceRequestService(filterModel);
        }

        private void validateServiceRequest(ServiceRequest serviceRequest)
        {
            if (string.IsNullOrEmpty(serviceRequest.RequestTitle))
                throw HiringBellException.ThrowBadRequest("Request title is null or empty");

            if (string.IsNullOrEmpty(serviceRequest.RequestDescription))
                throw HiringBellException.ThrowBadRequest("Request description is null or empty");

            if (string.IsNullOrEmpty(serviceRequest.RequestTypeId))
                throw HiringBellException.ThrowBadRequest("Request type is null or empty");

            if (string.IsNullOrEmpty(serviceRequest.AssignTo))
                throw HiringBellException.ThrowBadRequest("Please select at least one manager");

            switch (serviceRequest.RequestTypeId)
            {
                case "BOOKING":
                    if (serviceRequest.Duration < 0)
                        throw HiringBellException.ThrowBadRequest("Invalid duration you entered");

                    if (serviceRequest.ToDate == null)
                        throw HiringBellException.ThrowBadRequest("To date is invalid");

                    if (serviceRequest.FromDate == null)
                        throw HiringBellException.ThrowBadRequest("From date is invalid");

                    if (serviceRequest.FromDate.Date.Subtract(serviceRequest.ToDate.Date).TotalDays > 0)
                        throw HiringBellException.ThrowBadRequest("To date must be greater than from date");
                    break;
            }
        }
    }
}
