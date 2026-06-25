using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Domain.Events;
using Domain.Repositories;
using FluentValidation;
using MediatR;
using Shared;

namespace Application.Features.Auth
{
    public record RegisterCommand(
        string Email,
        string Password,
        string FirstName,
        string LastName) : IRequest<Result>;

    public class RegisterValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
        }
    }

    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result>
    {
        private readonly IAppUserRepository _userRepository;
        private readonly IUserSettingsRepository _settingsRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterCommandHandler(
            IAppUserRepository userRepository,
            IUserSettingsRepository settingsRepository,
            IPasswordHasher passwordHasher,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _settingsRepository = settingsRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            if (await _userRepository.ExistsByEmailAsync(request.Email))
            {
                return Result.Failure("User already exists");
            }

            var (hash, salt) = _passwordHasher.CreateHash(request.Password);

            var user = new AppUser
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = hash,
                PasswordSalt = salt
            };

            user.AddDomainEvent(new UserRegisteredEvent(user));

            _userRepository.Add(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var userSettings = new UserSettings { UserId = user.Id };
            _settingsRepository.Add(userSettings);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
