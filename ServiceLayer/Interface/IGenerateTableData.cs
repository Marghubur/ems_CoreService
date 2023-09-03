using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IGenerateTableData<T>
    {
        DataTable GenerateDynamicData(List<string> NewResultSetQueryData, int RowsToGenerate);
        DataSet GenerateDataForTable(DataSet ResultingDataSet, DataSet dataSet, int RowsToGenerate);
    }
}
