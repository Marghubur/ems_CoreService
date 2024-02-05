using Bot.CoreBottomHalf.CommonModal.Enums;
using Confluent.Kafka;
using EMailService.Modal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModalLayer;
using ModalLayer.Modal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class KafkaNotificationService
    {
        private readonly List<KafkaServiceConfig> _kafkaServiceConfig;
        private readonly ProducerConfig _producerConfig;
        private readonly ILogger<KafkaNotificationService> _logger;
        public readonly DefinedEnvironments _environment;

        public KafkaNotificationService(IOptions<List<KafkaServiceConfig>> options,
            ProducerConfig producerConfig,
            ILogger<KafkaNotificationService> logger,
            DefinedEnvironments environment)
        {
            _kafkaServiceConfig = options.Value;
            _producerConfig = producerConfig;
            _logger = logger;
            _environment = environment;
        }

        public async Task SendEmailNotification(dynamic attendanceRequestModal)
        {
            var kafkaConfig = _kafkaServiceConfig.Find(x => x.Topic == LocalConstants.SendEmail);
            if (kafkaConfig == null)
            {
                throw new HiringBellException($"No configuration found for the kafka", "service name", LocalConstants.SendEmail, HttpStatusCode.InternalServerError);
            }

            if (_environment == DefinedEnvironments.Production)
            {
                var result = JsonConvert.SerializeObject(attendanceRequestModal);

                _logger.LogInformation($"[Kafka] Starting kafka service to send mesage. Topic used: {kafkaConfig.Topic}, Service: {kafkaConfig.ServiceName}");
                using (var producer = new ProducerBuilder<Null, string>(_producerConfig).Build())
                {
                    _logger.LogInformation($"[Kafka] Sending mesage: {result}");
                    await producer.ProduceAsync(kafkaConfig.Topic, new Message<Null, string>
                    {
                        Value = result
                    });

                    producer.Flush(TimeSpan.FromSeconds(10));
                    _logger.LogInformation($"[Kafka] Messge send successfully");
                }
            }

            await Task.CompletedTask;
        }
    }
}
