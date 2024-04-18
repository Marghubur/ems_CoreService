using Microsoft.Extensions.Logging;
using ModalLayer.Modal;
using ModalLayer.Modal.Accounts;
using Newtonsoft.Json;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceLayer.Code
{
    public class ComponentsCalculationService : IComponentsCalculationService
    {
        private readonly ILogger<ComponentsCalculationService> _logger;

        public ComponentsCalculationService(ILogger<ComponentsCalculationService> logger)
        {
            _logger = logger;
        }

        public decimal StandardDeductionComponent(EmployeeCalculation empCal)
        {
            _logger.LogInformation("Starting method: StandardDeductionComponent");

            decimal amount = 0;
            SalaryComponents component = null;
            if (empCal.employeeDeclaration.SalaryComponentItems != null)
            {
                component = empCal.employeeDeclaration.SalaryComponentItems.Find(x => x.ComponentId == ComponentNames.StandardDeduction);
                if (component == null)
                    throw new HiringBellException("Standard Deduction component not found. Please add standard deduction components");

                if (empCal.employeeDeclaration.EmployeeCurrentRegime == 1 && empCal.expectedAnnualGrossIncome > 250000)
                    component.DeclaredValue = component.MaxLimit;
                else
                    component.DeclaredValue = 0;

                amount = component.DeclaredValue;
            }
            _logger.LogInformation("Leaving method: StandardDeductionComponent");

            return amount;
        }

        private decimal GetProfessionalTaxAmount(EmployeeCalculation empCal, int totalMonths)
        {
            _logger.LogInformation("Starting method: GetProfessionalTaxAmount");

            decimal ptaxAmount = 0;
            var professtionalTax = empCal.ptaxSlab;
            var monthlyIncome = empCal.expectedAnnualGrossIncome / 12;
            var maxMinimumIncome = professtionalTax.Max(i => i.MinIncome);
            PTaxSlab ptax = null;
            if (monthlyIncome >= maxMinimumIncome)
                ptax = professtionalTax.OrderByDescending(i => i.MinIncome).First();
            else
                ptax = professtionalTax.Find(x => monthlyIncome >= x.MinIncome && monthlyIncome <= x.MaxIncome);

            if (ptax != null)
            {
                ptaxAmount = ptax.TaxAmount * totalMonths;
            }

            _logger.LogInformation("Leaving method: GetProfessionalTaxAmount");
            return ptaxAmount;
        }

        public decimal ProfessionalTaxComponent(EmployeeCalculation empCal, List<PTaxSlab> pTaxSlabs, int totalMonths)
        {
            _logger.LogInformation("Starting method: ProfessionalTaxComponent");

            decimal amount = 0;
            SalaryComponents component = null;
            if (empCal.employeeDeclaration.SalaryComponentItems != null)
            {
                decimal ptaxAmount = GetProfessionalTaxAmount(empCal, totalMonths);
                component = empCal.employeeDeclaration.SalaryComponentItems.Find(x => x.ComponentId == ComponentNames.ProfessionalTax);
                if (component != null)
                {
                    if (empCal.employeeDeclaration.EmployeeCurrentRegime == 1)
                        component.DeclaredValue = ptaxAmount;
                    else
                        component.DeclaredValue = 0;

                    amount = component.DeclaredValue;
                }
            }

            _logger.LogInformation("Leaving method: ProfessionalTaxComponent");
            return amount;

        }

        public decimal EmployerProvidentFund(EmployeeDeclaration employeeDeclaration, SalaryGroup salaryGroup, int totalMonths)
        {
            _logger.LogInformation("Starting method: EmployerProvidentFund");

            decimal value = 0;
            SalaryComponents component = null;
            component = salaryGroup.GroupComponents.Find(x => x.ComponentId == ComponentNames.EmployerPF);
            if (component != null)
                value = (component.DeclaredValue / 12) * totalMonths;

            _logger.LogInformation("Leaving method: EmployerProvidentFund");
            return value;

        }

        private decimal CessOnTax(decimal GrossIncomeTax)
        {
            _logger.LogInformation("Starting method: CessOnTax");

            decimal Cess = 0;
            if (GrossIncomeTax > 0)
                Cess = (0.04M * GrossIncomeTax);
            _logger.LogInformation("Leaving method: CessOnTax");

            return Cess;
        }

        private decimal Surcharge(decimal GrossIncomeTax, List<SurChargeSlab> surChargeSlabs)
        {
            _logger.LogInformation("Starting method: Surcharge");

            decimal Surcharges = 0;
            var slab = surChargeSlabs.OrderByDescending(x => x.MinSurcahrgeSlab).First();
            if (GrossIncomeTax < slab.MinSurcahrgeSlab)
                slab = surChargeSlabs.Find(x => GrossIncomeTax >= x.MinSurcahrgeSlab && GrossIncomeTax <= x.MaxSurchargeSlab);
            if (slab != null)
                Surcharges = (slab.SurchargeRatePercentage * GrossIncomeTax) / 100;
            _logger.LogInformation("Leaving method: Surcharge");

            return Surcharges;
        }

        public void TaxRegimeCalculation(EmployeeDeclaration employeeDeclaration, List<TaxRegime> taxRegimeSlabs, List<SurChargeSlab> surChargeSlabs)
        {
            _logger.LogInformation("Starting method: TaxRegimeCalculation");

            decimal taxableIncome = employeeDeclaration.TotalAmount;
            if (taxableIncome < 0)
                throw new HiringBellException("Invalid TaxableIncome");

            decimal tax = 0;
            decimal remainingAmount = taxableIncome;
            taxRegimeSlabs = taxRegimeSlabs.OrderByDescending(x => x.MinTaxSlab).ToList<TaxRegime>();
            var taxSlab = new Dictionary<int, TaxSlabDetail>();
            decimal slabAmount = 0;
            var i = 0;
            taxSlab.Add(i, new TaxSlabDetail { Description = "Gross Income Tax", Value = tax });
            while (taxableIncome > 0)
            {
                slabAmount = 0;
                if (taxableIncome >= taxRegimeSlabs[i].MinTaxSlab)
                {
                    remainingAmount = taxableIncome - (taxRegimeSlabs[i].MinTaxSlab - 1);
                    slabAmount = (taxRegimeSlabs[i].TaxRatePercentage * remainingAmount) / 100;
                    tax += slabAmount;
                    taxableIncome -= remainingAmount;

                    var maxSlabAmount = taxRegimeSlabs[i].MaxTaxSlab.ToString() == "0" ? "Above" : taxRegimeSlabs[i].MaxTaxSlab.ToString();
                    var taxSlabDetail = new TaxSlabDetail
                    {
                        Description = $"{taxRegimeSlabs[i].TaxRatePercentage}% Tax on income between {taxRegimeSlabs[i].MinTaxSlab} and {maxSlabAmount}",
                        Value = slabAmount
                    };
                    taxSlab.Add(i + 1, taxSlabDetail);
                }

                i++;
            }

            // calculate surcharge
            employeeDeclaration.SurChargesAndCess = this.Surcharge(tax, surChargeSlabs);
            employeeDeclaration.TaxNeedToPay = tax + employeeDeclaration.SurChargesAndCess;

            // calculate cess
            var cess = this.CessOnTax(employeeDeclaration.TaxNeedToPay);
            employeeDeclaration.SurChargesAndCess += cess;
            employeeDeclaration.TaxNeedToPay += cess;

            taxSlab[0].Value = tax;
            employeeDeclaration.IncomeTaxSlab = taxSlab;
            _logger.LogInformation("Leaving method: TaxRegimeCalculation");

        }

        public void NewTaxRegimeCalculation(EmployeeCalculation eCal, List<TaxRegime> taxRegimeSlabs, List<SurChargeSlab> surChargeSlabs)
        {
            _logger.LogInformation("Starting method: NewTaxRegimeCalculation");

            EmployeeDeclaration employeeDeclaration = eCal.employeeDeclaration;
            decimal taxableIncome = eCal.expectedAnnualGrossIncome;
            if (taxableIncome < 0)
                throw new HiringBellException("Invalid TaxableIncome");

            decimal tax = 0;
            decimal remainingAmount = taxableIncome;
            taxRegimeSlabs = taxRegimeSlabs.OrderByDescending(x => x.MinTaxSlab).ToList<TaxRegime>();
            var taxSlab = new Dictionary<int, TaxSlabDetail>();
            decimal slabAmount = 0;
            var i = 0;
            taxSlab.Add(i, new TaxSlabDetail { Description = "Gross Income Tax", Value = tax });
            while (taxableIncome > 0)
            {
                slabAmount = 0;
                if (taxableIncome >= taxRegimeSlabs[i].MinTaxSlab)
                {
                    remainingAmount = taxableIncome - (taxRegimeSlabs[i].MinTaxSlab - 1);
                    slabAmount = (taxRegimeSlabs[i].TaxRatePercentage * remainingAmount) / 100;
                    tax += slabAmount;
                    taxableIncome -= remainingAmount;

                    var maxSlabAmount = taxRegimeSlabs[i].MaxTaxSlab.ToString() == "0" ? "Above" : taxRegimeSlabs[i].MaxTaxSlab.ToString();
                    var taxSlabDetail = new TaxSlabDetail
                    {
                        Description = $"{taxRegimeSlabs[i].TaxRatePercentage}% Tax on income between {taxRegimeSlabs[i].MinTaxSlab} and {maxSlabAmount}",
                        Value = slabAmount
                    };
                    taxSlab.Add(i + 1, taxSlabDetail);
                }

                i++;
            }

            // calculate surcharge
            employeeDeclaration.SurChargesAndCessOnNewRegim = this.Surcharge(tax, surChargeSlabs);
            employeeDeclaration.TaxNeedToPayOnNewRegim = tax + employeeDeclaration.SurChargesAndCess;

            // calculate cess
            var cessOnNewRegim = this.CessOnTax(employeeDeclaration.TaxNeedToPayOnNewRegim);
            employeeDeclaration.SurChargesAndCessOnNewRegim += cessOnNewRegim;
            employeeDeclaration.TaxNeedToPayOnNewRegim += cessOnNewRegim;

            taxSlab[0].Value = tax;
            employeeDeclaration.NewRegimIncomeTaxSlab = taxSlab;
            _logger.LogInformation("Leaving method: NewTaxRegimeCalculation");

        }

        public void HRAComponent(EmployeeDeclaration employeeDeclaration, List<CalculatedSalaryBreakupDetail> calculatedSalaryBreakupDetails)
        {
            _logger.LogInformation("Starting method: HRAComponent");

            var calculatedSalaryBreakupDetail = calculatedSalaryBreakupDetails.Find(x => x.ComponentId.ToUpper() == ComponentNames.HRA);
            if (calculatedSalaryBreakupDetail == null)
            {
                calculatedSalaryBreakupDetail = new CalculatedSalaryBreakupDetail
                {
                    ActualAmount = 0
                };
            }

            var basicComponent = calculatedSalaryBreakupDetails.Find(x => x.ComponentId.ToUpper() == ComponentNames.Basic);
            if (basicComponent == null)
                throw new HiringBellException("Invalid gross amount not found. Please contact to admin.");

            decimal HRA1 = calculatedSalaryBreakupDetail.ActualAmount;
            decimal HRA2 = basicComponent.ActualAmount / 2;
            decimal HRA3 = 0;
            decimal HRAAmount = 0;

            if (HRA1 < HRA2 && HRA1 > 0)
                HRAAmount = HRA1;
            else
                HRAAmount = HRA2;

            var hraComponent = employeeDeclaration.SalaryComponentItems.Find(x => x.ComponentId == ComponentNames.HRA);
            if (hraComponent != null && hraComponent.DeclaredValue > 0)
            {
                decimal declaredValue = hraComponent.DeclaredValue;
                HRA3 = declaredValue - (basicComponent.ActualAmount * .1M);

                if (HRA3 > 0 && HRA3 < HRAAmount)
                    HRAAmount = HRA3;
            }

            employeeDeclaration.HRADeatils = new EmployeeHRA { HRA1 = HRA1, HRA2 = HRA2, HRA3 = HRA3, HRAAmount = HRAAmount };
            _logger.LogInformation("Leaving method: HRAComponent");

        }

        public void BuildTaxDetail(long EmployeeId, EmployeeDeclaration employeeDeclaration, EmployeeSalaryDetail salaryBreakup)
        {
            _logger.LogInformation("Starting method: ProfessionalTaxComponent");

            List<TaxDetails> taxdetails = null;
            if (salaryBreakup.TaxDetail != null)
            {
                taxdetails = JsonConvert.DeserializeObject<List<TaxDetails>>(salaryBreakup.TaxDetail);

                if (taxdetails != null && taxdetails.Count > 0)
                {
                    decimal previousMonthTax = 0;
                    int i = taxdetails.FindIndex(x => x.Month == DateTime.Now.Month && x.Year == DateTime.Now.Year);
                    employeeDeclaration.TaxPaid = Convert.ToDecimal(string.Format("{0:0.00}", taxdetails.Select(x => x.TaxPaid).Aggregate((i, k) => i + k)));
                    int currentMonthIndex = i;
                    if (currentMonthIndex > 0)
                    {
                        previousMonthTax = taxdetails[currentMonthIndex - 1].TaxDeducted;
                    }

                    if (employeeDeclaration.TaxPaid == 0)
                        i = 0;

                    decimal currentMonthTax = Convert.ToDecimal(string.Format("{0:0.00}", ((employeeDeclaration.TaxNeedToPay - employeeDeclaration.TaxPaid) / (12 - i))));
                    while (i < taxdetails.Count)
                    {
                        taxdetails[i].TaxDeducted = currentMonthTax;
                        i++;
                    }
                }
            }
            else
            {
                if (employeeDeclaration.TaxNeedToPay > 0)
                {
                    var permonthTax = employeeDeclaration.TaxNeedToPay / 12;
                    taxdetails = new List<TaxDetails>();
                    DateTime financialYearMonth = new DateTime(DateTime.Now.Year, 4, 1);
                    int i = 0;
                    while (i <= 11)
                    {
                        taxdetails.Add(new TaxDetails
                        {
                            Month = financialYearMonth.AddMonths(i).Month,
                            Year = financialYearMonth.AddMonths(i).Year,
                            EmployeeId = EmployeeId,
                            TaxDeducted = Convert.ToDecimal(String.Format("{0:0.00}", permonthTax)),
                            TaxPaid = 0
                        });

                        i++;
                    }
                }
                employeeDeclaration.TaxPaid = 0;
            }
            salaryBreakup.TaxDetail = JsonConvert.SerializeObject(taxdetails);
            _logger.LogInformation("Leaving method: ProfessionalTaxComponent");

        }

        public decimal Get_80C_DeclaredAmount(EmployeeDeclaration employeeDeclaration)
        {
            _logger.LogInformation("Starting method: Get_80C_DeclaredAmount");

            decimal totalDeduction = 0;
            var items = employeeDeclaration.Declarations.FindAll(x => x.DeclarationName == ApplicationConstants.OneAndHalfLakhsExemptions);
            items.ForEach(i =>
            {
                decimal value = i.TotalAmountDeclared;
                if (i.TotalAmountDeclared >= i.MaxAmount)
                    value = i.MaxAmount;
                else
                    value = i.TotalAmountDeclared;

                totalDeduction += value;
            });
            _logger.LogInformation("Leaving method: Get_80C_DeclaredAmount");

            return totalDeduction;
        }

        public decimal OtherDeclarationComponent(EmployeeDeclaration employeeDeclaration)
        {
            _logger.LogInformation("Starting method: OtherDeclarationComponent");

            decimal totalDeduction = 0;
            var items = employeeDeclaration.Declarations.FindAll(x => x.DeclarationName == ApplicationConstants.OtherDeclarationName);
            items.ForEach(i =>
            {
                totalDeduction = i.TotalAmountDeclared;
            });
            _logger.LogInformation("Leaving method: OtherDeclarationComponent");

            return totalDeduction;
        }

        public decimal TaxSavingComponent(EmployeeDeclaration employeeDeclaration)
        {
            _logger.LogInformation("Starting method: TaxSavingComponent");

            decimal totalDeduction = 0;
            var items = employeeDeclaration.Declarations.FindAll(x => x.DeclarationName == ApplicationConstants.TaxSavingAlloanceName);
            items.ForEach(i =>
            {
                totalDeduction = i.TotalAmountDeclared;
            });
            _logger.LogInformation("Leaving method: TaxSavingComponent");

            return totalDeduction;
        }

        public decimal HRACalculation(EmployeeDeclaration employeeDeclaration, List<CalculatedSalaryBreakupDetail> calculatedSalaryBreakupDetails, int totalMonths)
        {
            _logger.LogInformation("Starting method: HRA component calculation");

            decimal totalDeduction = 0;

            var component = calculatedSalaryBreakupDetails.Find(x => x.Formula == ApplicationConstants.AutoCalculation);
            if (component == null)
            {
                component = calculatedSalaryBreakupDetails.Find(x => x.ComponentId == ComponentNames.HRA);
                if (component != null)
                {
                    // Calculate hra and apply on deduction
                    HRAComponent(employeeDeclaration, calculatedSalaryBreakupDetails);

                    if (employeeDeclaration.HRADeatils != null)
                        totalDeduction = (employeeDeclaration.HRADeatils.HRAAmount * totalMonths);
                }
            }
            else
            {
                component = calculatedSalaryBreakupDetails.Find(x => x.ComponentId == ComponentNames.HRA);
                HRAComponent(employeeDeclaration, calculatedSalaryBreakupDetails);
                if (component != null)
                    totalDeduction = component.FinalAmount * totalMonths;
            }


            return totalDeduction;
        }

        public decimal HousePropertyComponent(EmployeeDeclaration employeeDeclaration)
        {
            _logger.LogInformation("Starting method: HousePropertyComponent");

            decimal totalDeduction = 0;
            var items = employeeDeclaration.Declarations.FindAll(x => x.DeclarationName == ApplicationConstants.HouseProperty);
            items.ForEach(i =>
            {
                totalDeduction = i.TotalAmountDeclared;
            });
            _logger.LogInformation("Leaving method: HousePropertyComponent");

            return totalDeduction;
        }
    }
}
