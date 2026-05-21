using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ResQ.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetProfileId(this ClaimsPrincipal principal)
        => int.Parse(principal.FindFirstValue("profileId")!);

    public static int GetUserId(this ClaimsPrincipal principal)
        => int.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
}
