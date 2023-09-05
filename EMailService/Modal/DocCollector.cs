using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BottomhalfCore.BottomhalfModel
{
    public class DocCollector
    {
        public string ClassName { set; get; }
        public IList<MethodDefination> ObjMethodDefination { set; get; }
    }

    public class MethodDefination
    {
        public string MethodName { set; get; }
        public string Summary { set; get; }
    }
}
