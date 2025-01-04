using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using EMailService.Modal;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class TaxRegimeService(IDb _db,
                                  CurrentSession _currentSession) : ITaxRegimeService
    {
        public TaxRegimeDesc AddUpdateTaxRegimeDescService(TaxRegimeDesc taxRegimeDesc)
        {
            if (string.IsNullOrEmpty(taxRegimeDesc.RegimeName))
                throw new HiringBellException("Regime Name is null or empty");

            if (string.IsNullOrEmpty(taxRegimeDesc.Description))
                throw new HiringBellException("Description is null or empty");


            TaxRegimeDesc oldTaxRegimeDesc = _db.Get<TaxRegimeDesc>(Procedures.Tax_Regime_Desc_GetbyId, new { TaxRegimeDescId = taxRegimeDesc.TaxRegimeDescId });
            if (oldTaxRegimeDesc != null)
            {
                oldTaxRegimeDesc.Description = taxRegimeDesc.Description;
                oldTaxRegimeDesc.RegimeName = taxRegimeDesc.RegimeName;
            }
            else
            {
                oldTaxRegimeDesc = taxRegimeDesc;
            }
            var result = _db.Execute<TaxRegimeDesc>(Procedures.Tax_Regime_Desc_Insupd, oldTaxRegimeDesc, true);
            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Fail to insert or update tax regime description");
            taxRegimeDesc.TaxRegimeDescId = Convert.ToInt32(result);
            return taxRegimeDesc;
        }

        public dynamic GetAllRegimeService()
        {
            var resultSet = _db.FetchDataSet(Procedures.Tax_Regime_Desc_Getall);
            if (resultSet != null && resultSet.Tables.Count > 3)
                throw new HiringBellException("Fail to get tax regime");

            DataTable taxRegimeDesc = null;
            DataTable taxRegime = null;
            DataTable ageGroup = null;

            if (resultSet.Tables[0].Rows.Count > 0)
                taxRegimeDesc = resultSet.Tables[0];

            if (resultSet.Tables[1].Rows.Count > 0)
                taxRegime = resultSet.Tables[1];

            if (resultSet.Tables[2].Rows.Count > 0)
                ageGroup = resultSet.Tables[2];

            return new { taxRegimeDesc, taxRegime, ageGroup };
        }

        public TaxAgeGroup AddUpdateAgeGroupService(TaxAgeGroup taxAgeGroup)
        {
            if (taxAgeGroup.StartAgeGroup <= 0)
                throw new HiringBellException("Start age must be greater than zero");

            if (taxAgeGroup.EndAgeGroup <= 0)
                throw new HiringBellException("End ange must be greater than zero");

            if (taxAgeGroup.StartAgeGroup >= taxAgeGroup.EndAgeGroup)
                throw new HiringBellException("Please select a valid age group");

            TaxAgeGroup oldAgeGroup = _db.Get<TaxAgeGroup>(Procedures.Tax_Age_Group_Getby_Id, new { AgeGroupId = taxAgeGroup.AgeGroupId });
            if (oldAgeGroup != null)
            {
                oldAgeGroup.StartAgeGroup = taxAgeGroup.StartAgeGroup;
                oldAgeGroup.EndAgeGroup = taxAgeGroup.EndAgeGroup;
            }
            else
            {
                oldAgeGroup = taxAgeGroup;
            }
            var result = _db.Execute<TaxAgeGroup>(Procedures.Tax_Age_Group_Insupd, oldAgeGroup, true);
            if (string.IsNullOrEmpty(result))
                throw new HiringBellException("Fail to insert or update tax age group");
            taxAgeGroup.AgeGroupId = Convert.ToInt32(result);
            return taxAgeGroup;
        }
        public async Task<dynamic> AddUpdateTaxRegimeService(List<TaxRegime> taxRegimes)
        {
            try
            {
                ValidateTaxRegime(taxRegimes);
                List<TaxRegime> oldTaxRegimes = _db.GetList<TaxRegime>(Procedures.Tax_Regime_Getall);
                foreach (var taxRegime in taxRegimes)
                {
                    if (taxRegime.TaxRegimeId > 0)
                    {
                        var oldRegime = oldTaxRegimes.Find(x => x.TaxRegimeId == taxRegime.TaxRegimeId);
                        if (oldRegime != null)
                        {
                            oldRegime.RegimeDescId = taxRegime.RegimeDescId;
                            oldRegime.StartAgeGroup = taxRegime.StartAgeGroup;
                            oldRegime.EndAgeGroup = taxRegime.EndAgeGroup;
                            oldRegime.MinTaxSlab = taxRegime.MinTaxSlab;
                            oldRegime.MaxTaxSlab = taxRegime.MaxTaxSlab;
                            oldRegime.TaxRatePercentage = taxRegime.TaxRatePercentage;
                            oldRegime.TaxAmount = taxRegime.TaxAmount;
                        }

                    }
                    else
                    {
                        oldTaxRegimes.Add(taxRegime);
                    }
                }
                var regime = (from n in oldTaxRegimes
                              select new
                              {
                                  n.TaxRegimeId,
                                  n.RegimeDescId,
                                  n.StartAgeGroup,
                                  n.EndAgeGroup,
                                  n.MinTaxSlab,
                                  n.MaxTaxSlab,
                                  n.TaxRatePercentage,
                                  n.TaxAmount
                              }).ToList();

                var status = await _db.BulkExecuteAsync(Procedures.Tax_Regime_Insupd, regime, true);
                return this.GetAllRegimeService();
            }
            catch (Exception)
            {
                throw;
            }
        }
        public string DeleteTaxRegimeService(int TaxRegimeId)
        {
            if (TaxRegimeId <= 0)
                throw new HiringBellException("Invalid tax regime selected");

            var status = _db.Execute<long>(Procedures.Tax_Regime_Delete_Byid, new { TaxRegimeId }, true);
            if (string.IsNullOrEmpty(status))
                throw new HiringBellException("Fail to delete tax regime");

            return status;
        }
        private void ValidateTaxRegime(List<TaxRegime> taxRegimes)
        {
            taxRegimes = taxRegimes.OrderBy(x => x.RegimeIndex).ToList();
            decimal taxAmount = 0;
            decimal minTaxSlab = 0;
            int i = 0;
            while (i < taxRegimes.Count)
            {
                if (taxRegimes[i].RegimeDescId <= 0)
                    throw new HiringBellException("Please select a vlid Tax regime");

                if (taxRegimes[i].StartAgeGroup >= taxRegimes[i].EndAgeGroup)
                    throw new HiringBellException("Invalid age group selected");

                if (taxRegimes[i].MinTaxSlab > taxRegimes[i].MinTaxSlab)
                    throw new HiringBellException("Invalid taxslab enter");

                if (i > 0)
                {
                    if (taxRegimes[i].MinTaxSlab - taxRegimes[i - 1].MaxTaxSlab != 1)
                        throw new HiringBellException("Please enter a valid taxslab range");
                }
                if (taxRegimes[i].MinTaxSlab > 0)
                    minTaxSlab = taxRegimes[i].MinTaxSlab - 1;
                else
                    minTaxSlab = taxRegimes[i].MinTaxSlab;

                taxAmount = taxAmount + Math.Abs(((taxRegimes[i].MaxTaxSlab - minTaxSlab) * taxRegimes[i].TaxRatePercentage) / 100);
                if (taxRegimes[i].TaxAmount != taxAmount)
                    throw new HiringBellException("Tax amount calculation is mismatch");
                i++;
            }
        }

        public async Task<List<PTaxSlab>> AddUpdatePTaxSlabService(List<PTaxSlab> pTaxSlabs)
        {
            try
            {
                ValidatePTaxSlab(pTaxSlabs);

                List<PTaxSlab> oldPtaxSlab = _db.GetList<PTaxSlab>(Procedures.Ptax_Slab_Getby_CompId, new { _currentSession.CurrentUserDetail.CompanyId });
                foreach (var slab in pTaxSlabs)
                {
                    if (slab.PtaxSlabId > 0)
                    {
                        var ptax = oldPtaxSlab.Find(x => x.PtaxSlabId == slab.PtaxSlabId);
                        if (ptax != null)
                        {
                            ptax.StateName = slab.StateName;
                            ptax.MinIncome = slab.MinIncome;
                            ptax.MaxIncome = slab.MaxIncome;
                            ptax.TaxAmount = slab.TaxAmount;
                            ptax.Gender = slab.Gender;
                        }
                    }
                    else
                    {
                        oldPtaxSlab.Add(slab);
                    }
                }
                var allSlabs = (from n in oldPtaxSlab
                                select new
                                {
                                    n.PtaxSlabId,
                                    n.StateName,
                                    n.MinIncome,
                                    n.MaxIncome,
                                    n.TaxAmount,
                                    n.Gender,
                                }).ToList();

                var status = await _db.BulkExecuteAsync(Procedures.Ptax_Slab_Insupd, allSlabs, true);
                if (status != pTaxSlabs.Count)
                    throw HiringBellException.ThrowBadRequest("Fail to insert or update professiobnal tax detail");

                return GetPTaxSlabByCompIdService(_currentSession.CurrentUserDetail.CompanyId);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public string DeletePTaxSlabService(int PtaxSlabId)
        {
            if (PtaxSlabId <= 0)
                throw new HiringBellException("Invalid ptax slab selected");

            var status = _db.Execute<long>(Procedures.Ptax_Slab_Delete_Byid, new { PtaxSlabId }, true);
            if (string.IsNullOrEmpty(status))
                throw new HiringBellException("Fail to delete ptax slab");

            return status;
        }

        public List<PTaxSlab> GetPTaxSlabByCompIdService(int CompanyId)
        {
            if (CompanyId <= 0)
                throw new HiringBellException("Invalid company selected. Please select a valid compny");

            var result = _db.GetList<PTaxSlab>(Procedures.Ptax_Slab_Getby_CompId, new { CompanyId });
            return result;
        }

        private void ValidatePTaxSlab(List<PTaxSlab> pTaxSlabs)
        {
            pTaxSlabs.ForEach(i =>
            {
                if (string.IsNullOrEmpty(i.StateName))
                    throw new HiringBellException("State name is null or empty");

                if (i.MinIncome < 0 || i.MinIncome == null)
                    throw new HiringBellException("Invalid minimum income. Please enter a valid company.");

                if (i.MaxIncome < 0 || i.MaxIncome == null)
                    throw new HiringBellException("Invalid minimum income. Please enter a valid company.");

                if (i.TaxAmount < 0 || i.TaxAmount == null)
                    throw new HiringBellException("Invalid minimum income. Please enter a valid company.");
            });
        }

        public async Task<List<SurChargeSlab>> AddUpdateSurchargeService(List<SurChargeSlab> surChargeSlabs)
        {
            try
            {
                ValidateSurchargeSlab(surChargeSlabs);
                List<SurChargeSlab> oldsurcharge = _db.GetList<SurChargeSlab>(Procedures.Surcharge_Slab_Getall);
                foreach (var surchargeslab in surChargeSlabs)
                {
                    if (surchargeslab.SurchargeSlabId > 0)
                    {
                        var slab = oldsurcharge.Find(x => x.SurchargeSlabId == surchargeslab.SurchargeSlabId);
                        if (slab != null)
                        {
                            slab.MinSurcahrgeSlab = surchargeslab.MinSurcahrgeSlab;
                            slab.MaxSurchargeSlab = surchargeslab.MaxSurchargeSlab;
                            slab.SurchargeRatePercentage = surchargeslab.SurchargeRatePercentage;
                        }
                    }
                    else
                    {
                        oldsurcharge.Add(surchargeslab);
                    }
                }
                var slabs = (from n in oldsurcharge
                             select new
                             {
                                 n.SurchargeSlabId,
                                 n.MinSurcahrgeSlab,
                                 n.MaxSurchargeSlab,
                                 n.SurchargeRatePercentage
                             }).ToList();

                var status = await _db.BulkExecuteAsync(Procedures.Surcharge_Slab_Insupd, slabs, true);
                return this.GetAllSurchargeService();
            }
            catch (Exception)
            {
                throw;
            }
        }
        public List<SurChargeSlab> GetAllSurchargeService()
        {
            var result = _db.GetList<SurChargeSlab>(Procedures.Surcharge_Slab_Getall);
            return result;
        }

        public string DeleteSurchargeSlabService(long SurchargeSlabId)
        {
            if (SurchargeSlabId <= 0)
                throw new HiringBellException("Invalid surcharge slab selected");

            var status = _db.Execute<long>(Procedures.Surcharge_Slab_Delete_Byid, new { SurchargeSlabId }, true);
            if (string.IsNullOrEmpty(status))
                throw new HiringBellException("Fail to delete surcharge slab");

            return status;
        }

        private void ValidateSurchargeSlab(List<SurChargeSlab> surChargeSlabs)
        {
            int i = 0;
            while (i < surChargeSlabs.Count)
            {
                if (surChargeSlabs[i].MinSurcahrgeSlab > surChargeSlabs[i].MaxSurchargeSlab && (i + 1 != surChargeSlabs.Count))
                    throw new HiringBellException("Invalid surcharge slab enter");

                if (i > 0)
                {
                    if (surChargeSlabs[i].MinSurcahrgeSlab - surChargeSlabs[i - 1].MaxSurchargeSlab != 1)
                        throw new HiringBellException("Please enter a valid surcharge range");
                }
                i++;
            }
        }

        public async Task<string> ReadProfessionalTaxDataService(IFormFile files)
        {
            try
            {
                var uploadedEmployeeData = await ReadPTaxSlabExcel(files);
                var allSlabs = (from n in uploadedEmployeeData
                                select new
                                {
                                    n.PtaxSlabId,
                                    n.StateName,
                                    n.MinIncome,
                                    n.MaxIncome,
                                    n.TaxAmount,
                                    n.Gender,
                                }).ToList();

                var status = await _db.BulkExecuteAsync(Procedures.Ptax_Slab_Insupd, allSlabs, true);
                if (status != uploadedEmployeeData.Count)
                    throw HiringBellException.ThrowBadRequest("Fail to uploda ptab slab detail");

                return "Professional tax slap uploaded successfully";
            }
            catch
            {
                throw;
            }
        }

        private async Task<List<PTaxSlab>> ReadPTaxSlabExcel(IFormFile file)
        {
            DataTable dataTable = null;
            List<PTaxSlab> pTaxSlabs = new List<PTaxSlab>();

            try
            {
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    FileInfo fileInfo = new FileInfo(file.FileName);
                    if (fileInfo.Extension == ".xlsx" || fileInfo.Extension == ".xls")
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                        using (var reader = ExcelReaderFactory.CreateReader(ms))
                        {
                            var result = reader.AsDataSet(new ExcelDataSetConfiguration
                            {
                                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                                {
                                    UseHeaderRow = true
                                }
                            });

                            dataTable = result.Tables[0];

                            pTaxSlabs = dataTable.ToList<PTaxSlab>();
                        }
                    }
                    else
                    {
                        throw HiringBellException.ThrowBadRequest("Please select a valid excel file");
                    }
                }
            }
            catch
            {
                throw;
            }

            return pTaxSlabs;
        }
    }
}