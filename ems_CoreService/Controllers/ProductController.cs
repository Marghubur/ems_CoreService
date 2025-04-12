using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace OnlineDataBuilder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : BaseController
    {
        private readonly IProductService _productService;
        private readonly HttpContext _httpContext;

        public ProductController(IProductService productService, IHttpContextAccessor httpContext)
        {
            _productService = productService;
            _httpContext = httpContext.HttpContext;
        }

        [HttpPost("ProdcutAddUpdate")]
        public async Task<ApiResponse> ProdcutAddUpdate()
        {
            try
            {
                _httpContext.Request.Form.TryGetValue("productdetail", out StringValues ProductInfoData);
                if (ProductInfoData.Count > 0)
                {
                    var product = JsonConvert.DeserializeObject<Product>(ProductInfoData);
                    var productImg = _httpContext.Request.Form.Files.FirstOrDefault(x => x.Name == "productimage");
                    var fileCollection= _httpContext.Request.Form.Files.Where(x => x.Name == "productFiles").ToList();
                    var resetSet = await _productService.ProdcutAddUpdateService(product, productImg, fileCollection);
                    return BuildResponse(resetSet);
                }
                else
                {
                    return BuildResponse(this.responseMessage, HttpStatusCode.BadRequest);
                }
            }
            catch (Exception ex)
            {
                throw Throw(ex);
            }
        }

        [HttpPost("GetAllProducts")]
        public async Task<ApiResponse> GetAllProducts(FilterModel filterModel)
        {
            try
            {
                var result = await _productService.GetAllProductsService(filterModel);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }

        [HttpGet("GetProductImages/{FileIds}")]
        public IResponse<ApiResponse> GetProductImages([FromRoute] string FileIds)
        {
            try
            {
                var result = _productService.GetProductImagesService(FileIds);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, FileIds);
            }
        }

        [HttpPost("AddUpdateProductCatagory")]
        public async Task<ApiResponse> AddUpdateProductCatagory(ProductCatagory productCatagory)
        {
            try
            {
                var result = await _productService.AddUpdateProductCatagoryService(productCatagory);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, productCatagory);
            }
        }

        [HttpPost("GetAllCatagory")]
        public async Task<ApiResponse> GetAllCatagory(FilterModel filterModel)
        {
            try
            {
                var result = await _productService.GetProductCatagoryService(filterModel);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }

        [HttpGet("GetProductCategoryById/{productId}")]
        public async Task<ApiResponse> GetProductCategoryById([FromRoute] long productId)
        {
            try
            {
                var result = await _productService.GetProductCategoryByIdService(productId);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, productId);
            }
        }

        [HttpPut("DeleteProductAttachment/{productId}")]
        public async Task<ApiResponse> DeleteProductAttachment([FromRoute] long productId, [FromBody] Files files)
        {
            try
            {
                var result = await _productService.DeleteProductAttachmentService(productId, files);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, new { productId, files});
            }
        }
    }
}