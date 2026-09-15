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
    public class LogoutValidator : AbstractValidator<LogoutCommand>
    {
        public LogoutValidator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty();
        }
    }

    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
    {
        private readonly IUserSessionRepository _sessionRepository;
        private readonly IJwtProvider _jwtProvider;
        private readonly IUnitOfWork _unitOfWork;

        public LogoutCommandHandler(
            IUserSessionRepository sessionRepository,
            IJwtProvider jwtProvider,
            IUnitOfWork unitOfWork)
        {
            _sessionRepository = sessionRepository;
            _jwtProvider = jwtProvider;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var hash = _jwtProvider.HashToken(request.RefreshToken);
            var session = await _sessionRepository.GetByRefreshTokenHashAsync(hash, cancellationToken);

            if (session != null && session.IsActive)
            {
                session.IsActive = false;
                session.LastUsedAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result.Success();
        }
    }
}
