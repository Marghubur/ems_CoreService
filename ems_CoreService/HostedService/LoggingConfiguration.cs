using Bot.CoreBottomHalf.CommonModal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ModalLayer.Modal;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OnlineDataBuilder.HostedService
{
    public class LoggingConfiguration : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationConfiguration _applicationConfiguration;
        private readonly FileLocationDetail _fileLocationDetail;

        public LoggingConfiguration(
            IConfiguration configuration,
            ApplicationConfiguration applicationConfiguration,
            FileLocationDetail fileLocationDetail)
        {
            _configuration = configuration;
            _fileLocationDetail = fileLocationDetail;
            _applicationConfiguration = applicationConfiguration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.CheckCreateLogginFolder();
            return Task.CompletedTask;
        }

        private void CheckCreateLogginFolder()
        {
            _applicationConfiguration.LoggingFilePath = _configuration.GetSection("ExceptionLoggingPath").Value;
            var flag = _configuration.GetSection("Logging:LogTransaction").Value;
            if (flag.ToLower() == "true")
                _applicationConfiguration.IsLoggingEnabled = true;
            else
                _applicationConfiguration.IsLoggingEnabled = false;

            _applicationConfiguration.LoggingFilePath = Path.Combine(
                _fileLocationDetail.RootPath,
                _applicationConfiguration.LoggingFilePath);

            if (!Directory.Exists(_applicationConfiguration.LoggingFilePath))
            {
                Directory.CreateDirectory(_applicationConfiguration.LoggingFilePath);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}
