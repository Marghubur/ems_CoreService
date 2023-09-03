using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class RandomDataGenerator : IRandomDataGenerator<RandomDataGenerator>
    {
        public DataTable GenerateRandomVarcharData(Boolean IsVarchar, Boolean IsFloatingValue, Boolean IsNumericValue)
        {
            return null;
        }

        public void FillDataTable(DataSet ds, int TableIndex)
        {
            foreach (DataTable table in ds.Tables)
            {
                if (table != null && table.Rows.Count > 0)
                {
                    Type type = null;
                    int Index = 1;
                    foreach (DataRow row in table.Rows)
                    {
                        foreach (DataColumn column in table.Columns)
                        {
                            type = column.DataType;
                            if (row[column.ColumnName] == DBNull.Value)
                            {
                                dynamic IsNumeric = NumbericValue(type, Index);
                                if (IsNumeric != null)
                                    row[column.ColumnName] = IsNumeric;
                                else if (type == typeof(string))
                                    row[column.ColumnName] = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                                else if (type == typeof(DateTime))
                                    row[column.ColumnName] = DateTime.Now.AddDays(Index + 7).AddHours(Index * 17);
                            }
                        }
                        Index++;
                    }
                }
            }
        }

        private dynamic NumbericValue(Type type, int Index)
        {
            if (type == typeof(int))
                return Index;
            else if (type == typeof(float))
                return Convert.ToDouble(Index);
            else if (type == typeof(decimal))
                return Convert.ToDecimal(Index);
            else if (type == typeof(double))
                return Convert.ToDouble(Index);
            return null;
        }

        private async Task<List<string>> GenerateVarcharValuesAsync(int GenerateCount)
        {
            List<string> listData = new List<string>();
            await Task.Run(() =>
            {
                int index = 0;
                while (index < GenerateCount)
                {
                    listData.Add(Guid.NewGuid().ToString().Substring(0, 8).ToUpper());
                    index++;
                }
            });
            return listData;
        }
    }
}
