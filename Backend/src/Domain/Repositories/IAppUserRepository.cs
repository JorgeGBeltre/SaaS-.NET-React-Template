using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Repositories
{
    public interface IAppUserRepository
    {
        Task<AppUser?> GetByIdAsync(int id);
        Task<AppUser?> GetByIdAsync(int id, CancellationToken ct);
        Task<AppUser?> GetByEmailAsync(string email);
        Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct);
        Task<AppUser?> GetByGoogleIdAsync(string googleId);
        Task<AppUser?> GetByGoogleIdAsync(string googleId, CancellationToken ct);
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> ExistsByEmailAsync(string email, CancellationToken ct);
        void Add(AppUser user);
        void Update(AppUser user);
        void Delete(AppUser user);
    }
}
