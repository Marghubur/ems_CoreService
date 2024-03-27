namespace EMailService.Modal
{
    public class ContactUsDetail
    {
        public long ContactUsId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string CompanyName { get; set; }
        public string PhoneNumber { get; set; }
        public string Message { get; set; }
        public int HeadCount { get; set; }
        public long TrailRequestId { get; set; }
    }
}
