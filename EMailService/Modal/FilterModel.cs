namespace ModalLayer.Modal
{
    public class FilterModel
    {
        public bool? IsActive { get; set; } = true;
        public string SearchString { get; set; } = " 1=1 ";
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public int OffsetIndex { get; set; }
        public long EmployeeId { get; set; }
        public long RecordId { get; set; }
        public long ClientId { get; set; }
    }
}
