using ModalLayer.Modal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface ISqlSampleDataService
    {
        string GetMSSqlData(string SearchStr, string SortBy, int PageIndex, int PageSize);

        string GetMYSqlData(string SearchStr, string SortBy, int PageIndex, int PageSize);
        string GenerateTableService(DynamicTable dynamicTableSchema);
        (string, string) DownloadScriptService(DynamicTable dynamicTable);
        void TestDataTableJoning();
        string ExcelUploadedDataService(List<UploadedExcelRow> UploadedExcelData);
    }
}
