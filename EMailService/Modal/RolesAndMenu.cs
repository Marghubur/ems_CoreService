using System;
using System.Collections.Generic;
using System.Text;

namespace ModalLayer.Modal
{
    public class RolesAndMenu
    {
        public int AccessLevelId { get; set; }
        public List<MenuAndPermission> Menu { get; set; }
    }
    public class MenuAndPermission
    {
        public string Catagory { set; get; }
        public string Childs { set; get; }
        public string Link { set; get; }
        public string Icon { set; get; }
        public string Badge { set; get; }
        public string BadgeType { set; get; }
        public int AccessCode { set; get; }
        public int Permission { set; get; }
        public string ParentMenu { get; set; }
    }

    public class AddRole
    {
        public string RoleName { get; set; }
        public string AccessCodeDefination { get; set; }
    }
}
