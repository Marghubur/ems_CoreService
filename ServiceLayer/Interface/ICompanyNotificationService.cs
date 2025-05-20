using Bot.CoreBottomHalf.CommonModal;
using EMailService.Modal;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface ICompanyNotificationService
    {
        Task<List<CompanyNotification>> InsertUpdateNotificationService(CompanyNotification notification, List<Files> files, IFormFileCollection fileDetail);
        List<CompanyNotification> GetNotificationRecordService(FilterModel filterModel);
        DataSet GetDepartmentsAndRolesService();
        Task<List<EMailService.Modal.Notification.CompanyNotification>> GetCompanyNotificationFilterService(FilterModel filterModel);
    }
}
