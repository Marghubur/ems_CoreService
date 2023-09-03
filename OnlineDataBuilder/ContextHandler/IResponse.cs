using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace OnlineDataBuilder.ContextHandler
{
    public interface IResponse<T>
    {
        dynamic ResponseBody { set; get; }
        HttpStatusCode HttpStatusCode { set; get; }
        string HttpStatusMessage { set; get; }
        string AuthenticationToken { set; get; }

        IResponse<ApiResponse> BuildResponse(dynamic Data, HttpStatusCode httpStatusCode, string Resion = null, string Token = null);
    }
}