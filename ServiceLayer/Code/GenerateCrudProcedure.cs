using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceLayer.Code
{
    public class GenerateCrudProcedure : IGenerateCrudProcedure<GenerateCrudProcedure>
    {
        private readonly IGenerateParameters<GenerateParameters> generateParameters;
        public GenerateCrudProcedure(GenerateParameters generateParameters) => this.generateParameters = generateParameters;
        public string TableName = "";
        public string GetProcedureSchema(List<DynamicTableSchema> dynamicTableSchema, string TableName)
        {
            string ProcedureSchema = string.Empty;
            if (!string.IsNullOrEmpty(TableName))
            {

                this.TableName = TableName;
                StringBuilder ProcedureParamters = null;
                Dictionary<string, string> ColumnsDetail = this.generateParameters.GenerateParameter(dynamicTableSchema, out ProcedureParamters);
                ProcedureDetail procedureDetail = new ProcedureDetail
                {
                    ProcedureName = $"sp_{TableName}_InsertUpdate",
                    IsTransactionRequired = true,
                    IsTryCatchRequired = true,
                    Parameters = ProcedureParamters
                };
                ProcedureSchema = GetMSSqlProcedure(procedureDetail, ColumnsDetail);
            }
            else
            {
                throw new ApplicationException("Table name required");
            }

            return ProcedureSchema;
        }
        public string GetMSSqlProcedure(ProcedureDetail procedureDetail, Dictionary<string, string> ColumnsDetail)
        {
            StringBuilder parameterBuilder = new StringBuilder();
            string Query = PrepareInsertUpdateQuery(ColumnsDetail);
            string ProcedureTempalte = GetProcedure(procedureDetail, Query, "");
            return ProcedureTempalte;
        }

        private string PrepareInsertUpdateQuery(Dictionary<string, string> ColumnsDetail)
        {
            string InserQuery = $@"
            IF NOT EXISTS(SELECT 1 FROM {TableName} [EXISTING-CONDITION])
            BEGIN
                INSERT INTO {TableName} VALUES([PARAMETERS])
            END";
            string UpdateQuery = $@"
            ELSE
            BEGIN
                UPDATE {TableName} SET [UPDATEQUERY]
                [EXISTING-CONDITION]
            END";
            string Query = string.Empty;
            string InsertParam = string.Empty;
            string UpdateParam = string.Empty;
            string ColumnName = string.Empty;
            string LineBreaker = "";
            string ConstraintField = "";
            int index = 0;

            Nullable<KeyValuePair<string, string>> PrimayKeyField = null;
            PrimayKeyField = ColumnsDetail.Where(x => x.Value == "primary").FirstOrDefault();
            if (PrimayKeyField.Value.Value != null && PrimayKeyField.Value.Key != null)
            {
                ConstraintField = PrimayKeyField.Value.Key;
            }
            else
            {
                PrimayKeyField = ColumnsDetail.Where(x => x.Value == "unique").FirstOrDefault();
                if (PrimayKeyField.Value.Value != null && PrimayKeyField.Value.Key != null)
                {
                    ConstraintField = PrimayKeyField.Value.Key;
                }
            }

            if (ConstraintField != null && ConstraintField != "")
                ConstraintField = "WHERE " + ConstraintField.Replace("@", "") + " = " + ConstraintField;

            foreach (var Column in ColumnsDetail)
            {
                LineBreaker = "";
                ColumnName = Column.Key.ToString();
                if (index == 0)
                {
                    if (ConstraintField == null || ConstraintField == "")
                        ConstraintField = ColumnName.Replace("@", "") + " = " + ColumnName;
                    UpdateParam += "[" + ColumnName.Replace("@", "") + "] = " + ColumnName;
                    InsertParam += ColumnName;
                }
                else
                {
                    if (index % 5 == 0)
                        LineBreaker = "\n\t\t\t";
                    UpdateParam += ", " + LineBreaker + "[" + ColumnName.Replace("@", "") + "] = " + ColumnName;
                    InsertParam += ", " + LineBreaker + ColumnName;
                }
                index++;
            }

            InserQuery = InserQuery.Replace("[PARAMETERS]", InsertParam);
            InserQuery = InserQuery.Replace("[EXISTING-CONDITION]", ConstraintField);
            UpdateQuery = UpdateQuery.Replace("[UPDATEQUERY]", UpdateParam);
            UpdateQuery = UpdateQuery.Replace("[EXISTING-CONDITION]", ConstraintField);
            return "\t" + InserQuery + "\n\t" + UpdateQuery;
        }

        private string GetProcedure(ProcedureDetail procedureDetail, string Query, string TestQuery)
        {
            string Procedure = $@"
CREATE PROCEDURE [SP_{this.TableName}_INSUPD]  
    [PARAMETERS]  
    [TESTQUERY]
AS    
BEGIN                                       
    [BODY]            
END";

            string ProcedureWithTry = $@"
CREATE PROCEDURE [SP_{this.TableName}_INSUPD]  
    [PARAMETERS]  
    [TESTQUERY]
AS    
BEGIN    
    BEGIN TRY                                    
        [BODY]
    END TRY
    BEGIN CATCH
        Declare @ErrNumber varchar(50)              
        Declare @ErrState varchar(50)              
        Declare @Severity varchar(50)              
        Declare @ProcedureName varchar(100)              
        Declare @ErrLineNo varchar(50)              
        Declare @Message varchar(max)              
        Set @ErrNumber = ERROR_NUMBER()              
        Set @ErrState = ERROR_STATE()              
        Set @Severity = ERROR_SEVERITY()              
        Set @ProcedureName = ERROR_PROCEDURE()              
        Set @ErrLineNo = ERROR_LINE()              
        Set @Message = ERROR_MESSAGE()              
        -- Exec [EXCEPTION-LOGGER-PROCEDURE] @ErrNumber, @ErrState, @Severity, @ProcedureName, @ErrLineNo, @Message        
    END CATCH              
END";

            string ProcedureTryAndTran = $@"
CREATE PROCEDURE [SP_{this.TableName}_INSUPD]  
    [PARAMETERS]  
    [TESTQUERY]
AS    
BEGIN    
    BEGIN TRY
        BEGIN TRAN
            [BODY]
        COMMIT
    END TRY
    BEGIN CATCH
        ROLLBACK
        Declare @ErrNumber varchar(50)              
        Declare @ErrState varchar(50)              
        Declare @Severity varchar(50)              
        Declare @ProcedureName varchar(100)              
        Declare @ErrLineNo varchar(50)              
        Declare @Message varchar(max)              
        Set @ErrNumber = ERROR_NUMBER()              
        Set @ErrState = ERROR_STATE()              
        Set @Severity = ERROR_SEVERITY()              
        Set @ProcedureName = ERROR_PROCEDURE()              
        Set @ErrLineNo = ERROR_LINE()              
        Set @Message = ERROR_MESSAGE()              
        -- Exec [EXCEPTION-LOGGER-PROCEDURE] @ErrNumber, @ErrState, @Severity, @ProcedureName, @ErrLineNo, @Message        
    END CATCH              
END";

            string ActualProcedure = string.Empty;

            if (procedureDetail.IsTransactionRequired && procedureDetail.IsTryCatchRequired)
            {
                ActualProcedure = ProcedureTryAndTran.Replace("[PARAMETERS]", "\n" + procedureDetail.Parameters.ToString())
                                    .Replace("[TESTQUERY]", TestQuery)
                                    .Replace("[BODY]", Query);
            }
            else if (procedureDetail.IsTransactionRequired)
            {
                ActualProcedure = ProcedureWithTry.Replace("[PARAMETERS]", "\n" + procedureDetail.Parameters.ToString())
                                    .Replace("[TESTQUERY]", TestQuery);
            }
            else
            {
                ActualProcedure = Procedure.Replace("[PARAMETERS]", "\n" + procedureDetail.Parameters.ToString())
                                    .Replace("[TESTQUERY]", TestQuery)
                                    .Replace("[BODY]", Query);
            }

            return ActualProcedure;
        }
    }
}