namespace ModalLayer.Modal
{
    public class FileFolderDetail
    {
        public string FolderName { set; get; }
        public string FileName { set; get; }
        public string Location { set; get; }
        public string IsFolder { set; get; }
        public string FileExtension { set; get; }
        public FileSystemType fileSystemType { set; get; }
        public string CreatedOn { set; get; }
    }
}
