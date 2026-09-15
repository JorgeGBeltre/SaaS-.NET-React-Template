using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Enums;
using Domain.Repositories;
using FluentValidation;
using MediatR;
using Shared;

namespace Application.Features.Auth
{
    public class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Code).NotEmpty().Length(6);
            RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one number.");
        }
    }

    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
    {
        private readonly IAppUserRepository _userRepository;
        private readonly IUserSessionRepository _sessionRepository;
        private readonly IOtpService _otpService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;

        public ResetPasswordCommandHandler(
            IAppUserRepository userRepository,
            IUserSessionRepository sessionRepository,
            IOtpService otpService,
            IPasswordHasher passwordHasher,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _sessionRepository = sessionRepository;
            _otpService = otpService;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user == null)
            {
                return Result.Failure("Invalid or expired reset code.");
            }

            var isOtpValid = await _otpService.VerifyOtpAsync(user.Id, OtpPurpose.PasswordReset, request.Code);
            if (!isOtpValid)
            {
                return Result.Failure("Invalid or expired reset code.");
            }

            // Update user password and unlock account
            var (newHash, newSalt) = _passwordHasher.CreateHash(request.NewPassword);
            user.PasswordHash = newHash;
            user.PasswordSalt = newSalt;
            user.FailedLoginAttempts = 0;
            user.IsLocked = false;
            user.LockedUntil = null;

            // Revoke all existing active sessions upon password reset (RFC security best practice)
            await _sessionRepository.RevokeAllUserSessionsAsync(user.Id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
