using Bt.Lib.PipelineConfig.Services;
using ems_CoreService;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiceLayer.Interface;

namespace OnlineDataBuilder
{
    public class Startup
    {
        private IWebHostEnvironment _env { set; get; }
        private IConfiguration _configuration { get; }
        private readonly RegisterServices _registerService;
        private static string CorsPolicy = "EmstumCORS";

        public Startup(IWebHostEnvironment env)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(env.ContentRootPath)
                    .AddJsonFile($"appsettings.json", false, false)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", false, false)
                    .AddJsonFile("staffingbill.json", false, false)
                    .AddEnvironmentVariables();

                _configuration = config.Build();
                _env = env;
                _registerService = new RegisterServices(env);
            }
            catch
            {
                throw;
            }
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddHttpContextAccessor();

            // register folder paths
            _registerService.RegisterFolderPaths(_configuration, _env, services);

            // register service layer classes
            _registerService.RegisterServiceLayerServices(services);


            // register database
            _registerService.RegisterDatabase(services);

            var pipelineRegistry = new PipelineRegistry(services, _env, _configuration);

            pipelineRegistry
                .AddCurrentSessionClass()
                .AddPublicKeyConfiguration()
                .AddKafkaProducerService()
                .AddKafkaConsumerService()
                .AddCORS(CorsPolicy)
                .AddJWTSupport();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime, IAutoTriggerService autoTriggerService)
        {
            ConfigureMiddlewares.ConfigureDevelopmentMode(app, _env);

            ConfigureMiddlewares.OnApplicationStartUp(lifetime, autoTriggerService);

            ConfigureMiddlewares.Configure(app, CorsPolicy);
        }
    }
}