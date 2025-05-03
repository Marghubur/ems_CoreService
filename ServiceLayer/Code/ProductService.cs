using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.Enums;
using BottomhalfCore.DatabaseLayer.Common.Code;
using Bt.Lib.PipelineConfig.MicroserviceHttpRequest;
using Bt.Lib.PipelineConfig.Model;
using EMailService.Modal;
using FileManagerService.Model;
using Microsoft.AspNetCore.Http;
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
        private readonly CurrentSession _currentSession;
        private readonly RequestMicroservice _requestMicroservice;
        private readonly MicroserviceRegistry _microserviceUrlLogs;
        private readonly IUtilityService _utilityService;
        public ProductService(
            IDb db,
            FileLocationDetail fileLocationDetail,
            CurrentSession currentSession,
            RequestMicroservice requestMicroservice,
            MicroserviceRegistry microserviceUrlLogs,
            IUtilityService utilityService)
        {
            _db = db;
            _fileLocationDetail = fileLocationDetail;
            _currentSession = currentSession;
            _requestMicroservice = requestMicroservice;
            _microserviceUrlLogs = microserviceUrlLogs;
            _utilityService = utilityService;
        }

        public async Task<List<Product>> GetAllProductsService(FilterModel filterModel)
        {
            var product = _db.GetList<Product>(Procedures.Product_Getby_Filter, new
            {
                filterModel.SearchString,
                filterModel.PageIndex,
                filterModel.PageSize,
                filterModel.SortBy
            });

            return await Task.FromResult(product);
        }

        public DataSet GetProductImagesService(string FileIds)
        {
            var result = _db.FetchDataSet(Procedures.Company_Files_Get_Byids_Json, new { CompanyFileId = FileIds });
            return result;
        }

        public async Task<string> ProdcutAddUpdateService(Product product, IFormFile productImg, List<IFormFile> fileCollection)
        {
            validateProduct(product);

            var (oldproduct, productCatagory) = await GetProductAndCategoryByIdService(product.ProductId);
            if (oldproduct == null)
                oldproduct = product;
            else
            {
                oldproduct.CatagoryName = product.CatagoryName;
                oldproduct.Status = product.Status;
                oldproduct.Description = product.Description;
                oldproduct.Brand = product.Brand;
                oldproduct.Model = product.Model;
                oldproduct.SerialNo = product.SerialNo;
                oldproduct.PurchaseDate = product.PurchaseDate;
                oldproduct.InvoiceNo = product.InvoiceNo;
                oldproduct.OrignalValue = product.OrignalValue;
                oldproduct.CurrentValue = product.CurrentValue;
                oldproduct.IsWarranty = product.IsWarranty;
                oldproduct.WarrantyDate = product.WarrantyDate;
                oldproduct.Remarks = product.Remarks;
            }

            oldproduct.ProductDetail = (product.ProductDetails != null && product.ProductDetails.Any()) ? JsonConvert.SerializeObject(product.ProductDetails) : "[]";
            oldproduct.AdminId = _currentSession.CurrentUserDetail.UserId;

            await ExecuteProductDetail(oldproduct, productImg, fileCollection);

            return ApplicationConstants.Successfull;
        }

        private void validateProduct(Product product)
        {
            if (string.IsNullOrEmpty(product.Brand))
                throw HiringBellException.ThrowBadRequest("Brand is null or empty");

            if (string.IsNullOrEmpty(product.CatagoryName))
                throw HiringBellException.ThrowBadRequest("Catagory name is null or empty");

            if (product.CompanyId <= 0)
                throw HiringBellException.ThrowBadRequest("Invalid company selected");

            if (product.ProductDetails != null && product.ProductDetails.Any())
            {
                bool hasEmptyValue = product.ProductDetails.Exists(x => x.Value == null);
                if (hasEmptyValue)
                    throw HiringBellException.ThrowBadRequest("Product detail contain some empty value");
            }
        }

        private async Task ExecuteProductDetail(Product product, IFormFile productImg, List<IFormFile> FileCollection)
        {
            try
            {
                List<int> fileIds = new List<int>();
                bool isFilePresent = false;

                var folderPath = Path.Combine(_currentSession.CompanyCode, _fileLocationDetail.CompanyFiles, "products");

                if (productImg != null)
                {
                    var fileCollection = new FormFileCollection();
                    fileCollection.Add(productImg);
                    var files = await SaveProductFiles(fileCollection, folderPath);
                    var productimg = files.First();

                    product.ProfileImgPath = Path.Combine(productimg.FilePath, productimg.FileName);
                    isFilePresent = true;
                }

                if (FileCollection.Any())
                {
                    var fileCollection = new FormFileCollection();
                    fileCollection.AddRange(FileCollection);

                    var files = await SaveProductFiles(fileCollection, folderPath);

                    foreach (var file in files)
                    {
                        var fileId = await ProductFileInsertUpdate(product, file);

                        fileIds.Add(Convert.ToInt32(fileId));
                    }

                    isFilePresent = true;
                }

                if (isFilePresent)
                {
                    var oldfileid = new List<int>();
                    if (!string.IsNullOrEmpty(product.FileIds) && product.FileIds != "[]")
                    {
                        oldfileid = JsonConvert.DeserializeObject<List<int>>(product.FileIds);
                        fileIds = oldfileid.Concat(fileIds).ToList();
                    }

                    product.FileIds = JsonConvert.SerializeObject(fileIds);
                }

                await SaveProductDetail(product);
            }
            catch (Exception)
            {
                //_fileService.DeleteFiles(files);
                throw;
            }
        }

        private async Task SaveProductDetail(Product product)
        {
            var result = await _db.ExecuteAsync(Procedures.Product_Insupd, new
            {
                product.ProductId,
                _currentSession.CurrentUserDetail.CompanyId,
                product.CatagoryName,
                product.Status,
                product.Description,
                product.Brand,
                product.Model,
                product.SerialNo,
                product.PurchaseDate,
                product.InvoiceNo,
                product.OrignalValue,
                product.CurrentValue,
                product.IsWarranty,
                product.WarrantyDate,
                product.Remarks,
                product.ProfileImgPath,
                product.FileIds,
                product.ProductDetail,
                AdminId = _currentSession.CurrentUserDetail.UserId
            }, true);

            if (string.IsNullOrEmpty(result.statusMessage))
                throw HiringBellException.ThrowBadRequest("Fail to insert or update product details");
        }

        private async Task<string> ProductFileInsertUpdate(Product product, Files n)
        {
            string Result = _db.Execute<string>(Procedures.Company_Files_Insupd, new
            {
                CompanyFileId = n.FileUid,
                product.CompanyId,
                n.FilePath,
                n.FileName,
                n.FileExtension,
                n.FileDescription,
                n.FileRole,
                UserTypeId = (int)UserType.Compnay,
                AdminId = _currentSession.CurrentUserDetail.UserId
            }, true);
            if (string.IsNullOrEmpty(Result))
                throw new HiringBellException("Fail to update housing property document detail. Please contact to admin.");

            return await Task.FromResult(Result);
        }

        private async Task<List<Files>> SaveProductFiles(IFormFileCollection formFiles, string folderPath)
        {
            string url = $"{_microserviceUrlLogs.SaveApplicationFile}";
            FileFolderDetail fileFolderDetail = new FileFolderDetail
            {
                FolderPath = folderPath,
                OldFileName = null,
                ServiceName = LocalConstants.EmstumFileService
            };

            var microserviceRequest = MicroserviceRequest.Builder(url);
            microserviceRequest
            .SetFiles(formFiles)
            .SetPayload(fileFolderDetail)
            .SetConnectionString(_currentSession.LocalConnectionString)
            .SetCompanyCode(_currentSession.CompanyCode)
            .SetToken(_currentSession.Authorization);

            return await _requestMicroservice.UploadFile<List<Files>>(microserviceRequest);
        }

        public async Task<List<ProductCatagory>> AddUpdateProductCatagoryService(ProductCatagory productCatagory)
        {
            ValidateProductCategory(productCatagory);

            var catagory = _db.Get<ProductCatagory>(Procedures.Catagory_Getby_Id, new { productCatagory.CategoryId });
            if (catagory == null)
                catagory = productCatagory;
            else
            {
                catagory.CategoryCode = productCatagory.CategoryCode;
                catagory.CategoryDescription = productCatagory.CategoryDescription;
                catagory.GroupId = productCatagory.GroupId;
            }

            var result = _db.Execute<string>(Procedures.Catagory_Insupd, catagory, true);
            if (string.IsNullOrEmpty(result))
                throw HiringBellException.ThrowBadRequest("Fail to insert/update catagory");

            return await GetProductCatagoryService(new FilterModel());
        }

        private void ValidateProductCategory(ProductCatagory productCatagory)
        {
            if (string.IsNullOrEmpty(productCatagory.CategoryCode))
                throw HiringBellException.ThrowBadRequest("prodcut catagory is null or empty");

            if (string.IsNullOrEmpty(productCatagory.CategoryDescription))
                throw HiringBellException.ThrowBadRequest("Prodcut catagory description is null");
        }

        public async Task<List<ProductCatagory>> GetProductCatagoryService(FilterModel filterModel)
        {
            var result = _db.GetList<ProductCatagory>(Procedures.Catagory_Getby_Filter, new
            {
                filterModel.SearchString,
                filterModel.PageIndex,
                filterModel.PageSize,
                filterModel.SortBy
            });

            return await Task.FromResult(result);
        }

        public async Task<(Product, List<ProductCatagory>)> GetProductAndCategoryByIdService(long productId)
        {
            var (product, catagory) = _db.GetList<Product, ProductCatagory>(Procedures.Prdoduct_Getby_Id, new { productId });
            return await Task.FromResult((product.FirstOrDefault(), catagory));
        }

        public async Task<DataSet> DeleteProductAttachmentService(long productId, Files files)
        {
            if (productId == 0)
                throw HiringBellException.ThrowBadRequest("Invalid product selected");

            ValidateProductFile(files);

            var (oldproduct, productCatagory) = await GetProductAndCategoryByIdService(productId);
            if (oldproduct == null)
                throw HiringBellException.ThrowBadRequest("Product detail not found");

            var fileIds = JsonConvert.DeserializeObject<List<long>>(oldproduct.FileIds);
            fileIds.Remove(files.FileId);

            oldproduct.FileIds = fileIds.Any() ? JsonConvert.SerializeObject(fileIds) : "[]";

            await SaveProductDetail(oldproduct);

            string url = $"{_microserviceUrlLogs.DeleteFiles}";
            FileFolderDetail fileFolderDetail = new FileFolderDetail
            {
                FolderPath = files.FilePath,
                ServiceName = LocalConstants.EmstumFileService,
                DeletableFiles = new List<string> { files.FileName }
            };

            var microserviceRequest = MicroserviceRequest.Builder(url);
            microserviceRequest
            .SetPayload(fileFolderDetail)
            .SetDbConfig(_requestMicroservice.DiscretConnectionString(_currentSession.LocalConnectionString))
            .SetConnectionString(_currentSession.LocalConnectionString)
            .SetCompanyCode(_currentSession.CompanyCode)
            .SetToken(_currentSession.Authorization);

            await _requestMicroservice.PostRequest<string>(microserviceRequest);

            var result = await _db.ExecuteAsync("sp_company_files_delete_by_id", new { CompanyFileId = files.FileId });
            if (result.rowsEffected == 0)
                throw HiringBellException.ThrowBadRequest("Fail to delete record from db");

            return GetProductImagesService(oldproduct.FileIds);
        }

        private void ValidateProductFile(Files files)
        {
            if (files.FileId == 0)
                throw HiringBellException.ThrowBadRequest("Invalid company file id");

            if (string.IsNullOrEmpty(files.FileName))
                throw HiringBellException.ThrowBadRequest("INvalid file name");

            if (string.IsNullOrEmpty(files.FilePath))
                throw HiringBellException.ThrowBadRequest("Invalid file path");
        }

        public async Task<List<ProductCatagory>> UploadProductCategoryExcelService(IFormFile file)
        {
            var renameColumns = new List<RenameColumns>
            {
                new RenameColumns {NewName = "CategoryCode", OldName ="CategoryName"}
            };

            var productCategory = await _utilityService.ReadExcelData<ProductCatagory>(file, renameColumns);
            var existingRecord =await ManageProductCategory(productCategory);

            int chunkSize = 50;
            foreach (var batch in existingRecord.Chunk(chunkSize))
            {
                await UploadBulkProductCategoryDetail(batch.ToList());
            }

            return await GetProductCatagoryService(new FilterModel());
        }

        private async Task<List<ProductCatagory>> ManageProductCategory(List<ProductCatagory> productCategory)
        {
            if (productCategory == null || !productCategory.Any())
                throw HiringBellException.ThrowBadRequest("Product category not found");

            var existingRecord = new List<ProductCatagory>();

            foreach (var item in productCategory)
            {
                try
                {
                    ValidateProductCategory(item);
                    if (item.CategoryId == 0)
                    {
                        existingRecord.Add(item);
                    }
                    else
                    {
                        var category = _db.Get<ProductCatagory>(Procedures.Catagory_Getby_Id, new { item.CategoryId }); ;
                        if (category == null)
                            throw HiringBellException.ThrowBadRequest("Invalid category code passed");

                        category.CategoryDescription = item.CategoryDescription;
                        category.CategoryCode = item.CategoryCode;
                        existingRecord.Add(category);
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }

            return await Task.FromResult(existingRecord);
        }

        private async Task UploadBulkProductCategoryDetail(List<ProductCatagory> productCatagories)
        {
            var records = (from n in productCatagories
                           select new
                           {
                               n.CategoryId,
                               n.GroupId,
                               n.CategoryCode,
                               n.CategoryDescription
                           }).ToList();

            var result = await _db.BulkExecuteAsync(Procedures.Catagory_Insupd, records, true);

            if (result != records.Count)
                throw HiringBellException.ThrowBadRequest("Fail to insert the record");
        }
    }
}