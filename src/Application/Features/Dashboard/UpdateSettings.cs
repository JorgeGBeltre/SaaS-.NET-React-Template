using System.Threading;
using System.Threading.Tasks;
using Domain.Repositories;
using MediatR;
using Shared;

namespace Application.Features.Dashboard
{
    public record UpdateSettingsCommand(
        string Email,
        bool NotifyComments,
        bool NotifyUpdates,
        bool NotifyMarketing) : IRequest<Result>;

    public class UpdateSettingsCommandHandler : IRequestHandler<UpdateSettingsCommand, Result>
    {
        private readonly IUserSettingsRepository _settingsRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateSettingsCommandHandler(IUserSettingsRepository settingsRepository, IUnitOfWork unitOfWork)
        {
            _settingsRepository = settingsRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(UpdateSettingsCommand request, CancellationToken cancellationToken)
        {
            var settings = await _settingsRepository.GetByEmailAsync(request.Email);
            if (settings == null)
            {
                return Result.Failure("Settings not found");
            }

            settings.NotifyComments = request.NotifyComments;
            settings.NotifyUpdates = request.NotifyUpdates;
            settings.NotifyMarketing = request.NotifyMarketing;

            _settingsRepository.Update(settings);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
