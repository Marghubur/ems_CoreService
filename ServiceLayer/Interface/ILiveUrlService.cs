using ModalLayer.Modal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ServiceLayer.Interface
{
    public interface ILiveUrlService
    {
        DataSet LoadPageData(FilterModel filterModel);
        DataSet SaveUrlService(LiveUrlModal liveUrlModal);
    }
}
