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
    public record ProcessPaymentCommand(
        decimal Amount,
        string Currency,
        string Description,
        PaymentProvider? PreferredProvider = null
    ) : IRequest<Result<PaymentChargeResultDto>>;

    public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, Result<PaymentChargeResultDto>>
    {
        private readonly IPaymentStrategyResolver _strategyResolver;
        private readonly IPaymentCustomerRepository _paymentCustomerRepository;
        private readonly IPaymentTransactionRepository _transactionRepository;
        private readonly IAppUserRepository _userRepository;
        private readonly ICurrentUserProvider _currentUserProvider;
        private readonly IUnitOfWork _unitOfWork;

        public ProcessPaymentCommandHandler(
            IPaymentStrategyResolver strategyResolver,
            IPaymentCustomerRepository paymentCustomerRepository,
            IPaymentTransactionRepository transactionRepository,
            IAppUserRepository userRepository,
            ICurrentUserProvider currentUserProvider,
            IUnitOfWork unitOfWork)
        {
            _strategyResolver = strategyResolver;
            _paymentCustomerRepository = paymentCustomerRepository;
            _transactionRepository = transactionRepository;
            _userRepository = userRepository;
            _currentUserProvider = currentUserProvider;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PaymentChargeResultDto>> Handle(
            ProcessPaymentCommand request,
            CancellationToken cancellationToken)
        {
            var email = _currentUserProvider.Email;
            if (string.IsNullOrWhiteSpace(email))
            {
                return Result.Failure<PaymentChargeResultDto>("User is not authenticated.");
            }

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return Result.Failure<PaymentChargeResultDto>("User not found.");
            }

            PaymentCustomer? customerProfile = null;
            if (request.PreferredProvider.HasValue)
            {
                customerProfile = await _paymentCustomerRepository.GetByUserIdAndProviderAsync(user.Id, request.PreferredProvider.Value);
            }

            customerProfile ??= await _paymentCustomerRepository.GetActiveByUserIdAsync(user.Id);

            if (customerProfile == null || string.IsNullOrWhiteSpace(customerProfile.PaymentToken))
            {
                return Result.Failure<PaymentChargeResultDto>("No registered payment method found for user.");
            }

            var strategy = _strategyResolver.Resolve(customerProfile.Provider);
            var orderId = $"ord_{Guid.NewGuid():N}"[..16];

            var chargeRequest = new PaymentChargeRequest(
                UserId: user.Id,
                CustomerId: customerProfile.ProviderCustomerId,
                PaymentToken: customerProfile.PaymentToken,
                Amount: request.Amount,
                Currency: request.Currency,
                OrderId: orderId,
                Description: request.Description,
                Provider: customerProfile.Provider
            );

            var chargeResult = await strategy.ChargeAsync(chargeRequest, cancellationToken);

            var transaction = new PaymentTransaction
            {
                UserId = user.Id,
                Provider = customerProfile.Provider,
                ProviderTransactionId = chargeResult.TransactionId,
                AuthorizationCode = chargeResult.AuthorizationCode,
                OrderId = orderId,
                Amount = request.Amount,
                Currency = request.Currency,
                Status = chargeResult.IsSuccess ? PaymentTransactionStatus.Succeeded : PaymentTransactionStatus.Failed,
                Description = request.Description,
                ResponseCode = chargeResult.ResponseCode,
                ResponseMessage = chargeResult.ResponseMessage,
                ProcessedAt = chargeResult.Timestamp
            };

            _transactionRepository.Add(transaction);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!chargeResult.IsSuccess)
            {
                return Result.Failure<PaymentChargeResultDto>($"{chargeResult.ResponseCode}: {chargeResult.ResponseMessage}");
            }

            return Result.Success(chargeResult);
        }
    }
}
