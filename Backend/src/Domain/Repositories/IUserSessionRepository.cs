using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Repositories
{
    public interface IUserSessionRepository
    {
        Task<UserSession?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken ct = default);
        Task<UserSession?> GetActiveByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken ct = default);
        Task<List<UserSession>> GetActiveSessionsByUserIdAsync(int userId, CancellationToken ct = default);
        Task<List<UserSession>> GetInactiveSessionsByUserIdAsync(int userId, CancellationToken ct = default);
        void Add(UserSession session);
        void Update(UserSession session);
        Task RevokeAllUserSessionsAsync(int userId, CancellationToken ct = default);
    }
}
