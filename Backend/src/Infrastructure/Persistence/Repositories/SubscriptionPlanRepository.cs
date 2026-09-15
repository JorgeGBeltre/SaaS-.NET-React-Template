using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class SubscriptionPlanRepository : ISubscriptionPlanRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public SubscriptionPlanRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<SubscriptionPlan?> GetByIdAsync(int id)
        {
            return await _dbContext.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<SubscriptionPlan>> GetActivePlansAsync()
        {
            return await _dbContext.SubscriptionPlans
                .Where(x => x.IsActive)
                .ToListAsync();
        }

        public void Add(SubscriptionPlan plan)
        {
            _dbContext.SubscriptionPlans.Add(plan);
        }

        public void Update(SubscriptionPlan plan)
        {
            _dbContext.SubscriptionPlans.Update(plan);
        }
    }
}
