using Bot.CoreBottomHalf.CommonModal;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IOnlineDocumentService
    {
        List<OnlineDocumentModel> CreateDocument(CreatePageModel createPageModel);
        string DeleteFilesService(List<Files> fileDetails);
        Task<string> EditCurrentFileService(Files editFile);
        Task<string> UploadDocumentDetail(CreatePageModel createPageModel, IFormFileCollection files, List<Files> fileDetail);
        DataSet GetFilesAndFolderByIdService(string Type, string Uid, FilterModel filterModel);
        DocumentWithFileModel GetOnlineDocumentsWithFiles(FilterModel filterModel);
        List<Files> EditFileService(Files files);
        string DeleteDataService(string Uid);
        FileDetail ReGenerateService(GenerateBillFileDetail fileDetail);
        string UpdateRecord(FileDetail fileDetail, long Uid);
        Task<string> UploadDocumentRecord(List<ProfessionalUserDetail> uploadDocument);
        DataSet GetProfessionalCandidatesRecords(FilterModel filterModel);
        Task<DataSet> UploadFilesOrDocuments(List<Files> fileDetail, IFormFileCollection files);
        DataSet GetDocumentResultById(Files fileDetail);
    }
}
