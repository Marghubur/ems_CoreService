namespace EMailService.Modal
{
    public class ContactUsDetail
    {
        public long ContactUsId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string CompanyName { get; set; }
        public string OrganizationName { get; set; }
        public string PhoneNumber { get; set; }
        public string Message { get; set; }
        public int HeadCount { get; set; }
        public long TrailRequestId { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string FullAddress { get; set; }
        public bool IsProcessed { get; set; }
    }
}
