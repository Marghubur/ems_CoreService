using System;

namespace ModalLayer.Modal
{
    public class GstStatusModel
    {
        public long GstId { set; get; }
        public string Billno { set; get; }
        public int Gststatus { set; get; }
        public DateTime Paidon { set; get; }
        public long Paidby { set; get; }
        public double Amount { set; get; }
        public long FileId { set; get; }
    }
}
