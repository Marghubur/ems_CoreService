using ModalLayer.Modal.Accounts;
using ServiceLayer.Interface;
using System;

namespace ServiceLayer.Code
{
    public class UtilityService : IUtilityService
    {
        public bool CheckIsJoinedInCurrentFinancialYear(DateTime doj, CompanySetting companySetting)
        {
            if (doj.Year == companySetting.FinancialYear)
                if (doj.Month >= companySetting.DeclarationStartMonth)
                    return true;
                else if (doj.Year == companySetting.FinancialYear + 1)
                    if (doj.Month <= companySetting.DeclarationEndMonth) return true;

            return false;
        }
    }
}
