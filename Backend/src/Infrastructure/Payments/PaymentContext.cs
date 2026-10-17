using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Common.Models.Payments;
using Domain.Enums;

namespace Infrastructure.Payments
{
    /// <summary>
    /// GoF Strategy Pattern Context:
    /// Maintains a reference to one of the IPaymentStrategy objects.
    /// Can switch strategy at runtime via SetStrategy and delegates execution to it.
    /// </summary>
    public sealed class PaymentContext : IPaymentContext
    {
        private readonly IPaymentStrategyResolver _resolver;
        private IPaymentStrategy _strategy;

        public IPaymentStrategy CurrentStrategy => _strategy;

        public PaymentContext(IPaymentStrategyResolver resolver)
        {
            _resolver = resolver;
            _strategy = _resolver.ResolveDefault();
        }

        public void SetStrategy(PaymentProvider provider)
        {
            _strategy = _resolver.Resolve(provider);
        }

        public void SetStrategy(IPaymentStrategy strategy)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        }

        public Task<PaymentCheckoutSessionDto> ExecuteCheckoutAsync(
            CreateCheckoutRequest request,
            CancellationToken cancellationToken = default)
        {
            return _strategy.CreateCheckoutSessionAsync(request, cancellationToken);
        }

        public Task<PaymentProfileDto> ExecuteRegisterPaymentMethodAsync(
            RegisterPaymentMethodRequest request,
            CancellationToken cancellationToken = default)
        {
            return _strategy.RegisterPaymentMethodAsync(request, cancellationToken);
        }

        public Task<PaymentChargeResultDto> ExecuteChargeAsync(
            PaymentChargeRequest request,
            CancellationToken cancellationToken = default)
        {
            return _strategy.ChargeAsync(request, cancellationToken);
        }

        public Task<PaymentRefundResultDto> ExecuteRefundAsync(
            PaymentRefundRequest request,
            CancellationToken cancellationToken = default)
        {
            return _strategy.RefundAsync(request, cancellationToken);
        }
    }
}
