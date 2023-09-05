using System;
using System.Collections.Generic;

namespace ModalLayer.Modal
{
    public class UploadedPayrollData
    {
        public UploadedPayrollData()
        {
            Investments = new Dictionary<string, decimal>();
        }

        public long EmployeeId { set; get; }
        public string EmployeeName { set; get; }
        public DateTime DOJ { set; get; }
        public string PAN { set; get; }
        public string Address { set; get; }
        public decimal CTC { set; get; }
        public string Email { set; get; }
        public string Mobile { set; get; }
        public bool Status { set; get; }
        public string Regime { set; get; }
        public Dictionary<string, decimal> Investments { set; get; }

        // previous employer properties
        public decimal PR_EPER_PF_80C { set; get; }
        public decimal PR_EPER_PT { set; get; }
        public decimal PR_EPER_TDS { set; get; }
        public decimal PR_EPER_TotalIncome { set; get; }
    }
}
