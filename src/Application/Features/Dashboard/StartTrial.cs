using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Events;
using Domain.Repositories;
using MediatR;
using Shared;

namespace Application.Features.Dashboard
{
    public record StartTrialCommand(string Email) : IRequest<Result>;

    public class StartTrialCommandHandler : IRequestHandler<StartTrialCommand, Result>
    {
        private readonly IUserSettingsRepository _settingsRepository;
        private readonly IAppUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public StartTrialCommandHandler(
            IUserSettingsRepository settingsRepository,
            IAppUserRepository userRepository,
            IUnitOfWork unitOfWork)
        {
            _settingsRepository = settingsRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(StartTrialCommand request, CancellationToken cancellationToken)
        {
            var settings = await _settingsRepository.GetByEmailAsync(request.Email);
            if (settings == null)
            {
                return Result.Failure("Settings not found");
            }

            var now = DateTime.UtcNow;
            var isSubscriptionActive = settings.SubscriptionStatus == "active" && 
                (!settings.SubscriptionEndDate.HasValue || settings.SubscriptionEndDate > now);
            var isTrialActive = settings.SubscriptionStatus == "trial" && 
                (!settings.TrialEndDate.HasValue || settings.TrialEndDate > now);

            if (isSubscriptionActive || isTrialActive)
            {
                return Result.Failure("Already subscribed or in trial");
            }

            var trialEndDate = now.AddDays(14);
            settings.SubscriptionStatus = "trial";
            settings.TrialEndDate = trialEndDate;

            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user != null)
            {
                user.AddDomainEvent(new TrialStartedEvent(user, trialEndDate));
            }

            _settingsRepository.Update(settings);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
