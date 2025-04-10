using Bot.CoreBottomHalf.CommonModal;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IProductService
    {
        Task<dynamic> ProdcutAddUpdateService(Product product, IFormFileCollection productImg, IFormFileCollection fileCollection);
        Task<List<Product>> GetAllProductsService(FilterModel filterModel);
        DataSet GetProductImagesService(string FileIds);
        Task<List<ProductCatagory>> AddUpdateProductCatagoryService(ProductCatagory productCatagory);
        Task<List<ProductCatagory>> GetProductCatagoryService(FilterModel filterModel);
        Task<(Product, List<ProductCatagory>)> GetProductCategoryByIdService(long productId);
        Task<DataSet> DeleteProductAttachmentService(long productId, Files files);
    }
}
