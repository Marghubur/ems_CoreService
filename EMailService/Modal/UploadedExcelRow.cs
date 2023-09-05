using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModalLayer.Modal
{
    public class UploadedExcelRow
    {
        public string Key { set; get; }
        public RowDetail Value { set; get; }
    }

    public class RowDetail
    {
        public string Data { set; get; }
        public List<ColumnDetail> Keys { set; get; }
    }

    public class ColumnDetail
    {
        public string ColumnName { set; get; }
        public string ColumnType { set; get; }
    }
}
