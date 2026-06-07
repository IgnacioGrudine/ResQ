using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Settings;

namespace ResQ.API.Services.Jwt;

/// <summary>
/// Generates and validates JWT access tokens and opaque refresh tokens for the ResQ platform.
/// </summary>
/// <remarks>
/// Access tokens are signed HS256 JWTs that embed the user ID (<c>sub</c>), email, roles,
/// and profile ID as custom claims. Refresh tokens are cryptographically random opaque strings
/// (64 bytes, Base64-encoded) stored in the database — they carry no intrinsic claims.
/// </remarks>
public class JwtService(IOptions<JwtSettings> jwtOptions) : IJwtService
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    /// <summary>
    /// Generates a signed HS256 JWT access token containing identity and authorisation claims.
    /// </summary>
    /// <param name="user">The authenticated user entity providing <c>sub</c> and email claims.</param>
    /// <param name="roles">
    /// The set of roles to embed as <see cref="ClaimTypes.Role"/> claims.
    /// Typically a single role (Consumer or Merchant) for ResQ users.
    /// </param>
    /// <param name="profileId">
    /// The profile ID (<see cref="ConsumerProfile.Id"/> or <see cref="MerchantProfile.Id"/>)
    /// embedded as the custom <c>profileId</c> claim. Pass <c>null</c> for admin users or
    /// when no profile has been created yet.
    /// </param>
    /// <returns>A compact, URL-safe JWT string ready to be returned to the client.</returns>
    public string GenerateAccessToken(User user, IEnumerable<Role> roles, int? profileId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));

        if (profileId.HasValue)
            claims.Add(new Claim("profileId", profileId.Value.ToString()));

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _jwt.Issuer,
            audience:           _jwt.Audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a cryptographically random opaque refresh token.
    /// </summary>
    /// <returns>
    /// A Base64-encoded string of 64 random bytes (88 characters), suitable for use as
    /// a long-lived, database-persisted refresh token.
    /// </returns>
    /// <remarks>
    /// The token carries no claims; its validity is determined solely by the database record
    /// (expiry, revocation flag). Using 64 random bytes provides 512 bits of entropy.
    /// </remarks>
    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Validates a JWT's signature, issuer, and audience — but intentionally ignores token lifetime —
    /// and returns the claims principal it contains.
    /// </summary>
    /// <param name="token">The compact JWT string to validate.</param>
    /// <returns>
    /// A <see cref="ClaimsPrincipal"/> extracted from the token if the signature is valid;
    /// <c>null</c> if validation fails for any reason (invalid signature, malformed token, etc.).
    /// </returns>
    /// <remarks>
    /// Lifetime validation is deliberately disabled (<c>ValidateLifetime = false</c>) because
    /// this method is intended for the refresh-token rotation flow, where an expired but
    /// otherwise valid access token must still be readable to obtain the user's identity
    /// before issuing a new token pair.
    /// </remarks>
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey        = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey)),
            ValidateIssuer          = true,
            ValidIssuer             = _jwt.Issuer,
            ValidateAudience        = true,
            ValidAudience           = _jwt.Audience,
            ValidateLifetime        = false, // ignora expiración intencionalmente
        };

        var handler = new JwtSecurityTokenHandler();

        try
        {
            return handler.ValidateToken(token, parameters, out _);
        }
        catch
        {
            return null;
        }
    }
}
