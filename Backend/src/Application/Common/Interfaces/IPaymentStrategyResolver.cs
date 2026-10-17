using System.Collections.Generic;
using Application.Common.Models.Payments;
using Domain.Enums;

namespace Application.Common.Interfaces
{
    /// <summary>
    /// Strategy Factory / Resolver:
    /// Resolves the appropriate concrete IPaymentStrategy implementation dynamically based on provider or configuration.
    /// </summary>
    public interface IPaymentStrategyResolver
    {
        IPaymentStrategy Resolve(PaymentProvider provider);
        IPaymentStrategy ResolveDefault();
        IReadOnlyCollection<IPaymentStrategy> GetAllStrategies();
        IReadOnlyCollection<PaymentProviderInfo> GetProviderInfos();
    }
}
