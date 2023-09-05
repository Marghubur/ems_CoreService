using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;

namespace ModalLayer.Modal
{
    public class DynamicTableSchema
    {
        public string ColumnName { set; get; }
        public string MappedColumn { set; get; }
        public string MappedTable { set; get; }
        public string DataType { set; get; }
        public string Size { set; get; }
        public bool DefaultSize { set; get; }
        public string DefaultValue { set; get; }
        public bool IsPrimay { set; get; }
        public bool IsNullable { set; get; }
        public bool IsUnique { set; get; }
    }

    public class DynamicTable
    {
        public List<DynamicTableDetail> dynamicTableDetail { set; get; }
        public string GenerationType { set; get; }
        public int Rows { set; get; }
    }

    public class DynamicTableDetail
    {
        public List<DynamicTableSchema> Data { set; get; }
        public List<TableRelation> Relation { set; get; }
        public string TableName { set; get; }
    }

    public class TableRelation
    {
        public string ColumnName { set; get; }
        public string ReferenceColumnName { set; get; }
        public string ReferenceTableName { set; get; }
        public string TableName { set; get; }
    }

    public class SchemaDataResult
    {
        public string SelectBuildQuery { set; get; }
        public string InsertQuery { set; get; }
        public DataTable Table { set; get; }
        public string Schema { set; get; }
        public string ProcedureSchema { set; get; }
        public string TableName { set; get; }
    }

    public class DynamicSchemaDataResult
    {
        public List<SchemaDataResult> schemaDataResults { set; get; }
        public string FileName { set; get; }
    }
}