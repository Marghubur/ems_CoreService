using Bot.CoreBottomHalf.CommonModal;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IUserNotificationService
    {
        Task<List<EmployeeNotification>> CreateEmployeeNotificationService(EmployeeNotification notification, List<Files> files, IFormFileCollection fileDetail);
        List<EmployeeNotification> GetEmployeeNotificationService(FilterModel filterModel);
    }
}
