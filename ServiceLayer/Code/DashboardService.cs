using Bot.CoreBottomHalf.CommonModal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using BottomhalfCore.Services.Code;
using BottomhalfCore.Services.Interface;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using EMailService.Modal;
using EMailService.Modal.DashboardCalculation;
using ModalLayer.Modal;
using ModalLayer.Modal.Profile;
using MySqlX.XDevAPI.Common;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Data;
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

        public async Task<AdminDashboardResponse> GetProfitAndLossDetail(DataSet Result)
        {
            AdminDashboardResponse dashboard = new AdminDashboardResponse();

            List<ExpensesModel> expensesModel = Converter.ToList<ExpensesModel>(Result.Tables[0]);
            List<GSTExpensesModel> gstDetail = Converter.ToList<GSTExpensesModel>(Result.Tables[1]);
            List<GSTExpensesModel> billingDetail = Converter.ToList<GSTExpensesModel>(Result.Tables[2]);

            dashboard.expensesModel = null;
            dashboard.profitModel = null;

            return await Task.FromResult(dashboard);
        }
    }
}
