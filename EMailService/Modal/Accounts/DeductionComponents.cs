using System;

namespace ModalLayer.Modal.Accounts
{
    public class DeductionComponents
    {
        public long DeductionId { set; get; }
        public string DeductionDescription { set; get; }
        public bool IsPaidByEmployee { set; get; }
        public bool IsPaidByEmployeer { set; get; }
        public bool IsMandatory { set; get; }
        public bool IsFixedAmount { set; get; }
        public DateTime UpdatedOn { set; get; }
        public long AdminId { set; get; }
    }
}
