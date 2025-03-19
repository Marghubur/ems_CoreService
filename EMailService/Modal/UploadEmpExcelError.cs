using System;

namespace EMailService.Modal
{
    public class UploadEmpExcelError
    {
        public long UploadEmpExcelErrorId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string MobileNo { get; set; }
        public string Message { get; set; }
        public string EmployeeCode { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
