﻿using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.API;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
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
            Product product = null;
            try
            {
                StringValues ProductInfoData = default(string);
                _httpContext.Request.Form.TryGetValue("productdetail", out ProductInfoData);
                _httpContext.Request.Form.TryGetValue("fileDetail", out StringValues FileData);
                if (ProductInfoData.Count > 0)
                {
                    product = JsonConvert.DeserializeObject<Product>(ProductInfoData);
                    IFormFileCollection fileDetail = _httpContext.Request.Form.Files;
                    List<Files> files = JsonConvert.DeserializeObject<List<Files>>(FileData);
                    var resetSet = await _productService.ProdcutAddUpdateService(product, files, fileDetail);
                    return BuildResponse(resetSet);
                }
                else
                {
                    return BuildResponse(this.responseMessage, HttpStatusCode.BadRequest);
                }
            }
            catch (Exception ex)
            {
                throw Throw(ex, product);
            }
        }

        [HttpPost("GetAllProducts")]
        public IResponse<ApiResponse> GetAllProducts(FilterModel filterModel)
        {
            try
            {
                var result = _productService.GetAllProductsService(filterModel);
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
        public IResponse<ApiResponse> AddUpdateProductCatagory(ProductCatagory productCatagory)
        {
            try
            {
                var result = _productService.AddUpdateProductCatagoryService(productCatagory);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, productCatagory);
            }
        }

        [HttpPost("GetAllCatagory")]
        public IResponse<ApiResponse> GetAllCatagory(FilterModel filterModel)
        {
            try
            {
                var result = _productService.GetProductCatagoryService(filterModel);
                return BuildResponse(result);
            }
            catch (Exception ex)
            {
                throw Throw(ex, filterModel);
            }
        }
    }
}