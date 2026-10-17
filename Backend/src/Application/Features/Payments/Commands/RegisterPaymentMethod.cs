using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Common.Models.Payments;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using MediatR;
using Shared;

namespace Application.Features.Payments.Commands
{
    public record RegisterPaymentMethodCommand(
        string TokenOrPaymentMethodId,
        string? UniqueId = null,
        PaymentProvider Provider = PaymentProvider.CardNet
    ) : IRequest<Result<PaymentProfileDto>>;

    public class RegisterPaymentMethodCommandHandler : IRequestHandler<RegisterPaymentMethodCommand, Result<PaymentProfileDto>>
    {
        private readonly IPaymentStrategyResolver _strategyResolver;
        private readonly IPaymentCustomerRepository _paymentCustomerRepository;
        private readonly IAppUserRepository _userRepository;
        private readonly ICurrentUserProvider _currentUserProvider;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterPaymentMethodCommandHandler(
            IPaymentStrategyResolver strategyResolver,
            IPaymentCustomerRepository paymentCustomerRepository,
            IAppUserRepository userRepository,
            ICurrentUserProvider currentUserProvider,
            IUnitOfWork unitOfWork)
        {
            _strategyResolver = strategyResolver;
            _paymentCustomerRepository = paymentCustomerRepository;
            _userRepository = userRepository;
            _currentUserProvider = currentUserProvider;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PaymentProfileDto>> Handle(
            RegisterPaymentMethodCommand request,
            CancellationToken cancellationToken)
        {
            var email = _currentUserProvider.Email;
            if (string.IsNullOrWhiteSpace(email))
            {
                return Result.Failure<PaymentProfileDto>("User is not authenticated.");
            }

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return Result.Failure<PaymentProfileDto>("User not found.");
            }

            var strategy = _strategyResolver.Resolve(request.Provider);

            var registerReq = new RegisterPaymentMethodRequest(
                UserId: user.Id,
                Email: user.Email,
                TokenOrPaymentMethodId: request.TokenOrPaymentMethodId,
                UniqueId: request.UniqueId,
                Provider: request.Provider
            );

            var profile = await strategy.RegisterPaymentMethodAsync(registerReq, cancellationToken);

            // Persist or update customer payment profile
            var existingProfile = await _paymentCustomerRepository.GetByUserIdAndProviderAsync(user.Id, request.Provider);
            if (existingProfile == null)
            {
                var newProfile = new PaymentCustomer
                {
                    UserId = user.Id,
                    Provider = request.Provider,
                    ProviderCustomerId = user.Email,
                    PaymentProfileId = profile.PaymentProfileId,
                    PaymentToken = profile.Token,
                    CardBrand = profile.Brand,
                    CardLast4 = profile.Last4,
                    ExpirationMonth = profile.ExpirationMonth,
                    ExpirationYear = profile.ExpirationYear,
                    SubscriptionStatus = "active",
                    IsDefault = true
                };
                _paymentCustomerRepository.Add(newProfile);
            }
            else
            {
                existingProfile.PaymentProfileId = profile.PaymentProfileId;
                existingProfile.PaymentToken = profile.Token;
                existingProfile.CardBrand = profile.Brand;
                existingProfile.CardLast4 = profile.Last4;
                existingProfile.ExpirationMonth = profile.ExpirationMonth;
                existingProfile.ExpirationYear = profile.ExpirationYear;
                existingProfile.SubscriptionStatus = "active";
                _paymentCustomerRepository.Update(existingProfile);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(profile);
        }
    }
}
