using System;
using System.Collections.Generic;
using System.Text;

namespace ModalLayer.Modal
{
    public class JwtSetting
    {
        public string Key { set; get; }
        public string Issuer { get; set; }
        public long AccessTokenExpiryTimeInSeconds { set; get; }
        public long RefreshTokenExpiryTimeInSeconds { set; get; }
    }
}
