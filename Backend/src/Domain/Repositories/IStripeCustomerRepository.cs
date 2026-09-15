using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Repositories
{
    public interface IStripeCustomerRepository
    {
        Task<StripeCustomer?> GetByUserIdAsync(int userId);
        Task<StripeCustomer?> GetByEmailAsync(string email);
        void Add(StripeCustomer customer);
        void Update(StripeCustomer customer);
    }
}
