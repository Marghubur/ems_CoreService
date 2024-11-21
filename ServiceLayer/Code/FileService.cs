using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using EMailService.Modal;
using ems_CommonUtility.MicroserviceHttpRequest;
using ems_CommonUtility.Model;
using FileManagerService.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CoreServiceLayer.Implementation
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly IDb _db;
        private readonly CurrentSession _currentSession;
        private readonly MicroserviceRegistry _microserviceRegistry;
        private readonly RequestMicroservice _requestMicroservice;
        public FileService(
            IWebHostEnvironment hostingEnvironment,
            IDb db,
            FileLocationDetail fileLocationDetail,
            CurrentSession currentSession,
            RequestMicroservice requestMicroservice,
            IOptions<MicroserviceRegistry> options)
        {
            _hostingEnvironment = hostingEnvironment;
            _fileLocationDetail = fileLocationDetail;
            _db = db;
            _currentSession = currentSession;
            _requestMicroservice = requestMicroservice;
            _microserviceRegistry = options.Value;
        }

        public int DeleteFiles(List<Files> files)
        {
            int deleteCount = 0;
            if (files.Count > 0)
            {
                string url = $"{_microserviceRegistry.DeleteFiles}";
                FileFolderDetail fileFolderDetail = new FileFolderDetail
                {
                    FolderPath = files.First().FilePath,
                    ServiceName = LocalConstants.EmstumFileService,
                    DeletableFiles = files.Select(x => x.FileName).ToList()
                };

                var microserviceRequest = MicroserviceRequest.Builder(url);
                microserviceRequest
                .SetPayload(fileFolderDetail)
                .SetDbConfigModal(_requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString))
                .SetConnectionString(_currentSession.LocalConnectionString)
                .SetCompanyCode(_currentSession.CompanyCode)
                .SetToken(_currentSession.Authorization);

                Task.Run(() => _requestMicroservice.PostRequest<string>(microserviceRequest));

                //foreach (var file in files)
                //{
                //    if (Directory.Exists(Path.Combine(_hostingEnvironment.ContentRootPath, file.FilePath)))
                //    {
                //        string ActualPath = Path.Combine(_hostingEnvironment.ContentRootPath, file.FilePath, file.FileName);
                //        if (File.Exists(ActualPath))
                //        {
                //            File.Delete(ActualPath);
                //            deleteCount++;
                //        }
                //    }
                //}
            }
            return deleteCount;
        }

        public List<Files> SaveFile(string FolderPath, List<Files> fileDetail, IFormFileCollection formFiles, string oldFileName = null)
        {
            string Extension = "";
            string Email = string.Empty;
            string NewFileName = string.Empty;
            string ActualPath = string.Empty;
            string _folderPath = String.Empty;
            if (!string.IsNullOrEmpty(FolderPath))
            {
                foreach (var file in formFiles)
                {
                    _folderPath = FolderPath;
                    if (!string.IsNullOrEmpty(file.Name))
                    {
                        var currentFile = fileDetail.Where(x => x.FileName == file.Name).FirstOrDefault();
                        if (currentFile != null)
                        {
                            currentFile.FilePath = _folderPath;

                            if (!Directory.Exists(Path.Combine(_hostingEnvironment.ContentRootPath, _folderPath)))
                                Directory.CreateDirectory(Path.Combine(_hostingEnvironment.ContentRootPath, _folderPath));

                            Extension = file.FileName.Substring(file.FileName.LastIndexOf('.') + 1, file.FileName.Length - file.FileName.LastIndexOf('.') - 1);
                            currentFile.FileName = file.Name;
                            if (!file.Name.Contains("."))
                                NewFileName = file.Name + "." + Extension;
                            else
                                NewFileName = file.Name;

                            if (oldFileName != null)
                            {
                                string oldFilePath = Path.Combine(_hostingEnvironment.ContentRootPath, _folderPath, oldFileName);
                                if (File.Exists(oldFilePath) && !string.IsNullOrEmpty(oldFileName))
                                    File.Delete(oldFilePath);
                            }

                            string FilePath = Path.Combine(_hostingEnvironment.ContentRootPath, _folderPath, NewFileName);
                            if (File.Exists(FilePath))
                                File.Delete(FilePath);

                            currentFile.FileExtension = Extension;

                            using (FileStream fs = System.IO.File.Create(FilePath))
                            {
                                file.CopyTo(fs);
                                fs.Flush();
                            }
                        }
                    }
                }
            }

            return fileDetail;
        }

        //public List<Files> SaveFile(string FolderPath, List<Files> fileDetail, IFormFileCollection formFiles, string OldName)
        //{
        //    string Extension = "";
        //    // string Email = string.Empty;
        //    string NewFileName = string.Empty;
        //    string ActualPath = string.Empty;
        //    string _folderPath = String.Empty;
        //    if (!string.IsNullOrEmpty(FolderPath))
        //    {
        //        foreach (var file in formFiles)
        //        {
        //            _folderPath = FolderPath;
        //            if (!string.IsNullOrEmpty(file.Name))
        //            {
        //                var currentFile = fileDetail.Where(x => x.FileName == file.Name).FirstOrDefault();
        //                // Email = currentFile.Email.Replace("@", "_").Replace(".", "_");

        //                if (currentFile.FilePath != null)
        //                {
        //                    _folderPath = Path.Combine(_folderPath, currentFile.FileOwnerId.ToString());

        //                    if (currentFile.FilePath.IndexOf(Email) == -1 && _folderPath.IndexOf(Email) == -1)
        //                        _folderPath = Path.Combine(_folderPath, Email);
        //                }
        //                else
        //                {
        //                    _folderPath = Path.Combine(_folderPath, Email);
        //                }

        //                if (!string.IsNullOrEmpty(currentFile.FilePath))
        //                    ActualPath = Path.Combine(_folderPath, currentFile.FilePath);
        //                else
        //                    ActualPath = _folderPath;

        //                currentFile.FilePath = ActualPath;
        //                if (!Directory.Exists(Path.Combine(_hostingEnvironment.ContentRootPath, ActualPath)))
        //                    Directory.CreateDirectory(Path.Combine(_hostingEnvironment.ContentRootPath, ActualPath));

        //                Extension = file.FileName.Substring(file.FileName.LastIndexOf('.') + 1, file.FileName.Length - file.FileName.LastIndexOf('.') - 1);
        //                currentFile.FileName = file.Name;
        //                if (!file.Name.Contains("."))
        //                    NewFileName = file.Name + "." + Extension;
        //                else
        //                    NewFileName = file.Name;

        //                if (currentFile != null)
        //                {
        //                    string FilePath = Path.Combine(_hostingEnvironment.ContentRootPath, ActualPath, NewFileName);
        //                    if (File.Exists(FilePath))
        //                        File.Delete(FilePath);
        //                    if (OldName != null)
        //                    {
        //                        string oldFilePath = Path.Combine(_hostingEnvironment.ContentRootPath, ActualPath, OldName);
        //                        if (File.Exists(oldFilePath) && !string.IsNullOrEmpty(OldName))
        //                            File.Delete(oldFilePath);
        //                    }
        //                    currentFile.FileExtension = Extension;
        //                    currentFile.FilePath = ActualPath;

        //                    using (FileStream fs = System.IO.File.Create(FilePath))
        //                    {
        //                        file.CopyTo(fs);
        //                        fs.Flush();
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    return fileDetail;
        //}

        public List<Files> SaveFileToLocation(string FolderPath, List<Files> fileDetail, IFormFileCollection formFiles)
        {
            string Extension = "";
            string _folderPath = String.Empty;

            if (!string.IsNullOrEmpty(FolderPath))
            {
                int i = 0;
                foreach (var file in formFiles)
                {
                    _folderPath = FolderPath;
                    if (!string.IsNullOrEmpty(file.Name))
                    {
                        var currentFile = fileDetail.Where(x => x.FileName == file.Name).FirstOrDefault();

                        currentFile.FilePath = _folderPath;
                        if (!Directory.Exists(Path.Combine(_hostingEnvironment.ContentRootPath, _folderPath)))
                            Directory.CreateDirectory(Path.Combine(_hostingEnvironment.ContentRootPath, _folderPath));

                        Extension = file.FileName.Substring(file.FileName.LastIndexOf('.') + 1, file.FileName.Length - file.FileName.LastIndexOf('.') - 1);

                        if (!string.IsNullOrEmpty(currentFile.AlternateName))
                            currentFile.FileName = currentFile.AlternateName + "_" + i + "." + Extension;
                        else
                        {
                            if (!file.Name.Contains("."))
                                currentFile.FileName = file.Name + "." + Extension;
                            else
                                currentFile.FileName = file.Name;
                        }
                        if (currentFile != null)
                        {
                            string FilePath = Path.Combine(_hostingEnvironment.ContentRootPath, _folderPath, currentFile.FileName);
                            if (File.Exists(FilePath))
                            {
                                File.Delete(FilePath);
                            }

                            currentFile.FileExtension = Extension;
                            currentFile.DocumentId = 0;
                            currentFile.FilePath = _folderPath;

                            using (FileStream fs = System.IO.File.Create(FilePath))
                            {
                                file.CopyTo(fs);
                                fs.Flush();
                            }
                        }
                    }
                    i++;
                }
            }
            return fileDetail;
        }

        public DataSet DeleteFiles(long userId, List<string> fileIds, int userTypeId)
        {
            this.DeleteFilesEntry(fileIds, ApplicationConstants.GetUserFileById);
            var resultSet = GetUserFilesById(userId, userTypeId);
            return resultSet;
        }

        public DataSet GetUserFilesById(long userId, int userTypeId)
        {
            DataSet Result = null;
            if (userId > 0)
            {
                Result = _db.GetDataSet(Procedures.Document_Filedetail_Get, new
                {
                    OwnerId = userId,
                    UserTypeId = userTypeId,
                });
            }

            return Result;
        }

        public async Task<DataSet> CreateFolder(Files fileDetail)
        {
            bool isLocationFound = false;
            //string actualFolderPath = string.Empty;
            DataSet dataSet = null;
            if (fileDetail != null)
            {
                //fileDetail.FilePath = fileDetail.FilePath;
                if (string.IsNullOrEmpty(fileDetail.ParentFolder))
                    fileDetail.ParentFolder = Path.Combine(_currentSession.CompanyCode, _fileLocationDetail.User);
                else
                    fileDetail.ParentFolder = Path.Combine(_currentSession.CompanyCode, _fileLocationDetail.User, fileDetail.ParentFolder);

                //fileDetail.ParentFolder = fileDetail.ParentFolder;
                if (!string.IsNullOrEmpty(fileDetail.FilePath))
                {
                    switch (fileDetail.SystemFileType)
                    {
                        case FileSystemType.User:
                            isLocationFound = true;
                            fileDetail.FilePath = Path.Combine(_currentSession.CompanyCode, _fileLocationDetail.User, fileDetail.FilePath);

                            //fileDetail.FilePath = Path.Combine(
                            //        _fileLocationDetail.UserFolder,
                            //        fileDetail.FilePath
                            //    );

                            //fileDetail.FilePath = fileDetail.FilePath;
                            //actualFolderPath = Path.Combine(
                            //            _hostingEnvironment.ContentRootPath,
                            //            fileDetail.FilePath
                            //        );
                            break;
                        case FileSystemType.Bills:
                            break;
                    }

                    if (isLocationFound)
                    {
                        //if (!Directory.Exists(actualFolderPath))
                        //    Directory.CreateDirectory(actualFolderPath);

                        string url = $"{_microserviceRegistry.CreateFolder}";
                        FileFolderDetail fileFolderDetail = new FileFolderDetail
                        {
                            FolderPath = fileDetail.FilePath,
                            ServiceName = LocalConstants.EmstumFileService
                        };

                        var microserviceRequest = MicroserviceRequest.Builder(url);
                        microserviceRequest
                        .SetPayload(fileFolderDetail)
                        .SetDbConfigModal(_requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString))
                        .SetConnectionString(_currentSession.LocalConnectionString)
                        .SetCompanyCode(_currentSession.CompanyCode)
                        .SetToken(_currentSession.Authorization);

                        fileDetail.FilePath = await _requestMicroservice.PostRequest<string>(microserviceRequest);
                        int lastIndex = fileDetail.FilePath.LastIndexOf('\\');
                        if (lastIndex != -1)
                            fileDetail.ParentFolder = fileDetail.FilePath.Substring(0, lastIndex);

                        List<Files> files = new List<Files>
                            {
                                fileDetail
                            };

                        this.InsertFileDetails(files, ApplicationConstants.InserUserFileDetail);
                        dataSet = this.GetUserFilesById(fileDetail.UserId, (int)fileDetail.UserTypeId);

                    }
                }
            }

            return await Task.FromResult(dataSet);
        }

        public Tuple<string, bool> InsertFileDetails(List<Files> fileDetail, string procedure)
        {
            var Items = fileDetail.Where(x => x.UserId > 0);
            if (Items.Count() == 0)
            {
                return new Tuple<string, bool>("Incorrect userId provided.", false);
            }

            var fileInfo = JsonConvert.SerializeObject((from n in fileDetail.AsEnumerable()
                                                        select new
                                                        {
                                                            FileId = n.FileUid,
                                                            FileOwnerId = n.UserId,
                                                            FileName = n.FileName,
                                                            FilePath = n.FilePath,
                                                            ParentFolder = n.ParentFolder,
                                                            FileExtension = n.FileExtension,
                                                            StatusId = 0,
                                                            UserTypeId = (int)n.UserTypeId,
                                                            AdminId = 1
                                                        }));

            // DataTable table = Converter.ToDataTable(fileInfo);
            var result = _db.ExecuteAsync(procedure, new { InsertFileJsonData = fileInfo }, false);
            return new Tuple<string, bool>("Total " + result + " inserted/updated.", true);
        }

        public string DeleteFilesEntry(List<string> fileIds, string GetFilesProcedure)
        {
            string Result = "Fail";
            if (fileIds != null && fileIds.Count > 0)
            {
                DataSet FileSet = _db.GetDataSet(GetFilesProcedure, new
                {
                    FileIds = fileIds.Aggregate((x, y) => x + "," + y)
                });

                if (FileSet.Tables.Count > 0)
                {
                    List<Files> files = Converter.ToList<Files>(FileSet.Tables[0]);
                    var dbResult = _db.Execute(ApplicationConstants.deleteUserFile, new
                    {
                        FileIds = fileIds.Aggregate((x, y) => x + "," + y)
                    }, true);

                    Result = dbResult.statusMessage;
                    if (dbResult.statusMessage == "Deleted successfully")
                        DeleteFiles(files);
                }
            }
            return Result;
        }
    }
}
