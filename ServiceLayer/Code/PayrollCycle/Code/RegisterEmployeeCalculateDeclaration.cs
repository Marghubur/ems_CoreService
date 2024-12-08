using Bot.CoreBottomHalf.CommonModal;
using Bt.Lib.Common.Service.MicroserviceHttpRequest;
using EMailService.Modal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceLayer.Code.PayrollCycle.Interface;
using ServiceLayer.Interface;

namespace ServiceLayer.Code.PayrollCycle.Code
{
    public class RegisterEmployeeCalculateDeclaration : IRegisterEmployeeCalculateDeclaration
    {
        private readonly IEmployeeService _employeeService;
        // private readonly IDeclarationService _declarationService;
        private readonly ILogger<RegisterEmployeeCalculateDeclaration> _logger;
        private readonly CurrentSession _currentSession;
        private readonly MicroserviceRegistry _microserviceRegistry;
        private readonly RequestMicroservice _requestMicroservice;

        public RegisterEmployeeCalculateDeclaration(
            IEmployeeService employeeService,
            CurrentSession currentSession,
            // IDeclarationService declarationService,
            ILogger<RegisterEmployeeCalculateDeclaration> logger,
            IOptions<MicroserviceRegistry> options,
            RequestMicroservice requestMicroservice)
        {
            _employeeService = employeeService;
            _currentSession = currentSession;
            // _declarationService = declarationService;
            _logger = logger;
            _microserviceRegistry = options.Value;
            _requestMicroservice = requestMicroservice;
        }

        //public async Task<string> UpdateEmployeeService(Employee employee, UploadedPayrollData uploaded, IFormFileCollection fileCollection)
        //{
        //    if (employee.EmployeeUid <= 0)
        //        throw new HiringBellException { UserMessage = "Invalid EmployeeId.", FieldName = nameof(employee.EmployeeUid), FieldValue = employee.EmployeeUid.ToString() };

        //    EmployeeCalculation employeeCalculation = new EmployeeCalculation();
        //    employeeCalculation.employee = employee;
        //    _employeeService.GetEmployeeDetail(employeeCalculation);
        //    _employeeService.CreateFinancialStartEndDatetime(employeeCalculation);

        //    employeeCalculation.Doj = employee.DateOfJoining;
        //    employeeCalculation.IsFirstYearDeclaration = false;
        //    employeeCalculation.employee.IsCTCChanged = true;

        //    var result = await _employeeService.RegisterOrUpdateEmployeeDetail(employeeCalculation, fileCollection);

        //    if (!string.IsNullOrEmpty(result))
        //    {
        //        List<EmployeeDeclaration> employeeDeclarations = new List<EmployeeDeclaration>();
        //        string componentId = string.Empty;
        //        foreach (var item in uploaded.Investments)
        //        {
        //            var values = item.Key.Split(" (");
        //            if (values.Length > 0)
        //            {
        //                componentId = values[0].Trim();
        //                EmployeeDeclaration employeeDeclaration = new EmployeeDeclaration
        //                {
        //                    ComponentId = componentId,
        //                    DeclaredValue = item.Value,
        //                    Email = employee.Email,
        //                    EmployeeId = employee.EmployeeUid
        //                };
        //            }
        //        }
        //        try
        //        {
        //            string url = $"{_microserviceRegistry.UpdateBulkDeclarationDetail}/{employee.EmployeeDeclarationId}";
        //            await _requestMicroservice.PutRequest<string>(MicroserviceRequest.Builder(
        //                url,
        //                employeeDeclarations,
        //                _currentSession.Authorization,
        //                _currentSession.CompanyCode,
        //                null
        //                ));
        //        }
        //        catch
        //        {
        //            _logger.LogInformation($"Investment not found. Component id: {componentId}. Investment id: {employee.EmployeeDeclarationId}");
        //        }
        //    }

        //    return null;
        //}
    }
}
