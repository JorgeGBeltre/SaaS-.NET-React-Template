using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Common.Models.Payments;
using Application.Interfaces;
using Domain.Repositories;
using MediatR;
using Shared;

namespace Application.Features.Payments.Queries
{
    public record GetPaymentMethodsQuery : IRequest<Result<List<PaymentProfileDto>>>;

    public class GetPaymentMethodsQueryHandler : IRequestHandler<GetPaymentMethodsQuery, Result<List<PaymentProfileDto>>>
    {
        private readonly IPaymentCustomerRepository _customerRepository;
        private readonly IAppUserRepository _userRepository;
        private readonly ICurrentUserProvider _currentUserProvider;

        public GetPaymentMethodsQueryHandler(
            IPaymentCustomerRepository customerRepository,
            IAppUserRepository userRepository,
            ICurrentUserProvider currentUserProvider)
        {
            _customerRepository = customerRepository;
            _userRepository = userRepository;
            _currentUserProvider = currentUserProvider;
        }

        public async Task<Result<List<PaymentProfileDto>>> Handle(
            GetPaymentMethodsQuery request,
            CancellationToken cancellationToken)
        {
            var email = _currentUserProvider.Email;
            if (string.IsNullOrWhiteSpace(email))
            {
                return Result.Failure<List<PaymentProfileDto>>("User is not authenticated.");
            }

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return Result.Failure<List<PaymentProfileDto>>("User not found.");
            }

            var profiles = await _customerRepository.GetAllByUserIdAsync(user.Id);

            var dtos = profiles.Select(p => new PaymentProfileDto(
                PaymentProfileId: p.PaymentProfileId ?? p.Id.ToString(),
                Token: p.PaymentToken ?? string.Empty,
                Brand: p.CardBrand ?? "Card",
                Last4: p.CardLast4 ?? "****",
                ExpirationMonth: p.ExpirationMonth ?? 0,
                ExpirationYear: p.ExpirationYear ?? 0,
                IsDefault: p.IsDefault,
                Provider: p.Provider
            )).ToList();

            return Result.Success(dtos);
        }
    }
}
