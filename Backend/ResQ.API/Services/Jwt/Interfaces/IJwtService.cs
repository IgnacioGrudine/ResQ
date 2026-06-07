using System.Security.Claims;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;

namespace ResQ.API.Services.Jwt;

public interface IJwtService
{
    /// <summary>
    /// Generates a signed JWT access token for the given user, embedding their roles
    /// and the identifier of their active profile as custom claims.
    /// </summary>
    /// <param name="user">The authenticated user whose identifier and email are embedded in the token.</param>
    /// <param name="roles">The set of roles assigned to the user, encoded as role-claim entries in the token payload.</param>
    /// <param name="profileId">
    /// The identifier of the consumer or merchant profile associated with the user,
    /// or <c>null</c> if no profile has been created yet.
    /// </param>
    /// <returns>A compact, signed JWT string ready to be returned to the client.</returns>
    string GenerateAccessToken(User user, IEnumerable<Role> roles, int? profileId);

    /// <summary>
    /// Generates a cryptographically random, opaque refresh token string.
    /// </summary>
    /// <remarks>
    /// The token is not a JWT; it is a random byte sequence encoded as a URL-safe Base64 string.
    /// It must be stored in the database and validated on every refresh request.
    /// </remarks>
    /// <returns>A URL-safe Base64-encoded random refresh token string.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates the structure and signature of an expired access token and returns
    /// the claims principal extracted from it, without checking the expiration claim.
    /// </summary>
    /// <remarks>
    /// Used during the token-refresh flow to extract the user's identity from an expired
    /// access token so the refresh token can be correlated and a new pair issued.
    /// </remarks>
    /// <param name="token">The expired JWT access token string to parse.</param>
    /// <returns>
    /// A <see cref="ClaimsPrincipal"/> containing the claims from the token payload,
    /// or <c>null</c> if the token signature is invalid or the token is malformed.
    /// </returns>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
