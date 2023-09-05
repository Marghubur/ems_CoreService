using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BottomhalfCore.BottomhalfModel
{
    public class GraphContainerModal
    {
        public GraphContainerModal()
        {
            this.TypeDetail = new ConcurrentDictionary<string, TypeRefCollection>();
        }
        public ConcurrentDictionary<string, TypeRefCollection> TypeDetail = null;
    }
}
