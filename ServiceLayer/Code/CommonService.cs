using Bot.CoreBottomHalf.Modal;
using BottomhalfCore.DatabaseLayer.Common.Code;
using ModalLayer.Modal;
using ServiceLayer.Interface;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ServiceLayer.Code
{
    public class CommonService : ICommonService
    {
        private readonly IDb _db;

        public CommonService(IDb db)
        {
            _db = db;
        }

        public List<Employee> LoadEmployeeData()
        {
            FilterModel filterModel = new FilterModel();
            List<Employee> employeeTable = _db.GetList<Employee>("SP_Employees_Get", new
            {
                filterModel.SearchString,
                filterModel.SortBy,
                filterModel.PageIndex,
                filterModel.PageSize
            });

            return employeeTable;
        }

        public EmailTemplate GetTemplate(int EmailTemplateId)
        {
            (EmailTemplate emailTemplate, EmailSettingDetail emailSetting) =
                _db.GetMulti<EmailTemplate, EmailSettingDetail>("sp_email_template_by_id", new { EmailTemplateId });

            if (emailSetting == null)
                throw new HiringBellException("Email setting detail not found. Please contact to admin.");

            if (emailTemplate == null)
                throw new HiringBellException("Email template not found. Please contact to admin.");

            return emailTemplate;
        }

        public bool IsEmptyJson(string json)
        {
            if (!string.IsNullOrEmpty(json))
            {
                if (json == "" || json == "{}" || json == "[]")
                    return true;
            }
            else
                return true;

            return false;
        }

        public string GetUniquecode(long id, string name, int size = 10)
        {
            if (string.IsNullOrEmpty(name))
                throw HiringBellException.ThrowBadRequest("Name variable is null or empty");

            var uniqueCode = name.First() + "" + name.Last();
            var zeroCount = size - (uniqueCode.Length + id.ToString().Length);
            var i = 0;
            while (i < zeroCount)
            {
                uniqueCode += "0";
                i++;
            }
            uniqueCode += id.ToString();
            return uniqueCode;
        }
        public long DecryptUniqueCoe(string code)
        {
            string value = Regex.Replace(code, "[A-Za-z ]", "");
            long id = long.Parse(value);
            return id;
        }
    }
}
