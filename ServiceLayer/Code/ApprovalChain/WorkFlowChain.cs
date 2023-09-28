using BottomhalfCore.DatabaseLayer.Common.Code;

namespace ServiceLayer.Code.ApprovalChain
{

    public class WorkFlowChain
    {
        private readonly IDb _db;

        public WorkFlowChain(IDb db)
        {
            _db = db;
        }

    }
}
