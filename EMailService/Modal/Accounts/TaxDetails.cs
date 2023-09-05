namespace ModalLayer.Modal.Accounts
{
    public class TaxDetails
    {
        public int Index { set; get; }
        public long EmployeeId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TaxDeducted { get; set; }
        public bool IsPayrollCompleted { set; get; } = false;
        public decimal TaxPaid { get; set; }
    }
}
