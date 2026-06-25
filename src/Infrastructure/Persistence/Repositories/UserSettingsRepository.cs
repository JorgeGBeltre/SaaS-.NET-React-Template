using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class UserSettingsRepository : IUserSettingsRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public UserSettingsRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserSettings?> GetByUserIdAsync(int userId)
        {
            return await _dbContext.UserSettings
                .Include(x => x.SubscriptionPlan)
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task<UserSettings?> GetByEmailAsync(string email)
        {
            return await _dbContext.UserSettings
                .Include(x => x.SubscriptionPlan)
                .FirstOrDefaultAsync(x => x.User.Email == email);
        }

        public void Add(UserSettings settings)
        {
            _dbContext.UserSettings.Add(settings);
        }

        public void Update(UserSettings settings)
        {
            _dbContext.UserSettings.Update(settings);
        }
    }
}
