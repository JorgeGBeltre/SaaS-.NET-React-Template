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
    public class SendEmailOtpValidator : AbstractValidator<SendEmailOtpCommand>
    {
        public SendEmailOtpValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }

    public class SendEmailOtpCommandHandler : IRequestHandler<SendEmailOtpCommand, Result>
    {
        private readonly IAppUserRepository _userRepository;
        private readonly IOtpService _otpService;
        private readonly IEmailService _emailService;

        public SendEmailOtpCommandHandler(
            IAppUserRepository userRepository,
            IOtpService otpService,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _otpService = otpService;
            _emailService = emailService;
        }

        public async Task<Result> Handle(SendEmailOtpCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user == null)
            {
                // OWASP anti-enumeration
                return Result.Success();
            }

            if (user.EmailVerified)
            {
                return Result.Failure("Email is already verified.");
            }

            var plainCode = await _otpService.GenerateOtpAsync(user.Id, OtpPurpose.EmailVerification);
            await _emailService.SendOtpEmailAsync(user.Email, plainCode, OtpPurpose.EmailVerification, cancellationToken);

            return Result.Success();
        }
    }

    public class VerifyEmailOtpValidator : AbstractValidator<VerifyEmailOtpCommand>
    {
        public VerifyEmailOtpValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Code).NotEmpty().Length(6);
        }
    }

    public class VerifyEmailOtpCommandHandler : IRequestHandler<VerifyEmailOtpCommand, Result>
    {
        private readonly IAppUserRepository _userRepository;
        private readonly IOtpService _otpService;
        private readonly IUnitOfWork _unitOfWork;

        public VerifyEmailOtpCommandHandler(
            IAppUserRepository userRepository,
            IOtpService otpService,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _otpService = otpService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(VerifyEmailOtpCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user == null)
            {
                return Result.Failure("Invalid or expired verification code.");
            }

            var isValid = await _otpService.VerifyOtpAsync(user.Id, OtpPurpose.EmailVerification, request.Code);
            if (!isValid)
            {
                return Result.Failure("Invalid or expired verification code.");
            }

            user.EmailVerified = true;
            user.EmailVerifiedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
