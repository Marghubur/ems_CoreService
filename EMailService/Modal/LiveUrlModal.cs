using System;
using System.Collections.Generic;
using System.Text;

namespace ModalLayer.Modal
{
    public class LiveUrlModal
    {
        public long savedUrlId { set; get; }
        public string method { get; set; }
        public string paramters { get; set; }
        public string url { get; set; }
        public string lastUsed { get; set; }
    }
}
