using Bot.CoreBottomHalf.CommonModal;
using EMailService.Modal.Payroll;
using Microsoft.AspNetCore.Http;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ServiceLayer.Code.PayrollCycle.Interface
{
    public interface IDeclarationService
    {
        EmployeeDeclaration GetDeclarationByEmployee(long EmployeeId);
        EmployeeDeclaration GetDeclarationById(long EmployeeDeclarationId);
        Task<EmployeeDeclaration> UpdateDeclarationDetail(long EmployeeDeclarationId, EmployeeDeclaration employeeDeclaration, IFormFileCollection FileCollection, List<Files> fileDetail);
        Task UpdateBulkDeclarationDetail(long EmployeeDeclarationId, List<EmployeeDeclaration> employeeDeclaration);
        Task<EmployeeDeclaration> HouseRentDeclarationService(long EmployeeDeclarationId, HousingDeclartion DeclarationDetail, IFormFileCollection FileCollection, List<Files> fileDetail);
        Task<EmployeeDeclaration> GetEmployeeDeclarationDetail(long EmployeeId, bool reCalculateFlag = false);
        Task<EmployeeDeclaration> GetEmployeeIncomeDetailService(FilterModel filterModel);
        Task<EmployeeSalaryDetail> CalculateSalaryDetail(long EmployeeId, EmployeeDeclaration employeeDeclaration, bool reCalculateFlag = false, bool isCTCChanged = false);
        Task<string> UpdateTaxDetailsService(EmployeePayrollData payrollEmployeeData,
            PayrollMonthlyDetail payrollMonthlyDetail, DateTime payrollDate, bool IsTaxCalculationRequired);
        Task<string> UpdateTaxDetailsService(long EmployeeId, int PresentMonth, int PresentYear);
        Task<string> SwitchEmployeeTaxRegimeService(EmployeeDeclaration employeeDeclaration);
        Task<EmployeeDeclaration> DeleteDeclarationValueService(long DeclarationId, string ComponentId);
        Task<EmployeeDeclaration> DeleteDeclaredHRAService(long DeclarationId);
        Task<EmployeeDeclaration> DeleteDeclarationFileService(long DeclarationId, int FileId, string ComponentId);
        Task<EmployeeSalaryDetail> CalculateSalaryNDeclaration(EmployeeCalculation empCal, bool reCalculateFlag);
        Task<DataSet> ManagePreviousEmployemntService(int EmployeeId, List<PreviousEmployementDetail> previousEmployementDetail);
        Task<dynamic> GetPreviousEmployemntandEmpService(int EmployeeId);
        Task<DataSet> GetPreviousEmployemntService(int EmployeeId);
        // Task<string> EmptyEmpDeclarationService();
        Task<string> ExportEmployeeDeclarationService(List<int> EmployeeIds);

    }
}
