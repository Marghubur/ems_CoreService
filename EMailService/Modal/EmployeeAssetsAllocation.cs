using System;
using System.Collections.Generic;

namespace EMailService.Modal
{
    public class EmployeeAssetsAllocation
    {
        public long EmployeeAssetsAllocationId { get; set; }
        public long EmployeeId { get; set; }
        public string AssetsName { get; set; }
        public string AssetsDetail { get; set; }
        public DateTime AllocatedOn { get; set; }
        public long AllocatedBy { get; set; }
        public bool ReturnStatus { get; set; }
        public DateTime ReturnedOn { get; set; }
        public string CommentsOnReturnedItem { get; set; }
        public long ReturnedHandledBy { get; set; }
        public string ReturnHandleByName { get; set; }
        public string AllocatedByName { get; set; }
        public List<KeyValuePair<string, string>> AssetDetails { get; set; }
    }
}
