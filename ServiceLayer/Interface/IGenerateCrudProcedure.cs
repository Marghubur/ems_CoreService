using ModalLayer.Modal;
using System.Collections.Generic;

namespace ServiceLayer.Interface
{
    public interface IGenerateCrudProcedure<T>
    {
        string GetMSSqlProcedure(ProcedureDetail procedureDetail, Dictionary<string, string> ColumnsDetail);
        string GetProcedureSchema(List<DynamicTableSchema> dynamicTableSchema, string TableName);
    }
}
