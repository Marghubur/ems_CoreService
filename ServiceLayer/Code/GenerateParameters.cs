using ModalLayer.Modal;
using ServiceLayer.Interface;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceLayer.Code
{
    public class GenerateParameters : IGenerateParameters<GenerateParameters>
    {
        private readonly SqlMappedTypes sqlMappedTypes;
        public GenerateParameters(SqlMappedTypes sqlMappedTypes) => this.sqlMappedTypes = sqlMappedTypes;
        public Dictionary<string, string> GenerateParameter(List<DynamicTableSchema> dynamicTableSchema, out StringBuilder ProcedureParameters)
        {
            string DbType = default(string);
            StringBuilder ParameterList = null;
            Dictionary<string, string> ProcedureParam = null;
            string GivenDataType = "";
            string Seperator = "";
            string KeyType = "";
            int index = 0;
            int TotalParams = dynamicTableSchema.Count();
            if (dynamicTableSchema.Count() > 0)
            {
                ProcedureParam = new Dictionary<string, string>();
                ParameterList = new StringBuilder();
                foreach (DynamicTableSchema schema in dynamicTableSchema)
                {
                    if (schema.IsPrimay)
                        KeyType = "primary";
                    else if (schema.IsUnique)
                        KeyType = "unique";
                    else
                        KeyType = "";
                    if (!string.IsNullOrEmpty(schema.ColumnName) && !string.IsNullOrEmpty(schema.DataType))
                    {
                        GivenDataType = schema.DataType;
                        DbType = "";
                        if (index < (TotalParams - 1))
                            Seperator = ",";
                        else
                            Seperator = "";
                        if (this.sqlMappedTypes.IsLengthRequired(schema.DataType, out DbType))
                        {
                            ProcedureParam.Add("@" + schema.ColumnName.Replace(" ", "_"), KeyType);
                            ParameterList.Append("\n\t@" + schema.ColumnName.Replace(" ", "_") + " " + DbType + "(" + schema.Size + ")" + Seperator);
                        }
                        else
                        {
                            ProcedureParam.Add("@" + schema.ColumnName.Replace(" ", "_"), KeyType);
                            ParameterList.Append("\n\t@" + schema.ColumnName.Replace(" ", "_") + " " + DbType + Seperator);
                        }
                    }
                    index++;
                }
            }

            ProcedureParameters = ParameterList;
            return ProcedureParam;
        }
    }
}
