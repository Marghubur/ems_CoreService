using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface ITaxRegimeService
    {
        TaxRegimeDesc AddUpdateTaxRegimeDescService(TaxRegimeDesc taxRegimeDesc);
        dynamic GetAllRegimeService();
        TaxAgeGroup AddUpdateAgeGroupService(TaxAgeGroup taxAgeGroup);
        Task<dynamic> AddUpdateTaxRegimeService(List<TaxRegime> taxRegimes);
        string DeleteTaxRegimeService(int TaxRegimeId);
        Task<(List<PTaxSlab> ptaxSlab, Payroll payroll)> AddUpdatePTaxSlabService(List<PTaxSlab> pTaxSlabs);
        string DeletePTaxSlabService(int PtaxSlabId);
        (List<PTaxSlab> ptaxSlab, Payroll payroll) GetPTaxSlabByCompIdService();
        Task<List<SurChargeSlab>> AddUpdateSurchargeService(List<SurChargeSlab> surChargeSlabs);
        List<SurChargeSlab> GetAllSurchargeService();
        string DeleteSurchargeSlabService(long SurchargeSlabId);
        Task<string> ReadProfessionalTaxDataService(IFormFile files);
    }
}
