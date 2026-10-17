using System.Threading;
using System.Threading.Tasks;
using Application.Common.Models.Payments;
using Domain.Enums;

namespace Application.Common.Interfaces
{
    /// <summary>
    /// Strategy Interface (GoF Strategy Pattern):
    /// Declares operations common to all supported payment gateway providers (Stripe, CardNet, etc.).
    /// </summary>
    public interface IPaymentStrategy
    {
        PaymentProvider Provider { get; }

        Task<PaymentCustomerDto> CreateOrGetCustomerAsync(
            string customerRefId,
            string email,
            string? phone = null,
            CancellationToken cancellationToken = default);

        Task<PaymentCheckoutSessionDto> CreateCheckoutSessionAsync(
            CreateCheckoutRequest request,
            CancellationToken cancellationToken = default);

        Task<PaymentProfileDto> RegisterPaymentMethodAsync(
            RegisterPaymentMethodRequest request,
            CancellationToken cancellationToken = default);

        Task<PaymentChargeResultDto> ChargeAsync(
            PaymentChargeRequest request,
            CancellationToken cancellationToken = default);

        Task<PaymentRefundResultDto> RefundAsync(
            PaymentRefundRequest request,
            CancellationToken cancellationToken = default);

        Task<PaymentWebhookResultDto> ProcessWebhookAsync(
            string payload,
            string signatureHeader,
            CancellationToken cancellationToken = default);
    }
}
