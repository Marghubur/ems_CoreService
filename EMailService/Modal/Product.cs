using System;
using System.Collections.Generic;

namespace ModalLayer.Modal
{
    public class Product : CreationInfo
    {
        public int Total { get; set; }
        public int RowIndex { get; set; }
        public long ProductId { get; set; }
        public int CompanyId { get; set; }
        public string CatagoryName { get; set; }
        public int Status { get; set; }
        public string Description { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string SerialNo { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public string InvoiceNo { get; set; }
        public decimal OrignalValue { get; set; }
        public decimal CurrentValue { get; set; }
        public bool IsWarranty { get; set; }
        public DateTime? WarrantyDate { get; set; }
        public string Remarks { get; set; }
        public string FileIds { get; set; }
        public string ProfileImgPath { get; set; }
        public List<PairData>? ProductDetails { get; set; }
        public string ProductDetail { get; set; }
    }

    public class PairData
    {
        public string Key { get; set; }
        public object Value { get; set; }
    }
}
