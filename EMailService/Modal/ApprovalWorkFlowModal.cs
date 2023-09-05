using System;
using System.Collections.Generic;

namespace ModalLayer.Modal
{
    public class ApprovalWorkFlowChain : ApprovalWorkFlowModal
    {
        public List<ApprovalChainDetail> ApprovalChainDetails { set; get; }
    }

    public class ApprovalWorkFlowChainFilter : ApprovalChainDetail
    {
        public string Title { set; get; }
        public string TitleDescription { set; get; }
        public int Status { set; get; } = 2;
        public bool IsAutoExpiredEnabled { set; get; } = true;
        public int AutoExpireAfterDays { set; get; }
        public bool IsSilentListner { set; get; }
        public string ListnerDetail { set; get; }
        public long CreatedBy { set; get; }
        public long UpdatedBy { set; get; }
        public DateTime CreatedOn { set; get; }
        public DateTime? UpdatedOn { set; get; }
    }

    public class ApprovalWorkFlowModal
    {
        public int ApprovalChainDetailId { set; get; }
        public int ApprovalWorkFlowId { set; get; } 
        public string Title { set; get; }
        public string TitleDescription { set; get; }
        public int Status { set; get; } = 2;
        public bool IsAutoExpiredEnabled { set; get; } = true;
        public int AutoExpireAfterDays { set; get; }
        public bool IsSilentListner { set; get; }
        public string ListnerDetail { set; get; }
        public long CreatedBy { set; get; }
        public long UpdatedBy { set; get; }
        public DateTime CreatedOn { set; get; }
        public DateTime? UpdatedOn { set; get; }
        public int Index { get; set; }
        public int Total { get; set; }
    }
}
