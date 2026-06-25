using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Repositories;
using MediatR;
using Shared;

namespace Application.Features.Dashboard
{
    public class SubscriptionPlanDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Interval { get; set; } = string.Empty;
        public List<string> Features { get; set; } = new();
    }

    public record GetSubscriptionPlansQuery() : IRequest<Result<List<SubscriptionPlanDto>>>;

    public class GetSubscriptionPlansQueryHandler : IRequestHandler<GetSubscriptionPlansQuery, Result<List<SubscriptionPlanDto>>>
    {
        private readonly ISubscriptionPlanRepository _planRepository;

        public GetSubscriptionPlansQueryHandler(ISubscriptionPlanRepository planRepository)
        {
            _planRepository = planRepository;
        }

        public async Task<Result<List<SubscriptionPlanDto>>> Handle(GetSubscriptionPlansQuery request, CancellationToken cancellationToken)
        {
            var plans = await _planRepository.GetActivePlansAsync();
            var dtos = plans.Select(p => new SubscriptionPlanDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Price = p.Price,
                Interval = p.Interval,
                Features = p.Features
            }).ToList();

            return Result.Success(dtos);
        }
    }
}
