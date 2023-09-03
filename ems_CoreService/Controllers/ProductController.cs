using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ModalLayer.Modal;
using Newtonsoft.Json;
using OnlineDataBuilder.ContextHandler;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Net;

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
        public IResponse<ApiResponse> ProdcutAddUpdate()
        {
            try
            {
                StringValues ProductInfoData = default(string);
                _httpContext.Request.Form.TryGetValue("productdetail", out ProductInfoData);
                _httpContext.Request.Form.TryGetValue("fileDetail", out StringValues FileData);
                if (ProductInfoData.Count > 0)
                {
                    Product product = JsonConvert.DeserializeObject<Product>(ProductInfoData);
                    IFormFileCollection fileDetail = _httpContext.Request.Form.Files;
                    List<Files> files = JsonConvert.DeserializeObject<List<Files>>(FileData);
                    var resetSet = _productService.ProdcutAddUpdateService(product, files, fileDetail);
                    return BuildResponse(resetSet);
                }
                else
                {
                    return BuildResponse(this.responseMessage, HttpStatusCode.BadRequest);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost("GetAllProducts")]
        public IResponse<ApiResponse> GetAllProducts(FilterModel filterModel)
        {
            var result = _productService.GetAllProductsService(filterModel);
            return BuildResponse(result);
        }

        [HttpGet("GetProductImages/{FileIds}")]
        public IResponse<ApiResponse> GetProductImages([FromRoute] string FileIds)
        {
            var result = _productService.GetProductImagesService(FileIds);
            return BuildResponse(result);
        }

        [HttpPost("AddUpdateProductCatagory")]
        public IResponse<ApiResponse> AddUpdateProductCatagory(ProductCatagory productCatagory)
        {
            var result = _productService.AddUpdateProductCatagoryService(productCatagory);
            return BuildResponse(result);
        }

        [HttpPost("GetAllCatagory")]
        public IResponse<ApiResponse> GetAllCatagory(FilterModel filterModel)
        {
            var result = _productService.GetProductCatagoryService(filterModel);
            return BuildResponse(result);
        }
    }
}
