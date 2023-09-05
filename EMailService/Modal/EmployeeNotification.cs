using System;
using System.Collections.Generic;
using System.Text;

namespace ModalLayer.Modal
{
    public class EmployeeNotification
    {
        public long Notification { get; set; }
        public string Message { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public int UserTypeId { get; set; }
        public DateTime RequestedOn { get; set; }
        public long AssigneeId { get; set; }
        public int Status { get; set; }
        public DateTime ActionTakenOn { get; set; }
        public ItemStatus RequestTypeId { set; get; }
    }
}
