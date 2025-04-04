﻿using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using Bot.CoreBottomHalf.CommonModal.Enums;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using Bt.Lib.PipelineConfig.MicroserviceHttpRequest;
using Bt.Lib.PipelineConfig.Model;
using EMailService.Modal;
using FileManagerService.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using ModalLayer.Modal.Profile;
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
    public class UserService : IUserService
    {
        private readonly IDb _db;
        private readonly IFileService _fileService;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly CurrentSession _currentSession;
        private readonly IEmployeeService _employeeService;
        private readonly RequestMicroservice _requestMicroservice;
        private readonly MicroserviceRegistry _microserviceUrlLogs;

        public UserService(
            IDb db,
            IFileService fileService,
            FileLocationDetail fileLocationDetail,
            IWebHostEnvironment hostingEnvironment,
            CurrentSession currentSession,
            IEmployeeService employeeService,
            RequestMicroservice requestMicroservice,
            MicroserviceRegistry microserviceUrlLogs)
        {
            _db = db;
            _fileService = fileService;
            _fileLocationDetail = fileLocationDetail;
            _hostingEnvironment = hostingEnvironment;
            _currentSession = currentSession;
            _employeeService = employeeService;
            _requestMicroservice = requestMicroservice;
            _microserviceUrlLogs = microserviceUrlLogs;
        }

        public ProfileDetail UpdateProfile(ProfessionalUser professionalUser, int UserTypeId, int IsProfileImageRequest = 0)
        {
            professionalUser.ProfessionalDetailJson = JsonConvert.SerializeObject(professionalUser);
            var result = _db.Execute<ProfessionalUser>(Procedures.Professionaldetail_Insupd, new
            {
                professionalUser.EmployeeId,
                professionalUser.Mobile,
                professionalUser.Email,
                professionalUser.FirstName,
                professionalUser.LastName,
                professionalUser.ProfessionalDetailJson
            }, true);
            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Unable to insert of update");

            long employeeId = Convert.ToInt64(result);


            return GetUserDetail(employeeId);
        }

        public async Task<ProfileDetail> UploadUserInfo(string userId, ProfessionalUser professionalUser, IFormFileCollection FileCollection, int UserTypeId)
        {
            if (string.IsNullOrEmpty(professionalUser.Email))
            {
                throw new HiringBellException("Email id is required field.");
            }

            int IsProfileImageRequest = 0;

            Files file = new Files();
            if (FileCollection.Count > 0)
            {
                var ownerPath = Path.Combine(_fileLocationDetail.User, $"{nameof(UserType.Employee)}_{professionalUser.EmployeeId}");
                string url = $"{_microserviceUrlLogs.SaveApplicationFile}";
                FileFolderDetail fileFolderDetail = new FileFolderDetail
                {
                    FolderPath = ownerPath,
                    OldFileName = string.IsNullOrEmpty(professionalUser.OldFileName) ? null : new List<string> { professionalUser.OldFileName },
                    ServiceName = LocalConstants.EmstumFileService
                };

                var microserviceRequest = MicroserviceRequest.Builder(url);
                microserviceRequest
                .SetFiles(FileCollection)
                .SetPayload(fileFolderDetail)
                .SetConnectionString(_currentSession.LocalConnectionString)
                .SetCompanyCode(_currentSession.CompanyCode)
                .SetToken(_currentSession.Authorization);

                List<Files> files = await _requestMicroservice.UploadFile<List<Files>>(microserviceRequest);

                var fileInfo = (from n in files
                                select new
                                {
                                    FileId = professionalUser.FileId,
                                    FileOwnerId = professionalUser.EmployeeId,
                                    FileName = n.FileName,
                                    FilePath = n.FilePath,
                                    FileExtension = n.FileExtension,
                                    UserTypeId = UserTypeId,
                                    ItemStatusId = LocalConstants.Profile,
                                    AdminId = _currentSession.CurrentUserDetail.UserId
                                }).ToList();

                int insertedCount = await _db.BulkExecuteAsync(Procedures.Userfiledetail_Upload, fileInfo, true);
            }

            var value = this.UpdateProfile(professionalUser, UserTypeId, IsProfileImageRequest);
            return value;
        }

        public async Task<Files> UploadResume(string userId, ProfessionalUser professionalUser, IFormFileCollection FileCollection, int UserTypeId)
        {
            if (Int32.Parse(userId) <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid user");

            Files file = new Files();
            if (FileCollection.Count > 0)
            {
                var ownerPath = Path.Combine(_fileLocationDetail.User, $"{nameof(UserType.Employee)}_{professionalUser.EmployeeId}");
                string url = $"{_microserviceUrlLogs.SaveApplicationFile}";
                FileFolderDetail fileFolderDetail = new FileFolderDetail
                {
                    FolderPath = ownerPath,
                    OldFileName = string.IsNullOrEmpty(professionalUser.OldFileName) ? null : new List<string> { professionalUser.OldFileName },
                    ServiceName = LocalConstants.EmstumFileService
                };

                var microserviceRequest = MicroserviceRequest.Builder(url);
                microserviceRequest
                .SetFiles(FileCollection)
                .SetPayload(fileFolderDetail)
                .SetConnectionString(_currentSession.LocalConnectionString)
                .SetCompanyCode(_currentSession.CompanyCode)
                .SetToken(_currentSession.Authorization);

                List<Files> files = await _requestMicroservice.UploadFile<List<Files>>(microserviceRequest);

                var fileInfo = (from n in files
                                select new
                                {
                                    FileId = professionalUser.FileId,
                                    FileOwnerId = professionalUser.EmployeeId,
                                    FileName = n.FileName,
                                    FilePath = n.FilePath,
                                    FileExtension = n.FileExtension,
                                    UserTypeId = UserTypeId,
                                    ItemStatusId = LocalConstants.Resume,
                                    AdminId = _currentSession.CurrentUserDetail.UserId
                                }).ToList();

                var status = await _db.BulkExecuteAsync(Procedures.Userfiledetail_Upload, fileInfo, true);
                file = files[0];
            }

            return file;
        }

        //public async Task<string> UploadDeclaration(string UserId, int UserTypeId, UserDetail userDetail, IFormFileCollection FileCollection, List<Files> files)
        //{
        //    string result = string.Empty;
        //    if (Int32.Parse(UserId) <= 0)
        //        throw new HiringBellException("Invalid UserId");

        //    if (UserTypeId <= 0)
        //        throw new HiringBellException("Invalid UserTypeId");

        //    // Files file = new Files();
        //    if (FileCollection.Count > 0)
        //    {
        //        _fileService.SaveFile(_fileLocationDetail.UserFolder, files, FileCollection, UserId);
        //        var fileInfo = (from n in files
        //                        select new
        //                        {
        //                            FileId = n.FileUid,
        //                            FileOwnerId = UserId,
        //                            FileName = n.FileName,
        //                            FilePath = n.FilePath,
        //                            FileExtension = n.FileExtension,
        //                            UserTypeId = UserTypeId,
        //                            AdminId = _currentSession.CurrentUserDetail.UserId
        //                        }).ToList();

        //        int insertedCount = await _db.BulkExecuteAsync("", fileInfo, true);
        //        if (insertedCount == 1)
        //            result = "Declaration Uploaded Successfully.";
        //    }
        //    return result;
        //}

        public ProfileDetail GetUserDetail(long EmployeeId)
        {
            if (EmployeeId <= 0)
                throw new HiringBellException { UserMessage = "Invalid UserTypeId", FieldName = nameof(EmployeeId), FieldValue = EmployeeId.ToString() };

            var result = _db.FetchDataSet(Procedures.Professionaldetail_Get_Byid, new { EmployeeId });
            if (result.Tables.Count != 3)
                throw new HiringBellException("unable to get records");

            ProfileDetail profileDetail = new ProfileDetail();
            profileDetail.employee = Converter.ToType<Employee>(result.Tables[0]);
            ProfessionalUser professionalUser = Converter.ToType<ProfessionalUser>(result.Tables[1]);
            profileDetail.profileDetail = Converter.ToList<FileDetail>(result.Tables[2]);

            if (profileDetail.employee == null)
                throw new HiringBellException("Unable to get employee detail.");

            if (professionalUser.ProfessionalDetailJson != null)
                profileDetail.professionalUser = JsonConvert.DeserializeObject<ProfessionalUser>(professionalUser.ProfessionalDetailJson);

            return profileDetail;
        }

        public string GenerateResume(long userId)
        {
            if (userId <= 0)
                throw new HiringBellException { UserMessage = "Invalid User Id", FieldName = nameof(userId), FieldValue = userId.ToString() };

            var value = string.Empty;
            ProfileDetail profileDetail = new ProfileDetail();

            var Result = _db.GetDataSet(Procedures.Professionaldetail_Filter, new
            {
                UserId = userId,
                Mobile = _currentSession.CurrentUserDetail.Mobile,
                Email = _currentSession.CurrentUserDetail.EmailId,
            });

            if (Result.Tables.Count == 0)
            {
                throw new HiringBellException("Fail to get record.");
            }
            else
            {
                profileDetail.profileDetail = Converter.ToList<FileDetail>(Result.Tables[1]);
                string jsonData = Convert.ToString(Result.Tables[0].Rows[0][0]);
                if (!string.IsNullOrEmpty(jsonData))
                {
                    profileDetail.professionalUser = JsonConvert.DeserializeObject<ProfessionalUser>(jsonData);
                }
                else
                {
                    throw new HiringBellException("Fail to get record.");
                }

                string rootPath = _hostingEnvironment.ContentRootPath;
                string templatePath = Path.Combine(rootPath,
                    _fileLocationDetail.Location,
                    Path.Combine(_fileLocationDetail.resumePath.ToArray()),
                    _fileLocationDetail.resumeTemplate
                );
            }

            return value;
        }

        public async Task<DataSet> GetEmployeeAndChientListService()
        {
            DataSet ds = new DataSet();
            FilterModel filterModel = new FilterModel();
            filterModel.PageSize = 10000;

            ds = _db.FetchDataSet(Procedures.Employee_And_All_Clients_Get, new
            {
                SearchString = filterModel.SearchString,
                SortBy = filterModel.SortBy,
                PageIndex = filterModel.PageIndex,
                PageSize = filterModel.PageSize,
                IsActive = filterModel.IsActive
            });

            if (ds == null || ds.Tables.Count != 2)
                throw new HiringBellException("Unable to find employees");

            ds.Tables[0].TableName = "Employees";
            ds.Tables[1].TableName = "Clients";

            return await Task.FromResult(ds);
        }
    }
}
