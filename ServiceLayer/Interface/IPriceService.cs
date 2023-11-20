using EMailService.Modal;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interface
{
    public interface IPriceService
    {
        Task<List<PriceDetail>> GetPriceDetailService();
    }
}
