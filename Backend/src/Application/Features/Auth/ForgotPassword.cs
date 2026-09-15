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
    public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }

    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
    {
        private readonly IAppUserRepository _userRepository;
        private readonly IOtpService _otpService;
        private readonly IEmailService _emailService;

        public ForgotPasswordCommandHandler(
            IAppUserRepository userRepository,
            IOtpService otpService,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _otpService = otpService;
            _emailService = emailService;
        }

        public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user == null)
            {
                // OWASP ASVS: Prevent user enumeration by returning generic success
                return Result.Success();
            }

            // Generate cryptographically secure OTP and store hash with 15m TTL
            var plainCode = await _otpService.GenerateOtpAsync(user.Id, OtpPurpose.PasswordReset);

            // Dispatch notification email with plain OTP code
            await _emailService.SendOtpEmailAsync(user.Email, plainCode, OtpPurpose.PasswordReset, cancellationToken);

            return Result.Success();
        }
    }
}
