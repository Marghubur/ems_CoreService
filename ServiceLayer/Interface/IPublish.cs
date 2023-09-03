using ModalLayer.Modal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IPublish<T>
    {
        (string, string) PublishAsSqlFile(List<SchemaDataResult> schemaDataResults, string FilePath);
    }
}
