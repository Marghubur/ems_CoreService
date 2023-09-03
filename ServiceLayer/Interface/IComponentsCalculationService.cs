using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceLayer.Interface
{
    public interface IComponentsCalculationService
    {
        decimal StandardDeductionComponent(EmployeeCalculation empCal);
        decimal ProfessionalTaxComponent(EmployeeCalculation empCal, List<PTaxSlab> pTaxSlabs, int totalMonths);
        decimal EmployerProvidentFund(EmployeeDeclaration employeeDeclaration, SalaryGroup salaryGroup, int totalMonths);
        void TaxRegimeCalculation(EmployeeDeclaration employeeDeclaration, List<TaxRegime> taxRegimeSlabs, List<SurChargeSlab> surChargeSlabs);
        void NewTaxRegimeCalculation(EmployeeCalculation eCal, List<TaxRegime> taxRegimeSlabs, List<SurChargeSlab> surChargeSlabs);
        void HRAComponent(EmployeeDeclaration employeeDeclaration, List<CalculatedSalaryBreakupDetail> calculatedSalaryBreakupDetails);
        void BuildTaxDetail(long EmployeeId, EmployeeDeclaration employeeDeclaration, EmployeeSalaryDetail salaryBreakup);
        decimal Get_80C_DeclaredAmount(EmployeeDeclaration employeeDeclaration);
        decimal OtherDeclarationComponent(EmployeeDeclaration employeeDeclaration);
        decimal TaxSavingComponent(EmployeeDeclaration employeeDeclaration);
        decimal HousePropertyComponent(EmployeeDeclaration employeeDeclaration);
        decimal HRACalculation(EmployeeDeclaration employeeDeclaration, List<CalculatedSalaryBreakupDetail> calculatedSalaryBreakupDetails, int totalMonths);
    }
}
