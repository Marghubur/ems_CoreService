using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace FileManagerService.Model
{
    public class FileFolderDetail
    {
        public string FolderPath { get; set; }
        public List<string> DeletableFiles { get; set; }
        public IFormFileCollection FormFiles { get; set; }
        public List<string> OldFileName { get; set; }
        public string ServiceName { get; set; }
    }
}
