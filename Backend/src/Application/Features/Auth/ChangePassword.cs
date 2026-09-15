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
    public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordValidator()
        {
            RuleFor(x => x.UserId).GreaterThan(0);
            RuleFor(x => x.CurrentPassword).NotEmpty();
            RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one number.");
        }
    }

    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
    {
        private readonly IAppUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUserSessionRepository _sessionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ChangePasswordCommandHandler(
            IAppUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IUserSessionRepository sessionRepository,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _sessionRepository = sessionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return Result.Failure("User not found.");
            }

            if (user.PasswordHash == null || user.PasswordSalt == null ||
                !_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            {
                return Result.Failure("Current password is incorrect.");
            }

            var (newHash, newSalt) = _passwordHasher.CreateHash(request.NewPassword);
            user.PasswordHash = newHash;
            user.PasswordSalt = newSalt;

            // Invalidate all active sessions across devices
            await _sessionRepository.RevokeAllUserSessionsAsync(user.Id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
