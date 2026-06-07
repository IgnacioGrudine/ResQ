using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ResQ.API.Extensions;

/// <summary>
/// Extension methods on <see cref="ClaimsPrincipal"/> that provide typed access to
/// the custom claims embedded in ResQ JWT access tokens, eliminating repeated
/// string-based claim lookups across controllers and services.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extracts the authenticated user's role-specific profile identifier from the
    /// <c>profileId</c> custom claim embedded in the JWT.
    /// For a consumer token this is the <c>ConsumerProfile.Id</c>;
    /// for a merchant token this is the <c>MerchantProfile.Id</c>.
    /// </summary>
    /// <param name="principal">The claims principal representing the currently authenticated user.</param>
    /// <returns>The integer profile ID extracted from the <c>profileId</c> claim.</returns>
    public static int GetProfileId(this ClaimsPrincipal principal)
        => int.Parse(principal.FindFirstValue("profileId")!);

    /// <summary>
    /// Extracts the authenticated user's database identifier from the JWT <c>sub</c> claim
    /// (<see cref="JwtRegisteredClaimNames.Sub"/>), which is set to <c>User.Id</c> at token issuance.
    /// </summary>
    /// <param name="principal">The claims principal representing the currently authenticated user.</param>
    /// <returns>The integer user ID extracted from the <c>sub</c> claim.</returns>
    public static int GetUserId(this ClaimsPrincipal principal)
        => int.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
}
