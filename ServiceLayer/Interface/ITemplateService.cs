using ModalLayer.Modal;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface ITemplateService
    {
        EmailTemplate GetBillingTemplateDetailService();
        Task<AnnexureOfferLetter> AnnexureOfferLetterInsertUpdateService(AnnexureOfferLetter annexureOfferLetter, int LetterType);
        Task<AnnexureOfferLetter> GetOfferLetterService(int CompanyId, int LetterType);
        List<AnnexureOfferLetter> GetAnnextureService(int CompanyId, int LetterType);
        string EmailLinkConfigInsUpdateService(EmailLinkConfig emailLinkConfig);
        Task<dynamic> EmailLinkConfigGetByPageNameService(string PageName, int CompanyId);
        Task<string> GenerateUpdatedPageMailService(EmailLinkConfig emailLinkConfig);
    }
}
