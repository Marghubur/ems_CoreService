using Bot.CoreBottomHalf.CommonModal;
using ems_CommonUtility.Model;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMailService.Modal
{
    public class MicroserviceRequestBuilder(CurrentSession _currentSession)
    {
        public MicroserviceRequest Build(string url, dynamic payload)
        {
            return MicroserviceRequest.Builder(url, payload, _currentSession.Authorization, _currentSession.CompanyCode, null)
        }

        public static MicroserviceRequest BuildWithFile(string url, dynamic payload, IFormFileCollection files)
        {
            return new MicroserviceRequest();
        }
    }
}
