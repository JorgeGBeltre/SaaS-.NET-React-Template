using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    public class PaymentCustomer : BaseEntity
    {
        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;
        public PaymentProvider Provider { get; set; } = PaymentProvider.Stripe;
        public string ProviderCustomerId { get; set; } = string.Empty;
        public string? PaymentProfileId { get; set; }
        public string? PaymentToken { get; set; }
        public string? CardBrand { get; set; }
        public string? CardLast4 { get; set; }
        public int? ExpirationMonth { get; set; }
        public int? ExpirationYear { get; set; }
        public string? SubscriptionId { get; set; }
        public string SubscriptionStatus { get; set; } = "inactive"; // active, trialing, past_due, canceled, inactive
        public string? CurrentPlanSlug { get; set; }
        public bool IsDefault { get; set; } = true;
    }
}
