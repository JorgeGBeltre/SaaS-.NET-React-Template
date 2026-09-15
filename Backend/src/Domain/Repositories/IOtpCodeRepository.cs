using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories
{
    public interface IOtpCodeRepository
    {
        Task<OtpCode?> GetActiveOtpAsync(int userId, OtpPurpose purpose);
        void Add(OtpCode otpCode);
        void Update(OtpCode otpCode);
        Task InvalidatePreviousOtpsAsync(int userId, OtpPurpose purpose);
    }
}
