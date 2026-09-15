using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Repositories;
using FluentValidation;
using MediatR;
using Shared;

namespace Application.Features.Auth
{
    public class Enable2faCommandHandler : IRequestHandler<Enable2faCommand, Result<Enable2faResponse>>
    {
        private readonly IAppUserRepository _userRepository;
        private readonly ITotpService _totpService;
        private readonly IUnitOfWork _unitOfWork;

        public Enable2faCommandHandler(
            IAppUserRepository userRepository,
            ITotpService totpService,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _totpService = totpService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Enable2faResponse>> Handle(Enable2faCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result.Failure<Enable2faResponse>("User not found.");
            }

            var (secret, qrCodeUri) = _totpService.GenerateSecret(user.Email);
            user.TotpSecret = secret;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(new Enable2faResponse(secret, qrCodeUri));
        }
    }

    public class Verify2faValidator : AbstractValidator<Verify2faCommand>
    {
        public Verify2faValidator()
        {
            RuleFor(x => x.UserId).GreaterThan(0);
            RuleFor(x => x.Code).NotEmpty().Length(6);
        }
    }

    public class Verify2faCommandHandler : IRequestHandler<Verify2faCommand, Result>
    {
        private readonly IAppUserRepository _userRepository;
        private readonly ITotpService _totpService;
        private readonly IUnitOfWork _unitOfWork;

        public Verify2faCommandHandler(
            IAppUserRepository userRepository,
            ITotpService totpService,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _totpService = totpService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(Verify2faCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null || string.IsNullOrWhiteSpace(user.TotpSecret))
            {
                return Result.Failure("Two-factor configuration not initialized.");
            }

            var isValid = _totpService.VerifyCode(user.TotpSecret, request.Code);
            if (!isValid)
            {
                return Result.Failure("Invalid verification code.");
            }

            user.TotpEnabled = true;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }

    public class Disable2faValidator : AbstractValidator<Disable2faCommand>
    {
        public Disable2faValidator()
        {
            RuleFor(x => x.UserId).GreaterThan(0);
            RuleFor(x => x.Code).NotEmpty().Length(6);
        }
    }

    public class Disable2faCommandHandler : IRequestHandler<Disable2faCommand, Result>
    {
        private readonly IAppUserRepository _userRepository;
        private readonly ITotpService _totpService;
        private readonly IUnitOfWork _unitOfWork;

        public Disable2faCommandHandler(
            IAppUserRepository userRepository,
            ITotpService totpService,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _totpService = totpService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(Disable2faCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null || !user.TotpEnabled || string.IsNullOrWhiteSpace(user.TotpSecret))
            {
                return Result.Failure("Two-factor authentication is not active.");
            }

            var isValid = _totpService.VerifyCode(user.TotpSecret, request.Code);
            if (!isValid)
            {
                return Result.Failure("Invalid verification code.");
            }

            user.TotpEnabled = false;
            user.TotpSecret = null;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
