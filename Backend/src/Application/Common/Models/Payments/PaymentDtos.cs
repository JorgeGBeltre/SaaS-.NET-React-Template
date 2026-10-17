using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Application.Common.Models.Payments
{
    public sealed record PaymentCustomerDto(
        string CustomerId,
        string Email,
        string? Phone,
        PaymentProvider Provider,
        List<PaymentProfileDto>? PaymentProfiles = null
    );

    public sealed record PaymentCheckoutSessionDto(
        string SessionId,
        string CheckoutUrl,
        PaymentProvider Provider,
        string? PublicKey = null,
        string? UniqueId = null,
        Dictionary<string, string>? Metadata = null
    );

    public sealed record PaymentProfileDto(
        string PaymentProfileId,
        string Token,
        string Brand,
        string Last4,
        int ExpirationMonth,
        int ExpirationYear,
        bool IsDefault,
        PaymentProvider Provider
    );

    public sealed record PaymentChargeResultDto(
        bool IsSuccess,
        string ResponseCode,
        string ResponseMessage,
        string? TransactionId,
        string? AuthorizationCode,
        decimal Amount,
        string Currency,
        PaymentProvider Provider,
        DateTime Timestamp
    );

    public sealed record PaymentRefundResultDto(
        bool IsSuccess,
        string ResponseCode,
        string ResponseMessage,
        string? TransactionId,
        string? AuthorizationCode,
        decimal Amount,
        string Currency,
        PaymentProvider Provider,
        DateTime Timestamp
    );

    public sealed record PaymentWebhookResultDto(
        bool Handled,
        string EventType,
        string? CustomerId,
        string? SubscriptionId,
        string? Status,
        string? Message
    );

    public sealed record CreateCheckoutRequest(
        int UserId,
        string Email,
        string PlanSlug,
        string Interval,
        decimal Amount,
        string Currency,
        string SuccessUrl,
        string CancelUrl,
        PaymentProvider Provider = PaymentProvider.Stripe
    );

    public sealed record RegisterPaymentMethodRequest(
        int UserId,
        string Email,
        string TokenOrPaymentMethodId,
        string? UniqueId = null,
        PaymentProvider Provider = PaymentProvider.CardNet
    );

    public sealed record PaymentChargeRequest(
        int UserId,
        string CustomerId,
        string PaymentToken,
        decimal Amount,
        string Currency,
        string OrderId,
        string Description,
        string? IdempotencyKey = null,
        PaymentProvider Provider = PaymentProvider.Stripe
    );

    public sealed record PaymentRefundRequest(
        string CustomerId,
        string OriginalTransactionId,
        decimal Amount,
        string Currency,
        string OrderId,
        string? Reason = null,
        string? IdempotencyKey = null,
        PaymentProvider Provider = PaymentProvider.Stripe
    );

    public sealed record PaymentProviderInfo(
        PaymentProvider Provider,
        string Name,
        string DisplayName,
        string SupportedCurrency,
        bool IsDefault,
        string? PublicKey,
        string? Mode
    );
}
