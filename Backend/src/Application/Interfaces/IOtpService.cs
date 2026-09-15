using System.Threading.Tasks;
using Domain.Enums;

namespace Application.Interfaces
{
    public interface IOtpService
    {
        Task<string> GenerateOtpAsync(int userId, OtpPurpose purpose, string? ipAddress = null);
        Task<bool> VerifyOtpAsync(int userId, OtpPurpose purpose, string code);
    }
}
