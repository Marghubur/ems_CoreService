using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using EMailService.Modal;
using EMailService.Modal.DashboardCalculation;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class DashboardService : IDashboardService
    {
        private readonly IDb _db;
        private readonly ITimezoneConverter _timezoneConverter;
        private readonly CurrentSession _currentSession;

        public DashboardService(IDb db, CurrentSession currentSession, ITimezoneConverter timezoneConverter)
        {
            _db = db;
            _currentSession = currentSession;
            _timezoneConverter = timezoneConverter;
        }

        public async Task<AdminDashboardResponse> GetSystemDashboardService(AttendenceDetail userDetails)
        {
            AdminDashboardResponse dashboard = default(AdminDashboardResponse);
            var presentDate = _timezoneConverter.ToTimeZoneDateTime(DateTime.UtcNow, _currentSession.TimeZone);

            var Result = _db.GetDataSet(Procedures.AdminDashboard_Get, new
            {
                ForYear = presentDate.Year,
                ForMonth = presentDate.Month,
                Period = 30
            });


            if (Result != null && Result.Tables.Count == 7)
            {
                dashboard = await GetProfitAndLossDetail(Result);

                dashboard.projects = Result.Tables[3];
                dashboard.clients = Result.Tables[4];
                dashboard.newJoinees = Result.Tables[5];
                dashboard.leaves = Result.Tables[6];
            }

            return dashboard;
        }

        private async Task<AdminDashboardResponse> GetProfitAndLossDetail(DataSet Result)
        {
            AdminDashboardResponse dashboard = new AdminDashboardResponse();

            List<ExpensesModel> expensesModel = Converter.ToList<ExpensesModel>(Result.Tables[0]);
            List<GSTExpensesModel> gstDetail = Converter.ToList<GSTExpensesModel>(Result.Tables[1]);
            List<GSTExpensesModel> billingDetail = Converter.ToList<GSTExpensesModel>(Result.Tables[2]);

            int currentMonth = DateTime.UtcNow.Month;
            int currentYear = DateTime.UtcNow.Year;
            dashboard.expensesModel = new List<ProfitExpenseModel>();
            dashboard.profitModel = new List<ProfitExpenseModel>();

            for (int i = 1; i <= currentMonth; i++)
            {
                dashboard.expensesModel.Add(new ProfitExpenseModel
                {
                    Amount = getTotalMonthlyExpense(expensesModel, gstDetail, i),
                    Month = i,
                    Year = currentYear
                });

                dashboard.profitModel.Add(new ProfitExpenseModel
                {
                    Amount = getTotalMonthlyProfit(billingDetail, i),
                    Month = i,
                    Year = currentYear
                });
            }

            return await Task.FromResult(dashboard);
        }

        private decimal getTotalMonthlyExpense(List<ExpensesModel> expensesModel, List<GSTExpensesModel> gstDetail, int month)
        {
            decimal totalExpense = 0;
            if (expensesModel.Count > 0)
            {
                var monthlyExpense = expensesModel.Find(x => x.ForMonth == month);
                if (monthlyExpense != null)
                    totalExpense = monthlyExpense.TotalPayableToEmployees + monthlyExpense.TotalPFByEmployer + monthlyExpense.TotalProfessionalTax;
            }

            if (gstDetail.Count > 0)
            {
                var monththlyGst = gstDetail.FindAll(x => x.PaidOn.Month == month);
                if (monththlyGst.Count > 0)
                    totalExpense += monththlyGst.Aggregate(0m, (sum, value) => sum + value.Amount);
            }

            return totalExpense;
        }

        private decimal getTotalMonthlyProfit(List<GSTExpensesModel> billingDetail, int month)
        {
            decimal totalProfit = 0;
            if (billingDetail.Count > 0)
            {
                var monthlyBills = billingDetail.FindAll(x => x.PaidOn.Month == month);
                if (monthlyBills.Count > 0)
                    totalProfit = monthlyBills.Aggregate(0m, (sum, current) => sum + current.PaidAmount);
            }

            return totalProfit;
        }
    }
}
