using System.Security.Claims;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;

namespace ResQ.API.Services.Jwt;

public interface IJwtService
{
    string GenerateAccessToken(User user, IEnumerable<Role> roles, int? profileId);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
