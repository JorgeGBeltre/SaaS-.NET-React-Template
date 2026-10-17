using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class PaymentTransactionRepository : IPaymentTransactionRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public PaymentTransactionRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PaymentTransaction?> GetByIdAsync(int id)
        {
            return await _dbContext.PaymentTransactions
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PaymentTransaction?> GetByProviderTransactionIdAsync(string providerTransactionId)
        {
            return await _dbContext.PaymentTransactions
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.ProviderTransactionId == providerTransactionId);
        }

        public async Task<List<PaymentTransaction>> GetByUserIdAsync(int userId, int limit = 50)
        {
            return await _dbContext.PaymentTransactions
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public void Add(PaymentTransaction transaction)
        {
            _dbContext.PaymentTransactions.Add(transaction);
        }

        public void Update(PaymentTransaction transaction)
        {
            _dbContext.PaymentTransactions.Update(transaction);
        }
    }
}
