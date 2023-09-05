using System;

namespace ModalLayer.Modal
{
    public class Product:CreationInfo
    {
        public int Total {get; set;}
        public int Index {get; set;}
        public DateTime OrderDate {get; set;}
        public int StockStatus {get; set;}
        public int Quantity {get; set;}
        public string ModalNum {get; set;}
        public string SiteUrl {get; set;}
        public decimal MRP {get; set;}
        public string FileIds { get; set; }
        public long ProductId { get; set; }
        public int CompanyId { get; set; }
        public string CatagoryName { get; set; }
        public string Brand { get; set; }
        public string TitleName { get; set; }
        public string SerialNo { get; set; }
        public string ProductCode { get; set; }
        public decimal PurchasePrice { get; set; }
    }
}
