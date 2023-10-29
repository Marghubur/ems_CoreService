namespace EMailService.Modal
{
    public class Procedures
    {
        /*------------- Attendance service proceudures -------*/
        public static string Attendance_Requests_By_Filter = "sp_attendance_requests_by_filter";
        public static string Attendance_Update_Request = "sp_attendance_update_request";

        /*-------------- Leave service proceudures ------------*/
        public static string Leave_Notification_And_Request_InsUpdate = "sp_leave_notification_and_request_InsUpdate";
        public static string Employee_Leave_Level_Migration = "sp_employee_leave_level_migration";
        public static string Leave_Requests_By_Filter = "sp_leave_requests_by_filter";
    }
}
