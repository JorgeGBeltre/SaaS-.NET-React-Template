using System.Threading;
using System.Threading.Tasks;
using Domain.Repositories;
using MediatR;
using Shared;

namespace Application.Features.Subscriptions
{
    public class SubscriptionDto
    {
        public bool HasSubscription { get; set; }
        public string? StripeCustomerId { get; set; }
        public string? SubscriptionId { get; set; }
        public string? Status { get; set; }
    }

    public record GetSubscriptionQuery(string Email) : IRequest<Result<SubscriptionDto>>;

    public class GetSubscriptionQueryHandler : IRequestHandler<GetSubscriptionQuery, Result<SubscriptionDto>>
    {
        private readonly IStripeCustomerRepository _customerRepository;

        public GetSubscriptionQueryHandler(IStripeCustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<Result<SubscriptionDto>> Handle(GetSubscriptionQuery request, CancellationToken cancellationToken)
        {
            var customer = await _customerRepository.GetByEmailAsync(request.Email);
            if (customer == null)
            {
                return Result.Success(new SubscriptionDto { HasSubscription = false });
            }

            return Result.Success(new SubscriptionDto
            {
                HasSubscription = true,
                StripeCustomerId = customer.StripeCustomerId,
                SubscriptionId = customer.StripeSubscriptionId,
                Status = customer.SubscriptionStatus
            });
        }
    }
}
