using ModalLayer.Modal;
using System.Collections.Generic;
using System.Data;

namespace ServiceLayer.Interface
{
    public interface IGenerateDataTableSchema
    {
        DataTable GenerateEmptyDataTableSchema(List<DynamicTableSchema> dynamicTableSchema, string TableName);
    }
}
