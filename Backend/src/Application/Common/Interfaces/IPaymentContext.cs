using System.Threading;
using System.Threading.Tasks;
using Application.Common.Models.Payments;
using Domain.Enums;

namespace Application.Common.Interfaces
{
    /// <summary>
    /// Context (GoF Strategy Pattern):
    /// The Context maintains a reference to one of the IPaymentStrategy objects.
    /// It delegates payment operations to the active strategy object instead of implementing multiple versions.
    /// </summary>
    public interface IPaymentContext
    {
        IPaymentStrategy CurrentStrategy { get; }
        void SetStrategy(PaymentProvider provider);
        void SetStrategy(IPaymentStrategy strategy);

        Task<PaymentCheckoutSessionDto> ExecuteCheckoutAsync(
            CreateCheckoutRequest request,
            CancellationToken cancellationToken = default);

        Task<PaymentProfileDto> ExecuteRegisterPaymentMethodAsync(
            RegisterPaymentMethodRequest request,
            CancellationToken cancellationToken = default);

        Task<PaymentChargeResultDto> ExecuteChargeAsync(
            PaymentChargeRequest request,
            CancellationToken cancellationToken = default);

        Task<PaymentRefundResultDto> ExecuteRefundAsync(
            PaymentRefundRequest request,
            CancellationToken cancellationToken = default);
    }
}
