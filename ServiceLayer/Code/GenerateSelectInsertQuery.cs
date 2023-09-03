using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace ServiceLayer.Code
{
    public class GenerateSelectInsertQuery : IGenerateSelectInsertQuery
    {
        private readonly MasterTables masterData;
        public GenerateSelectInsertQuery(SqlMappedTypes sqlMappedTypes)
        {
        }

        public string BuildSelectQueryColumns(List<DynamicTableSchema> dynamicTableSchemas, int RowCount)
        {
            StringBuilder columns = default(StringBuilder);
            StringBuilder tableJoin = default(StringBuilder);
            Dictionary<string, List<string>> columnCollection = null;
            string TableAliaseName = default(string);
            StringBuilder OrderByClouse = new StringBuilder();
            string FinalQuery = default(string);
            int Index = 1;
            Boolean Flag = true;
            Nullable<Guid> guid = null;
            if (dynamicTableSchemas != null)
            {
                columns = new StringBuilder();
                tableJoin = new StringBuilder();
                columnCollection = new Dictionary<string, List<string>>();
                foreach (DynamicTableSchema schema in dynamicTableSchemas)
                {
                    guid = Guid.NewGuid();
                    if (!string.IsNullOrEmpty(schema.MappedTable))
                    {
                        Flag = true;
                        if (columns.Length == 0)
                            columns.Append($"\n\t[TABLE-ALIASE].[{schema.MappedColumn}] [{schema.ColumnName}]");
                        else
                            columns.Append($"\n\t, [TABLE-ALIASE].[{schema.MappedColumn}] as [{schema.ColumnName}]");
                    }
                    else
                    {
                        Flag = false;
                        if (columns.Length == 0)
                            columns.Append($"\n\tCONVERT({TypeService.GetSqlMappedObject(schema.DataType).DbType}, NULL) as [{schema.ColumnName}]");
                        else
                            columns.Append($"\n\t, CONVERT({TypeService.GetSqlMappedObject(schema.DataType).DbType}, NULL) as [{schema.ColumnName}]");
                    }

                    if (!tableJoin.ToString().Contains($"[{schema.MappedTable}]") && Flag)
                    {
                        TableAliaseName = $"TB{Index}";
                        if (tableJoin.Length == 0)
                            tableJoin.Append($"[{schema.MappedTable}] as TB{Index}");
                        else
                            tableJoin.Append($" \nleft Join [{schema.MappedTable}] TB{Index} on TB{Index}.[RowIndex] = TB{(Index - 1)}.[RowIndex]");

                        OrderByClouse.Append(masterData.masterQuaryClouse.Where(x => x.TableName == schema.MappedTable)
                            .FirstOrDefault()
                            .OrderByClouse.Replace("[TBL-ALIASE]", TableAliaseName)
                        );
                    }
                    columns = columns.Replace("[TABLE-ALIASE]", TableAliaseName);
                    Index++;
                }
            }
            if (string.IsNullOrEmpty(tableJoin.ToString()))
                FinalQuery = $"SELECT TOP {RowCount} {columns} \nfrom sys.columns as DUMMYTB0 Cross Join sys.columns As DUMMYTB1";
            else
                FinalQuery = $"SELECT TOP {RowCount} {columns} \nfrom {tableJoin} \n ORDER BY {OrderByClouse.ToString()}";
            return FinalQuery;
        }

        private void ManageQuery(string TableName, string ColumnName, Dictionary<string, List<string>> columnCollection)
        {
            List<string> requestedColumn = null;
            if (columnCollection.Where(x => x.Key == TableName).FirstOrDefault().Value == null)
            {
                requestedColumn = new List<string>();
                requestedColumn.Add(ColumnName);
                columnCollection.Add(TableName, requestedColumn);
            }
            else
            {
                requestedColumn = columnCollection.Where(x => x.Key == TableName).FirstOrDefault().Value;
                if (!requestedColumn.ToString().Contains(ColumnName))
                {
                    requestedColumn.Add(ColumnName);
                    columnCollection[TableName] = requestedColumn;
                }
            }
        }

        private string AddColumnName(List<string> columnBuilderCollection, Func<List<string>, string> ColumnAddfunction)
        {
            return ColumnAddfunction(columnBuilderCollection);
        }

        private string SingleQuoteRequired(Type DataType)
        {
            string SingleQuote = "";
            if (DataType == typeof(string) || DataType == typeof(Guid) || DataType == typeof(DateTime) || DataType == typeof(char))
                SingleQuote = "'";
            return SingleQuote;
        }

        public string CreateInsertQuery(DataTable table, string TableName)
        {
            string ColumnSet = default(string);
            string Values = default(string);
            string InserQuery = default(string);
            string SingleQuote = "";
            int rowIndex = 0;
            if (table.Rows.Count > 0)
            {
                var Columns = table.Columns;
                foreach (DataRow row in table.Rows)
                {
                    ColumnSet = "";
                    Values = "";
                    if (Columns.Count == 1)
                    {
                        SingleQuote = SingleQuoteRequired(row[0].GetType());
                        ColumnSet = $"{Columns[0]}";
                        Values = $"{SingleQuote}{row[0]}{SingleQuote}";
                    }
                    else
                    {
                        int columnCount = 0;
                        while (columnCount < Columns.Count)
                        {
                            SingleQuote = SingleQuoteRequired(row[columnCount].GetType());
                            if (columnCount == (Columns.Count - 1))
                                Values += $"{SingleQuote}{row[columnCount]}{SingleQuote}";
                            else
                                Values += $"{SingleQuote}{row[columnCount]}{SingleQuote}, ";


                            if (columnCount == (Columns.Count - 1))
                                ColumnSet += $"{Columns[columnCount]}";
                            else
                                ColumnSet += $"{Columns[columnCount]}, ";
                            columnCount++;
                        }
                    }

                    InserQuery += $"INSERT INTO {TableName}({ColumnSet}) VALUES ({Values})\n";
                    rowIndex++;
                }
            }

            return InserQuery;
        }
        public string CreateInsertQuery(DataSet ds, string TableName)
        {
            string ColumnSet = default(string);
            string Values = default(string);
            string InserQuery = default(string);
            int index = 0;
            while (index < ds.Tables.Count)
            {
                int rowIndex = 0;
                var Columns = ds.Tables[index].Columns;
                foreach (DataRow row in ds.Tables[index].Rows)
                {
                    ColumnSet = "";
                    Values = "";
                    if (Columns.Count == 1)
                    {
                        ColumnSet = $"{Columns[0]}";
                        Values = $"{row[0]}";
                    }
                    else
                    {
                        int columnCount = 0;
                        while (columnCount < Columns.Count)
                        {
                            if (columnCount == (Columns.Count - 1))
                                Values += $"{row[columnCount]}";
                            else
                                Values += $"{row[columnCount]}, ";


                            if (columnCount == (Columns.Count - 1))
                                ColumnSet += $"{Columns[columnCount]}";
                            else
                                ColumnSet += $"{Columns[columnCount]}, ";
                            columnCount++;
                        }
                    }

                    InserQuery += $"INSERT INTO {TableName}({ColumnSet}) VALUES ({Values})\n";
                    rowIndex++;
                }
                index++;
            }

            return InserQuery;
        }

    }
}