using System.Threading;
using System.Threading.Tasks;
using Domain.Enums;

namespace Application.Interfaces
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string email, string code, OtpPurpose purpose, CancellationToken ct = default);
    }
}
