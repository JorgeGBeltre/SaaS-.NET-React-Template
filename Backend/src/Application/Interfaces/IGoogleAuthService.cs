using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public record GoogleUserPayload(string Subject, string Email, string Name);

    public interface IGoogleAuthService
    {
        Task<GoogleUserPayload?> ValidateTokenAsync(string idToken, CancellationToken ct = default);
    }
}
