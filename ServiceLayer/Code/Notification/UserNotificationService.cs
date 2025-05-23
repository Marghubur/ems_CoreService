using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using Bt.Lib.PipelineConfig.MicroserviceHttpRequest;
using Bt.Lib.PipelineConfig.Model;
using EMailService.Modal;
using FileManagerService.Model;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class UserNotificationService : IUserNotificationService
    {
        private readonly IDb _db;
        private readonly CurrentSession _currentSession;
        private readonly ICommonService _commonService;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly IFileService _fileService;
        private readonly MicroserviceRegistry _microserviceUrlLogs;
        private readonly RequestMicroservice _requestMicroservice;
        public UserNotificationService(IDb db,
            CurrentSession currentSession,
            ICommonService commonService,
            FileLocationDetail fileLocationDetail,
            IFileService fileService,
            RequestMicroservice requestMicroservice,
            MicroserviceRegistry microserviceUrlLogs)
        {
            _db = db;
            _currentSession = currentSession;
            _commonService = commonService;
            _fileLocationDetail = fileLocationDetail;
            _fileService = fileService;
            _requestMicroservice = requestMicroservice;
            _microserviceUrlLogs = microserviceUrlLogs;
        }

        public List<EmployeeNotification> GetEmployeeNotificationService(FilterModel filterModel)
        {
            if (string.IsNullOrEmpty(filterModel.SearchString) || filterModel.SearchString.Trim() == "1=1")
            {
                filterModel.SearchString = $"1=1 and UserId={_currentSession.CurrentUserDetail.UserId}";
            }

            var result = _db.GetList<EmployeeNotification>(Procedures.EMPLOYEE_NOTIFICATION_FILTER, new
            {
                filterModel.SearchString,
                filterModel.PageIndex,
                filterModel.PageSize,
                filterModel.SortBy
            });

            return result;
        }

        public async Task<List<EmployeeNotification>> CreateEmployeeNotificationService(EmployeeNotification notification, List<Files> files, IFormFileCollection FileCollection)
        {
            ValidateEmployeeNotificationModel(notification);
            var oldNotification = _db.Get<EmployeeNotification>(Procedures.EMPLOYEE_NOTIFICATION_FILTER, new
            {
                notification.NotificationId
            });

            if (oldNotification == null)
            {
                oldNotification = notification;
            }
            else
            {
                oldNotification.Title = notification.Title;
                oldNotification.SubTitle = notification.SubTitle;
                oldNotification.PlainMessage = notification.PlainMessage;
                oldNotification.ParsedContentLink = notification.ParsedContentLink;
                oldNotification.FileIds = notification.FileIds;
                oldNotification.NotifierId = notification.NotifierId;
                oldNotification.IsViewed = notification.IsViewed;
            }

            await SaveEmployeeNotificationFiles(oldNotification, FileCollection);

            FilterModel filterModel = new FilterModel
            {
                SearchString = $"1=1 and UserId={_currentSession.CurrentUserDetail.UserId}"
            };

            return GetEmployeeNotificationService(filterModel);
        }

        private async Task SaveEmployeeNotificationFiles(EmployeeNotification notification, IFormFileCollection FileCollection)
        {
            try
            {
                List<int> fileIds = new List<int>();
                if (FileCollection.Count > 0)
                {
                    // save file to server filesystem
                    var folderPath = Path.Combine(_currentSession.CompanyCode, _fileLocationDetail.CompanyFiles, "EmployeeNotification");

                    var url = $"{_microserviceUrlLogs.SaveApplicationFile}";
                    FileFolderDetail fileFolderDetail = new FileFolderDetail
                    {
                        FolderPath = folderPath,
                        OldFileName = null,
                        ServiceName = LocalConstants.EmstumFileService
                    };

                    var microserviceRequest = MicroserviceRequest.Builder(url);
                    microserviceRequest
                    .SetFiles(FileCollection)
                    .SetPayload(fileFolderDetail)
                    .SetConnectionString(_currentSession.LocalConnectionString)
                    .SetCompanyCode(_currentSession.CompanyCode)
                    .SetToken(_currentSession.Authorization);

                    var savefiles = await _requestMicroservice.UploadFile<List<Files>>(microserviceRequest);

                    int idIndex = 0;
                    foreach (var file in savefiles)
                    {
                        file.FileId = ++idIndex;
                    }

                    notification.Attachment = JsonConvert.SerializeObject(savefiles.Select(x => new { x.FilePath, x.FileId }).ToList());
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void ValidateEmployeeNotificationModel(EmployeeNotification notification)
        {
            if (string.IsNullOrEmpty(notification.Title))
                throw HiringBellException.ThrowBadRequest("Title is a required field");

            if (notification.NotificationId == 0)
                throw HiringBellException.ThrowBadRequest("Please add atleast one notifier");

            if (string.IsNullOrEmpty(notification.PlainMessage) && string.IsNullOrEmpty(notification.ParsedContentLink))
                throw HiringBellException.ThrowBadRequest("Body is required for the notification");
        }
    }
}