using System;

namespace ModalLayer.Modal
{
    public class AnnexureOfferLetter
    {
        public int AnnexureOfferLetterId { set; get; }
        public string TemplateName { set; get; }
        public string BodyContent { set; get; }
        public int CompanyId { get; set; }
        public int FileId { get; set; }
        public string FilePath { get; set; }
        public long AdminId { set; get; }
        public int LetterType { get; set; }
        public DateTime UpdatedOn { set; get; }
    }
}
