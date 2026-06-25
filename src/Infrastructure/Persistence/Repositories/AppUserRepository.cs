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

        public async Task<AppUser?> GetByIdAsync(int id)
        {
            return await _dbContext.AppUsers.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<AppUser?> GetByEmailAsync(string email)
        {
            return await _dbContext.AppUsers.FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _dbContext.AppUsers.AnyAsync(x => x.Email == email);
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
