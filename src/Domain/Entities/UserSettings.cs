using System;
using Domain.Common;

namespace Domain.Entities
{
    public class UserSettings : BaseEntity
    {
        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;
        public bool NotifyComments { get; set; }
        public bool NotifyUpdates { get; set; }
        public bool NotifyMarketing { get; set; }
        public string ApiKey { get; set; } = string.Empty;
        public DateTime? ApiKeyCreatedAt { get; set; }
        public int? SubscriptionPlanId { get; set; }
        public SubscriptionPlan? SubscriptionPlan { get; set; }
        public string SubscriptionStatus { get; set; } = "inactive";
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public DateTime? TrialEndDate { get; set; }
    }
}
