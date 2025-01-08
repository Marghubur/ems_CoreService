using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.Enums;
using BottomhalfCore.DatabaseLayer.Common.Code;
using Bt.Lib.Common.Service.MicroserviceHttpRequest;
using Bt.Lib.Common.Service.Model;
using EMailService.Modal;
using FileManagerService.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ModalLayer.Modal;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class ProductService : IProductService
    {
        private readonly IDb _db;
        private readonly FileLocationDetail _fileLocationDetail;
        private readonly IFileService _fileService;
        private readonly CurrentSession _currentSession;
        private readonly RequestMicroservice _requestMicroservice;
        private readonly MicroserviceUrlLogs _microserviceUrlLogs;
        public ProductService(
            IDb db, 
            FileLocationDetail fileLocationDetail, 
            IFileService fileService, 
            CurrentSession currentSession,
            RequestMicroservice requestMicroservice,
            MicroserviceUrlLogs microserviceUrlLogs)
        {
            _db = db;
            _fileLocationDetail = fileLocationDetail;
            _fileService = fileService;
            _currentSession = currentSession;
            _requestMicroservice = requestMicroservice;
            _microserviceUrlLogs = microserviceUrlLogs;
        }

        public dynamic GetAllProductsService(FilterModel filterModel)
        {
            (List<Product> product, List<ProductCatagory> productCatagory) = _db.GetList<Product, ProductCatagory>(Procedures.Product_Getby_Filter, new
            {
                filterModel.SearchString,
                filterModel.PageIndex,
                filterModel.PageSize,
                filterModel.SortBy
            });
            return new { product, productCatagory };
        }

        public DataSet GetProductImagesService(string FileIds)
        {
            var result = _db.FetchDataSet(Procedures.Company_Files_Get_Byids_Json, new { CompanyFileId = FileIds });
            return result;
        }

        public async Task<dynamic> ProdcutAddUpdateService(Product product, List<Files> files, IFormFileCollection fileCollection)
        {
            validateProduct(product);
            var oldproduct = _db.Get<Product>(Procedures.Prdoduct_Getby_Id, new { ProductId = product.ProductId });
            if (oldproduct == null)
                oldproduct = product;
            else
            {
                oldproduct.MRP = product.MRP;
                oldproduct.Brand = product.Brand;
                oldproduct.ModalNum = product.ModalNum;
                oldproduct.StockStatus = product.StockStatus;
                oldproduct.Quantity = product.Quantity;
                oldproduct.SiteUrl = product.SiteUrl;
                oldproduct.CompanyId = product.CompanyId;
                oldproduct.CatagoryName = product.CatagoryName;
                oldproduct.TitleName = product.TitleName;
                oldproduct.SerialNo = product.SerialNo;
                oldproduct.ProductCode = product.ProductCode;
                oldproduct.PurchasePrice = product.PurchasePrice;
            }
            oldproduct.AdminId = _currentSession.CurrentUserDetail.UserId;

            await ExecuteProductDetail(oldproduct, files, fileCollection);

            FilterModel filterModel = new FilterModel
            {
                SearchString = $"1=1 and CompanyId={product.CompanyId}"
            };
            return this.GetAllProductsService(filterModel);
        }

        private void validateProduct(Product product)
        {
            if (product.Quantity < 0)
                throw HiringBellException.ThrowBadRequest("Qauntity is less than 0");

            if (product.StockStatus <= 0)
                throw HiringBellException.ThrowBadRequest("Stock status is invalid");

            if (string.IsNullOrEmpty(product.Brand))
                throw HiringBellException.ThrowBadRequest("Brand is null or empty");

            if (string.IsNullOrEmpty(product.CatagoryName))
                throw HiringBellException.ThrowBadRequest("Catagory name is null or empty");


            if (string.IsNullOrEmpty(product.TitleName))
                throw HiringBellException.ThrowBadRequest("Title name is null or empty");

            if (product.CompanyId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid company selected");

        }

        private async Task ExecuteProductDetail(Product product, List<Files> files, IFormFileCollection FileCollection)
        {
            try
            {
                string Result = null;
                List<int> fileIds = new List<int>();
                if (FileCollection.Count > 0)
                {
                    // save file to server filesystem
                    var folderPath = Path.Combine(_currentSession.CompanyCode, _fileLocationDetail.CompanyFiles, "products");
                    //_fileService.SaveFile(folderPath, files, FileCollection, product.ProductId.ToString());

                    string url = $"{_microserviceUrlLogs.SaveApplicationFile}";
                    FileFolderDetail fileFolderDetail = new FileFolderDetail
                    {
                        FolderPath = folderPath,
                        OldFileName = null,
                        ServiceName = LocalConstants.EmstumFileService
                    };

                    var microserviceRequest = MicroserviceRequest.Builder(url);
                    microserviceRequest
                    .SetFiles(FileCollection)
                    .SetPayload(fileFolderDetail)
                    .SetConnectionString(_currentSession.LocalConnectionString)
                    .SetCompanyCode(_currentSession.CompanyCode)
                    .SetToken(_currentSession.Authorization);

                    files = await _requestMicroservice.UploadFile<List<Files>>(microserviceRequest);

                    foreach (var n in files)
                    {
                        Result = _db.Execute<string>(Procedures.Company_Files_Insupd, new
                        {
                            CompanyFileId = n.FileUid,
                            CompanyId = product.CompanyId,
                            FilePath = n.FilePath,
                            FileName = n.FileName,
                            FileExtension = n.FileExtension,
                            FileDescription = n.FileDescription,
                            FileRole = n.FileRole,
                            UserTypeId = (int)UserType.Compnay,
                            AdminId = _currentSession.CurrentUserDetail.UserId
                        }, true);

                        if (string.IsNullOrEmpty(Result))
                            throw new HiringBellException("Fail to update housing property document detail. Please contact to admin.");

                        fileIds.Add(Convert.ToInt32(Result));
                    }
                }
                var oldfileid = new List<int>();
                if (!string.IsNullOrEmpty(product.FileIds))
                    oldfileid = JsonConvert.DeserializeObject<List<int>>(product.FileIds);

                if (oldfileid.Count > 0)
                    fileIds = oldfileid.Concat(fileIds).ToList();

                product.FileIds = JsonConvert.SerializeObject(fileIds);
                var result = _db.Execute<CompanyNotification>(Procedures.Product_Insupd, product, true);
                if (string.IsNullOrEmpty(result))
                    throw HiringBellException.ThrowBadRequest("Fail to insert or update product details");
            }
            catch (System.Exception)
            {
                _fileService.DeleteFiles(files);
                throw;
            }
        }

        public List<ProductCatagory> AddUpdateProductCatagoryService(ProductCatagory productCatagory)
        {
            if (string.IsNullOrEmpty(productCatagory.CatagoryCode))
                throw HiringBellException.ThrowBadRequest("prodcut catagory is null or empty");

            if (string.IsNullOrEmpty(productCatagory.CatagoryDescription))
                throw HiringBellException.ThrowBadRequest("Prodcut catagory description is null");

            var catagory = _db.Get<ProductCatagory>(Procedures.Catagory_Getby_Id, new { CatagoryId = productCatagory.CatagoryId });
            if (catagory == null)
                catagory = productCatagory;
            else
            {
                catagory.CatagoryCode = productCatagory.CatagoryCode;
                catagory.CatagoryDescription = productCatagory.CatagoryDescription;
                catagory.GroupId = productCatagory.GroupId;
            }
            var result = _db.Execute<string>(Procedures.Catagory_Insupd, catagory, true);
            if (string.IsNullOrEmpty(result))
                throw HiringBellException.ThrowBadRequest("Fail to insert/update catagory");
            FilterModel filterModel = new FilterModel();
            return this.GetProductCatagoryService(filterModel);
        }

        public List<ProductCatagory> GetProductCatagoryService(FilterModel filterModel)
        {
            var result = _db.GetList<ProductCatagory>(Procedures.Catagory_Getby_Filter, new
            {
                filterModel.SearchString,
                filterModel.PageIndex,
                filterModel.PageSize,
                filterModel.SortBy
            });
            return result;
        }
    }
}
