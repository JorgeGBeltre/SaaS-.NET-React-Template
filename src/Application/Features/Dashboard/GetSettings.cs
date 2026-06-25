using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Repositories;
using MediatR;
using Shared;

namespace Application.Features.Dashboard
{
    public record GetSettingsQuery(string Email) : IRequest<Result<SettingsDto>>;

    public class GetSettingsQueryHandler : IRequestHandler<GetSettingsQuery, Result<SettingsDto>>
    {
        private readonly IUserSettingsRepository _settingsRepository;

        public GetSettingsQueryHandler(IUserSettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;
        }

        public async Task<Result<SettingsDto>> Handle(GetSettingsQuery request, CancellationToken cancellationToken)
        {
            var settings = await _settingsRepository.GetByEmailAsync(request.Email);
            if (settings == null)
            {
                return Result.Failure<SettingsDto>("Settings not found");
            }

            var now = DateTime.UtcNow;
            var isSubscriptionActive = settings.SubscriptionStatus == "active" && 
                (!settings.SubscriptionEndDate.HasValue || settings.SubscriptionEndDate > now);
            var isTrialActive = settings.SubscriptionStatus == "trial" && 
                (!settings.TrialEndDate.HasValue || settings.TrialEndDate > now);

            return Result.Success(new SettingsDto
            {
                Notifications = new NotificationsDto
                {
                    Comments = settings.NotifyComments,
                    Updates = settings.NotifyUpdates,
                    Marketing = settings.NotifyMarketing
                },
                Api = new ApiKeyDto
                {
                    HasKey = !string.IsNullOrEmpty(settings.ApiKey),
                    KeyCreatedAt = settings.ApiKeyCreatedAt
                },
                Subscription = new SubscriptionDetailsDto
                {
                    Plan = settings.SubscriptionPlan?.Name,
                    Status = settings.SubscriptionStatus,
                    IsActive = isSubscriptionActive,
                    IsTrial = isTrialActive
                }
            });
        }
    }
}
