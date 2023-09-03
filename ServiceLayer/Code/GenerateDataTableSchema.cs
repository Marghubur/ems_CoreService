using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ServiceLayer.Code
{
    public class GenerateDataTableSchema : IGenerateDataTableSchema
    {
        private readonly SqlMappedTypes sqlMappedTypes;
        public GenerateDataTableSchema(SqlMappedTypes sqlMappedTypes)
        {
            this.sqlMappedTypes = sqlMappedTypes;
        }
        public DataTable GenerateEmptyDataTableSchema(List<DynamicTableSchema> dynamicTableSchema, string TableName)
        {
            string DbType = default(string);
            DataTable table = null;
            Type type = null;
            DataColumn column = null;
            string GivenDataType = "";
            if (dynamicTableSchema.Count() > 0)
            {
                table = new DataTable();
                table.TableName = TableName;
                foreach (DynamicTableSchema schema in dynamicTableSchema)
                {
                    if (!string.IsNullOrEmpty(schema.ColumnName) && !string.IsNullOrEmpty(schema.DataType))
                    {
                        type = null;
                        GivenDataType = schema.DataType;
                        type = this.sqlMappedTypes.GetSqlMappedType(schema.DataType);
                        if (type != null)
                        {
                            column = new DataColumn(schema.ColumnName.Replace(" ", "_"), type);
                            DbType = "";
                            if (this.sqlMappedTypes.IsLengthRequired(schema.DataType, out DbType))
                                column.MaxLength = GetSize(Convert.ToInt32(schema.Size));
                            if (!string.IsNullOrEmpty(schema.DefaultValue))
                                column.DefaultValue = schema.DefaultValue;
                            table.Columns.Add(column);
                        }
                    }
                }
            }
            return table;
        }

        private int GetSize(int Size)
        {
            int NewSize = Size;
            if (Size <= 0)
                NewSize = 50;
            return NewSize;
        }
    }
}
