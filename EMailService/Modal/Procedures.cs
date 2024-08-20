namespace EMailService.Modal
{
    public class Procedures
    {
        /*------------- Attendance Request service proceudures -------*/
        public static string Attendance_Requests_By_Filter = "sp_attendance_requests_by_filter";
        public static string Attendance_Update_Request = "sp_attendance_update_request";
        public static string Attendance_Get_ById = "sp_attendance_get_byid";
        public static string LEAVE_PLAN_TYPE_BY_LEAVEID = "sp_get_leave_plan_type_by_leaveid";
        public static string DAILY_ATTENDANCE_UPD_WEEKLY = "sp_daily_attendance_upd_weekly";
        public static string DAILY_ATTENDANCE_BET_DATES = "sp_daily_attendance_bet_dates";
        public static string DAILY_ATTENDANCE_BET_DATES_EMPID = "sp_daily_attendance_bet_dates_EmpId";
        public static string LEAVE_TIMESHEET_AND_ATTENDANCE_REQUESTS_GET_BY_ROLE = "sp_leave_timesheet_and_attendance_requests_get_by_role";
        public static string LEAVE_TIMESHEET_AND_ATTENDANCE_REQUESTS_GET = "sp_leave_timesheet_and_attendance_requests_get";
        public static string DAILY_ATTENDANCE_FILTER = "sp_daily_attendance_filter";

        /*------------- Leave Request service proceudures -------*/
        public static string Leave_Notification_And_Request_InsUpdate = "sp_leave_notification_and_request_InsUpdate";
        public static string Employee_Leave_Level_Migration = "sp_employee_leave_level_migration";
        public static string Leave_Requests_By_Filter = "sp_leave_requests_by_filter";

        /*-------------- Leave service proceudures ------------*/
        public static string Leave_Plans_Get = "sp_leave_plans_get";
        public static string Leave_Plan_Insupd = "sp_leave_plan_insupd";
        public static string Leave_Plans_Type_Insupd = "sp_leave_plans_type_insupd";
        public static string Leave_Plans_Type_Get = "sp_leave_plans_type_get";
        public static string Leave_Plans_Type_GetbyId = "sp_leave_plans_type_getbyId";
        public static string Leave_Detail_InsUpdate = "sp_leave_detail_InsUpdate";
        public static string Leave_Plan_And_Type_Get_By_Ids_Json = "sp_leave_plan_andtype_get_by_ids_json";
        public static string Leave_Request_Notification_Get_ById = "sp_leave_request_notification_get_byId";
        public static string Leave_Request_Notification_InsUpdate = "sp_leave_request_notification_InsUpdate";
        public static string Company_Calendar_Get_By_Company = "sp_company_calendar_get_by_company";
        public static string User_Files_Get_Byids_Json = "sp_user_files_get_byids_json";
        public static string Employee_Leave_Request_GetById = "sp_employee_leave_request_GetById";
        public static string Employee_Leave_Request_By_Empid = "sp_employee_leave_request_by_empid";
        public static string Employee_Leave_Request_InsUpdate = "sp_employee_leave_request_InsUpdate";
        public static string Leave_Plan_Calculation_Get = "sp_leave_plan_calculation_get";
        public static string EMPLOYEE_SALARY_DETAIL_BY_EMPID_YEAR = "sp_employee_salary_detail_by_empid_year";
        public static string LEAVE_PLAN_SET_DEFAULT = "sp_leave_plan_set_default";
        public static string LEAVE_PLAN_TYPE_GET_BY_IDS_JSON = "sp_leave_plan_type_get_by_ids_json";
        public static string LEAVE_REQUEST_NOTIFICATION_DAILY_ATTENDANCE_INSUPDATE = "sp_leave_request_notification_daily_attendance_insupdate";

        /*-------------- Leave Calculation proceudures ------------*/
        public static string Leave_Type_Detail_Get_By_EmployeeId = "sp_leave_type_detail_get_by_employeeId";
        public static string Company_Setting_Get_All = "sp_company_setting_get_all";
        public static string Leave_Accrual_Cycle_Data_By_Employee = "sp_leave_accrual_cycle_data_by_employee";
        public static string Employees_ById = "SP_Employees_ById";
        public static string Employee_Leave_Request_Update_Accrual_Detail = "sp_employee_leave_request_update_accrual_detail";
        public static string Leave_Accrual_Cycle_Master_Data = "sp_leave_accrual_cycle_master_data";
        public static string SP_LEAVE_YEAREND_PROCESSING_ALL = "sp_leave_yearend_processing_all";
        public static string Userfiledetail_Upload = "sp_userfiledetail_Upload";
        public static string Leave_Approver_By_Workflow = "sp_leave_approver_by_workflow";
        public static string Project_basic_Detail_Page_By_Offset = "sp_project_basic_detail_page_by_offset";

        /*-------------- Attendance Service proceudures ------------*/
        public static string Attendance_Insupd = "sp_attendance_insupd";
        public static string Employee_GetAll = "SP_Employee_GetAll";
        public static string Attendance_Get_By_Empid = "sp_attendance_get_by_empid";
        public static string Leave_Request_Notification_Get_By_Empid = "sp_leave_request_notification_get_by_empid";
        public static string Attendance_Get = "sp_attendance_get";
        public static string Attendance_Detall_Pending = "sp_attendance_detall_pending";
        public static string Complaint_Or_Request_Get_By_Employeeid = "sp_complaint_or_request_get_by_employeeid";
        public static string Attendance_Employee_Detail_Id = "sp_attendance_employee_detail_id";
        public static string Complaint_Or_Request_InsUpdate = "sp_complaint_or_request_InsUpdate";
        public static string Employee_Performance_Get = "sp_employee_performance_get";
        public static string Work_Shifts_Getby_Empid = "sp_work_shifts_getby_empid";
        public static string Leave_And_Lop_Get = "sp_leave_and_lop_get";
        public static string Complaint_Or_Request_Update_Status = "sp_complaint_or_request_update_status";
        public static string DAILY_ATTENDANCE_INSERT = "sp_daily_attendance_insert";


        /*--------------  Authentication Service procedures ------------*/
        public static string AuthenticationToken_VerifyAndGet = "SP_AuthenticationToken_VerifyAndGet";
        public static string UpdateRefreshToken = "sp_UpdateRefreshToken";

        /*--------------  Bill Service procedures ------------*/
        public static string Billing_Detail = "sp_Billing_detail";
        public static string Filedetail_Insupd = "sp_filedetail_insupd";
        public static string Gstdetail_Insupd = "sp_gstdetail_insupd";
        public static string Payslip_Detail = "sp_payslip_detail";

        /*--------------  Clients Service procedures ------------*/
        public static string Client_ById = "SP_Client_ById";
        public static string Client_IntUpd = "SP_Client_IntUpd";
        public static string DeactivateOrganization_Delandgetall = "sp_deactivateOrganization_delandgetall";

        /*--------------  Common Service procedures ------------*/
        public static string Employees_Get = "SP_Employees_Get";
        public static string Email_Template_By_Id = "sp_email_template_by_id";

        /*--------------  Company Calender Service procedures ------------*/
        public static string Company_Calender_Getby_Filter = "SP_company_calender_getby_filter";
        public static string Company_Calendar_Insupd = "sp_company_calendar_insupd";
        public static string Company_Calender_Delete_By_Calenderid = "sp_company_calender_delete_by_calenderid";

        /*--------------  Company Notification Service procedures ------------*/
        public static string Department_And_Roles_Getall = "sp_department_and_roles_getall";
        public static string Company_Notification_Getby_Filter = "SP_company_notification_getby_filter";
        public static string Company_Notification_Getby_Id = "SP_company_notification_getby_id";
        public static string Company_Files_Insupd = "sp_company_files_insupd";
        public static string Company_Notification_Insupd = "sp_company_notification_insupd";

        /*--------------  Company Service procedures ------------*/
        public static string Company_Get = "sp_company_get";
        public static string Company_GetById = "sp_company_getById";
        public static string Company_Intupd = "sp_company_intupd";
        public static string UserFiles_GetBy_OwnerId = "sp_userFiles_GetBy_OwnerId";
        public static string Organization_Detail_Get = "sp_organization_detail_get";
        public static string Organization_Intupd = "sp_organization_intupd";
        public static string Bank_Accounts_GetById = "sp_bank_accounts_getById";
        public static string Bank_Accounts_Intupd = "sp_bank_accounts_intupd";
        public static string Bank_Accounts_Getby_CmpId = "sp_bank_accounts_getby_cmpId";
        public static string Company_Setting_Get_Byid = "sp_company_setting_get_byid";
        public static string Company_Setting_Insupd = "sp_company_setting_insupd";
        public static string Company_Files_Get_Byid = "sp_company_files_get_byid";

        /*--------------  Dashboard Service procedures ------------*/
        public static string Dashboard_Get = "sp_dashboard_get";
        public static string AdminDashboard_Get = "sp_admin_dashboard_get";

        /*--------------  Declaration Service procedures ------------*/
        public static string Employee_Declaration_Get_ById = "sp_employee_declaration_get_byId";
        public static string Employee_Declaration_Get_ByEmployeeId = "sp_employee_declaration_get_byEmployeeId";
        public static string Employee_Declaration_Insupd = "sp_employee_declaration_insupd";
        public static string Tax_Regime_By_Id_Age = "sp_tax_regime_by_id_age";
        public static string Employee_Salary_Detail_InsUpd = "sp_employee_salary_detail_InsUpd";
        public static string Employee_Salary_Detail_Upd_On_Payroll_Run = "sp_employee_salary_detail_upd_on_payroll_run";
        public static string Employee_Taxregime_Update = "sp_employee_taxregime_update";
        public static string Employee_Declaration_Components_Get_ById = "sp_employee_declaration_components_get_byId";
        public static string Userfiledetail_Get_Files = "sp_userfiledetail_get_files";
        public static string Userdetail_Del_By_File_Id = "sp_userdetail_del_by_file_id";
        public static string Previous_Employement_And_Salary_Details_By_Empid = "sp_previous_employement_and_salary_details_by_empid";
        public static string Employee_Salary_Detail_Upd_Salarydetail = "sp_employee_salary_detail_upd_salarydetail";
        public static string Previous_Employement_Details_And_Emp_By_Empid = "sp_previous_employement_details_and_emp_by_empid";
        public static string Previous_Employement_Details_By_Empid = "sp_previous_employement_details_by_empid";
        public static string Declaration_Get_Filter_By_Empid = "sp_declaration_get_filter_by_empid";

        /*--------------  Email Service procedures ------------*/
        public static string Email_Template_Get = "sp_email_template_get";
        public static string Email_Setting_Detail_Get = "sp_email_setting_detail_get";
        public static string Email_Setting_Detail_By_CompanyId = "sp_email_setting_detail_by_companyId";
        public static string Email_Setting_Detail_Insupd = "sp_email_setting_detail_insupd";
        public static string Email_Template_Insupd = "sp_email_template_insupd";
        public static string Email_Template_Getby_Filter = "sp_email_template_getby_filter";
        public static string Email_Mapped_Template_GetById = "sp_email_mapped_template_getById";
        public static string Email_Mapped_Template_Insupd = "sp_email_mapped_template_insupd";
        public static string Email_Mapped_Template_By_Comid = "sp_email_mapped_template_by_comid";

        /*--------------  Employee Service procedures ------------*/
        public static string Employee_GetAllInActive = "SP_Employee_GetAllInActive";
        public static string Leave_Detail_Getby_EmployeeId = "sp_leave_detail_getby_employeeId";
        public static string Attandence_Detail_By_EmployeeId = "sp_attandence_detail_by_employeeId";
        public static string Manage_Employee_Detail_Get = "sp_manage_employee_detail_get";
        public static string MappedClients_Get = "SP_MappedClients_Get";
        public static string Employees_MappedClient_Get_By_Employee_Id = "sp_employees_mappedClient_get_by_employee_id";
        public static string Employees_Addupdate_Remote_Client = "sp_employees_addupdate_remote_client";
        public static string Employee_GetCompleteDetail = "sp_Employee_GetCompleteDetail";
        public static string Employee_GetArcheiveCompleteDetail = "sp_Employee_GetArcheiveCompleteDetail";
        public static string Employee_DeActivate = "sp_Employee_DeActivate";
        public static string Employee_Activate = "sp_Employee_Activate";
        public static string Employee_Getbyid_To_Reg_Or_Upd = "sp_employee_getbyid_to_reg_or_upd";
        public static string Employees_Ins_Upd = "sp_employees_ins_upd";
        public static string Employee_Delete_by_EmpId = "sp_employee_delete_by_EmpId";
        public static string Employees_Create = "sp_employees_create";
        public static string Employee_LastId = "sp_employee_lastId";
        public static string Annexure_Offer_Letter_Getby_Lettertype = "sp_annexure_offer_letter_getby_lettertype";
        public static string Employee_And_Declaration_Get_Byid = "sp_employee_and_declaration_get_byid";
        public static string Active_Employees_By_Ids = "sp_active_employees_by_ids";
        public static string EMPLOYEE_NOTICE_PERIOD_GETBY_EMPID = "sp_employee_notice_period_getby_empid";
        public static string EMPLOYEE_NOTICE_PERIOD_INSUPD = "sp_employee_notice_period_insupd";
        public static string EMPLOYEE_NOTICE_PERIOD_GETBY_ID = "sp_employee_notice_period_getby_id";
        public static string EMPLOYEE_PAYROLL_GET_BY_PAGE = "sp_employee_payroll_get_by_page";
        public static string EMPLOYEE_GETBYID_TO_REG_OR_UPD_BY_EXCEL = "sp_employee_getbyid_to_reg_or_upd_by_excel";
        public static string EMPLOYEE_REGISTRATION_COMMON_DATA = "sp_employee_registration_common_data";

        /*--------------  File Service procedures ------------*/
        public static string Document_Filedetail_Get = "sp_document_filedetail_get";

        /*--------------  Initial Registration Service procedures ------------*/
        public static string New_Registration = "sp_new_registration";

        /*--------------  Leave Request Service procedures ------------*/
        public static string Leave_Request_And_Notification_Update_Level = "sp_leave_request_and_notification_update_level";

        /*--------------  Login Service procedures ------------*/
        public static string UserDetail_GetByMobileOrEmail = "sp_UserDetail_GetByMobileOrEmail";

        /*--------------  Manage Leave Plan Service procedures ------------*/
        public static string Leave_Plans_Type_And_Workflow_ById = "sp_leave_plans_type_and_workflow_byId";
        public static string Leave_Detail_Insupd = "sp_leave_detail_insupd";
        public static string Leave_From_Management_Insupd = "sp_leave_from_management_insupd";
        public static string Leave_Accrual_InsUpdate = "sp_leave_accrual_InsUpdate";
        public static string Leave_Plans_GetbyId = "sp_leave_plans_getbyId";
        public static string Leave_Plan_Upd_Configuration = "sp_leave_plan_upd_configuration";
        public static string Leave_Apply_Detail_InsUpdate = "sp_leave_apply_detail_InsUpdate";
        public static string Leave_Plan_Restriction_Insupd = "sp_leave_plan_restriction_insupd";
        public static string Leave_Holidays_And_Weekoff_Insupd = "sp_leave_holidays_and_weekoff_insupd";
        public static string Leave_Approval_Insupd = "sp_leave_approval_insupd";
        public static string Leave_Endyear_Processing_Insupd = "sp_leave_endyear_processing_insupd";
        public static string Employee_Leaveplan_Upd = "sp_employee_leaveplan_upd";
        public static string Employee_Leaveplan_Mapping_GetByPlanId = "sp_employee_leaveplan_mapping_GetByPlanId";

        /*--------------  Manage User Comments Service procedures ------------*/
        public static string UserComments_INSUPD = "SP_UserComments_INSUPD";
        public static string UserComments_Get = "SP_UserComments_Get";

        /*--------------  Objective Service procedures ------------*/
        public static string Performance_Objective_Get_By_Id = "sp_performance_objective_get_by_id";
        public static string Performance_Objective_Insupd = "sp_performance_objective_insupd";
        public static string Performance_Objective_Getby_Filter = "sp_performance_objective_getby_filter";
        public static string Objective_Getby_Compid = "sp_objective_getby_compid";
        public static string Employee_Performance_Getby_Id = "sp_employee_performance_getby_id";
        public static string Employee_Performance_Insupd = "sp_employee_performance_insupd";

        /*--------------  Online Documents Service procedures ------------*/
        public static string OnlineDocument_InsUpd = "SP_OnlineDocument_InsUpd";
        public static string OnlineDocument_Get = "SP_OnlineDocument_Get";
        public static string OnlineDocument_With_Files_Get = "SP_OnlineDocument_With_Files_Get";
        public static string OnlieDocument_GetFiles = "sp_OnlieDocument_GetFiles";
        public static string OnlieDocument_Del_Multi = "sp_OnlieDocument_Del_Multi";
        public static string Files_InsUpd = "sp_Files_InsUpd";
        public static string Billdata_Get = "sp_billdata_get";
        public static string Billdetail_Filter = "sp_billdetail_filter";
        public static string ExistingBill_GetById = "sp_ExistingBill_GetById";
        public static string FileDetail_PatchRecord = "sp_FileDetail_PatchRecord";
        public static string ProfessionalCandidates_InsUpdate = "sp_ProfessionalCandidates_InsUpdate";
        public static string Professionalcandidates_Filter = "SP_professionalcandidates_filter";

        /*--------------  Product Service procedures ------------*/
        public static string Product_Getby_Filter = "SP_product_getby_filter";
        public static string Company_Files_Get_Byids_Json = "sp_company_files_get_byids_json";
        public static string Prdoduct_Getby_Id = "sp_prdoduct_getby_id";
        public static string Product_Insupd = "sp_product_insupd";
        public static string Catagory_Getby_Id = "sp_catagory_getby_id";
        public static string Catagory_Insupd = "sp_catagory_insupd";
        public static string Catagory_Getby_Filter = "sp_catagory_getby_filter";
        public static string PAYROLL_AND_SALARY_DETAIL_INSUPD = "sp_payroll_and_salary_detail_insupd";
        public static string HIKE_BONUS_SALARY_ADHOC_TAXDETAIL_INS_UPDATE = "sp_hike_bonus_salary_adhoc_taxdetail_ins_update";

        /*--------------  Project Service procedures ------------*/
        public static string Project_Detail_Getby_Id = "sp_project_detail_getby_id";
        public static string Wiki_Detail_Upd = "sp_wiki_detail_upd";
        public static string Project_Detail_Insupd = "sp_project_detail_insupd";
        public static string Project_Detail_Getall = "sp_project_detail_getall";
        public static string Project_Detail_Filter = "sp_project_detail_filter";
        public static string Project_Get_Page_Data = "sp_project_get_page_data";
        public static string Project_Member_Getby_Projectid = "sp_project_member_getby_projectid";
        public static string Team_Member_Upd = "sp_team_member_upd";

        /*--------------  Salary Component Service procedures ------------*/
        public static string Salary_Components_Get = "sp_salary_components_get";
        public static string Salary_Group_GetbyCompanyId = "sp_salary_group_getbyCompanyId";
        public static string Salary_Components_Insupd = "sp_salary_components_insupd";
        public static string Salary_Group_Get_If_Exists = "sp_salary_group_get_if_exists";
        public static string Salary_Group_Get_Initial_Components = "sp_salary_group_get_initial_components";
        public static string Salary_Group_Insupd = "sp_salary_group_insupd";
        public static string Salary_Group_GetAll = "sp_salary_group_getAll";
        public static string Adhoc_Detail_Get = "sp_adhoc_detail_get";
        public static string Salary_Group_Get_By_Id_Or_Ctc = "sp_salary_group_get_by_id_or_ctc";
        public static string Salary_Group_Get_By_Ctc = "sp_salary_group_get_by_ctc";
        public static string Employee_Salary_Detail_Get_By_Empid = "sp_employee_salary_detail_get_by_empid";
        public static string Pf_Esi_Setting_Get = "sp_pf_esi_setting_get";
        public static string Employee_Salary_Detail_GetbyFilter = "sp_employee_salary_detail_getbyFilter";
        public static string Salary_Components_Group_By_Employeeid = "sp_salary_components_group_by_employeeid";
        public static string HIKE_BONUS_SALARY_ADHOC_INS_UPDATE = "sp_hike_bonus_salary_adhoc_ins_update";
        public static string Payroll_Cycle_Setting_Get_All = "sp_payroll_cycle_setting_get_all";
        public static string SALARY_GROUP_AND_COMPONENTS_GET = "sp_salary_group_and_components_get";

        /*--------------  Service Request Service procedures ------------*/
        public static string Service_Request_Filter = "sp_service_request_filter";
        public static string Service_Request_Sel_By_Id = "sp_service_request_sel_by_id";
        public static string Service_Request_Ins_Upd = "sp_service_request_ins_upd";

        /*--------------  Setting Service Service procedures ------------*/
        public static string Pf_Esi_Setting_Insupd = "sp_pf_esi_setting_insupd";
        public static string Employee_Salary_Detail_Get = "sp_employee_salary_detail_get";
        public static string Organization_Setting_Get = "sp_organization_setting_get";
        public static string Bank_Accounts_Get_By_OrgId = "sp_bank_accounts_get_by_orgId";
        public static string Payroll_Cycle_Setting_GetById = "sp_payroll_cycle_setting_getById";
        public static string Payroll_Cycle_Setting_Intupd = "sp_payroll_cycle_setting_intupd";
        public static string Employee_Salary_Detail_Get_By_Groupid = "sp_employee_salary_detail_get_by_groupid";
        public static string Salary_Components_Get_Type = "sp_salary_components_get_type";
        public static string User_Layout_Configuration_Ins_Upt = "sp_user_layout_configuration_ins_upt";

        /*--------------  Shift Service procedures ------------*/
        public static string Work_Shifts_Filter = "sp_work_shifts_filter";
        public static string Work_Shifts_Getby_Id = "sp_work_shifts_getby_id";
        public static string Work_Shifts_Insupd = "sp_work_shifts_insupd";

        /*--------------  Tax Regime Service procedures ------------*/
        public static string Tax_Regime_Desc_GetbyId = "sp_tax_regime_desc_getbyId";
        public static string Tax_Regime_Desc_Insupd = "sp_tax_regime_desc_insupd";
        public static string Tax_Regime_Desc_Getall = "sp_tax_regime_desc_getall";
        public static string Tax_Age_Group_Getby_Id = "sp_tax_age_group_getby_id";
        public static string Tax_Age_Group_Insupd = "sp_tax_age_group_insupd";
        public static string Tax_Regime_Getall = "sp_tax_regime_getall";
        public static string Tax_Regime_Insupd = "sp_tax_regime_insupd";
        public static string Tax_Regime_Delete_Byid = "sp_tax_regime_delete_byid";
        public static string Ptax_Slab_Getby_CompId = "sp_ptax_slab_getby_compId";
        public static string Ptax_Slab_Insupd = "sp_ptax_slab_insupd";
        public static string Ptax_Slab_Delete_Byid = "sp_ptax_slab_delete_byid";
        public static string Surcharge_Slab_Getall = "sp_surcharge_slab_getall";
        public static string Surcharge_Slab_Insupd = "sp_surcharge_slab_insupd";
        public static string Surcharge_Slab_Delete_Byid = "sp_surcharge_slab_delete_byid";

        /*--------------  Timesheet Request Service procedures ------------*/
        public static string Employee_Timesheet_Getby_Timesheetid = "sp_employee_timesheet_getby_timesheetid";
        public static string Timesheet_Upd_By_Id = "sp_timesheet_upd_by_id";
        public static string Timesheet_Requests_By_Filter = "sp_timesheet_requests_by_filter";

        /*--------------  User Service procedures ------------*/
        public static string Professionaldetail_Insupd = "sp_professionaldetail_insupd";
        public static string Professionaldetail_Get_Byid = "sp_professionaldetail_get_byid";
        public static string Professionaldetail_Filter = "sp_professionaldetail_filter";
        public static string Employee_And_All_Clients_Get = "sp_employee_and_all_clients_get";

        /*--------------  CronJobSetting Service procedures ------------*/
        public static string APPLICATION_SETTING_INSUPD = "sp_application_setting_insupd";
        public static string APPLICATION_SETTING_GET_BY_COMPID = "sp_application_setting_get_by_compid";

        /*--------------  Home Page Service Procedure -----------------------*/
        public static string CONTACT_US_INSUPD = "sp_contact_us_insupd";
        public static string TRAIL_REQUEST_INSUPD = "sp_trail_request_insupd";
        public static string TRAIL_REQUEST_GETBY_EMAIL_PHONE = "sp_trail_request_getby_email_phone";

        /*--------------  Timesheet Service procedures ------------*/
        public static string Employee_Timesheet_Get = "sp_employee_timesheet_get";
        public static string Employee_Timesheet_Getby_Empid = "sp_employee_timesheet_getby_empid";
        public static string EmployeeBillDetail_ById = "sp_EmployeeBillDetail_ById";
        public static string EMPLOYEE_TIMESHEET_FILTER = "sp_employee_timesheet_filter";
        public static string TIMESHEET_RUNWEEKLY_DATA = "sp_timesheet_runweekly_data";
        public static string EMPLOYEE_TIMESHEET_SHIFT_GETBY_TIMESHEETID = "sp_employee_timesheet_shift_getby_timesheetId";
        public static string WORK_SHIFTS_BY_CLIENTID = "sp_work_shifts_by_clientId";
        public static string TIMESHEET_INSUPD = "sp_timesheet_insupd";
    }
}