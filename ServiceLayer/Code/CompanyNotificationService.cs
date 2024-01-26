using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.Enums;
using BottomhalfCore.DatabaseLayer.Common.Code;
using EMailService.Modal;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace ServiceLayer.Code
{
    public class CompanyNotificationService : ICompanyNotificationService
    {
        private readonly IDb _db;
        private readonly CurrentSession _currentSession;
        private readonly ICommonService _commonService;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly IFileService _fileService;

        public CompanyNotificationService(IDb db,
            CurrentSession currentSession,
            ICommonService commonService,
            FileLocationDetail fileLocationDetail,
            IFileService fileService)
        {
            _db = db;
            _currentSession = currentSession;
            _commonService = commonService;
            _fileLocationDetail = fileLocationDetail;
            _fileService = fileService;
        }

        public DataSet GetDepartmentsAndRolesService()
        {
            var result = _db.FetchDataSet(Procedures.Department_And_Roles_Getall, new { CompanyId = _currentSession.CurrentUserDetail.CompanyId });
            return result;
        }

        public List<CompanyNotification> GetNotificationRecordService(FilterModel filterModel)
        {
            var result = _db.GetList<CompanyNotification>(Procedures.Company_Notification_Getby_Filter, new
            {
                filterModel.SearchString,
                filterModel.PageIndex,
                filterModel.PageSize,
                filterModel.SortBy
            });
            foreach (var item in result)
            {
                item.AnnouncementId = _commonService.GetUniquecode(item.NotificationId, item.Topic);
                if (item.EndDate.Subtract(item.StartDate).TotalDays > 0)
                    item.IsExpired = true;
            }
            return result;
        }

        public List<CompanyNotification> InsertUpdateNotificationService(CompanyNotification notification, List<Files> files, IFormFileCollection FileCollection)
        {
            ValidateCompanyNotification(notification);
            var oldNotification = _db.Get<CompanyNotification>(Procedures.Company_Notification_Getby_Id, new { NotificationId = notification.NotificationId });
            if (oldNotification == null)
                oldNotification = notification;
            else
            {
                oldNotification.Topic = notification.Topic;
                oldNotification.BriefDetail = notification.BriefDetail;
                oldNotification.CompleteDetail = notification.CompleteDetail;
                oldNotification.StartDate = notification.StartDate;
                oldNotification.EndDate = notification.EndDate;
                oldNotification.IsGeneralAnnouncement = notification.IsGeneralAnnouncement;
                oldNotification.AnnouncementType = notification.AnnouncementType;
            }
            if (notification.DepartmentsList != null)
                oldNotification.Departments = JsonConvert.SerializeObject(notification.DepartmentsList);
            else
                oldNotification.Departments = "[]";

            oldNotification.AdminId = _currentSession.CurrentUserDetail.UserId;
            ExecuteCompanyNotification(oldNotification, files, FileCollection);

            FilterModel filterModel = new FilterModel
            {
                SearchString = $"1=1 and CompanyId={notification.CompanyId}"
            };
            return this.GetNotificationRecordService(filterModel);
        }
        private void ExecuteCompanyNotification(CompanyNotification notification, List<Files> files, IFormFileCollection FileCollection)
        {
            try
            {
                string Result = null;
                List<int> fileIds = new List<int>();
                if (FileCollection.Count > 0)
                {
                    // save file to server filesystem
                    var folderPath = Path.Combine(_fileLocationDetail.DocumentFolder, _fileLocationDetail.CompanyFiles, "Notification");
                    _fileService.SaveFile(folderPath, files, FileCollection, notification.CompanyId.ToString());

                    foreach (var n in files)
                    {
                        Result = _db.Execute<string>(Procedures.Company_Files_Insupd, new
                        {
                            CompanyFileId = n.FileUid,
                            CompanyId = notification.CompanyId,
                            FilePath = n.FilePath,
                            FileName = n.FileName,
                            FileExtension = n.FileExtension,
                            FileDescription = n.FileDescription,
                            FileRole = n.FileRole,
                            UserTypeId = (int)UserType.Compnay,
                            AdminId = _currentSession.CurrentUserDetail.UserId
                        }, true);

                        if (string.IsNullOrEmpty(Result))
                            throw new HiringBellException("Fail to update housing property document detail. Please contact to admin.");

                        fileIds.Add(Convert.ToInt32(Result));
                    }
                }
                if (notification.FileIds != null)
                {
                    var oldfileid = JsonConvert.DeserializeObject<List<int>>(notification.FileIds);
                    if (oldfileid != null && oldfileid.Count > 0)
                    {
                        fileIds = oldfileid.Concat(fileIds).ToList();
                    }
                }
                notification.FileIds = JsonConvert.SerializeObject(fileIds);
                var result = _db.Execute<CompanyNotification>(Procedures.Company_Notification_Insupd, notification, true);
                if (string.IsNullOrEmpty(result))
                    throw HiringBellException.ThrowBadRequest("Fail to insert or update company notification");
            }
            catch (System.Exception)
            {
                _fileService.DeleteFiles(files);
                throw;
            }
        }

        private void ValidateCompanyNotification(CompanyNotification notification)
        {
            if (string.IsNullOrEmpty(notification.CompleteDetail))
                throw HiringBellException.ThrowBadRequest("Complete detail is null or empty");

            if (string.IsNullOrEmpty(notification.BriefDetail))
                throw HiringBellException.ThrowBadRequest("Brief detail is null or empty");

            if (string.IsNullOrEmpty(notification.Topic))
                throw HiringBellException.ThrowBadRequest("Topic is null or empty");

            if (notification.CompanyId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid company id");

            if (notification.StartDate == null)
                throw HiringBellException.ThrowBadRequest("Start date is null");

            if (notification.EndDate == null)
                throw HiringBellException.ThrowBadRequest("End date is null");

            if (notification.StartDate.Date > notification.EndDate.Date)
                throw HiringBellException.ThrowBadRequest("Invalid end date selected");

            if (!notification.IsGeneralAnnouncement)
            {
                if (notification.AnnouncementType <= 0)
                    throw HiringBellException.ThrowBadRequest("Invalid announcement type selected");

                if (notification.DepartmentsList.Count <= 0)
                    throw HiringBellException.ThrowBadRequest("Department details are null or empty");

                if (notification.DepartmentsList.Count > 0)
                {
                    foreach (var item in notification.DepartmentsList)
                    {
                        if (item.Id <= 0)
                            throw HiringBellException.ThrowBadRequest("Invalid department selected");

                        if (string.IsNullOrEmpty(item.Value))
                            throw HiringBellException.ThrowBadRequest("Department name is null or empty");
                    }
                }
            }

        }
    }
}
