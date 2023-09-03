using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace ServiceLayer.Code
{
    public class GenerateSchema : IGenerateSchema
    {
        private readonly SqlMappedTypes sqlMappedTypes;
        public GenerateSchema(SqlMappedTypes sqlMappedTypes)
        {
            this.sqlMappedTypes = sqlMappedTypes;
        }
        public string GenerateSchemaString(DynamicTableDetail dynamicTableDetail)
        {
            List<DynamicTableSchema> dynamicTableSchema = dynamicTableDetail.Data;
            string TableName = dynamicTableDetail.TableName;
            List<TableRelation> Relation = dynamicTableDetail.Relation;
            string StringifyResult = string.Empty;
            DataTable table = null;
            Type type = null;
            DataColumn column = null;
            string ColumnFormat = "\t[{0}] {1}({2}) {3}";
            string ConstantColumnFormat = "\t[{0}] {1} {2}";
            string Constraint = "";
            StringBuilder stringBuilder = null;
            string RelationSeparator = "";
            if (Relation != null)
                RelationSeparator = Relation.Count() > 0 ? "," : "";
            if (dynamicTableSchema.Count() > 0)
            {
                int index = 0;
                table = new DataTable();
                stringBuilder = new StringBuilder();
                stringBuilder.Append(
"\rCREATE TABLE [" + TableName + "]( \n");
                foreach (DynamicTableSchema schema in dynamicTableSchema)
                {
                    if (index > 0)
                        stringBuilder.Append(",\n");
                    if (!string.IsNullOrEmpty(schema.ColumnName) && !string.IsNullOrEmpty(schema.DataType))
                    {
                        type = null;
                        type = this.sqlMappedTypes.GetSqlMappedType(schema.DataType);
                        if (type != null)
                        {
                            column = new DataColumn(schema.ColumnName.Replace(" ", "_"), type);
                            if (schema.IsPrimay)
                                Constraint = "PRIMARY KEY";
                            else
                            {
                                if (schema.IsUnique)
                                {
                                    if (schema.IsUnique && !schema.IsNullable)
                                        Constraint = "UNIQUE NOT NULL";
                                    else if (schema.IsUnique)
                                        Constraint = "UNIQUE";
                                }
                                else
                                {
                                    if (schema.IsNullable)
                                        Constraint = "NULL";
                                    else
                                        Constraint = "NOT NULL";
                                }
                            }

                            string DbType = "";
                            if (this.sqlMappedTypes.IsLengthRequired(schema.DataType, out DbType))
                            {
                                stringBuilder.Append(
                                    string.Format(ColumnFormat,
                                        schema.ColumnName,
                                        DbType,
                                        schema.Size.ToString(),
                                        Constraint
                                    )
                                );
                            }
                            else
                            {
                                stringBuilder.Append(
                                    string.Format(ConstantColumnFormat,
                                        schema.ColumnName,
                                        DbType,
                                        Constraint
                                    )
                                );
                            }


                        }
                    }
                    index++;
                }
                if (Relation != null && Relation.Count > 0)
                    stringBuilder.Append($"{RelationSeparator}\n{GenerateRelationQuery(Relation)})");
                else
                    stringBuilder.Append($"{RelationSeparator}\n)");
            }

            StringifyResult = stringBuilder.ToString();
            return StringifyResult;
        }

        private string GenerateRelationQuery(List<TableRelation> Relation)
        {
            StringBuilder mappingTemplate = new StringBuilder();
            if (Relation != null && Relation.Count() > 0)
            {
                string MappingQuery = string.Empty;
                string Comma = "";
                int Index = 0;
                int TotalRelationMapped = Relation.Count();
                foreach (TableRelation tableRelation in Relation)
                {
                    if (Index < (TotalRelationMapped - 1)) Comma = ","; else Comma = "";
                    mappingTemplate.AppendLine($"\tCONSTRAINT FK_{tableRelation.TableName}_{tableRelation.ColumnName}_{tableRelation.ReferenceTableName}_{tableRelation.ReferenceColumnName} FOREIGN KEY ({tableRelation.ColumnName}) REFERENCES {tableRelation.ReferenceTableName}({tableRelation.ReferenceColumnName}){Comma}");
                    Index++;
                }
            }
            return mappingTemplate.ToString();
        }
    }
}