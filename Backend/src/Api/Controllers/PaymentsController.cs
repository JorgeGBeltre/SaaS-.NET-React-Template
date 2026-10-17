using System.IO;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Features.Payments.Commands;
using Application.Features.Payments.Queries;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class PaymentsController : ApiControllerBase
    {
        private readonly IPaymentStrategyResolver _strategyResolver;

        public PaymentsController(IPaymentStrategyResolver strategyResolver)
        {
            _strategyResolver = strategyResolver;
        }

        /// <summary>
        /// Gets available payment providers (Stripe, CardNET) and their public client configuration.
        /// </summary>
        [HttpGet("providers")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProviders()
        {
            var result = await Mediator.Send(new GetAvailableProvidersQuery());
            return MatchResult(result);
        }

        /// <summary>
        /// Creates a checkout session using the Strategy Pattern for the selected provider (Stripe or CardNet).
        /// </summary>
        [HttpPost("checkout")]
        public async Task<IActionResult> CreateCheckout([FromBody] CreateCheckoutSessionCommand command)
        {
            var result = await Mediator.Send(command);
            return MatchResult(result);
        }

        /// <summary>
        /// Registers a token or payment method (CardNET One-Time Token or Stripe PaymentMethodId).
        /// </summary>
        [HttpPost("methods/register")]
        public async Task<IActionResult> RegisterMethod([FromBody] RegisterPaymentMethodCommand command)
        {
            var result = await Mediator.Send(command);
            return MatchResult(result);
        }

        /// <summary>
        /// Lists saved payment methods for the authenticated user.
        /// </summary>
        [HttpGet("methods")]
        public async Task<IActionResult> GetMethods()
        {
            var result = await Mediator.Send(new GetPaymentMethodsQuery());
            return MatchResult(result);
        }

        /// <summary>
        /// Processes a payment charge using the registered card on file.
        /// </summary>
        [HttpPost("charge")]
        public async Task<IActionResult> Charge([FromBody] ProcessPaymentCommand command)
        {
            var result = await Mediator.Send(command);
            return MatchResult(result);
        }

        /// <summary>
        /// Webhook endpoint for payment providers (e.g. /api/payments/webhook/stripe or /api/payments/webhook/cardnet).
        /// </summary>
        [HttpPost("webhook/{provider}")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook([FromRoute] string provider)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var payload = await reader.ReadToEndAsync();
            var signatureHeader = Request.Headers["Stripe-Signature"].ToString();
            if (string.IsNullOrWhiteSpace(signatureHeader))
            {
                signatureHeader = Request.Headers["X-CardNet-Signature"].ToString();
            }

            PaymentProvider targetProvider = string.Equals(provider, "cardnet", System.StringComparison.OrdinalIgnoreCase)
                ? PaymentProvider.CardNet
                : PaymentProvider.Stripe;

            var strategy = _strategyResolver.Resolve(targetProvider);
            var result = await strategy.ProcessWebhookAsync(payload, signatureHeader);

            return Ok(new { success = true, result });
        }
    }
}
