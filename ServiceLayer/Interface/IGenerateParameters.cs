using ModalLayer.Modal;
using System.Collections.Generic;
using System.Text;

namespace ServiceLayer.Interface
{
    public interface IGenerateParameters<T>
    {
        Dictionary<string, string> GenerateParameter(List<DynamicTableSchema> dynamicTableSchema, out StringBuilder ProcedureParameters);
    }
}
