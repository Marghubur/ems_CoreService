using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.DatabaseLayer.MySql.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using Confluent.Kafka;
using CoreServiceLayer.Implementation;
using DocMaker.ExcelMaker;
using DocMaker.HtmlToDocx;
using DocMaker.PdfService;
using EMailService.Service;
using HtmlService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ModalLayer;
using ModalLayer.Modal;
using Newtonsoft.Json.Serialization;
using OnlineDataBuilder.Model;
using SchoolInMindServer.MiddlewareServices;
using ServiceLayer;
using ServiceLayer.Caching;
using ServiceLayer.Code;
using ServiceLayer.Code.ApprovalChain;
using ServiceLayer.Code.HostedServicesJobs;
using ServiceLayer.Code.Leaves;
using ServiceLayer.Code.PayrollCycle;
using ServiceLayer.Code.PayrollCycle.Code;
using ServiceLayer.Code.PayrollCycle.Interface;
using ServiceLayer.Code.SendEmail;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OnlineDataBuilder
{
    public class Startup
    {
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
                //AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: false, reloadOnChange: false);

                this.Configuration = config.Build();
                this.Env = env;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private IWebHostEnvironment Env { set; get; }
        public IConfiguration Configuration { get; }
        public string CorsPolicy = "BottomhalfCORS";

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
               .AddJwtBearer(x =>
               {
                   x.SaveToken = true;
                   x.RequireHttpsMetadata = false;
                   x.TokenValidationParameters = new TokenValidationParameters
                   {
                       ValidateIssuer = false,
                       ValidateAudience = false,
                       ValidateLifetime = true,
                       ValidateIssuerSigningKey = true,
                       IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["JwtSetting:Key"])),
                       ClockSkew = TimeSpan.Zero
                   };
               });

            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
            });

            services.Configure<JwtSetting>(o => Configuration.GetSection(nameof(JwtSetting)).Bind(o));
            services.Configure<Dictionary<string, List<string>>>(o => Configuration.GetSection("TaxSection").Bind(o));
            services.Configure<KafkaServiceConfig>(x => Configuration.GetSection(nameof(KafkaServiceConfig)).Bind(x));

            string connectionString = Configuration.GetConnectionString("EmsMasterCS");
            services.AddSingleton<IDb, Db>();
            services.AddSingleton<ICacheManager, CacheManager>(x =>
            {
                return CacheManager.GetInstance(connectionString);
            });


            // services.AddSingleton<AppUtilityService>();
            services.AddSingleton<IUtilityService, UtilityService>();

            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IEvaluationPostfixExpression, EvaluationPostfixExpression>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ILoginService, LoginService>();
            services.AddScoped<IRolesAndMenuService, RolesAndMenuService>();
            services.AddScoped<IOnlineDocumentService, OnlineDocumentService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<ILiveUrlService, LiveUrlService>();
            services.AddScoped<IUserService, UserService>();

            services.AddHttpContextAccessor();
            services.AddScoped<CurrentSession>(x =>
            {
                return new CurrentSession
                {
                    Environment = Env.EnvironmentName == nameof(ModalLayer.Modal.Environments.Development) ?
                                    ModalLayer.Modal.Environments.Development :
                                    ModalLayer.Modal.Environments.Production
                };
            });
            services.AddScoped<IFileMaker, CreatePDFFile>();
            services.AddScoped<IHtmlMaker, ToHtml>();
            services.AddScoped<IManageUserCommentService, ManageUserCommentService>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<CommonFilterService>();
            services.AddScoped<IClientsService, ClientsService>();
            services.AddScoped<IBillService, BillService>();
            services.AddScoped<IAttendanceService, AttendanceService>();
            services.AddScoped<ICommonService, CommonService>();
            services.AddScoped<IHTMLConverter, HTMLConverter>();
            services.AddScoped<ITemplateService, TemplateService>();
            services.AddScoped<HtmlConverterService>();
            services.AddScoped<IDOCXToHTMLConverter, DOCXToHTMLConverter>();
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<ExcelWriter>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IObjectiveService, ObjectiveService>();

            services.AddScoped<IInitialRegistrationService, InitialRegistrationService>();
            services.AddSingleton<FileLocationDetail>(service =>
            {
                var fileLocationDetail = Configuration.GetSection("BillingFolders").Get<FileLocationDetail>();
                var locationDetail = new FileLocationDetail
                {
                    RootPath = this.Env.ContentRootPath,
                    BillsPath = fileLocationDetail.BillsPath,
                    Location = fileLocationDetail.Location,
                    HtmlTemplatePath = fileLocationDetail.HtmlTemplatePath,
                    StaffingBillPdfTemplate = fileLocationDetail.StaffingBillPdfTemplate,
                    StaffingBillTemplate = fileLocationDetail.StaffingBillTemplate,
                    PaysliplTemplate = fileLocationDetail.PaysliplTemplate,
                    DocumentFolder = fileLocationDetail.Location,
                    UserFolder = Path.Combine(fileLocationDetail.Location, fileLocationDetail.User),
                    BillFolder = Path.Combine(fileLocationDetail.Location, fileLocationDetail.BillsPath),
                    LogoPath = Path.Combine(fileLocationDetail.Location, fileLocationDetail.LogoPath)
                };

                return locationDetail;
            });

            var kafkaServerDetail = new ProducerConfig();
            Configuration.Bind("KafkaServerDetail", kafkaServerDetail);

            services.AddSingleton<ProducerConfig>(kafkaServerDetail);
            services.AddSingleton<KafkaNotificationService>(x =>
            {
                return new KafkaNotificationService(
                    x.GetRequiredService<IOptions<KafkaServiceConfig>>(),
                    x.GetRequiredService<ProducerConfig>(),
                    x.GetRequiredService<ILogger<KafkaNotificationService>>(),
                    Env.EnvironmentName == nameof(ModalLayer.Modal.Environments.Development) ?
                                    ModalLayer.Modal.Environments.Development :
                                    ModalLayer.Modal.Environments.Production
                );
            });

            services.AddScoped<IEMailManager, EMailManager>();
            services.AddSingleton<ITimezoneConverter, TimezoneConverter>();
            services.AddScoped<IDocumentProcessing, DocumentProcessing>();
            services.AddScoped<HtmlToPdfConverter>();
            services.AddScoped<ISettingService, SettingService>();
            services.AddScoped<ISalaryComponentService, SalaryComponentService>();
            services.AddScoped<ICompanyService, CompanyService>();
            services.AddScoped<IDeclarationService, DeclarationService>();
            services.AddScoped<ILeaveService, LeaveService>();
            services.AddScoped<IManageLeavePlanService, ManageLeavePlanService>();
            services.AddScoped<ITimesheetService, TimesheetService>();
            services.AddScoped<IComponentsCalculationService, ComponentsCalculationService>();
            services.AddTransient<ILeaveCalculation, LeaveCalculation>();
            services.AddScoped<IAttendanceRequestService, AttendanceRequestService>();
            services.AddScoped<ILeaveRequestService, LeaveRequestService>();
            services.AddScoped<ITimesheetRequestService, TimesheetRequestService>();
            services.AddSingleton<ApplicationConfiguration>();
            services.AddScoped<ITaxRegimeService, TaxRegimeService>();
            services.AddScoped<AttendanceEmailService>();
            services.AddScoped<ApprovalEmailService>();
            services.AddScoped<BillingToClientEmailService>();
            services.AddScoped<ForgotPasswordEmailService>();
            services.AddScoped<LeaveEmailService>();
            services.AddScoped<NewProjectAssignEmailService>();
            services.AddScoped<OfferLetterEmailService>();
            services.AddScoped<RegistrationEmailService>();
            services.AddScoped<TimesheetEmailService>();
            services.AddScoped<IHolidaysAndWeekoffs, HolidaysAndWeekoffs>();
            services.AddScoped<ICompanyCalendar, CompanyCalendar>();
            services.AddScoped<Quota>();
            services.AddScoped<Apply>();
            services.AddScoped<Accrual>();
            services.AddScoped<LeaveFromManagement>();
            services.AddScoped<Approval>();
            services.AddScoped<Restriction>();
            services.AddScoped<YearEndCalculation>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICompanyNotificationService, CompanyNotificationService>();
            services.AddScoped<IServiceRequestService, ServiceRequestService>();
            services.AddScoped<IApprovalChainService, ApprovalChainService>();
            services.AddScoped<IPayrollService, PayrollService>();
            services.AddScoped<IShiftService, ShiftService>();
            services.AddScoped<WorkFlowChain>();
            services.AddScoped<IUploadPayrollDataService, UploadPayrollDataService>();
            services.AddScoped<IPriceService, PriceService>();
            services.AddSingleton<IAutoTriggerService, AutoTriggerService>();
            services.AddScoped<IYearEndLeaveProcessingJob, YearEndLeaveProcessingJob>();

            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicy, policy =>
                {
                    policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders("Authorization");
                });
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(Policies.Admin, Policies.AdminPolicy());
                options.AddPolicy(Policies.User, Policies.UserPolicy());
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                   Path.Combine(Directory.GetCurrentDirectory())),
                RequestPath = "/Files"
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseMiddleware<ExceptionHandlerMiddleware>();

            app.UseRouting();

            app.UseCors(CorsPolicy);

            app.UseMiddleware<RequestMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
