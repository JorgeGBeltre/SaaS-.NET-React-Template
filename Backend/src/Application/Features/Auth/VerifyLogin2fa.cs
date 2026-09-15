using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Domain.Repositories;
using FluentValidation;
using MediatR;
using Shared;

namespace Application.Features.Auth
{
    public class VerifyLogin2faValidator : AbstractValidator<VerifyLogin2faCommand>
    {
        public VerifyLogin2faValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Code).NotEmpty().Length(6);
        }
    }

    public class VerifyLogin2faCommandHandler : IRequestHandler<VerifyLogin2faCommand, Result<AuthResponse>>
    {
        private readonly IAppUserRepository _userRepository;
        private readonly ITotpService _totpService;
        private readonly IJwtProvider _jwtProvider;
        private readonly IUserSessionRepository _sessionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public VerifyLogin2faCommandHandler(
            IAppUserRepository userRepository,
            ITotpService totpService,
            IJwtProvider jwtProvider,
            IUserSessionRepository sessionRepository,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _totpService = totpService;
            _jwtProvider = jwtProvider;
            _sessionRepository = sessionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<AuthResponse>> Handle(VerifyLogin2faCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || !user.TotpEnabled || string.IsNullOrWhiteSpace(user.TotpSecret))
            {
                return Result.Failure<AuthResponse>("Invalid two-factor request.");
            }

            var isValid = _totpService.VerifyCode(user.TotpSecret, request.Code);
            if (!isValid)
            {
                return Result.Failure<AuthResponse>("Invalid authentication code.");
            }

            // Generate tokens and create session
            var accessToken = _jwtProvider.Generate(user);
            var rawRefreshToken = _jwtProvider.GenerateRefreshToken();
            var refreshTokenHash = _jwtProvider.HashToken(rawRefreshToken);

            var session = new UserSession
            {
                UserId = user.Id,
                RefreshTokenHash = refreshTokenHash,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                LastUsedAt = DateTime.UtcNow
            };

            _sessionRepository.Add(session);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(new AuthResponse(
                Token: accessToken,
                RefreshToken: rawRefreshToken,
                Email: user.Email,
                FullName: $"{user.FirstName} {user.LastName}".Trim(),
                Requires2fa: false));
        }
    }
}
