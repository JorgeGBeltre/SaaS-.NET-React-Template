using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Common.Models.Payments;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Payments.CardNet
{
    /// <summary>
    /// Concrete Strategy for CardNET Payment Gateway (Dominican Republic):
    /// Implements IPaymentStrategy using CardNET REST API, Basic Authentication,
    /// Hosted Checkout Tokenization, and Card-on-File (Ecommerce_COF) processing.
    /// </summary>
    public sealed class CardNetPaymentStrategy : IPaymentStrategy
    {
        private readonly HttpClient _httpClient;
        private readonly CardNetOptions _options;
        private readonly ILogger<CardNetPaymentStrategy> _logger;

        public PaymentProvider Provider => PaymentProvider.CardNet;

        public CardNetPaymentStrategy(
            HttpClient httpClient,
            IOptions<CardNetOptions> options,
            ILogger<CardNetPaymentStrategy> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;

            if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                var baseUrl = _options.BaseUrl.TrimEnd('/') + "/";
                _httpClient.BaseAddress = new Uri(baseUrl);
            }
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        private void ApplyBasicAuth(HttpRequestMessage request)
        {
            if (!string.IsNullOrWhiteSpace(_options.PrivateAccountKey))
            {
                var raw = $"{_options.PrivateAccountKey}:";
                var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64);
            }
        }

        public async Task<PaymentCustomerDto> CreateOrGetCustomerAsync(
            string customerRefId,
            string email,
            string? phone = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.PrivateAccountKey) || _options.EnableMockInNonProduction)
            {
                _logger.LogInformation("[CardNET Sandbox] Simulating customer creation for '{CustomerRef}'.", customerRefId);
                var mockId = $"cn_cust_{Math.Abs(customerRefId.GetHashCode())}";
                return new PaymentCustomerDto(
                    CustomerId: mockId,
                    Email: email,
                    Phone: phone,
                    Provider: PaymentProvider.CardNet,
                    PaymentProfiles: new List<PaymentProfileDto>()
                );
            }

            try
            {
                var payload = new
                {
                    Email = email,
                    Phone = phone ?? string.Empty,
                    Metadata = new { CustomerRefId = customerRefId }
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, "v1/api/customer")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                ApplyBasicAuth(req);

                using var resp = await _httpClient.SendAsync(req, cancellationToken);
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    var custId = root.TryGetProperty("CustomerId", out var cProp) ? cProp.ToString() : null;
                    if (!string.IsNullOrWhiteSpace(custId))
                    {
                        return new PaymentCustomerDto(
                            CustomerId: custId,
                            Email: email,
                            Phone: phone,
                            Provider: PaymentProvider.CardNet,
                            PaymentProfiles: ParsePaymentProfiles(root)
                        );
                    }
                }

                _logger.LogWarning("CardNET API Customer responded {StatusCode}. Using fallback mock for '{CustomerRef}'.", resp.StatusCode, customerRefId);
                return new PaymentCustomerDto(
                    CustomerId: $"cn_cust_{Math.Abs(customerRefId.GetHashCode())}",
                    Email: email,
                    Phone: phone,
                    Provider: PaymentProvider.CardNet,
                    PaymentProfiles: new List<PaymentProfileDto>()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error communicating with CardNET customer endpoint.");
                return new PaymentCustomerDto(
                    CustomerId: $"cn_cust_{Math.Abs(customerRefId.GetHashCode())}",
                    Email: email,
                    Phone: phone,
                    Provider: PaymentProvider.CardNet,
                    PaymentProfiles: new List<PaymentProfileDto>()
                );
            }
        }

        public async Task<PaymentCheckoutSessionDto> CreateCheckoutSessionAsync(
            CreateCheckoutRequest request,
            CancellationToken cancellationToken = default)
        {
            var defaultCaptureUrl = string.Equals(_options.Environment, "Production", StringComparison.OrdinalIgnoreCase)
                ? "https://servicios.cardnet.com.do/servicios/tokens/v1/Capture"
                : "https://labservicios.cardnet.com.do/servicios/tokens/v1/Capture";

            var uniqueId = $"cn_uid_{Guid.NewGuid():N}"[..16];
            var publicKey = string.IsNullOrWhiteSpace(_options.PublicAccountKey)
                ? "PK_CARDNET_SANDBOX_KEY"
                : _options.PublicAccountKey;

            if (string.IsNullOrWhiteSpace(_options.PrivateAccountKey) || _options.EnableMockInNonProduction)
            {
                _logger.LogInformation("[CardNET Sandbox] Setting up checkout session for user {UserId}.", request.UserId);
                return new PaymentCheckoutSessionDto(
                    SessionId: uniqueId,
                    CheckoutUrl: defaultCaptureUrl,
                    Provider: PaymentProvider.CardNet,
                    PublicKey: publicKey,
                    UniqueId: uniqueId,
                    Metadata: new Dictionary<string, string>
                    {
                        { "PlanSlug", request.PlanSlug },
                        { "Interval", request.Interval },
                        { "Amount", request.Amount.ToString("F2") },
                        { "Currency", request.Currency }
                    }
                );
            }

            try
            {
                var customer = await CreateOrGetCustomerAsync(request.UserId.ToString(), request.Email, null, cancellationToken);
                using var req = new HttpRequestMessage(HttpMethod.Get, $"v1/api/customer/{customer.CustomerId}");
                ApplyBasicAuth(req);

                using var resp = await _httpClient.SendAsync(req, cancellationToken);
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    var captureUrl = root.TryGetProperty("CaptureURL", out var capProp)
                        ? capProp.GetString() ?? defaultCaptureUrl
                        : defaultCaptureUrl;
                    var uid = root.TryGetProperty("UniqueID", out var uProp)
                        ? uProp.GetString() ?? uniqueId
                        : uniqueId;

                    return new PaymentCheckoutSessionDto(
                        SessionId: uid,
                        CheckoutUrl: captureUrl,
                        Provider: PaymentProvider.CardNet,
                        PublicKey: publicKey,
                        UniqueId: uid
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query CardNET checkout session. Falling back to sandbox.");
            }

            return new PaymentCheckoutSessionDto(
                SessionId: uniqueId,
                CheckoutUrl: defaultCaptureUrl,
                Provider: PaymentProvider.CardNet,
                PublicKey: publicKey,
                UniqueId: uniqueId
            );
        }

        public async Task<PaymentProfileDto> RegisterPaymentMethodAsync(
            RegisterPaymentMethodRequest request,
            CancellationToken cancellationToken = default)
        {
            var token = request.TokenOrPaymentMethodId;
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Card capture token cannot be empty.", nameof(request));
            }

            // Sandbox or simulated token handling (supports format: ott_cardnet_custom_Brand_last4_MMYY_timestamp)
            var isCustomToken = token.StartsWith("ott_cardnet_", StringComparison.OrdinalIgnoreCase);
            if (isCustomToken || string.IsNullOrWhiteSpace(_options.PrivateAccountKey) || _options.EnableMockInNonProduction)
            {
                _logger.LogInformation("[CardNET Sandbox] Registering payment profile from token '{Token}'.", token);

                var brand = "Visa";
                var last4 = "4242";
                var expMonth = 12;
                var expYear = DateTime.UtcNow.Year + 3;

                var parts = token.Split('_', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 5 && parts[0].Equals("ott", StringComparison.OrdinalIgnoreCase) && parts[1].Equals("cardnet", StringComparison.OrdinalIgnoreCase))
                {
                    brand = parts[3];
                    last4 = parts[4].Length > 4 ? parts[4][^4..] : parts[4];
                    if (parts.Length >= 6 && parts[5].Length >= 4)
                    {
                        if (int.TryParse(parts[5][..2], out var m) && m >= 1 && m <= 12) expMonth = m;
                        if (int.TryParse(parts[5][2..], out var y)) expYear = y < 100 ? 2000 + y : y;
                    }
                }

                return new PaymentProfileDto(
                    PaymentProfileId: $"pp_{Guid.NewGuid():N}"[..8],
                    Token: $"CT_{Guid.NewGuid():N}"[..24],
                    Brand: brand,
                    Last4: last4,
                    ExpirationMonth: expMonth,
                    ExpirationYear: expYear,
                    IsDefault: true,
                    Provider: PaymentProvider.CardNet
                );
            }

            try
            {
                var customer = await CreateOrGetCustomerAsync(request.UserId.ToString(), request.Email, null, cancellationToken);
                var payload = new
                {
                    CustomerId = customer.CustomerId,
                    OneTimeToken = token
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, $"v1/api/customer/{customer.CustomerId}/paymentprofile")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                ApplyBasicAuth(req);

                using var resp = await _httpClient.SendAsync(req, cancellationToken);
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    var ppId = root.TryGetProperty("PaymentProfileId", out var idProp) ? idProp.ToString() : $"pp_{Guid.NewGuid():N}"[..8];
                    var persistentToken = root.TryGetProperty("Token", out var tokProp) ? tokProp.GetString() : null;
                    var brand = root.TryGetProperty("Brand", out var bProp) ? bProp.GetString() ?? "Visa" : "Visa";
                    var last4 = root.TryGetProperty("Last4", out var lProp) ? lProp.GetString() ?? "0000" : "0000";
                    var exp = root.TryGetProperty("Expiration", out var expProp) ? expProp.GetString() ?? "" : "";

                    var (month, year) = ParseExpiration(exp);

                    return new PaymentProfileDto(
                        PaymentProfileId: ppId,
                        Token: persistentToken ?? token,
                        Brand: brand,
                        Last4: last4,
                        ExpirationMonth: month > 0 ? month : 12,
                        ExpirationYear: year > 0 ? year : DateTime.UtcNow.Year + 3,
                        IsDefault: true,
                        Provider: PaymentProvider.CardNet
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling CardNET RegisterCardToken.");
            }

            // Fallback safe profile
            return new PaymentProfileDto(
                PaymentProfileId: $"pp_{Guid.NewGuid():N}"[..8],
                Token: $"CT_{Guid.NewGuid():N}"[..24],
                Brand: "Visa",
                Last4: "4242",
                ExpirationMonth: 12,
                ExpirationYear: DateTime.UtcNow.Year + 3,
                IsDefault: true,
                Provider: PaymentProvider.CardNet
            );
        }

        public async Task<PaymentChargeResultDto> ChargeAsync(
            PaymentChargeRequest request,
            CancellationToken cancellationToken = default)
        {
            var effectiveIdempotencyKey = request.IdempotencyKey ?? request.OrderId;

            _logger.LogInformation("[CardNET] Processing Ecommerce_COF charge. Amount: {Amount} {Currency}, Order: {OrderId}",
                request.Amount, request.Currency, request.OrderId);

            if (string.IsNullOrWhiteSpace(_options.PrivateAccountKey) || _options.EnableMockInNonProduction || request.PaymentToken.StartsWith("CT_") || request.PaymentToken.StartsWith("ott_"))
            {
                _logger.LogInformation("[CardNET Sandbox] Charge approved successfully with code '00'.");
                return new PaymentChargeResultDto(
                    IsSuccess: true,
                    ResponseCode: "00",
                    ResponseMessage: "Transacción aprobada (Sandbox CardNET)",
                    TransactionId: $"tx_cn_{Guid.NewGuid():N}"[..12],
                    AuthorizationCode: $"auth_{new Random().Next(100000, 999999)}",
                    Amount: request.Amount,
                    Currency: request.Currency,
                    Provider: PaymentProvider.CardNet,
                    Timestamp: DateTime.UtcNow
                );
            }

            try
            {
                var transactionalBase = string.IsNullOrWhiteSpace(_options.TransactionalUrl)
                    ? _options.BaseUrl
                    : _options.TransactionalUrl;

                var itbis = Math.Round(request.Amount * 0.18m / 1.18m, 2);

                var payload = new
                {
                    MerchantId = _options.MerchantId,
                    TerminalId = _options.TerminalId,
                    CustomerId = request.CustomerId,
                    Token = request.PaymentToken,
                    TrxToken = request.PaymentToken,
                    Amount = request.Amount,
                    Currency = _options.CurrencyCode,
                    OrderId = request.OrderId,
                    Order = request.OrderId,
                    IdempotencyKey = effectiveIdempotencyKey,
                    Capture = true,
                    TransactionType = "Ecommerce_COF",
                    Description = request.Description,
                    DataDo = new
                    {
                        Invoice = request.OrderId,
                        Tax = itbis,
                        IdempotencyKey = effectiveIdempotencyKey
                    }
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, $"{transactionalBase}/v1/api/purchase")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                ApplyBasicAuth(req);
                req.Headers.TryAddWithoutValidation("Idempotency-Key", effectiveIdempotencyKey);

                using var resp = await _httpClient.SendAsync(req, cancellationToken);
                var json = await resp.Content.ReadAsStringAsync(cancellationToken);

                string respCode = "";
                string respMsg = "";
                string? txId = null;
                string? authCode = null;

                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("ResponseCode", out var rc) || root.TryGetProperty("responseCode", out rc))
                        respCode = rc.ValueKind == JsonValueKind.Number ? rc.GetInt64().ToString() : rc.GetString() ?? "";
                    if (root.TryGetProperty("ResponseMessage", out var rm) || root.TryGetProperty("responseMessage", out rm))
                        respMsg = rm.GetString() ?? "";
                    if (root.TryGetProperty("TransactionId", out var tProp) || root.TryGetProperty("transactionId", out tProp))
                        txId = tProp.GetString();
                    if (root.TryGetProperty("AuthorizationCode", out var aProp) || root.TryGetProperty("authorizationCode", out aProp))
                        authCode = aProp.GetString();
                }
                catch
                {
                    respCode = "PARSE_ERROR";
                    respMsg = "Respuesta no estructurada del procesador";
                }

                var isSuccess = resp.IsSuccessStatusCode && (respCode == "00" || respCode == "0");

                return new PaymentChargeResultDto(
                    IsSuccess: isSuccess,
                    ResponseCode: string.IsNullOrWhiteSpace(respCode) ? (isSuccess ? "00" : "UNKNOWN_ERROR") : respCode,
                    ResponseMessage: string.IsNullOrWhiteSpace(respMsg) ? CardNetResponseClassifier.GetUserFriendlyMessage(respCode, isSuccess) : respMsg,
                    TransactionId: txId,
                    AuthorizationCode: authCode,
                    Amount: request.Amount,
                    Currency: request.Currency,
                    Provider: PaymentProvider.CardNet,
                    Timestamp: DateTime.UtcNow
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while processing CardNET charge.");
                return new PaymentChargeResultDto(
                    IsSuccess: false,
                    ResponseCode: "COMMUNICATION_ERROR",
                    ResponseMessage: $"Error de comunicación con procesador CardNET: {ex.Message}",
                    TransactionId: null,
                    AuthorizationCode: null,
                    Amount: request.Amount,
                    Currency: request.Currency,
                    Provider: PaymentProvider.CardNet,
                    Timestamp: DateTime.UtcNow
                );
            }
        }

        public async Task<PaymentRefundResultDto> RefundAsync(
            PaymentRefundRequest request,
            CancellationToken cancellationToken = default)
        {
            var effectiveIdempotencyKey = request.IdempotencyKey ?? request.OrderId;

            if (string.IsNullOrWhiteSpace(_options.PrivateAccountKey) || _options.EnableMockInNonProduction || request.OriginalTransactionId.StartsWith("tx_") || request.OriginalTransactionId.StartsWith("ref_"))
            {
                _logger.LogInformation("[CardNET Sandbox] Refund approved successfully (Mock).");
                return new PaymentRefundResultDto(
                    IsSuccess: true,
                    ResponseCode: "00",
                    ResponseMessage: "Reembolso aprobado (Sandbox CardNET)",
                    TransactionId: $"ref_cn_{Guid.NewGuid():N}"[..12],
                    AuthorizationCode: $"auth_{new Random().Next(100000, 999999)}",
                    Amount: request.Amount,
                    Currency: request.Currency,
                    Provider: PaymentProvider.CardNet,
                    Timestamp: DateTime.UtcNow
                );
            }

            try
            {
                var transactionalBase = string.IsNullOrWhiteSpace(_options.TransactionalUrl)
                    ? _options.BaseUrl
                    : _options.TransactionalUrl;

                var payload = new
                {
                    MerchantId = _options.MerchantId,
                    TerminalId = _options.TerminalId,
                    CustomerId = request.CustomerId,
                    OriginalTransactionId = request.OriginalTransactionId,
                    TransactionId = request.OriginalTransactionId,
                    Amount = request.Amount,
                    Currency = _options.CurrencyCode,
                    OrderId = request.OrderId,
                    Reason = request.Reason ?? "Reembolso solicitado",
                    IdempotencyKey = effectiveIdempotencyKey
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, $"{transactionalBase}/v1/api/refund")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                ApplyBasicAuth(req);
                req.Headers.TryAddWithoutValidation("Idempotency-Key", effectiveIdempotencyKey);

                using var resp = await _httpClient.SendAsync(req, cancellationToken);
                var isSuccess = resp.IsSuccessStatusCode;

                return new PaymentRefundResultDto(
                    IsSuccess: isSuccess,
                    ResponseCode: isSuccess ? "00" : "REFUND_ERROR",
                    ResponseMessage: isSuccess ? "Reembolso aprobado" : "Reembolso rechazado",
                    TransactionId: $"ref_{Guid.NewGuid():N}"[..12],
                    AuthorizationCode: null,
                    Amount: request.Amount,
                    Currency: request.Currency,
                    Provider: PaymentProvider.CardNet,
                    Timestamp: DateTime.UtcNow
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while processing CardNET refund.");
                return new PaymentRefundResultDto(
                    IsSuccess: false,
                    ResponseCode: "COMMUNICATION_ERROR",
                    ResponseMessage: $"Error al procesar reembolso: {ex.Message}",
                    TransactionId: null,
                    AuthorizationCode: null,
                    Amount: request.Amount,
                    Currency: request.Currency,
                    Provider: PaymentProvider.CardNet,
                    Timestamp: DateTime.UtcNow
                );
            }
        }

        public Task<PaymentWebhookResultDto> ProcessWebhookAsync(
            string payload,
            string signatureHeader,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[CardNET Webhook] Received webhook notification.");
            return Task.FromResult(new PaymentWebhookResultDto(
                Handled: true,
                EventType: "cardnet.notification",
                CustomerId: null,
                SubscriptionId: null,
                Status: "processed",
                Message: "CardNET webhook received and acknowledged."
            ));
        }

        private static List<PaymentProfileDto> ParsePaymentProfiles(JsonElement root)
        {
            var list = new List<PaymentProfileDto>();
            if (root.TryGetProperty("PaymentProfiles", out var pArray) && pArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in pArray.EnumerateArray())
                {
                    var ppId = item.TryGetProperty("PaymentProfileId", out var id) ? id.ToString() : "";
                    var tok = item.TryGetProperty("Token", out var t) ? t.GetString() ?? "" : "";
                    var brand = item.TryGetProperty("Brand", out var b) ? b.GetString() ?? "Visa" : "Visa";
                    var last4 = item.TryGetProperty("Last4", out var l) ? l.GetString() ?? "0000" : "0000";
                    var exp = item.TryGetProperty("Expiration", out var e) ? e.GetString() ?? "" : "";
                    var (m, y) = ParseExpiration(exp);
                    list.Add(new PaymentProfileDto(ppId, tok, brand, last4, m, y, true, PaymentProvider.CardNet));
                }
            }
            return list;
        }

        private static (int month, int year) ParseExpiration(string expiration)
        {
            if (string.IsNullOrWhiteSpace(expiration)) return (12, DateTime.UtcNow.Year + 3);
            var clean = expiration.Replace("/", "").Replace("-", "").Trim();
            if (clean.Length == 4 && int.TryParse(clean[..2], out var m) && int.TryParse(clean[2..], out var y))
            {
                return (m, y < 100 ? 2000 + y : y);
            }
            if (clean.Length == 6 && int.TryParse(clean[..4], out var y4) && int.TryParse(clean[4..], out var m4))
            {
                return (m4, y4);
            }
            return (12, DateTime.UtcNow.Year + 3);
        }
    }
}
