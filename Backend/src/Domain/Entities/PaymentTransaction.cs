using System;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    public class PaymentTransaction : BaseEntity
    {
        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;
        public PaymentProvider Provider { get; set; }
        public string? ProviderTransactionId { get; set; }
        public string? AuthorizationCode { get; set; }
        public string? OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Pending;
        public string? Description { get; set; }
        public string? ResponseCode { get; set; }
        public string? ResponseMessage { get; set; }
        public string? IdempotencyKey { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
