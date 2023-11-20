using EMailService.Modal;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class PriceService : IPriceService
    {
        public async Task<List<PriceDetail>> GetPriceDetailService()
        {
            string jsonFilePath = "Model/PriceDetail.json";
            string json = File.ReadAllText(jsonFilePath);
            List<PriceDetail> priceDetail = JsonConvert.DeserializeObject<List<PriceDetail>>(json);
            return await Task.FromResult(priceDetail);
        }
    }
}
