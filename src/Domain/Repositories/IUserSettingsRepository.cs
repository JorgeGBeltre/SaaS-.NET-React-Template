using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Repositories
{
    public interface IUserSettingsRepository
    {
        Task<UserSettings?> GetByUserIdAsync(int userId);
        Task<UserSettings?> GetByEmailAsync(string email);
        void Add(UserSettings settings);
        void Update(UserSettings settings);
    }
}
