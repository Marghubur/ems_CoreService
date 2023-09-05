namespace ModalLayer.Modal
{
    public class EmailMappedTemplate
    {
        public int EmailTempMappingId {set; get;}
        public string EmailTemplateName { set; get;}
        public int TemplateId { set; get;}
        public int CompanyId { get; set; }
        public long AdminId { get; set; }
        public int Total { get; set; }
    }
}
