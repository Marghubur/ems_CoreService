using System;
using System.Collections.Generic;
using System.Text;

namespace ModalLayer.Modal
{
    public class TaxRegime
    {
        public int TaxRegimeId { get; set; }
        public int RegimeIndex { get; set; }
        public int RegimeDescId { get; set; }
        public int StartAgeGroup { get; set; }
        public int EndAgeGroup { get; set; }
        public decimal MinTaxSlab { get; set; }
        public decimal MaxTaxSlab { get; set; }
        public int TaxRatePercentage { get; set; }
        public decimal TaxAmount { get; set; }
    }
}
