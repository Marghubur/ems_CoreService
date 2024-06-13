using Newtonsoft.Json;

namespace EMailService.Modal
{
    public class MicroserviceRequest
    {
        public static MicroserviceRequest Builder(string url, dynamic payload)
        {
            return new MicroserviceRequest
            {
                Url = url,
                Payload = JsonConvert.SerializeObject(payload)
            };
        }

        public static MicroserviceRequest Builder(string url)
        {
            return new MicroserviceRequest
            {
                Url = url
            };
        }

        public string Url { set; get; }
        public string Payload { set; get; }
    }
}
