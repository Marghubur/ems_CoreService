using System;

namespace ModalLayer.Modal
{
    public class CurrentSession
    {
        public CurrentSession()
        {
            CurrentUserDetail = new UserDetail();
        }
        public string UserAgent { set; get; }
        public string Authorization { set; get; }
        public string Culture { set; get; } = "en";
        public string RequestPath { set; get; }
        public string FileUploadFolderName { set; get; }
        public UserDetail CurrentUserDetail { set; get; }
        public TimeZoneInfo TimeZone { set; get; }
        public DateTime TimeZoneNow { set; get; }
        public string CompanyCode { set; get; }
    }
}
