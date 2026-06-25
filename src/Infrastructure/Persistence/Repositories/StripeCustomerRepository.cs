using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class StripeCustomerRepository : IStripeCustomerRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public StripeCustomerRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<StripeCustomer?> GetByUserIdAsync(int userId)
        {
            return await _dbContext.StripeCustomers.FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task<StripeCustomer?> GetByEmailAsync(string email)
        {
            return await _dbContext.StripeCustomers
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.User.Email == email);
        }

        public void Add(StripeCustomer customer)
        {
            _dbContext.StripeCustomers.Add(customer);
        }

        public void Update(StripeCustomer customer)
        {
            _dbContext.StripeCustomers.Update(customer);
        }
    }
}
