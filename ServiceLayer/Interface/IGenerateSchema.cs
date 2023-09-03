using ModalLayer.Modal;

namespace ServiceLayer.Interface
{
    public interface IGenerateSchema
    {
        string GenerateSchemaString(DynamicTableDetail dynamicTableDetail);
    }
}
