using CommonModal.Modal.HtmlTagModel;

namespace ServiceLayer.Interface
{
    public interface IDocumentProcessing
    {
        void ProcessHtml(HtmlNodeDetail htmlNodeDetail);
    }
}
