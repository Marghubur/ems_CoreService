using ModalLayer.Modal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IServiceRequestService
    {
        Task<List<ServiceRequest>> GetServiceRequestService(FilterModel filter);
        Task<List<ServiceRequest>> AddUpdateServiceRequestService(ServiceRequest serviceRequest);
    }
}
