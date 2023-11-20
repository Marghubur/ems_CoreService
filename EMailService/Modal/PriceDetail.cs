using System.Collections.Generic;

namespace EMailService.Modal
{
    public class PriceDetail
    {
        public string PlanName { get; set; }
        public string PlanSpecification { get; set; }
        public string Description { get; set; }
        public string Price { get; set; }
        public string EmployeeLimit { get; set; }
        public string AdditionalPriceDetail { get; set; }
        public string AdditionalPrice { get; set; }
        public List<Feature> Features{ get; set; }
    }

    public class Feature
    {
        public string FeatureName { get; set; }
        public List<string> FeatureDetail { get; set; }
    }
}
