using Bot.CoreBottomHalf.CommonModal;
using EMailService.Modal;
using ModalLayer.Modal;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Code.HttpRequest
{
    public class RequestMicroservice(CurrentSession _currentSession)
    {
        public static async Task<dynamic> PostRequest(MicroserviceRequest microserviceRequest)
        {
            var content = new StringContent(microserviceRequest.Payload, Encoding.UTF8, ApplicationConstants.ApplicationJson);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("reCalculateFlag", true.ToString());
            HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(microserviceRequest.Url, content);
            httpResponseMessage.EnsureSuccessStatusCode();

            return null; // await GetResponseBody(httpResponseMessage);
        }

        public async Task<T> PutRequest<T>(MicroserviceRequest microserviceRequest)
        {            
            var content = new StringContent(microserviceRequest.Payload, Encoding.UTF8, ApplicationConstants.ApplicationJson);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", _currentSession.Authorization);
            HttpResponseMessage httpResponseMessage = await httpClient.PutAsync(microserviceRequest.Url, content);
            httpResponseMessage.EnsureSuccessStatusCode();

            return await GetResponseBody<T>(httpResponseMessage);
        }

        public async Task<T> PotRequest<T>(MicroserviceRequest microserviceRequest)
        {
            var content = new StringContent(microserviceRequest.Payload, Encoding.UTF8, ApplicationConstants.ApplicationJson);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", _currentSession.Authorization);
            HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(microserviceRequest.Url, content);
            httpResponseMessage.EnsureSuccessStatusCode();

            return await GetResponseBody<T>(httpResponseMessage);
        }

        public async Task<T> GetRequest<T>(MicroserviceRequest microserviceRequest)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", _currentSession.Authorization);
            HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(microserviceRequest.Url);
            httpResponseMessage.EnsureSuccessStatusCode();

            return await GetResponseBody<T>(httpResponseMessage);
        }

        private async Task<T> GetResponseBody<T>(HttpResponseMessage httpResponseMessage)
        {
            var response = await httpResponseMessage.Content.ReadAsStringAsync();
            if (httpResponseMessage.Content.Headers.ContentType.MediaType != ApplicationConstants.ApplicationJson)
            {
                throw HiringBellException.ThrowBadRequest("Fail to get http call to salary and declaration service.");
            }

            var apiResponse = JsonConvert.DeserializeObject<MicroserviceResponse<T>>(response);
            if (apiResponse == null || apiResponse.ResponseBody == null)
            {
                throw HiringBellException.ThrowBadRequest("Fail to get http call to salary and declaration service.");
            }

            return apiResponse.ResponseBody;

        }
    }

    public class MicroserviceResponse<T>
    {
        public T ResponseBody { get; set; }
    }
}
