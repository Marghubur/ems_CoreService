using Bot.CoreBottomHalf.CommonModal;
using EMailService.Modal;
using ModalLayer.Modal;
using Newtonsoft.Json;
using System;
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

            DbConfigModal dbConfigModal = DiscretConnectionString(_currentSession.LocalConnectionString);
            httpClient.DefaultRequestHeaders.Add("database", JsonConvert.SerializeObject(dbConfigModal));
            httpClient.DefaultRequestHeaders.Add("companyCode", _currentSession.CompanyCode);


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

        private DbConfigModal DiscretConnectionString(string cs)
        {
            DbConfigModal dbConfigModal = new DbConfigModal();
            string[] splittedCS = cs.Split(';');
            if (splittedCS.Length > 1)
            {
                foreach (string item in splittedCS)
                {
                    var fields = item.Split('=');
                    if (fields[0].ToLower() == nameof(DbConfigModal.OrganizationCode).ToLower())
                    {
                        dbConfigModal.OrganizationCode = fields[1];
                    }
                    else if (fields[0].ToLower() == nameof(DbConfigModal.Code).ToLower())
                    {
                        dbConfigModal.Code = fields[1];
                    }
                    else if (fields[0].ToLower() == nameof(DbConfigModal.Schema).ToLower())
                    {
                        dbConfigModal.Schema = fields[1];
                    }
                    else if (fields[0].ToLower() == nameof(DbConfigModal.DatabaseName).ToLower())
                    {
                        dbConfigModal.DatabaseName = fields[1];
                    }
                    else if (fields[0].ToLower() == nameof(DbConfigModal.Server).ToLower())
                    {
                        dbConfigModal.Server = fields[1];
                    }
                    else if (fields[0].ToLower() == nameof(DbConfigModal.Port).ToLower())
                    {
                        dbConfigModal.Port = fields[1];
                    }
                    else if (fields[0].ToLower() == nameof(DbConfigModal.Database).ToLower())
                    {
                        dbConfigModal.Database = fields[1];
                    }
                    else if (fields[0].ToLower() == "user id")
                    {
                        dbConfigModal.UserId = fields[1];
                    }
                    else if (fields[0].ToLower() == nameof(DbConfigModal.Password).ToLower())
                    {
                        dbConfigModal.Password = fields[1];
                    }
                    else if (fields[0].ToLower() == "connection timeout")
                    {
                        dbConfigModal.ConnectionTimeout = fields[1] != null ? Convert.ToInt32(fields[1]) : 0;
                    }
                    else if (fields[0].ToLower() == "connection lifetime")
                    {
                        dbConfigModal.ConnectionLifetime = fields[1] != null ? Convert.ToInt32(fields[1]) : 0;
                    }
                    else if (fields[0].ToLower() == "min pool size")
                    {
                        dbConfigModal.MinPoolSize = fields[1] != null ? Convert.ToInt32(fields[1]) : 0;
                    }
                    else if (fields[0].ToLower() == "max pool size")
                    {
                        dbConfigModal.MaxPoolSize = fields[1] != null ? Convert.ToInt32(fields[1]) : 0;
                    }
                    else if (fields[0].ToLower() == nameof(DbConfigModal.Pooling).ToLower())
                    {
                        dbConfigModal.Pooling = fields[1] != null ? Convert.ToBoolean(fields[1]) : false;
                    }
                }
            }

            return dbConfigModal;
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
