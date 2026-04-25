using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class StripeCustomer
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;
        public string StripeCustomerId { get; set; } = string.Empty;
        public string StripeSubscriptionId { get; set; } = string.Empty;
        public string SubscriptionStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
