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
    public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenValidator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty();
        }
    }

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
    {
        private readonly IUserSessionRepository _sessionRepository;
        private readonly IAppUserRepository _userRepository;
        private readonly IJwtProvider _jwtProvider;
        private readonly IUnitOfWork _unitOfWork;

        public RefreshTokenCommandHandler(
            IUserSessionRepository sessionRepository,
            IAppUserRepository userRepository,
            IJwtProvider jwtProvider,
            IUnitOfWork unitOfWork)
        {
            _sessionRepository = sessionRepository;
            _userRepository = userRepository;
            _jwtProvider = jwtProvider;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var tokenHash = _jwtProvider.HashToken(request.RefreshToken);
            var session = await _sessionRepository.GetByRefreshTokenHashAsync(tokenHash, cancellationToken);

            if (session == null)
            {
                return Result.Failure<AuthResponse>("Invalid refresh token.");
            }

            // RFC 6819 Token Reuse Detection
            // If an already rotated or invalidated token is reused, assume token theft.
            if (!session.IsActive)
            {
                await _sessionRepository.RevokeAllUserSessionsAsync(session.UserId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result.Failure<AuthResponse>("Security alert: Compromised session detected. All user sessions have been terminated.");
            }

            if (session.ExpiresAt <= DateTime.UtcNow)
            {
                session.IsActive = false;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Failure<AuthResponse>("Refresh token has expired.");
            }

            var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken);
            if (user == null || user.IsLocked)
            {
                session.IsActive = false;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Failure<AuthResponse>("User account is locked or unavailable.");
            }

            // Invalidate current session (Rotation)
            session.IsActive = false;
            session.LastUsedAt = DateTime.UtcNow;

            // Generate new access and refresh token pair
            var newAccessToken = _jwtProvider.Generate(user);
            var newRawRefreshToken = _jwtProvider.GenerateRefreshToken();
            var newRefreshTokenHash = _jwtProvider.HashToken(newRawRefreshToken);

            var newSession = new UserSession
            {
                UserId = user.Id,
                RefreshTokenHash = newRefreshTokenHash,
                IpAddress = request.IpAddress ?? session.IpAddress,
                UserAgent = request.UserAgent ?? session.UserAgent,
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                LastUsedAt = DateTime.UtcNow
            };

            _sessionRepository.Add(newSession);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(new AuthResponse(
                Token: newAccessToken,
                RefreshToken: newRawRefreshToken,
                Email: user.Email,
                FullName: $"{user.FirstName} {user.LastName}".Trim(),
                Requires2fa: false));
        }
    }
}
