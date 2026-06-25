using Domain.Entities;

namespace Application.Interfaces
{
    public interface IJwtProvider
    {
        string Generate(AppUser user);
    }
}
