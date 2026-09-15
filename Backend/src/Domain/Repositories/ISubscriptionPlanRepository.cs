using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Repositories
{
    public interface ISubscriptionPlanRepository
    {
        Task<SubscriptionPlan?> GetByIdAsync(int id);
        Task<List<SubscriptionPlan>> GetActivePlansAsync();
        void Add(SubscriptionPlan plan);
        void Update(SubscriptionPlan plan);
    }
}
