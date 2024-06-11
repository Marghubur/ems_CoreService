using ems_CommonUtility.Middlewares;
using MailKit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using ServiceLayer.Interface;
using System.IO;
using System.Threading.Tasks;

namespace ems_CoreService
{
    public class ConfigureMiddlewares
    {
        public static void Configure(IApplicationBuilder app, string _corsPolicy)
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                  Path.Combine(Directory.GetCurrentDirectory())),
                RequestPath = "/Files"
            });           

            app.UseMiddleware<ExceptionHandlerMiddleware>();

            app.UseRouting();

            app.UseCors(_corsPolicy);

            app.UseMiddleware<RequestMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    
        public static void ConfigureDevelopmentMode(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
        }

        public static void OnApplicationStartUp(IHostApplicationLifetime lifetime, IAutoTriggerService autoTriggerService)
        {
            lifetime.ApplicationStarted.Register(() =>
            {
                Task.Run(() => autoTriggerService.ScheduledJobManager());
            });
        }
    }
}
