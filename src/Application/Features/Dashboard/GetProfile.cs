using System.Threading;
using System.Threading.Tasks;
using Domain.Repositories;
using MediatR;
using Shared;

namespace Application.Features.Dashboard
{
    public record GetProfileQuery(string Email) : IRequest<Result<ProfileDto>>;

    public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, Result<ProfileDto>>
    {
        private readonly IAppUserRepository _userRepository;

        public GetProfileQueryHandler(IAppUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<ProfileDto>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return Result.Failure<ProfileDto>("User not found");
            }

            return Result.Success(new ProfileDto
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            });
        }
    }
}
