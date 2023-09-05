using System;

namespace ModalLayer.Modal
{
    public class EmployeeOfferLetter
    {
        public string FirstName { set; get; }
        public string LastName { set; get; }
        public string Email { set; get; }
        public string Designation { set; get; }
        public int CTC { get; set; }
        public new int CompanyId { set; get; }
        public DateTime JoiningDate { get; set; }
    }
}
