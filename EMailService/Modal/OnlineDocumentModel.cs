using System;
using System.Collections.Generic;

namespace ModalLayer.Modal
{
    public class CreatePageModel
    {
        public string SearchString { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public string SortBy { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public OnlineDocumentModel OnlineDocumentModel { set; get; }
    }

    public class OnlineDocumentModel
    {
        public int DocumentId { set; get; }
        public string Title { set; get; }
        public string Description { set; get; }
        public int TotalRows { set; get; }
        public Guid UserId { set; get; }
        public string DocPath { set; get; }
        public DateTime CreatedOn { set; get; }
        public DateTime? UpdatedOn { set; get; }

    }

    public class DocumentWithFileModel
    {
        public List<OnlineDocumentModel> onlineDocumentModel { get; set; }
        public List<Files> files { get; set; }
        public long TotalRecord { get; set; }
    }
}
