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

public class JwtService(IOptions<JwtSettings> jwtOptions) : IJwtService
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

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

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

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
