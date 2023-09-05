using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BottomhalfCore.BottomhalfModel
{
    public class SessionObject
    {
        public DateTime LastUpdatedOn { set; get; }
        public Object UserObject { set; get; }
        public string SessionConnectionString { set; get; }
    }
}
