using System;
using System.Collections.Generic;
using System.Text;

namespace ModalLayer.Modal
{
    public class TokenModal
    {
        public long UserId { set; get; }
        public string RefreshToken { set; get; }
        public DateTime ExpiryTime { set; get; }
    }
}
