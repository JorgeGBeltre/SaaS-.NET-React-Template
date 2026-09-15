using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class UserSessionRepository : IUserSessionRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public UserSessionRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserSession?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken ct = default)
        {
            return await _dbContext.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshTokenHash, ct);
        }

        public async Task<UserSession?> GetActiveByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken ct = default)
        {
            return await _dbContext.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshTokenHash && s.IsActive && s.ExpiresAt > DateTime.UtcNow, ct);
        }

        public async Task<List<UserSession>> GetActiveSessionsByUserIdAsync(int userId, CancellationToken ct = default)
        {
            return await _dbContext.UserSessions
                .Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(ct);
        }

        public async Task<List<UserSession>> GetInactiveSessionsByUserIdAsync(int userId, CancellationToken ct = default)
        {
            return await _dbContext.UserSessions
                .Where(s => s.UserId == userId && !s.IsActive)
                .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
                .Take(20)
                .ToListAsync(ct);
        }

        public void Add(UserSession session)
        {
            _dbContext.UserSessions.Add(session);
        }

        public void Update(UserSession session)
        {
            _dbContext.UserSessions.Update(session);
        }

        public async Task RevokeAllUserSessionsAsync(int userId, CancellationToken ct = default)
        {
            var sessions = await _dbContext.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync(ct);

            foreach (var session in sessions)
            {
                session.IsActive = false;
                _dbContext.UserSessions.Update(session);
            }
        }
    }
}
