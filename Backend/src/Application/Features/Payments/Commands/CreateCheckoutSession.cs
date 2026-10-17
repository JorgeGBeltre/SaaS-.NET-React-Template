using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Common.Models.Payments;
using Application.Interfaces;
using Domain.Enums;
using Domain.Repositories;
using MediatR;
using Shared;

namespace Application.Features.Payments.Commands
{
    public record CreateCheckoutSessionCommand(
        string PlanSlug,
        string Interval,
        PaymentProvider Provider,
        string SuccessUrl,
        string CancelUrl
    ) : IRequest<Result<PaymentCheckoutSessionDto>>;

    public class CreateCheckoutSessionCommandHandler : IRequestHandler<CreateCheckoutSessionCommand, Result<PaymentCheckoutSessionDto>>
    {
        private readonly IPaymentStrategyResolver _strategyResolver;
        private readonly ISubscriptionPlanRepository _planRepository;
        private readonly IAppUserRepository _userRepository;
        private readonly ICurrentUserProvider _currentUserProvider;

        public CreateCheckoutSessionCommandHandler(
            IPaymentStrategyResolver strategyResolver,
            ISubscriptionPlanRepository planRepository,
            IAppUserRepository userRepository,
            ICurrentUserProvider currentUserProvider)
        {
            _strategyResolver = strategyResolver;
            _planRepository = planRepository;
            _userRepository = userRepository;
            _currentUserProvider = currentUserProvider;
        }

        public async Task<Result<PaymentCheckoutSessionDto>> Handle(
            CreateCheckoutSessionCommand request,
            CancellationToken cancellationToken)
        {
            var email = _currentUserProvider.Email;
            if (string.IsNullOrWhiteSpace(email))
            {
                return Result.Failure<PaymentCheckoutSessionDto>("User is not authenticated.");
            }

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return Result.Failure<PaymentCheckoutSessionDto>("User not found.");
            }

            var plans = await _planRepository.GetActivePlansAsync();
            var plan = plans.Find(p => string.Equals(p.Slug, request.PlanSlug, System.StringComparison.OrdinalIgnoreCase))
                       ?? plans.Find(p => string.Equals(p.Name, request.PlanSlug, System.StringComparison.OrdinalIgnoreCase));

            var amount = plan?.Price ?? 29.00m;
            var currency = request.Provider == PaymentProvider.CardNet ? "DOP" : "USD";

            var strategy = _strategyResolver.Resolve(request.Provider);

            var checkoutRequest = new CreateCheckoutRequest(
                UserId: user.Id,
                Email: user.Email,
                PlanSlug: request.PlanSlug,
                Interval: request.Interval,
                Amount: amount,
                Currency: currency,
                SuccessUrl: request.SuccessUrl,
                CancelUrl: request.CancelUrl,
                Provider: request.Provider
            );

            var session = await strategy.CreateCheckoutSessionAsync(checkoutRequest, cancellationToken);
            return Result.Success(session);
        }
    }
}
