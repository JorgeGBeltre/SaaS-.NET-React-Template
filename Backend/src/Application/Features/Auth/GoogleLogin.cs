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
    public class GoogleLoginValidator : AbstractValidator<GoogleLoginCommand>
    {
        public GoogleLoginValidator()
        {
            RuleFor(x => x.IdToken).NotEmpty();
        }
    }

    public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, Result<AuthResponse>>
    {
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IAppUserRepository _userRepository;
        private readonly IUserSettingsRepository _settingsRepository;
        private readonly IJwtProvider _jwtProvider;
        private readonly IUserSessionRepository _sessionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public GoogleLoginCommandHandler(
            IGoogleAuthService googleAuthService,
            IAppUserRepository userRepository,
            IUserSettingsRepository settingsRepository,
            IJwtProvider jwtProvider,
            IUserSessionRepository sessionRepository,
            IUnitOfWork unitOfWork)
        {
            _googleAuthService = googleAuthService;
            _userRepository = userRepository;
            _settingsRepository = settingsRepository;
            _jwtProvider = jwtProvider;
            _sessionRepository = sessionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<AuthResponse>> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
        {
            var payload = await _googleAuthService.ValidateTokenAsync(request.IdToken, cancellationToken);
            if (payload == null || string.IsNullOrWhiteSpace(payload.Email))
            {
                return Result.Failure<AuthResponse>("Invalid or expired Google authentication token.");
            }

            // Look up by Google Subject ID or Email
            var user = await _userRepository.GetByGoogleIdAsync(payload.Subject, cancellationToken);
            if (user == null)
            {
                user = await _userRepository.GetByEmailAsync(payload.Email);
                if (user != null)
                {
                    // Link existing email user with Google Account
                    user.GoogleId = payload.Subject;
                    if (!user.EmailVerified)
                    {
                        user.EmailVerified = true;
                        user.EmailVerifiedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    // Create new user authenticated via Google
                    var names = (payload.Name ?? "Google User").Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    user = new AppUser
                    {
                        Email = payload.Email,
                        FirstName = names.Length > 0 ? names[0] : "Google",
                        LastName = names.Length > 1 ? names[1] : "User",
                        GoogleId = payload.Subject,
                        EmailVerified = true,
                        EmailVerifiedAt = DateTime.UtcNow,
                        PasswordHash = Array.Empty<byte>(),
                        PasswordSalt = Array.Empty<byte>()
                    };

                    _userRepository.Add(user);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    var settings = new UserSettings { UserId = user.Id };
                    _settingsRepository.Add(settings);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            if (user.IsLocked)
            {
                if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
                {
                    return Result.Failure<AuthResponse>($"Account is temporarily locked. Try again after {user.LockedUntil.Value:u}.");
                }

                user.IsLocked = false;
                user.FailedLoginAttempts = 0;
                user.LockedUntil = null;
            }

            user.FailedLoginAttempts = 0;
            user.LastLoginAt = DateTime.UtcNow;
            user.LastLoginIp = request.IpAddress;

            // Generate token pair and user session
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
