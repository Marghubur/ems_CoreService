using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModalLayer.Modal
{
    public class AuthUser
    {
        public string MobileNo { set; get; }
        public string Email { set; get; }
        public string UserId { set; get; }
        public string Password { set; get; } = null;
        public string SessionToken { set; get; }
    }
}
