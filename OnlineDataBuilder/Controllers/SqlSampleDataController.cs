using Microsoft.AspNetCore.Mvc;
using ModalLayer.Modal;
using OnlineDataBuilder.ContextHandler;
using ServiceLayer.Code;
using ServiceLayer.Interface;
using System.Collections.Generic;
using System.Net;

namespace OnlineDataBuilder.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SqlSampleDataController : BaseController
    {
        private readonly ISqlSampleDataService sqlSampleDataService;
        public SqlSampleDataController(SqlSampleDataService sqlSampleDataService)
        {
            this.sqlSampleDataService = sqlSampleDataService;
        }

        [HttpGet]
        [Route("api/GetSqlData")]
        public IResponse<ApiResponse> GetSqlData(string SearchStr, string SortBy, int PageIndex, int PageSize)
        {
            string ResultSet = this.sqlSampleDataService.GetMSSqlData(SearchStr, SortBy, PageIndex, PageSize);
            BuildResponse(ResultSet, HttpStatusCode.OK);
            return apiResponse;
        }

        [HttpPost]
        [Route("api/GenerateTable")]
        public IResponse<ApiResponse> GenerateTable(DynamicTable dynamicTableSchema)
        {
            string ResultSet = this.sqlSampleDataService.GenerateTableService(dynamicTableSchema);
            BuildResponse(ResultSet, HttpStatusCode.OK);
            return apiResponse;
        }

        [HttpPost]
        [Route("api/UploadExcelData")]
        public IResponse<ApiResponse> UploadExcelData([FromBody] List<UploadedExcelRow> UploadedExcelData)
        {
            string ResultSet = this.sqlSampleDataService.ExcelUploadedDataService(UploadedExcelData);
            BuildResponse(ResultSet, HttpStatusCode.OK);
            return apiResponse;
        }
    }
}
