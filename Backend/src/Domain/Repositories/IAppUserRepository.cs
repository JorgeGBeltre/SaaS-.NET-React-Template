using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Repositories
{
    public interface IAppUserRepository
    {
        Task<AppUser?> GetByIdAsync(int id);
        Task<AppUser?> GetByEmailAsync(string email);
        Task<bool> ExistsByEmailAsync(string email);
        void Add(AppUser user);
        void Update(AppUser user);
        void Delete(AppUser user);
    }
}
