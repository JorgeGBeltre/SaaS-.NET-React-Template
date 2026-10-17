using System;
using System.Net.Http;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Common.Models.Payments;
using Domain.Enums;
using Infrastructure.Payments;
using Infrastructure.Payments.CardNet;
using Infrastructure.Payments.Stripe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace UnitTests
{
    public class PaymentStrategyTests
    {
        private readonly CardNetPaymentStrategy _cardNetStrategy;
        private readonly StripePaymentStrategy _stripeStrategy;
        private readonly IPaymentStrategyResolver _resolver;

        public PaymentStrategyTests()
        {
            var cardNetOptions = Options.Create(new CardNetOptions
            {
                Environment = "QA",
                EnableMockInNonProduction = true,
                CurrencyCode = "214",
                PublicAccountKey = "PK_TEST_KEY"
            });

            var stripeOptions = Options.Create(new StripeOptions
            {
                SecretKey = "sk_test_placeholder",
                PublishableKey = "pk_test_placeholder"
            });

            _cardNetStrategy = new CardNetPaymentStrategy(
                new HttpClient(),
                cardNetOptions,
                NullLogger<CardNetPaymentStrategy>.Instance);

            _stripeStrategy = new StripePaymentStrategy(
                stripeOptions,
                NullLogger<StripePaymentStrategy>.Instance);

            var services = new ServiceCollection();
            services.AddSingleton<IPaymentStrategy>(_cardNetStrategy);
            services.AddSingleton<IPaymentStrategy>(_stripeStrategy);
            var serviceProvider = services.BuildServiceProvider();

            _resolver = new PaymentStrategyResolver(serviceProvider, stripeOptions, cardNetOptions);
        }

        [Fact]
        public void StrategyResolver_ShouldResolve_CardNetStrategy()
        {
            var strategy = _resolver.Resolve(PaymentProvider.CardNet);
            Assert.NotNull(strategy);
            Assert.Equal(PaymentProvider.CardNet, strategy.Provider);
        }

        [Fact]
        public void StrategyResolver_ShouldResolve_StripeStrategy()
        {
            var strategy = _resolver.Resolve(PaymentProvider.Stripe);
            Assert.NotNull(strategy);
            Assert.Equal(PaymentProvider.Stripe, strategy.Provider);
        }

        [Fact]
        public async Task CardNetStrategy_ShouldRegisterCardToken_AndReturnPaymentProfile()
        {
            var request = new RegisterPaymentMethodRequest(
                UserId: 1,
                Email: "user@example.com",
                TokenOrPaymentMethodId: "ott_cardnet_custom_Visa_4242_1228_123456",
                Provider: PaymentProvider.CardNet
            );

            var profile = await _cardNetStrategy.RegisterPaymentMethodAsync(request);

            Assert.NotNull(profile);
            Assert.Equal(PaymentProvider.CardNet, profile.Provider);
            Assert.Equal("Visa", profile.Brand);
            Assert.Equal("4242", profile.Last4);
            Assert.Equal(12, profile.ExpirationMonth);
            Assert.Equal(2028, profile.ExpirationYear);
            Assert.StartsWith("CT_", profile.Token);
        }

        [Fact]
        public async Task CardNetStrategy_ShouldChargeCardOnFile_InSandbox()
        {
            var request = new PaymentChargeRequest(
                UserId: 1,
                CustomerId: "cn_cust_123",
                PaymentToken: "CT_mock_token_123456",
                Amount: 1500.00m,
                Currency: "DOP",
                OrderId: "ord_test_001",
                Description: "Monthly Subscription Pro",
                Provider: PaymentProvider.CardNet
            );

            var result = await _cardNetStrategy.ChargeAsync(request);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal("00", result.ResponseCode);
            Assert.Equal(PaymentProvider.CardNet, result.Provider);
            Assert.Equal(1500.00m, result.Amount);
            Assert.NotNull(result.TransactionId);
        }

        [Fact]
        public async Task StripeStrategy_ShouldCreateCheckoutSession_InSandbox()
        {
            var request = new CreateCheckoutRequest(
                UserId: 1,
                Email: "test@example.com",
                PlanSlug: "pro",
                Interval: "monthly",
                Amount: 29.00m,
                Currency: "USD",
                SuccessUrl: "http://localhost:5173/success",
                CancelUrl: "http://localhost:5173/cancel",
                Provider: PaymentProvider.Stripe
            );

            var session = await _stripeStrategy.CreateCheckoutSessionAsync(request);

            Assert.NotNull(session);
            Assert.Equal(PaymentProvider.Stripe, session.Provider);
            Assert.NotNull(session.SessionId);
            Assert.Contains("session_id=", session.CheckoutUrl);
        }

        [Fact]
        public async Task PaymentContext_ShouldExecuteDelegatedStrategy()
        {
            var context = new PaymentContext(_resolver);
            context.SetStrategy(PaymentProvider.CardNet);
            Assert.Equal(PaymentProvider.CardNet, context.CurrentStrategy.Provider);

            var chargeReq = new PaymentChargeRequest(
                UserId: 2,
                CustomerId: "cn_cust_999",
                PaymentToken: "CT_token_777",
                Amount: 29.99m,
                Currency: "DOP",
                OrderId: "ord_context_1",
                Description: "Context strategy test",
                Provider: PaymentProvider.CardNet
            );

            var chargeResult = await context.ExecuteChargeAsync(chargeReq);
            Assert.True(chargeResult.IsSuccess);
            Assert.Equal(PaymentProvider.CardNet, chargeResult.Provider);

            // Switch strategy at runtime (Classic GoF Strategy Pattern behavior)
            context.SetStrategy(PaymentProvider.Stripe);
            Assert.Equal(PaymentProvider.Stripe, context.CurrentStrategy.Provider);
        }

        [Theory]
        [InlineData("00", true, CardNetResponseCategory.Success)]
        [InlineData("51", false, CardNetResponseCategory.InsufficientFunds)]
        [InlineData("54", false, CardNetResponseCategory.CardExpiredOrInvalid)]
        [InlineData("99", false, CardNetResponseCategory.ConfigurationOrData)]
        [InlineData("91", false, CardNetResponseCategory.InfrastructureError)]
        public void CardNetResponseClassifier_ShouldClassifyCorrectly(string code, bool isSuccess, CardNetResponseCategory expected)
        {
            var category = CardNetResponseClassifier.Classify(code, isSuccess);
            Assert.Equal(expected, category);
        }
    }
}
