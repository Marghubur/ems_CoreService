using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IFileService
    {
        List<Files> SaveFile(string FolderPath, List<Files> fileDetail, IFormFileCollection formFiles, string oldFileName = null);
        List<Files> SaveFileToLocation(string FolderPath, List<Files> fileDetail, IFormFileCollection formFiles);
        int DeleteFiles(List<Files> files);
        DataSet CreateFolder(Files file);
        DataSet DeleteFiles(long userId, List<string> fileIds, int userTypeId);
    }
}
