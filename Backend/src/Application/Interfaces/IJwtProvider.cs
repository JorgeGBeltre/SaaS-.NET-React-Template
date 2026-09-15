using System.Security.Claims;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IJwtProvider
    {
        string Generate(AppUser user);
        string GenerateRefreshToken();
        string HashToken(string token);
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
