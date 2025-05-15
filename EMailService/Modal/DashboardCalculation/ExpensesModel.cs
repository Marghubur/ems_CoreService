using EMailService.Modal.Notification;
using System;
using System.Collections.Generic;
using System.Data;

namespace EMailService.Modal.DashboardCalculation
{
    public class ExpensesModel
    {
        public decimal PayableToEmployee { set; get; }
        public decimal PFByEmployer { set; get; }
        public decimal ProfessionalTax { set; get; }
        public int ForYear { set; get; }
        public int ForMonth { set; get; }
    }

    public class GSTExpensesModel
    {
        public decimal PaidAmount { set; get; }
        public DateTime PaidOn { set; get; }
        public decimal Amount { set; get; }
    }

    public class ProfitExpenseModel
    {
        public decimal Amount { set; get; }
        public int Month { set; get; }
        public int Year { set; get; }
    }

    public class AdminDashboardResponse
    {
        public List<ProfitExpenseModel> expensesModel { set; get; }
        public List<ProfitExpenseModel> profitModel { set; get; }
        public DataTable projects { set; get; }
        public DataTable clients { set; get; }
        public DataTable newJoinees { set; get; }
        public DataTable leaves { set; get; }
        public DataTable companyNotifications { set; get; }
    }
}
