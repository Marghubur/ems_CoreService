using ModalLayer.Modal.HtmlTagModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceLayer.Interface
{
    public interface IDocumentProcessing
    {
        void ProcessHtml(HtmlNodeDetail htmlNodeDetail);
    }
}
