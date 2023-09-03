using ModalLayer.Modal.Accounts;
using System;

namespace ServiceLayer.Interface
{
    public interface IUtilityService
    {
        bool CheckIsJoinedInCurrentFinancialYear(DateTime doj, CompanySetting companySetting);
    }
}
