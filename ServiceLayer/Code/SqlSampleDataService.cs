using BottomhalfCore.DatabaseLayer.Common.Code;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace ServiceLayer.Code
{
    public class SqlSampleDataService : ISqlSampleDataService
    {
        private readonly IDb db;
        private readonly IGenerateSelectInsertQuery generateSelectInsertQuery;
        private readonly IGenerateSchema generateSchema;
        private readonly IGenerateDataTableSchema generateDataTableSchema;
        public SqlSampleDataService(IDb db,
            GenerateSchema generateSchema,
            GenerateDataTableSchema generateDataTableSchema,
            GenerateSelectInsertQuery generateSelectInsertQuery)
        {
            this.db = db;
            this.generateSchema = generateSchema;
            this.generateDataTableSchema = generateDataTableSchema;
            this.generateSelectInsertQuery = generateSelectInsertQuery;
        }

        public void TestDataTableJoning()
        {
            DataTable dt = new DataTable();
            dt.Clear();
            dt.Columns.Add(new DataColumn { ColumnName = "RowIndex", DataType = typeof(int) });
            dt.Columns.Add(new DataColumn { ColumnName = "Name", DataType = typeof(string) });
            dt.Columns.Add(new DataColumn { ColumnName = "Email", DataType = typeof(string) });
            dt.Columns.Add(new DataColumn { ColumnName = "Salary", DataType = typeof(double) });
            dt.Columns.Add(new DataColumn { ColumnName = "Mobile", DataType = typeof(string) });

            int Index = 0;
            DataRow row = null;
            while (Index < 50)
            {
                row = dt.NewRow();
                row["RowIndex"] = Index + 1;
                row["Name"] = "User";
                row["Email"] = $"user{100 + Index}@gmail.com";
                row["Salary"] = 78000.90 + Index * 3;
                row["Mobile"] = $"9908980{(100 + Index).ToString()}";
                dt.Rows.Add(row);
            }

            DataTable dt1 = new DataTable();
            dt1.Clear();
            dt1.Columns.Add(new DataColumn { ColumnName = "RowIndex", DataType = typeof(int) });
            dt1.Columns.Add(new DataColumn { ColumnName = "UserName", DataType = typeof(string) });
            dt1.Columns.Add(new DataColumn { ColumnName = "Password", DataType = typeof(string) });

            Index = 0;
            row = null;
            while (Index < 50)
            {
                row = dt1.NewRow();
                row["RowIndex"] = Index + 1;
                row["UserName"] = "User";
                row["Password"] = $"user{100 + Index}@gmail.com";
                dt1.Rows.Add(row);
            }

            DataTable dt2 = new DataTable();
            dt2.Clear();
            dt2.Columns.Add(new DataColumn { ColumnName = "RowIndex", DataType = typeof(int) });
            dt2.Columns.Add(new DataColumn { ColumnName = "ManagerName", DataType = typeof(string) });
            dt2.Columns.Add(new DataColumn { ColumnName = "ManagerEmail", DataType = typeof(string) });

            Index = 0;
            row = null;
            while (Index < 50)
            {
                row = dt2.NewRow();
                row["RowIndex"] = Index + 1;
                row["ManagerName"] = "User";
                row["ManagerEmail"] = $"user{100 + Index}@gmail.com";
                dt2.Rows.Add(row);
            }
        }

        public string GetMSSqlData(string SearchStr, string SortBy, int PageIndex, int PageSize)
        {
            string Result = string.Empty;
            try
            {
                if (string.IsNullOrEmpty(SearchStr))
                    SearchStr = "1=1";
                if (string.IsNullOrEmpty(SortBy))
                    SortBy = "StudentFirstName";
                if (PageIndex < 0)
                    PageIndex = 0;
                if (PageSize < 10)
                    PageSize = 10;
                DataSet ResultSet = null;
                DbParam[] param = null;
                //param = new DbParam[]
                //{
                //new DbParam(SearchStr, typeof(System.String), "@SearchStr"),
                //new DbParam(SortBy, typeof(System.String), "@SortBy"),
                //new DbParam(PageIndex, typeof(System.Int32), "@PageIndex"),
                //new DbParam(PageSize, typeof(System.Int32), "@PageSize")
                //};

                ResultSet = db.GetDataSet("sp_Students_FilterRecord");
                if (ResultSet != null && ResultSet.Tables.Count == 2)
                {
                    ResultSet.Tables[0].TableName = "Record";
                    ResultSet.Tables[1].TableName = "RecordCount";
                    Result = JsonConvert.SerializeObject(ResultSet);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return Result;
        }

        public (string, string) DownloadScriptService(DynamicTable dynamicTable)
        {
            string StringifySchemaData = GenerateTableService(dynamicTable);
            if (!string.IsNullOrEmpty(StringifySchemaData))
            {
                List<SchemaDataResult> schemaDataResultList = JsonConvert.DeserializeObject<List<SchemaDataResult>>(StringifySchemaData);
                IPublish<Publish> publish = new Publish();
                string SqlFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DynamicScript");
                if (!Directory.Exists(SqlFolderPath))
                    Directory.CreateDirectory(SqlFolderPath);
                return publish.PublishAsSqlFile(schemaDataResultList, SqlFolderPath);
            }
            return ("", "");
        }

        public string GenerateTableService(DynamicTable dynamicTable)
        {
            DynamicSchemaDataResult dynamicSchemaDataResult = new DynamicSchemaDataResult();
            DataSet SampleDataSet = default(DataSet);
            DataTable table = default(DataTable);
            string Result = string.Empty;
            SchemaDataResult schemaDataResult = null;
            List<SchemaDataResult> schemaDataResultList = null;
            string TableName = string.Empty;
            bool DataRequiredFlag = false;
            if (dynamicTable.dynamicTableDetail != null && dynamicTable.dynamicTableDetail.Count() > 0)
            {
                int Index = 0;
                SampleDataSet = new DataSet();
                schemaDataResultList = new List<SchemaDataResult>();
                while (Index < dynamicTable.dynamicTableDetail.Count())
                {
                    table = null;
                    schemaDataResult = null;
                    TableName = dynamicTable.dynamicTableDetail[Index].TableName;
                    table = this.generateDataTableSchema.GenerateEmptyDataTableSchema(dynamicTable.dynamicTableDetail[Index].Data, TableName);
                    switch (dynamicTable.GenerationType)
                    {
                        case "schema":
                            schemaDataResult = GetTableScheme(dynamicTable.dynamicTableDetail[Index], table.TableName);
                            DataRequiredFlag = false;
                            break;
                        case "completescript":
                            schemaDataResult = GetTableSchemeWithSampleData(dynamicTable.dynamicTableDetail[Index], table.TableName, dynamicTable.Rows);
                            DataRequiredFlag = true;
                            break;
                        case "jsondata":
                            schemaDataResult = null;
                            break;
                        default:
                            break;
                    }
                    SampleDataSet.Tables.Add(table);
                    schemaDataResult.TableName = TableName;
                    schemaDataResultList.Add(schemaDataResult);
                    Index++;
                }
            }

            dynamicSchemaDataResult.schemaDataResults = schemaDataResultList;
            if (schemaDataResultList.Count > 0)
            {
                string FinalSelectQuery = default(string);
                List<string> TableNames = new List<string>();
                foreach (var SingleSchema in schemaDataResultList)
                {
                    if (!string.IsNullOrEmpty(SingleSchema.SelectBuildQuery))
                        FinalSelectQuery = FinalSelectQuery + SingleSchema.SelectBuildQuery + "\n";
                    TableNames.Add(SingleSchema.TableName);
                }

                schemaDataResultList = ReOrderList(schemaDataResultList, dynamicTable);
                DataSet ds = null;
                if (DataRequiredFlag)
                {
                    ds = GetQueryData(FinalSelectQuery, SampleDataSet, dynamicTable, TableNames);
                    foreach (var singleSchema in schemaDataResultList)
                    {
                        if (ds.Tables[singleSchema.TableName] != null)
                        {
                            singleSchema.Table = ds.Tables[singleSchema.TableName];
                            singleSchema.InsertQuery = this.generateSelectInsertQuery.CreateInsertQuery(ds.Tables[singleSchema.TableName], singleSchema.TableName);
                        }
                    }
                }
                IPublish<Publish> publish = new Publish();
                string SqlFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DynamicScript");
                if (!Directory.Exists(SqlFolderPath))
                    Directory.CreateDirectory(SqlFolderPath);
                (string FolderPath, string FileName) = publish.PublishAsSqlFile(schemaDataResultList, SqlFolderPath);
                dynamicSchemaDataResult.FileName = FileName;
            }

            return JsonConvert.SerializeObject(dynamicSchemaDataResult);
        }

        private List<SchemaDataResult> ReOrderList(List<SchemaDataResult> schemaDataResultList, DynamicTable dynamicTable)
        {
            List<SchemaDataResult> orderedSchemaDataResultList = new List<SchemaDataResult>();
            foreach (var TableDetial in dynamicTable.dynamicTableDetail)
            {
                if (schemaDataResultList.Where(x => x.TableName == TableDetial.TableName).FirstOrDefault() != null)
                    orderedSchemaDataResultList.Add(schemaDataResultList.Where(x => x.TableName == TableDetial.TableName).FirstOrDefault());
            }

            return orderedSchemaDataResultList;
        }

        private void GetReferencedTableDetail(string TableName, DynamicTable dynamicTable, Dictionary<string, TableRelation> referencedTableDetail)
        {
            List<TableRelation> ReferenceTableDetail = null;
            if (dynamicTable.dynamicTableDetail.Where(x => x.TableName == TableName).FirstOrDefault() != null)
            {
                ReferenceTableDetail = dynamicTable.dynamicTableDetail.Where(x => x.TableName == TableName).FirstOrDefault().Relation;
                if (ReferenceTableDetail != null)
                {
                    foreach (var tableDetail in ReferenceTableDetail)
                    {
                        GetReferencedTableDetail(tableDetail.ReferenceTableName, dynamicTable, referencedTableDetail);
                        if (referencedTableDetail.Where(x => x.Key == tableDetail.TableName).FirstOrDefault().Value == null)
                            referencedTableDetail.Add(tableDetail.TableName, tableDetail);
                    }
                }
            }
        }

        private DataSet GetQueryData(string SelectQuery, DataSet SampleDataSet, DynamicTable dynamicTable, List<string> TableNames)
        {
            try
            {
                List<string> MissingTable = new List<string>();
                DataSet ResultSet = null;
                if (SelectQuery != null)
                {
                    ResultSet = db.GetDataSet("sp_DynamicSampleDataGenerator");
                }
                else
                {
                    ResultSet = new DataSet();
                }

                IRandomDataGenerator<RandomDataGenerator> randomDataGenerator = new RandomDataGenerator();
                int Index = 0;
                if (ResultSet.Tables.Count > 0)
                {
                    foreach (string SchemaTableName in TableNames)
                    {
                        ResultSet.Tables[Index].TableName = SchemaTableName;
                        Index++;
                    }
                }

                if (ResultSet.Tables.Count != SampleDataSet.Tables.Count)
                {
                    ResultSet.Merge(SampleDataSet);
                    foreach (DataTable table in ResultSet.Tables)
                        if (TableNames.Where(x => x == table.TableName).FirstOrDefault() == null)
                        {
                            MissingTable.Add(table.TableName);
                            TableNames.Add(table.TableName);
                        }
                }

                Index = 0;
                string SchemaTable = default(string);
                while(Index < TableNames.Count())
                {
                    SchemaTable = TableNames.ElementAt(Index);
                    Dictionary<string, TableRelation> referencedTableDetail = new Dictionary<string, TableRelation>();
                    GetReferencedTableDetail(SchemaTable, dynamicTable, referencedTableDetail);
                    foreach (DataColumn column in ResultSet.Tables[SchemaTable].Columns)
                    {
                        int LoopIndex = 0;
                        int TotalRows = dynamicTable.Rows;
                        while (LoopIndex < TotalRows)
                        {
                            if (ResultSet.Tables[SchemaTable].Rows.Count < TotalRows)
                            {
                                dynamic Value = TypeService.GetDefaultValue(column.DataType, LoopIndex);
                                ResultSet.Tables[SchemaTable].Rows.Add(ResultSet.Tables[SchemaTable].NewRow());
                                ResultSet.Tables[SchemaTable].Rows[LoopIndex][column.ColumnName] = Value;
                            }
                            else
                            {
                                if (ResultSet.Tables[SchemaTable].Rows[LoopIndex][column.ColumnName] == DBNull.Value)
                                {
                                    dynamic Value = TypeService.GetDefaultValue(column.DataType, LoopIndex);
                                    ResultSet.Tables[SchemaTable].Rows[LoopIndex][column.ColumnName] = Value;

                                    if (referencedTableDetail.Where(x => x.Value.ColumnName == column.ColumnName && x.Value.TableName == SchemaTable).FirstOrDefault().Value != null)
                                    {
                                        foreach (var refences in referencedTableDetail)
                                        {
                                            if (ResultSet.Tables[refences.Value.ReferenceTableName] != null)
                                            {
                                                if (MissingTable.Where(x => x == refences.Value.ReferenceTableName).FirstOrDefault() != null)
                                                    ResultSet.Tables[refences.Value.ReferenceTableName].Rows.Add(ResultSet.Tables[refences.Value.ReferenceTableName].NewRow());
                                                ResultSet.Tables[refences.Value.ReferenceTableName].Rows[LoopIndex][refences.Value.ReferenceColumnName] = Value;
                                            }
                                        }
                                    }
                                }
                            }
                            LoopIndex++;
                        }
                    }
                    Index++;
                }
                return ResultSet;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private SchemaDataResult GetTableScheme(DynamicTableDetail dynamicTableDetail, string TableName)
        {
            SchemaDataResult schemaDataResult = new SchemaDataResult();
            IGenerateSchema generateSchema = new GenerateSchema(null);
            IGenerateCrudProcedure<GenerateCrudProcedure> generateCrudProcedure = new GenerateCrudProcedure(null);
            var Result = generateSchema.GenerateSchemaString(dynamicTableDetail);
            schemaDataResult.ProcedureSchema = generateCrudProcedure.GetProcedureSchema(dynamicTableDetail.Data, TableName);
            schemaDataResult.Schema = Result;
            return schemaDataResult;
        }

        private SchemaDataResult GetTableSchemeWithSampleData(DynamicTableDetail dynamicTableDetail, string TableName, int Rows)
        {
            SchemaDataResult schemaDataResult = GetTableScheme(dynamicTableDetail, TableName);
            schemaDataResult.SelectBuildQuery = this.generateSelectInsertQuery.BuildSelectQueryColumns(dynamicTableDetail.Data, Rows);
            return schemaDataResult;
        }

        public string GetMYSqlData(string SearchStr, string SortBy, int PageIndex, int PageSize)
        {
            string Result = string.Empty;
            try
            {
                DataSet ResultSet = null;
                ResultSet = db.GetDataSet("sp_Students_FilterRecord");
                if (ResultSet != null && ResultSet.Tables.Count == 2)
                {
                    ResultSet.Tables[0].TableName = "Record";
                    ResultSet.Tables[1].TableName = "RecordCount";
                    Result = JsonConvert.SerializeObject(ResultSet);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return Result;
        }

        private dynamic GetDynamicallyCreatedClass()
        {
            dynamic RuntimeObject = new ExpandoObject();
            RuntimeObject["Name"] = "";
            RuntimeObject["Number"] = 10;
            return RuntimeObject;
        }

        public string ExcelUploadedDataService(List<UploadedExcelRow> UploadedExcelData)
        {
            string ResultSet = null;
            DynamicTable dynamicTable = new DynamicTable();
            if (UploadedExcelData != null)
            {
                dynamicTable.dynamicTableDetail = new List<DynamicTableDetail>();
                DynamicTableSchema dynamicTableSchema = null;
                RowDetail rowDetail = null;
                foreach (var ExcelData in UploadedExcelData)
                {
                    DynamicTableDetail dynamicTableDetail = new DynamicTableDetail();
                    dynamicTableDetail.Data = new List<DynamicTableSchema>();
                    rowDetail = ExcelData.Value;
                    if (rowDetail != null)
                    {
                        foreach (var Item in rowDetail.Keys)
                        {
                            dynamicTableSchema = new DynamicTableSchema();
                            dynamicTableSchema.ColumnName = Item.ColumnName;
                            dynamicTableSchema.DataType = Item.ColumnType;
                            if (Item.ColumnType == "string")
                                dynamicTableSchema.Size = "50";
                            else
                                dynamicTableSchema.DefaultSize = true;
                            dynamicTableDetail.Data.Add(dynamicTableSchema);
                        }
                        dynamicTableDetail.TableName = ExcelData.Key;
                    }
                    dynamicTable.dynamicTableDetail.Add(dynamicTableDetail);
                    rowDetail = null;
                }

                if (dynamicTable.dynamicTableDetail.Count() > 0)
                {
                    dynamicTable.Rows = 1000;
                    dynamicTable.GenerationType = "completescript";
                    ResultSet = GenerateTableService(dynamicTable);
                }
            }
            return ResultSet;
        }
    }
}
