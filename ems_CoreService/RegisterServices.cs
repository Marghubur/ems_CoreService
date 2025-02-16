using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.DatabaseLayer.MySql.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using Bt.Lib.PipelineConfig.MicroserviceHttpRequest;
using Bt.Lib.PipelineConfig.Services;
using CoreServiceLayer.Implementation;
using DocMaker.ExcelMaker;
using DocMaker.HtmlToDocx;
using DocMaker.PdfService;
using EMailService.Modal;
using EMailService.Service;
using HtmlService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiceLayer;
using ServiceLayer.Code;
using ServiceLayer.Code.ApprovalChain;
using ServiceLayer.Code.HostedServiceJobs;
using ServiceLayer.Code.Leaves;
using ServiceLayer.Code.PayrollCycle.Code;
using ServiceLayer.Code.PayrollCycle.Interface;
using ServiceLayer.Code.SendEmail;
using ServiceLayer.Interface;
using System.IO;

namespace ems_CoreService
{
    public class RegisterServices
    {
        private readonly PipelineRegistry _registry;
        private readonly IWebHostEnvironment _env;
        public RegisterServices(IWebHostEnvironment env)
        {
            _env = env;
        }

        public void RegisterDatabase(IServiceCollection services)
        {
            services.AddScoped<IDb, Db>();
            //services.AddSingleton<ICacheManager, CacheManager>(x =>
            //{
            //    return CacheManager.GetInstance(connectionString);
            //});
        }

        public void RegisterServiceLayerServices(IServiceCollection services)
        {
            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB limit
            });
            services.AddHttpClient();
            services.AddSingleton<IUtilityService, UtilityService>();
            services.AddScoped<IAutoTriggerService, AutoTriggerService>();
            services.AddSingleton<GitHubConnector>();

            services.AddScoped<IEvaluationPostfixExpression, EvaluationPostfixExpression>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IRolesAndMenuService, RolesAndMenuService>();
            services.AddScoped<IOnlineDocumentService, OnlineDocumentService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<ILiveUrlService, LiveUrlService>();
            services.AddScoped<IUserService, UserService>();

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

            services.AddScoped<IEMailManager, EMailManager>();
            services.AddSingleton<ITimezoneConverter, TimezoneConverter>(x =>
            {
                return TimezoneConverter.Instance;
            });
            services.AddScoped<IDocumentProcessing, DocumentProcessing>();
            services.AddScoped<HtmlToPdfConverter>();
            services.AddScoped<ISettingService, SettingService>();
            services.AddScoped<ICompanyService, CompanyService>();
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
            services.AddScoped<IShiftService, ShiftService>();
            services.AddScoped<WorkFlowChain>();
            services.AddScoped<IUploadPayrollDataService, UploadPayrollDataService>();
            services.AddScoped<IPriceService, PriceService>();
            services.AddScoped<ICronJobSettingService, CronJobSettingService>();
            services.AddScoped<IRunLeaveEndYearService, RunLeaveEndYearService>();
            services.AddScoped<IWeeklyTimesheetCreationJob, WeeklyTimesheetCreationJob>();
            services.AddScoped<ILeaveAccrualJob, LeaveAccrualJob>();
            services.AddScoped<IRegisterEmployeeCalculateDeclaration, RegisterEmployeeCalculateDeclaration>();
            services.AddScoped<RequestMicroservice>();
            services.AddScoped<IOvertimeService, OvertimeService>();
        }
        public void RegisterFolderPaths(IConfiguration configuration, IWebHostEnvironment env, IServiceCollection services)
        {
            services.Configure<MicroserviceRegistry>(x => configuration.GetSection(nameof(MicroserviceRegistry)).Bind(x));
            services.AddSingleton(x =>
                x.GetRequiredService<IOptions<MicroserviceRegistry>>().Value
            );

            services.AddSingleton<FileLocationDetail>(service =>
            {
                var fileLocationDetail = configuration.GetSection("BillingFolders").Get<FileLocationDetail>();
                var locationDetail = new FileLocationDetail
                {
                    RootPath = env.ContentRootPath,
                    BillsPath = fileLocationDetail.BillsPath,
                    Location = fileLocationDetail.Location,
                    HtmlTemplatePath = fileLocationDetail.HtmlTemplatePath,
                    StaffingBillPdfTemplate = fileLocationDetail.StaffingBillPdfTemplate,
                    StaffingBillTemplate = fileLocationDetail.StaffingBillTemplate,
                    PaysliplTemplate = fileLocationDetail.PaysliplTemplate,
                    DocumentFolder = fileLocationDetail.Location,
                    UserFolder = Path.Combine(fileLocationDetail.Location, fileLocationDetail.User),
                    User = fileLocationDetail.User,
                    BillFolder = Path.Combine(fileLocationDetail.Location, fileLocationDetail.BillsPath),
                    LogoPath = Path.Combine(fileLocationDetail.Location, fileLocationDetail.LogoPath)
                };

                return locationDetail;
            });
        }
    }
}