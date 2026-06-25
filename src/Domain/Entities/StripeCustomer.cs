using Domain.Common;

namespace Domain.Entities
{
    public class StripeCustomer : BaseEntity
    {
        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;
        public string StripeCustomerId { get; set; } = string.Empty;
        public string StripeSubscriptionId { get; set; } = string.Empty;
        public string SubscriptionStatus { get; set; } = string.Empty;
    }
}
