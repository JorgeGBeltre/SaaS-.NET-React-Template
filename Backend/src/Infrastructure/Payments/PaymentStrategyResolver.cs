using System;
using System.Collections.Generic;
using System.Linq;
using Application.Common.Interfaces;
using Application.Common.Models.Payments;
using Domain.Enums;
using Infrastructure.Payments.CardNet;
using Infrastructure.Payments.Stripe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Payments
{
    /// <summary>
    /// Factory / Resolver for Payment Strategies:
    /// Injects all registered IPaymentStrategy implementations from DI container
    /// and resolves the correct one based on the requested PaymentProvider.
    /// </summary>
    public sealed class PaymentStrategyResolver : IPaymentStrategyResolver
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly StripeOptions _stripeOptions;
        private readonly CardNetOptions _cardNetOptions;

        public PaymentStrategyResolver(
            IServiceProvider serviceProvider,
            IOptions<StripeOptions> stripeOptions,
            IOptions<CardNetOptions> cardNetOptions)
        {
            _serviceProvider = serviceProvider;
            _stripeOptions = stripeOptions.Value;
            _cardNetOptions = cardNetOptions.Value;
        }

        public IPaymentStrategy Resolve(PaymentProvider provider)
        {
            var strategies = _serviceProvider.GetServices<IPaymentStrategy>();
            var strategy = strategies.FirstOrDefault(s => s.Provider == provider);

            if (strategy == null)
            {
                throw new NotSupportedException($"Payment strategy for provider '{provider}' is not registered or supported.");
            }

            return strategy;
        }

        public IPaymentStrategy ResolveDefault()
        {
            // Default to Stripe, or CardNet if Stripe secret is not configured
            var strategies = _serviceProvider.GetServices<IPaymentStrategy>();
            var stripeStrategy = strategies.FirstOrDefault(s => s.Provider == PaymentProvider.Stripe);
            var cardNetStrategy = strategies.FirstOrDefault(s => s.Provider == PaymentProvider.CardNet);

            return stripeStrategy ?? cardNetStrategy ?? throw new InvalidOperationException("No payment strategies registered.");
        }

        public IReadOnlyCollection<IPaymentStrategy> GetAllStrategies()
        {
            return _serviceProvider.GetServices<IPaymentStrategy>().ToList();
        }

        public IReadOnlyCollection<PaymentProviderInfo> GetProviderInfos()
        {
            return new List<PaymentProviderInfo>
            {
                new PaymentProviderInfo(
                    Provider: PaymentProvider.Stripe,
                    Name: "Stripe",
                    DisplayName: "Credit / Debit Card (Stripe)",
                    SupportedCurrency: "USD",
                    IsDefault: true,
                    PublicKey: _stripeOptions.PublishableKey,
                    Mode: "International"
                ),
                new PaymentProviderInfo(
                    Provider: PaymentProvider.CardNet,
                    Name: "CardNet",
                    DisplayName: "CardNET (República Dominicana)",
                    SupportedCurrency: "DOP",
                    IsDefault: false,
                    PublicKey: _cardNetOptions.PublicAccountKey,
                    Mode: _cardNetOptions.Environment
                )
            };
        }
    }
}
