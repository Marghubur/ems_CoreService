using System.Net;

namespace OnlineDataBuilder.ContextHandler
{
    public class ApiResponse : IResponse<ApiResponse>
    {
        public dynamic ResponseBody { set; get; }
        public HttpStatusCode HttpStatusCode { set; get; }
        public string HttpStatusMessage { set; get; }
        public string AuthenticationToken { set; get; }

        public IResponse<ApiResponse> BuildResponse(dynamic Data, HttpStatusCode httpStatusCode, string Resion = null, string Token = null)
        {
            IResponse<ApiResponse> apiResponse = new ApiResponse();
            apiResponse.AuthenticationToken = Token;
            apiResponse.HttpStatusMessage = Resion;
            apiResponse.HttpStatusCode = httpStatusCode;
            apiResponse.ResponseBody = Data;
            return apiResponse;
        }
    }
}