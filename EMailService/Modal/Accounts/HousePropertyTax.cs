namespace ModalLayer.Modal.Accounts
{
    public class HousingDeclartion
    {
        public long EmployeeId { set; get; }
        public HousePropertyTax HousePropertyDetail { set; get; }
        public string ComponentId { set; get; }
        public string Email { set; get; }
    }
    public class HousePropertyTax
    {
        public string RentedFrom { get; set; }
        public string RentedTo { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public decimal TotalRent { get; set; }
        public string OwnerName { get; set; }
        public string PANNo { get; set; }
        public string OwnerAddress { get; set; }
        public bool IsPANNo { get; set; }
        public bool IsOwnerAddressSame { get; set; }
        public string LandlordType { get; set; }
        public bool IsSignedDeclaration { get; set; }
    }
}
