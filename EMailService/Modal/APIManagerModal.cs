using System;
using System.Collections.Generic;
using System.Text;

namespace BottomhalfCore.Model
{
    public class APIManagerModal
    {
        public string URL { set; get; }
        public string MethodName { set; get; }
        public List<dynamic> Parameters { set; get; }
    }
}
