using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.Enums;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.DatabaseLayer.MySql.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using Confluent.Kafka;
using CoreServiceLayer.Implementation;
using DocMaker.ExcelMaker;
using DocMaker.HtmlToDocx;
using DocMaker.PdfService;
using EMailService.Modal.Jobs;
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
using ServiceLayer.Code.HostedServiceJobs;
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
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
            catch
            {
                throw;
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

            services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
            });

            services.Configure<JwtSetting>(o => Configuration.GetSection(nameof(JwtSetting)).Bind(o));
            services.Configure<Dictionary<string, List<string>>>(o => Configuration.GetSection("TaxSection").Bind(o));

            services.Configure<List<KafkaServiceConfig>>(x => Configuration.GetSection(nameof(KafkaServiceConfig)).Bind(x));

            services.Configure<MasterDatabase>(x => Configuration.GetSection(nameof(MasterDatabase)).Bind(x));

            string connectionString = Configuration.GetConnectionString("EmsMasterCS");
            services.AddScoped<IDb, Db>();
            services.AddSingleton<ICacheManager, CacheManager>(x =>
            {
                return CacheManager.GetInstance(connectionString);
            });


            // services.AddSingleton<AppUtilityService>();
            services.AddSingleton<IUtilityService, UtilityService>();

            services.AddScoped<IEvaluationPostfixExpression, EvaluationPostfixExpression>();
            services.AddScoped<IEmailService, EmailService>();
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
                    Environment = Env.EnvironmentName == nameof(DefinedEnvironments.Development) ?
                                    DefinedEnvironments.Development :
                                    DefinedEnvironments.Production
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
                    x.GetRequiredService<IOptions<List<KafkaServiceConfig>>>(),
                    x.GetRequiredService<ProducerConfig>(),
                    x.GetRequiredService<ILogger<KafkaNotificationService>>(),
                    Env.EnvironmentName == nameof(DefinedEnvironments.Development) ?
                                    DefinedEnvironments.Development :
                                    DefinedEnvironments.Production
                );
            });

            services.AddScoped<IEMailManager, EMailManager>();
            services.AddSingleton<ITimezoneConverter, TimezoneConverter>(x =>
            {
                return TimezoneConverter.Instance;
            });
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
            services.AddScoped<IAutoTriggerService, AutoTriggerService>();
            services.AddScoped<ICronJobSettingService, CronJobSettingService>();
            services.AddScoped<IRunLeaveEndYearService, RunLeaveEndYearService>();
            services.AddScoped<IWeeklyTimesheetCreationJob, WeeklyTimesheetCreationJob>();
            services.AddScoped<ILeaveAccrualJob, LeaveAccrualJob>();
            services.AddScoped<IRegisterEmployeeCalculateDeclaration, RegisterEmployeeCalculateDeclaration>();
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
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime, IAutoTriggerService autoTriggerService)
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

            lifetime.ApplicationStarted.Register(() =>
            {
                Task.Run(() => autoTriggerService.ScheduledJobManager());
            });

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

    public class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateTime = reader.GetDateTime();
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToUniversalTime());
        }
    }
}