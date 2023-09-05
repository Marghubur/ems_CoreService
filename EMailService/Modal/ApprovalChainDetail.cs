using System;

namespace ModalLayer.Modal
{
    public class ApprovalChainDetail
    {
        public int ApprovalChainDetailId { set; get; }
        public int ApprovalWorkFlowId { set; get; }
        public long AssignieId { set; get; }
        public string AssignieeEmail { set; get; }
        public bool IsRequired { set; get; }
        public bool IsForwardEnabled { set; get; }
        public int ForwardWhen { set; get; }
        public int ForwardAfterDays { set; get; }
        public DateTime LastUpdatedOn { set; get; }
        public int ApprovalStatus { set; get; } = 2;
        public int AutoExpireAfterDays { set; get; }
    }
}
