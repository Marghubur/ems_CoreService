﻿using Bot.CoreBottomHalf.CommonModal;
using Bot.CoreBottomHalf.CommonModal.EmployeeDetail;
using ModalLayer.Modal.Leaves;
using System;

namespace ModalLayer.Modal
{
    public class EmployeeArchiveModal
    {
        public long EmployeeId { set; get; }
        public string EmployeeCompleteJsonData { set; get; }
        public long CreatedBy { set; get; }
        public DateTime CreatedOn { set; get; }
        public string ClientJson { set; get; }
        public int Total { set; get; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }
    }

    public class EmployeeCompleteDetailModal
    {
        public Employee EmployeeDetail { set; get; }
        public EmployeeProfessionDetail EmployeeProfessionalDetail { set; get; }
        public EmployeePersonalDetail PersonalDetail { set; get; }
        public LoginDetail EmployeeLoginDetail { set; get; }
        public EmployeeDeclaration EmployeeDeclarations { set; get; }
        public Leave LeaveRequestDetail { set; get; }
        public EmployeeNoticePeriod NoticePeriod { set; get; }
        public EmployeeSalaryDetail SalaryDetail { set; get; }
        public TimesheetDetail TimesheetDetails { set; get; }
        public EmployeeMappedClient MappedClient { set; get; }

    }
}
