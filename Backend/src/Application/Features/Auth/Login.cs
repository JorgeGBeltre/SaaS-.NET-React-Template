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
    public record LoginCommand(
        string Email,
        string Password,
        string? IpAddress = null,
        string? UserAgent = null) : IRequest<Result<AuthResponse>>;

    public class LoginValidator : AbstractValidator<LoginCommand>
    {
        public LoginValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
    {
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private readonly IAppUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtProvider _jwtProvider;
        private readonly IUserSessionRepository _sessionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public LoginCommandHandler(
            IAppUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtProvider jwtProvider,
            IUserSessionRepository sessionRepository,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtProvider = jwtProvider;
            _sessionRepository = sessionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                // Mitigate timing attacks and user enumeration
                return Result.Failure<AuthResponse>("Invalid credentials");
            }

            // Check Account Lockout
            if (user.IsLocked)
            {
                if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
                {
                    return Result.Failure<AuthResponse>($"Account is temporarily locked. Try again after {user.LockedUntil.Value:u}.");
                }

                // Lockout period expired: automatically reset
                user.IsLocked = false;
                user.FailedLoginAttempts = 0;
                user.LockedUntil = null;
            }

            // Verify Password
            if (user.PasswordHash == null || user.PasswordSalt == null ||
                !_passwordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.IsLocked = true;
                    user.LockedUntil = DateTime.UtcNow.Add(LockoutDuration);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result.Failure<AuthResponse>(
                    user.IsLocked
                        ? $"Account locked due to {MaxFailedAttempts} consecutive failed attempts. Try again in 15 minutes."
                        : "Invalid credentials");
            }

            // Reset failed login attempts on successful password verification
            user.FailedLoginAttempts = 0;
            user.IsLocked = false;
            user.LockedUntil = null;
            user.LastLoginAt = DateTime.UtcNow;
            user.LastLoginIp = request.IpAddress;

            // Check if Two-Factor Authentication (TOTP) is required
            if (user.TotpEnabled && !string.IsNullOrWhiteSpace(user.TotpSecret))
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result.Success(new AuthResponse(
                    Token: null,
                    RefreshToken: null,
                    Email: user.Email,
                    FullName: $"{user.FirstName} {user.LastName}".Trim(),
                    Requires2fa: true,
                    Message: "Two-factor authentication code required."));
            }

            // Generate Access and Refresh Tokens
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
