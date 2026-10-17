using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories
{
    public interface IPaymentTransactionRepository
    {
        Task<PaymentTransaction?> GetByIdAsync(int id);
        Task<PaymentTransaction?> GetByProviderTransactionIdAsync(string providerTransactionId);
        Task<List<PaymentTransaction>> GetByUserIdAsync(int userId, int limit = 50);
        void Add(PaymentTransaction transaction);
        void Update(PaymentTransaction transaction);
    }
}
