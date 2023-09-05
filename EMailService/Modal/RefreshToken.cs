using System;
using System.Collections.Generic;
using System.Text;

namespace ModalLayer.Modal
{
    public class RefreshTokenModal
    {
        public string Token { set; get; }
        public string RefreshToken { set; get; }
        public DateTime Expires { set; get; }
        public DateTime Created { set; get; }
        public string CreatedByIp { set; get; }
    }
}
