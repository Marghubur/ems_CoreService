using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface ICacheManagerAccessor<T>
    {
        string KeyName { set; get; }
        Object LoadData();
    }
}
