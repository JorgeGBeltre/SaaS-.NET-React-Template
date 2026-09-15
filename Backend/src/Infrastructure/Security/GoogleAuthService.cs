using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Security
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleAuthService> _logger;

        public GoogleAuthService(IConfiguration configuration, ILogger<GoogleAuthService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<GoogleUserPayload?> ValidateTokenAsync(string idToken, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                return null;

            try
            {
                var clientId = _configuration["Authentication:Google:ClientId"];
                var settings = new GoogleJsonWebSignature.ValidationSettings();
                if (!string.IsNullOrWhiteSpace(clientId))
                {
                    settings.Audience = new[] { clientId };
                }

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
                if (payload == null)
                    return null;

                return new GoogleUserPayload(payload.Subject, payload.Email, payload.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Google ID token validation failed.");
                return null;
            }
        }
    }
}
