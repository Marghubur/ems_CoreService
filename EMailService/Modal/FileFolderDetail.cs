using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace FileManagerService.Model
{
    public class FileFolderDetail
    {
        public string FolderPath { get; set; }
        public List<string> DeletableFiles { get; set; }
        // public List<Files> FileDetail { get; set; }
        public IFormFileCollection FormFiles { get; set; }
        public string OldFileName { get; set; }
        // public List<string> FileIds { get; set; }
        // public string Procedure { get; set; }
        public string ServiceName { get; set; }
        // public int UserTypeId { get; set; }
        // public long UserId { get; set; }
    }
}
