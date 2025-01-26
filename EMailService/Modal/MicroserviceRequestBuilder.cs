using Bot.CoreBottomHalf.CommonModal;
using Bt.Lib.PipelineConfig.Model;
using Microsoft.AspNetCore.Http;

namespace EMailService.Modal
{
    public class MicroserviceRequestBuilder(CurrentSession _currentSession)
    {
        public MicroserviceRequest Build(string url, dynamic payload)
        {
            return MicroserviceRequest.Builder(url, payload, _currentSession.Authorization, _currentSession.CompanyCode, null);
        }

        public MicroserviceRequest BuildWithFile(string url, dynamic payload, IFormFileCollection files)
        {
            return MicroserviceRequest.Builder(url, payload, _currentSession.Authorization, _currentSession.CompanyCode, null, files);
        }
    }
}
