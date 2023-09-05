using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModalLayer.Modal
{
    public class ProcedureDetail
    {
        public string ProcedureName { set; get; }
        public string TableName { set; get; }
        public StringBuilder Parameters { set; get; }
        public Boolean IsTryCatchRequired { set; get; }
        public Boolean IsTransactionRequired { set; get; }
    }
}
