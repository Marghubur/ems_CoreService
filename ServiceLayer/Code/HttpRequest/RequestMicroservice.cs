using Bot.CoreBottomHalf.CommonModal;
using EMailService.Modal;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Code.HttpRequest
{
    public class RequestMicroservice(CurrentSession _currentSession)
    {
        public static async Task<string> PostRequest(MicroserviceRequest microserviceRequest)
        {
            //var jsonData = JsonConvert.SerializeObject(eCal);
            //string url = "http://localhost:5281/api/ExportEmployeeDeclaration";

            var content = new StringContent(microserviceRequest.Payload, Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("reCalculateFlag", true.ToString());
            HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(microserviceRequest.Url, content);
            httpResponseMessage.EnsureSuccessStatusCode();

            return await httpResponseMessage.Content.ReadAsStringAsync();
            //if (httpResponseMessage.Content.Headers.ContentType.MediaType == "application/json")
            //{
            //    var employeeSalaryDetail = JsonConvert.DeserializeObject<EmployeeSalaryDetail>(response);
            //}
        }

        public async Task<string> PutRequest(MicroserviceRequest microserviceRequest)
        {
            var content = new StringContent(microserviceRequest.Payload, Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", _currentSession.Authorization);
            HttpResponseMessage httpResponseMessage = await httpClient.PutAsync(microserviceRequest.Url, content);
            httpResponseMessage.EnsureSuccessStatusCode();

            return await httpResponseMessage.Content.ReadAsStringAsync();
            //if (httpResponseMessage.Content.Headers.ContentType.MediaType == "application/json")
            //{
            //    var employeeSalaryDetail = JsonConvert.DeserializeObject<EmployeeSalaryDetail>(response);
            //}
        }
    }
}
