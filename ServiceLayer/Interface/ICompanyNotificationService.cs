using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using System.Collections.Generic;
using System.Data;

namespace ServiceLayer.Interface
{
    public interface ICompanyNotificationService
    {
        List<CompanyNotification> InsertUpdateNotificationService(CompanyNotification notification, List<Files> files, IFormFileCollection fileDetail);
        List<CompanyNotification> GetNotificationRecordService(FilterModel filterModel);
        DataSet GetDepartmentsAndRolesService();
    }
}
