namespace ModalLayer.Modal
{
    public class TimesheetModel
    {
        public string Date { set; get; }
        public string ResourceName { set; get; }
        public string StartTime { set; get; }
        public string EndTime { set; get; }
        public double TotalHrs { set; get; }
        public string Comments { set; get; }
        public string Status { set; get; }
    }
}
