using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Code;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class OnlineDocumentController : BaseController
    {
        private readonly IOnlineDocumentService _ionlineDocumentService;
        private readonly CommonFilterService _commonFilterService;
        private readonly HttpContext _httpContext;

        public OnlineDocumentController(IOnlineDocumentService ionlineDocumentService,
            IHttpContextAccessor httpContext,
            CommonFilterService commonFilterService)
        {
            _ionlineDocumentService = ionlineDocumentService;
            _httpContext = httpContext.HttpContext;
            _commonFilterService = commonFilterService;
        }

        [HttpPost]
        [Route("GetOnlineDocuments")]
        public IResponse<ApiResponse> GetOnlineDocuments([FromBody] FilterModel filterModel)
        {
            try
            {
                var Result = _commonFilterService.GetResult<OnlineDocumentModel>(filterModel, "SP_OnlineDocument_Get");
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }

        [HttpPost]
        [Route("GetOnlineDocumentsWithFiles")]
        public IResponse<ApiResponse> GetOnlineDocumentsWithFiles([FromBody] FilterModel filterModel)
        {
            try
            {
                var Result = _ionlineDocumentService.GetOnlineDocumentsWithFiles(filterModel);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }

        [HttpPost]
        [Route("CreateDocument")]
        public IResponse<ApiResponse> CreateDocument([FromBody] CreatePageModel createPageModel)
        {
            try
            {
                var Result = _ionlineDocumentService.CreateDocument(createPageModel);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, createPageModel);
            }
        }

        [HttpPost("DeleteFiles")]
        public IResponse<ApiResponse> DeleteFiles([FromBody] List<Files> fileDetails)
        {
            try
            {
                var Result = _ionlineDocumentService.DeleteFilesService(fileDetails);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, fileDetails);
            }
        }

        [HttpPost("EditCurrentFile")]
        public async Task<ApiResponse> EditCurrentFile([FromBody] Files fileDetail)
        {
            try
            {
                var Result = await _ionlineDocumentService.EditCurrentFileService(fileDetail);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, fileDetail);
            }
        }

        [HttpPost("UploadDocumentDetail")]
        public async Task<ApiResponse> UploadDocumentDetail()
        {
            CreatePageModel createPageModel = null;
            try
            {
                _httpContext.Request.Form.TryGetValue("facultObject", out StringValues RegistrationData);
                _httpContext.Request.Form.TryGetValue("fileDetail", out StringValues FileData);
                if (RegistrationData.Count > 0)
                {
                    createPageModel = JsonConvert.DeserializeObject<CreatePageModel>(RegistrationData[0]);
                    List<Files> fileDetail = JsonConvert.DeserializeObject<List<Files>>(FileData);
                    if (createPageModel != null)
                    {
                        IFormFileCollection files = _httpContext.Request.Form.Files;
                        var Result = await _ionlineDocumentService.UploadDocumentDetail(createPageModel, files, fileDetail);
                        BuildResponse(Result, HttpStatusCode.OK);
                    }
                }
                return apiResponse;
            }
            catch (Exception ex)
            {
                throw Throw(ex, createPageModel);
            }
        }

        [HttpPost("GetFilesAndFolderById/{Type}/{Uid}")]
        public ApiResponse GetFilesAndFolderById(string Type, string Uid, [FromBody] FilterModel filterModel)
        {
            try
            {
                var Result = _ionlineDocumentService.GetFilesAndFolderByIdService(Type, Uid, filterModel);
                return BuildResponse(Result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { Type = Type, Uid = Uid, FilterModel = filterModel });
            }
        }

        [HttpPost("EditFile")]
        public IResponse<ApiResponse> EditFile([FromBody] Files files)
        {
            try
            {
                var result = _ionlineDocumentService.EditFileService(files);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, files);
            }
        }

        [HttpGet("DeleteData/{Uid}")]
        public IResponse<ApiResponse> DeleteData(string Uid)
        {
            try
            {
                var result = _ionlineDocumentService.DeleteDataService(Uid);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, Uid);
            }
        }

        [HttpPost("UpdateRecord/{Uid}")]
        public IResponse<ApiResponse> UpdateRecord([FromBody] FileDetail fileDetail, [FromRoute] long Uid)
        {
            var result = _ionlineDocumentService.UpdateRecord(fileDetail, Uid);
            return BuildResponse(result, HttpStatusCode.OK);
        }

        [HttpPost("UploadDocumentRecords")]
        public async Task<ApiResponse> UploadDocumentRecords([FromBody] List<ProfessionalUserDetail> uploadDocument)
        {
            try
            {
                var result = await _ionlineDocumentService.UploadDocumentRecord(uploadDocument);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, uploadDocument);
            }
        }

        [HttpPost("GetUploadedRecords")]
        public IResponse<ApiResponse> GetProfessionalCandidatesRecords(FilterModel filterModel)
        {
            try
            {
                var result = _ionlineDocumentService.GetProfessionalCandidatesRecords(filterModel);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }

        [HttpPost("GetDocumentByUserId")]
        public IResponse<ApiResponse> GetDocumentByUserId(Files filterModel)
        {
            try
            {
                var result = _ionlineDocumentService.GetDocumentResultById(filterModel);
                return BuildResponse(result, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }

        [HttpPost("UploadFile")]
        public async Task<IResponse<ApiResponse>> UploadProfessionalCandidatesFile()
        {
            List<Files> fileDetail = null;
            try
            {
                StringValues RegistrationData = default(string);
                StringValues FileData = default(string);
                _httpContext.Request.Form.TryGetValue("fileDetail", out FileData);
                if (FileData.Count > 0)
                {
                    fileDetail = JsonConvert.DeserializeObject<List<Files>>(FileData);
                    IFormFileCollection files = _httpContext.Request.Form.Files;
                    var Result = await _ionlineDocumentService.UploadFilesOrDocuments(fileDetail, files);
                    return BuildResponse(Result, HttpStatusCode.OK);
                }
                return BuildResponse("No files found", HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw Throw(ex, fileDetail);
            }
        }
    }
}