using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModalLayer;
using ModalLayer.Modal.HtmlTemplateModel;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class KafkaNotificationService
    {
        private readonly KafkaServiceConfig _kafkaServiceConfig;
        private readonly ProducerConfig _producerConfig;
        private readonly ILogger<KafkaNotificationService> _logger;

        public KafkaNotificationService(IOptions<KafkaServiceConfig> options,
            ProducerConfig producerConfig,
            ILogger<KafkaNotificationService> logger)
        {
            _kafkaServiceConfig = options.Value;
            _producerConfig = producerConfig;
            _logger = logger;
        }

        public async Task SendEmailNotification(dynamic attendanceRequestModal)
        {
            var result = JsonConvert.SerializeObject(attendanceRequestModal);

            _logger.LogInformation($"[Kafka] Starting kafka service to send mesage. Topic used: {_kafkaServiceConfig.AttendanceEmailTopic}, Service: {_kafkaServiceConfig.ServiceName}");
            using (var producer = new ProducerBuilder<Null, string>(_producerConfig).Build())
            {
                _logger.LogInformation($"[Kafka] Sending mesage: {result}");
                await producer.ProduceAsync(_kafkaServiceConfig.AttendanceEmailTopic, new Message<Null, string>
                {
                    Value = result
                });

                producer.Flush(TimeSpan.FromSeconds(10));
                _logger.LogInformation($"[Kafka] Messge send successfully");
            }

            await Task.CompletedTask;
        }
    }
}
