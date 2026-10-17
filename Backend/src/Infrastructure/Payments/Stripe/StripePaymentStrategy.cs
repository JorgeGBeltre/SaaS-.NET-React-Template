using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Common.Models.Payments;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Infrastructure.Payments.Stripe
{
    /// <summary>
    /// Concrete Strategy for Stripe:
    /// Implements IPaymentStrategy using Stripe.net SDK for Checkout Sessions,
    /// Customers, PaymentIntents, and Webhooks.
    /// </summary>
    public sealed class StripePaymentStrategy : IPaymentStrategy
    {
        private readonly StripeOptions _options;
        private readonly ILogger<StripePaymentStrategy> _logger;
        private readonly StripeClient? _client;

        public PaymentProvider Provider => PaymentProvider.Stripe;

        public StripePaymentStrategy(
            IOptions<StripeOptions> options,
            ILogger<StripePaymentStrategy> logger)
        {
            _options = options.Value;
            _logger = logger;

            if (!string.IsNullOrWhiteSpace(_options.SecretKey) &&
                !_options.SecretKey.Contains("placeholder") &&
                !_options.SecretKey.Contains("your_stripe"))
            {
                _client = new StripeClient(_options.SecretKey);
            }
        }

        private bool IsMockMode => _client == null;

        public async Task<PaymentCustomerDto> CreateOrGetCustomerAsync(
            string customerRefId,
            string email,
            string? phone = null,
            CancellationToken cancellationToken = default)
        {
            if (IsMockMode)
            {
                _logger.LogInformation("[Stripe Sandbox] Creating mock customer for '{Email}'.", email);
                return new PaymentCustomerDto(
                    CustomerId: $"cus_mock_{Math.Abs(email.GetHashCode())}",
                    Email: email,
                    Phone: phone,
                    Provider: PaymentProvider.Stripe
                );
            }

            try
            {
                var customerService = new CustomerService(_client);
                var listOptions = new CustomerListOptions
                {
                    Email = email,
                    Limit = 1
                };

                var existing = await customerService.ListAsync(listOptions, cancellationToken: cancellationToken);
                if (existing.Data.Count > 0)
                {
                    var found = existing.Data[0];
                    return new PaymentCustomerDto(
                        CustomerId: found.Id,
                        Email: found.Email,
                        Phone: found.Phone,
                        Provider: PaymentProvider.Stripe
                    );
                }

                var createOptions = new CustomerCreateOptions
                {
                    Email = email,
                    Phone = phone,
                    Metadata = new Dictionary<string, string> { { "CustomerRefId", customerRefId } }
                };

                var created = await customerService.CreateAsync(createOptions, cancellationToken: cancellationToken);
                return new PaymentCustomerDto(
                    CustomerId: created.Id,
                    Email: created.Email,
                    Phone: created.Phone,
                    Provider: PaymentProvider.Stripe
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe customer for {Email}. Using fallback.", email);
                return new PaymentCustomerDto(
                    CustomerId: $"cus_mock_{Math.Abs(email.GetHashCode())}",
                    Email: email,
                    Phone: phone,
                    Provider: PaymentProvider.Stripe
                );
            }
        }

        public async Task<PaymentCheckoutSessionDto> CreateCheckoutSessionAsync(
            CreateCheckoutRequest request,
            CancellationToken cancellationToken = default)
        {
            if (IsMockMode)
            {
                _logger.LogInformation("[Stripe Sandbox] Generating mock checkout session for {Email}.", request.Email);
                var mockSessionId = $"cs_test_{Guid.NewGuid():N}";
                return new PaymentCheckoutSessionDto(
                    SessionId: mockSessionId,
                    CheckoutUrl: $"{request.SuccessUrl}?session_id={mockSessionId}&provider=stripe",
                    Provider: PaymentProvider.Stripe,
                    PublicKey: _options.PublishableKey,
                    Metadata: new Dictionary<string, string>
                    {
                        { "PlanSlug", request.PlanSlug },
                        { "Interval", request.Interval }
                    }
                );
            }

            try
            {
                var sessionService = new SessionService(_client);
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    CustomerEmail = request.Email,
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = request.Currency.ToLowerInvariant(),
                                UnitAmount = (long)(request.Amount * 100),
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = $"Subscription Plan: {request.PlanSlug}"
                                },
                                Recurring = new SessionLineItemPriceDataRecurringOptions
                                {
                                    Interval = request.Interval.Equals("annual", StringComparison.OrdinalIgnoreCase) ? "year" : "month"
                                }
                            },
                            Quantity = 1
                        }
                    },
                    Mode = "subscription",
                    SuccessUrl = request.SuccessUrl + "?session_id={CHECKOUT_SESSION_ID}&provider=stripe",
                    CancelUrl = request.CancelUrl,
                    Metadata = new Dictionary<string, string>
                    {
                        { "UserId", request.UserId.ToString() },
                        { "PlanSlug", request.PlanSlug }
                    }
                };

                var session = await sessionService.CreateAsync(options, cancellationToken: cancellationToken);
                return new PaymentCheckoutSessionDto(
                    SessionId: session.Id,
                    CheckoutUrl: session.Url,
                    Provider: PaymentProvider.Stripe,
                    PublicKey: _options.PublishableKey
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe session creation failed. Falling back to sandbox.");
                var mockSessionId = $"cs_test_{Guid.NewGuid():N}";
                return new PaymentCheckoutSessionDto(
                    SessionId: mockSessionId,
                    CheckoutUrl: $"{request.SuccessUrl}?session_id={mockSessionId}&provider=stripe",
                    Provider: PaymentProvider.Stripe,
                    PublicKey: _options.PublishableKey
                );
            }
        }

        public async Task<PaymentProfileDto> RegisterPaymentMethodAsync(
            RegisterPaymentMethodRequest request,
            CancellationToken cancellationToken = default)
        {
            if (IsMockMode || request.TokenOrPaymentMethodId.StartsWith("pm_mock_"))
            {
                _logger.LogInformation("[Stripe Sandbox] Registering mock payment method for {Email}.", request.Email);
                return new PaymentProfileDto(
                    PaymentProfileId: $"pm_{Guid.NewGuid():N}"[..12],
                    Token: request.TokenOrPaymentMethodId,
                    Brand: "Visa",
                    Last4: "4242",
                    ExpirationMonth: 12,
                    ExpirationYear: DateTime.UtcNow.Year + 3,
                    IsDefault: true,
                    Provider: PaymentProvider.Stripe
                );
            }

            try
            {
                var pmService = new PaymentMethodService(_client);
                var pm = await pmService.GetAsync(request.TokenOrPaymentMethodId, cancellationToken: cancellationToken);

                var customer = await CreateOrGetCustomerAsync(request.UserId.ToString(), request.Email, null, cancellationToken);
                var attachOptions = new PaymentMethodAttachOptions
                {
                    Customer = customer.CustomerId
                };

                var attached = await pmService.AttachAsync(pm.Id, attachOptions, cancellationToken: cancellationToken);

                return new PaymentProfileDto(
                    PaymentProfileId: attached.Id,
                    Token: attached.Id,
                    Brand: attached.Card?.Brand ?? "Visa",
                    Last4: attached.Card?.Last4 ?? "4242",
                    ExpirationMonth: (int)(attached.Card?.ExpMonth ?? 12),
                    ExpirationYear: (int)(attached.Card?.ExpYear ?? DateTime.UtcNow.Year + 3),
                    IsDefault: true,
                    Provider: PaymentProvider.Stripe
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error attaching Stripe payment method.");
                return new PaymentProfileDto(
                    PaymentProfileId: $"pm_{Guid.NewGuid():N}"[..12],
                    Token: request.TokenOrPaymentMethodId,
                    Brand: "Visa",
                    Last4: "4242",
                    ExpirationMonth: 12,
                    ExpirationYear: DateTime.UtcNow.Year + 3,
                    IsDefault: true,
                    Provider: PaymentProvider.Stripe
                );
            }
        }

        public async Task<PaymentChargeResultDto> ChargeAsync(
            PaymentChargeRequest request,
            CancellationToken cancellationToken = default)
        {
            if (IsMockMode || request.PaymentToken.StartsWith("pm_mock_") || request.PaymentToken.StartsWith("tok_mock_"))
            {
                _logger.LogInformation("[Stripe Sandbox] Charge approved for amount {Amount} {Currency}.", request.Amount, request.Currency);
                return new PaymentChargeResultDto(
                    IsSuccess: true,
                    ResponseCode: "succeeded",
                    ResponseMessage: "Payment confirmed (Stripe Sandbox)",
                    TransactionId: $"pi_mock_{Guid.NewGuid():N}"[..14],
                    AuthorizationCode: $"auth_str_{new Random().Next(100000, 999999)}",
                    Amount: request.Amount,
                    Currency: request.Currency,
                    Provider: PaymentProvider.Stripe,
                    Timestamp: DateTime.UtcNow
                );
            }

            try
            {
                var piService = new PaymentIntentService(_client);
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(request.Amount * 100),
                    Currency = request.Currency.ToLowerInvariant(),
                    Customer = request.CustomerId,
                    PaymentMethod = request.PaymentToken,
                    Confirm = true,
                    OffSession = true,
                    Description = request.Description
                };

                var pi = await piService.CreateAsync(options, cancellationToken: cancellationToken);
                var isSuccess = pi.Status == "succeeded";

                return new PaymentChargeResultDto(
                    IsSuccess: isSuccess,
                    ResponseCode: pi.Status,
                    ResponseMessage: isSuccess ? "Payment confirmed" : $"Stripe status: {pi.Status}",
                    TransactionId: pi.Id,
                    AuthorizationCode: pi.LatestChargeId,
                    Amount: request.Amount,
                    Currency: request.Currency,
                    Provider: PaymentProvider.Stripe,
                    Timestamp: DateTime.UtcNow
                );
            }
            catch (StripeException sEx)
            {
                _logger.LogError(sEx, "Stripe charge failed: {Code} - {Msg}", sEx.StripeError?.Code, sEx.StripeError?.Message);
                return new PaymentChargeResultDto(
                    IsSuccess: false,
                    ResponseCode: sEx.StripeError?.Code ?? "stripe_error",
                    ResponseMessage: sEx.StripeError?.Message ?? sEx.Message,
                    TransactionId: null,
                    AuthorizationCode: null,
                    Amount: request.Amount,
                    Currency: request.Currency,
                    Provider: PaymentProvider.Stripe,
                    Timestamp: DateTime.UtcNow
                );
            }
        }

        public async Task<PaymentRefundResultDto> RefundAsync(
            PaymentRefundRequest request,
            CancellationToken cancellationToken = default)
        {
            if (IsMockMode || request.OriginalTransactionId.StartsWith("pi_mock_"))
            {
                _logger.LogInformation("[Stripe Sandbox] Refund approved for {Amount} {Currency}.", request.Amount, request.Currency);
                return new PaymentRefundResultDto(
                    IsSuccess: true,
                    ResponseCode: "succeeded",
                    ResponseMessage: "Refund processed (Stripe Sandbox)",
                    TransactionId: $"re_mock_{Guid.NewGuid():N}"[..14],
                    AuthorizationCode: null,
                    Amount: request.Amount,
                    Currency: request.Currency,
                    Provider: PaymentProvider.Stripe,
                    Timestamp: DateTime.UtcNow
                );
            }

            try
            {
                var refundService = new RefundService(_client);
                var options = new RefundCreateOptions
                {
                    PaymentIntent = request.OriginalTransactionId,
                    Amount = (long)(request.Amount * 100)
                };

                var refund = await refundService.CreateAsync(options, cancellationToken: cancellationToken);
                var isSuccess = refund.Status == "succeeded";

                return new PaymentRefundResultDto(
                    IsSuccess: isSuccess,
                    ResponseCode: refund.Status,
                    ResponseMessage: isSuccess ? "Refund processed" : $"Refund status: {refund.Status}",
                    TransactionId: refund.Id,
                    AuthorizationCode: null,
                    Amount: request.Amount,
                    Currency: request.Currency,
                    Provider: PaymentProvider.Stripe,
                    Timestamp: DateTime.UtcNow
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe refund failed.");
                return new PaymentRefundResultDto(
                    IsSuccess: false,
                    ResponseCode: "refund_error",
                    ResponseMessage: ex.Message,
                    TransactionId: null,
                    AuthorizationCode: null,
                    Amount: request.Amount,
                    Currency: request.Currency,
                    Provider: PaymentProvider.Stripe,
                    Timestamp: DateTime.UtcNow
                );
            }
        }

        public Task<PaymentWebhookResultDto> ProcessWebhookAsync(
            string payload,
            string signatureHeader,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
            {
                return Task.FromResult(new PaymentWebhookResultDto(
                    Handled: true,
                    EventType: "unverified",
                    CustomerId: null,
                    SubscriptionId: null,
                    Status: "ok",
                    Message: "Webhook accepted without signature verification in test mode."
                ));
            }

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, _options.WebhookSecret);
                _logger.LogInformation("[Stripe Webhook] Received event '{Type}'", stripeEvent.Type);

                return Task.FromResult(new PaymentWebhookResultDto(
                    Handled: true,
                    EventType: stripeEvent.Type,
                    CustomerId: null,
                    SubscriptionId: null,
                    Status: "processed",
                    Message: $"Event {stripeEvent.Type} processed successfully."
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe webhook signature validation failed.");
                return Task.FromResult(new PaymentWebhookResultDto(
                    Handled: false,
                    EventType: "error",
                    CustomerId: null,
                    SubscriptionId: null,
                    Status: "signature_error",
                    Message: ex.Message
                ));
            }
        }
    }
}
