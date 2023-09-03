using ModalLayer.Modal;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ServiceLayer.Interface
{
    public interface IGenerateSelectInsertQuery
    {
        string BuildSelectQueryColumns(List<DynamicTableSchema> dynamicTableSchemas, int RowCount);
        string CreateInsertQuery(DataSet ds, string TableName);
        string CreateInsertQuery(DataTable table, string TableName);
    }
}
