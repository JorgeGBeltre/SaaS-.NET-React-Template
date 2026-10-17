using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class PaymentCustomerRepository : IPaymentCustomerRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public PaymentCustomerRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PaymentCustomer?> GetByIdAsync(int id)
        {
            return await _dbContext.PaymentCustomers
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PaymentCustomer?> GetByUserIdAndProviderAsync(int userId, PaymentProvider provider)
        {
            return await _dbContext.PaymentCustomers
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Provider == provider);
        }

        public async Task<PaymentCustomer?> GetActiveByUserIdAsync(int userId)
        {
            return await _dbContext.PaymentCustomers
                .Include(x => x.User)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.IsDefault)
                .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<PaymentCustomer>> GetAllByUserIdAsync(int userId)
        {
            return await _dbContext.PaymentCustomers
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.IsDefault)
                .ToListAsync();
        }

        public async Task<PaymentCustomer?> GetByProviderCustomerIdAsync(PaymentProvider provider, string providerCustomerId)
        {
            return await _dbContext.PaymentCustomers
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Provider == provider && x.ProviderCustomerId == providerCustomerId);
        }

        public void Add(PaymentCustomer customer)
        {
            _dbContext.PaymentCustomers.Add(customer);
        }

        public void Update(PaymentCustomer customer)
        {
            _dbContext.PaymentCustomers.Update(customer);
        }
    }
}
