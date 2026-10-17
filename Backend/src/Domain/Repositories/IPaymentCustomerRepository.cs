using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories
{
    public interface IPaymentCustomerRepository
    {
        Task<PaymentCustomer?> GetByIdAsync(int id);
        Task<PaymentCustomer?> GetByUserIdAndProviderAsync(int userId, PaymentProvider provider);
        Task<PaymentCustomer?> GetActiveByUserIdAsync(int userId);
        Task<List<PaymentCustomer>> GetAllByUserIdAsync(int userId);
        Task<PaymentCustomer?> GetByProviderCustomerIdAsync(PaymentProvider provider, string providerCustomerId);
        void Add(PaymentCustomer customer);
        void Update(PaymentCustomer customer);
    }
}
