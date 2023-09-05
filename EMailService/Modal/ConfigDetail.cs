using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BottomhalfCore.BottomhalfModel
{
    public class ConfigDetail
    {
        public IDictionary<string, string> AppSettingCollection { set; get; }
        public IDictionary<string, string> ConnectionStringCollection { set; get; }
        public IDictionary<string, string> ConfigurationOtherCollection { set; get; }
    }
}
