using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Common.Models.Payments;
using MediatR;
using Shared;

namespace Application.Features.Payments.Queries
{
    public record GetAvailableProvidersQuery : IRequest<Result<List<PaymentProviderInfo>>>;

    public class GetAvailableProvidersQueryHandler : IRequestHandler<GetAvailableProvidersQuery, Result<List<PaymentProviderInfo>>>
    {
        private readonly IPaymentStrategyResolver _strategyResolver;

        public GetAvailableProvidersQueryHandler(IPaymentStrategyResolver strategyResolver)
        {
            _strategyResolver = strategyResolver;
        }

        public Task<Result<List<PaymentProviderInfo>>> Handle(
            GetAvailableProvidersQuery request,
            CancellationToken cancellationToken)
        {
            var infos = _strategyResolver.GetProviderInfos().ToList();
            return Task.FromResult(Result.Success(infos));
        }
    }
}
