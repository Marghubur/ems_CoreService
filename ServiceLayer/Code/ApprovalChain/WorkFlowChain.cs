using BottomhalfCore.DatabaseLayer.Common.Code;
using ModalLayer.Modal;
using ModalLayer.Modal.Leaves;
using Newtonsoft.Json;
using System.Linq;

namespace ServiceLayer.Code.ApprovalChain
{

    public class WorkFlowChain
    {
        private readonly IDb _db;

        public WorkFlowChain(IDb db)
        {
            _db = db;
        }

        public long GetNextRequestor(LeavePlanType leavePlanType, CompleteLeaveDetail completeLeaveDetail, long employeeId)
        {
            long assigneId = 0;
            if (completeLeaveDetail.RequestChain != null && completeLeaveDetail.RequestChain.Count > 0)
            {
                if (employeeId == 0)
                {
                    var nextRequestChain = completeLeaveDetail.RequestChain.First();
                    assigneId = nextRequestChain.ExecuterId;
                }
                else
                {
                    var present = completeLeaveDetail.RequestChain.OrderBy(x => x.Level).Where(x => x.ExecuterId == employeeId).FirstOrDefault();
                    if (present != null)
                    {
                        var nextRequestChain = completeLeaveDetail.RequestChain.Find(x => x.Level == present.Level + 1);
                        if (nextRequestChain != null)
                            assigneId = nextRequestChain.ExecuterId;
                    }

                    //var flag = IsForwardToNextApprover(leavePlanType, assigneId);
                    //if (!flag)
                    //    assigneId = 0;
                }
            }

            return assigneId;
        }

        private bool IsForwardToNextApprover(LeavePlanType leavePlanType, long assigneeId)
        {
            bool flag = true;
            var configuration = JsonConvert.DeserializeObject<LeavePlanConfiguration>(leavePlanType.PlanConfigurationDetail);
            if (configuration == null)
                throw HiringBellException.ThrowBadRequest("Fail to get leave plan configuration detail. Please contact to admin");

            var chainDetails = _db.GetList<ApprovalChainDetail>("", new { configuration.leaveApproval.ApprovalWorkFlowId });
            if (chainDetails == null)
                throw HiringBellException.ThrowBadRequest("Fail to get approval work flow detail. Please contact to admin");

            var approvalChainDetail = chainDetails.Find(x => x.AssignieId == assigneeId);

            // if approved
            //if (approvalChainDetail.IsRequired)
            //{
            //    if (approvalChainDetail.ForwardWhen == (int)ItemStatus.Pending)
            //        flag = false;
            //}

            return flag;
        }
    }
}
