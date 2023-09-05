namespace Bot.CoreBottomHalf.Modal.Leaves
{
    public class LeavePlanTypeBrief
    {
        public int LeavePlanTypeId { get; set; }

        public int LeavePlanId { get; set; }

        public string LeavePlanCode { get; set; }

        public string PlanName { get; set; }

        public string PlanDescription { get; set; }

        public bool ShowDescription { get; set; }
    }
}
