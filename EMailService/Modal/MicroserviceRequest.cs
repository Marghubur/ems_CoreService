namespace EMailService.Modal
{
    public class MicroserviceRequest
    {
        public static MicroserviceRequest Builder(string url, string payload)
        {
            return new MicroserviceRequest
            {
                Url = url,
                Payload = payload
            };
        }

        public string Url { set; get; }
        public string Payload { set; get; }
    }
}
