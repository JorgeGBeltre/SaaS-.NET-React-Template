using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Domain.Repositories;
using MediatR;
using Shared;

namespace Application.Features.Dashboard
{
    public record GenerateApiKeyCommand(string Email) : IRequest<Result<string>>;

    public class GenerateApiKeyCommandHandler : IRequestHandler<GenerateApiKeyCommand, Result<string>>
    {
        private readonly IUserSettingsRepository _settingsRepository;
        private readonly IUnitOfWork _unitOfWork;

        public GenerateApiKeyCommandHandler(IUserSettingsRepository settingsRepository, IUnitOfWork unitOfWork)
        {
            _settingsRepository = settingsRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<string>> Handle(GenerateApiKeyCommand request, CancellationToken cancellationToken)
        {
            var settings = await _settingsRepository.GetByEmailAsync(request.Email);
            if (settings == null)
            {
                return Result.Failure<string>("Settings not found");
            }

            var keyBytes = RandomNumberGenerator.GetBytes(32);
            settings.ApiKey = Convert.ToBase64String(keyBytes);
            settings.ApiKeyCreatedAt = DateTime.UtcNow;

            _settingsRepository.Update(settings);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(settings.ApiKey);
        }
    }
}
