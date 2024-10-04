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
        Task<dynamic> ProdcutAddUpdateService(Product product, List<Files> files, IFormFileCollection fileCollection);
        dynamic GetAllProductsService(FilterModel filterModel);
        DataSet GetProductImagesService(string FileIds);
        List<ProductCatagory> AddUpdateProductCatagoryService(ProductCatagory productCatagory);
        List<ProductCatagory> GetProductCatagoryService(FilterModel filterModel);
    }
}
