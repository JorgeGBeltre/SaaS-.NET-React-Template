using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class AppUserRepository : IAppUserRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public AppUserRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<AppUser?> GetByIdAsync(int id) => GetByIdAsync(id, CancellationToken.None);

        public async Task<AppUser?> GetByIdAsync(int id, CancellationToken ct)
        {
            return await _dbContext.AppUsers.FirstOrDefaultAsync(x => x.Id == id, ct);
        }

        public Task<AppUser?> GetByEmailAsync(string email) => GetByEmailAsync(email, CancellationToken.None);

        public async Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct)
        {
            return await _dbContext.AppUsers.FirstOrDefaultAsync(x => x.Email == email, ct);
        }

        public Task<AppUser?> GetByGoogleIdAsync(string googleId) => GetByGoogleIdAsync(googleId, CancellationToken.None);

        public async Task<AppUser?> GetByGoogleIdAsync(string googleId, CancellationToken ct)
        {
            return await _dbContext.AppUsers.FirstOrDefaultAsync(x => x.GoogleId == googleId, ct);
        }

        public Task<bool> ExistsByEmailAsync(string email) => ExistsByEmailAsync(email, CancellationToken.None);

        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct)
        {
            return await _dbContext.AppUsers.AnyAsync(x => x.Email == email, ct);
        }

        public void Add(AppUser user)
        {
            _dbContext.AppUsers.Add(user);
        }

        public void Update(AppUser user)
        {
            _dbContext.AppUsers.Update(user);
        }

        public void Delete(AppUser user)
        {
            _dbContext.AppUsers.Remove(user);
        }
    }
}
