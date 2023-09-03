using ModalLayer.Modal;
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
        Task<List<PTaxSlab>> AddUpdatePTaxSlabService(List<PTaxSlab> pTaxSlabs);
        string DeletePTaxSlabService(int PtaxSlabId);
        List<PTaxSlab> GetPTaxSlabByCompIdService(int CompanyId);
        Task<List<SurChargeSlab>> AddUpdateSurchargeService(List<SurChargeSlab> surChargeSlabs);
        List<SurChargeSlab> GetAllSurchargeService();
        string DeleteSurchargeSlabService(long SurchargeSlabId);
    }
}
