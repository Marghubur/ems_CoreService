using BottomhalfCore.DatabaseLayer.Common.Code;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Data;
using TimeZoneConverter;

namespace ServiceLayer.Code
{
    public class DashboardService : IDashboardService
    {
        private readonly IDb _db;

        public DashboardService(IDb db)
        {
            _db = db;
        }

        public DataSet GetSystemDashboardService(AttendenceDetail userDetails)
        {
            var Result = _db.GetDataSet("sp_dashboard_get", new
            {
                userId = userDetails.UserId,
                employeeUid = userDetails.EmployeeUid,
                fromDate = userDetails.AttendenceFromDay,
                toDate = userDetails.AttendenceToDay,
            });

            if (Result != null && Result.Tables.Count == 4)
            {
                Result.Tables[0].TableName = "BillDetail";
                Result.Tables[1].TableName = "GSTDetail";
                Result.Tables[2].TableName = "AttendaceDetail";
                Result.Tables[3].TableName = "YearGrossIncome";
                return Result;
            }

            return Result;
        }


    }
}
