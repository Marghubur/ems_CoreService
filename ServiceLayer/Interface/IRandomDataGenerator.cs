using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IRandomDataGenerator<T>
    {
        DataTable GenerateRandomVarcharData(Boolean IsVarchar, Boolean IsFloatingValue, Boolean IsNumericValue);
        void FillDataTable(DataSet dataSet, int TableIndex);
    }
}
