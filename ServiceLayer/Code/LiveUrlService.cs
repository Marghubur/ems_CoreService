using BottomhalfCore.DatabaseLayer.Common.Code;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ServiceLayer.Code
{
    public class LiveUrlService : ILiveUrlService
    {
        private readonly IDb db;
        public LiveUrlService(IDb db)
        {
            this.db = db;
        }

        public DataSet LoadPageData(FilterModel filterModel)
        {
            if (string.IsNullOrEmpty(filterModel.SearchString))
                filterModel.SearchString = "1=1";
            if (filterModel.PageIndex <= 0)
                filterModel.PageIndex = 1;
            if (filterModel.PageSize < 10)
                filterModel.PageSize = 10;

            DataSet ds = this.db.GetDataSet("SP_liveurl_get", new
            {
                searchString = filterModel.SearchString,
                pageIndex = filterModel.PageIndex,
                pageSize = filterModel.PageSize,
            });

            return ds;
        }

        public DataSet SaveUrlService(LiveUrlModal liveUrlModal)
        {
            if (string.IsNullOrEmpty(liveUrlModal.method))
                return null;
            if (string.IsNullOrEmpty(liveUrlModal.url))
                return null;

            this.db.Execute("SP_liveurl_InsUpd", new
            {
                savedUrlId = liveUrlModal.savedUrlId,
                method = liveUrlModal.method,
                parameter = liveUrlModal.paramters,
                url = liveUrlModal.url,
            }, false);

            DataSet ds = LoadPageData(new FilterModel { SearchString = "1=1" });
            return ds;
        }
    }
}
