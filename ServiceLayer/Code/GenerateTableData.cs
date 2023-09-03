using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class GenerateTableData : IGenerateTableData<GenerateTableData>
    {
        private readonly SqlMappedTypes sqlMappedTypes;
        public GenerateTableData(SqlMappedTypes sqlMappedTypes) => this.sqlMappedTypes = sqlMappedTypes;

        public DataTable GenerateDynamicData(List<string> NewResultSetQueryData, int RowsToGenerate)
        {
            if (NewResultSetQueryData != null && NewResultSetQueryData.Count > 0)
            {
                DataTable table = new DataTable();
                table.TableName = "Random-Generated-Table";
                table.Columns.Add(new DataColumn { ColumnName = "RowIndex", DataType = typeof(int) });
                int Index = 0;
                int ColumnCount = NewResultSetQueryData.Count;
                while (Index < ColumnCount)
                {
                    var SplitedColunmDetail = NewResultSetQueryData[Index].Split(new[] { "$BH$" }, System.StringSplitOptions.None);
                    if (SplitedColunmDetail != null && SplitedColunmDetail.Length == 2)
                        table.Columns.Add(new DataColumn
                        {
                            ColumnName = SplitedColunmDetail[0],
                            DataType = this.sqlMappedTypes.GetSqlMappedType(SplitedColunmDetail[1])
                        });
                    Index++;
                }

                table = this.sqlMappedTypes.GenerateValuesAsync(table, RowsToGenerate);
                return table;
            }
            return default(DataTable);
        }

        public DataSet GenerateDataForTable(DataSet ResultingDataSet, DataSet dataSet, int RowsToGenerate)
        {
            int TableIndex = 0;
            DataTable table = default(DataTable);
            while (TableIndex < dataSet.Tables.Count)
            {
                table = dataSet.Tables[TableIndex];
                int index = 0;
                int innerIndex = 0;
                while (index < RowsToGenerate)
                {
                    DataRow row = table.NewRow();
                    foreach (DataColumn column in table.Columns)
                    {
                        innerIndex++;
                        if (column.Prefix != null && column.Prefix != "")
                        {
                            if (column.Prefix == "email")
                                row[column.ColumnName] = this.sqlMappedTypes.GenerateEmail(column.DefaultValue.ToString(), innerIndex);
                            else if (column.Prefix == "mobile")
                                row[column.ColumnName] = this.sqlMappedTypes.GenerateMobileNo(column.DefaultValue.ToString(), innerIndex);
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(column.DefaultValue.ToString()))
                                row[column.ColumnName] = this.sqlMappedTypes.GenerateValue(column, index, innerIndex);
                            else
                                row[column.ColumnName] = this.sqlMappedTypes.GetDefaultValue(column);
                        }
                    }
                    table.Rows.Add(row);
                    index++;
                }
                TableIndex++;
            }
            return ResultingDataSet;
        }
    }
}
