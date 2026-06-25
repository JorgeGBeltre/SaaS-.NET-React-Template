using System.Collections.Generic;
using Domain.Common;

namespace Domain.Entities
{
    public class SubscriptionPlan : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Interval { get; set; } = "monthly"; // monthly, yearly
        public List<string> Features { get; set; } = [];
        public bool IsActive { get; set; } = true;
    }
}
