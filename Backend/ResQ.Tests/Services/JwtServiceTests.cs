using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Settings;
using ResQ.API.Services.Jwt;

namespace ResQ.Tests.Services;

public class JwtServiceTests
{
    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static JwtSettings CreateSettings(
        string secretKey = "super-secret-test-signing-key-32chars!!",
        string issuer = "resq-test-issuer",
        string audience = "resq-test-audience",
        int accessTokenExpirationMinutes = 15,
        int refreshTokenExpirationDays = 7)
        => new()
        {
            SecretKey                    = secretKey,
            Issuer                       = issuer,
            Audience                     = audience,
            AccessTokenExpirationMinutes = accessTokenExpirationMinutes,
            RefreshTokenExpirationDays   = refreshTokenExpirationDays
        };

    private static JwtService CreateSut(JwtSettings? settings = null)
        => new(Options.Create(settings ?? CreateSettings()));

    private static string BuildRawToken(
        JwtSettings settings,
        DateTime expires,
        string? issuer = null,
        string? audience = null,
        string? secretKey = null)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey ?? settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             issuer ?? settings.Issuer,
            audience:           audience ?? settings.Audience,
            claims:             [new Claim(JwtRegisteredClaimNames.Sub, "1")],
            expires:            expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GenerateAccessToken
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void GenerateAccessToken_IncludesSubEmailAndJtiClaims()
    {
        // Arrange
        var sut  = CreateSut();
        var user = new User { Id = 42, Email = "user@test.com" };

        // Act
        var token = sut.GenerateAccessToken(user, [Role.Consumer], null);

        // Assert
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("42", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("user@test.com", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateAccessToken_IncludesRoleClaimForEachRole()
    {
        // Arrange
        var sut  = CreateSut();
        var user = new User { Id = 1, Email = "u@test.com" };

        // Act
        var token = sut.GenerateAccessToken(user, [Role.Consumer, Role.Merchant], null);

        // Assert
        var jwt        = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var roleClaims = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Contains("Consumer", roleClaims);
        Assert.Contains("Merchant", roleClaims);
    }

    [Fact]
    public void GenerateAccessToken_WhenProfileIdProvided_IncludesProfileIdClaim()
    {
        // Arrange
        var sut  = CreateSut();
        var user = new User { Id = 1, Email = "u@test.com" };

        // Act
        var token = sut.GenerateAccessToken(user, [Role.Consumer], 99);

        // Assert
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("99", jwt.Claims.First(c => c.Type == "profileId").Value);
    }

    [Fact]
    public void GenerateAccessToken_WhenProfileIdIsNull_OmitsProfileIdClaim()
    {
        // Arrange
        var sut  = CreateSut();
        var user = new User { Id = 1, Email = "u@test.com" };

        // Act
        var token = sut.GenerateAccessToken(user, [Role.Consumer], null);

        // Assert
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "profileId");
    }

    [Fact]
    public void GenerateAccessToken_SetsIssuerAndAudienceFromSettings()
    {
        // Arrange
        var settings = CreateSettings(issuer: "custom-issuer", audience: "custom-audience");
        var sut      = CreateSut(settings);
        var user     = new User { Id = 1, Email = "u@test.com" };

        // Act
        var token = sut.GenerateAccessToken(user, [Role.Consumer], null);

        // Assert
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("custom-issuer", jwt.Issuer);
        Assert.Contains("custom-audience", jwt.Audiences);
    }

    [Fact]
    public void GenerateAccessToken_SetsExpiryAccordingToSettings()
    {
        // Arrange
        var settings = CreateSettings(accessTokenExpirationMinutes: 30);
        var sut      = CreateSut(settings);
        var user     = new User { Id = 1, Email = "u@test.com" };
        var before   = DateTime.UtcNow;

        // Act
        var token = sut.GenerateAccessToken(user, [Role.Consumer], null);

        // Assert
        var jwt             = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var expectedExpiry  = before.AddMinutes(30);
        Assert.True(Math.Abs((jwt.ValidTo - expectedExpiry).TotalSeconds) < 10);
    }

    [Fact]
    public void GenerateAccessToken_CalledTwice_ProducesDifferentJtiClaims()
    {
        // Arrange
        var sut  = CreateSut();
        var user = new User { Id = 1, Email = "u@test.com" };

        // Act
        var token1 = sut.GenerateAccessToken(user, [Role.Consumer], null);
        var token2 = sut.GenerateAccessToken(user, [Role.Consumer], null);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jti1    = handler.ReadJwtToken(token1).Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2    = handler.ReadJwtToken(token2).Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        Assert.NotEqual(jti1, jti2);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GenerateRefreshToken
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64StringDecodingTo64Bytes()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var token = sut.GenerateRefreshToken();

        // Assert
        var bytes = Convert.FromBase64String(token);
        Assert.Equal(64, bytes.Length);
    }

    [Fact]
    public void GenerateRefreshToken_CalledTwice_ReturnsDifferentValues()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var token1 = sut.GenerateRefreshToken();
        var token2 = sut.GenerateRefreshToken();

        // Assert
        Assert.NotEqual(token1, token2);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetPrincipalFromExpiredToken
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetPrincipalFromExpiredToken_WithValidExpiredToken_ReturnsPrincipal()
    {
        // Arrange
        var settings = CreateSettings();
        var sut      = CreateSut(settings);
        var raw      = BuildRawToken(settings, DateTime.UtcNow.AddMinutes(-30));

        // Act
        var principal = sut.GetPrincipalFromExpiredToken(raw);

        // Assert
        Assert.NotNull(principal);
        Assert.Equal("1", principal!.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithValidNonExpiredToken_ReturnsPrincipal()
    {
        // Arrange
        var settings = CreateSettings();
        var sut      = CreateSut(settings);
        var user     = new User { Id = 7, Email = "u@test.com" };
        var token    = sut.GenerateAccessToken(user, [Role.Consumer], null);

        // Act
        var principal = sut.GetPrincipalFromExpiredToken(token);

        // Assert
        Assert.NotNull(principal);
        Assert.Equal("7", principal!.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithTamperedSignature_ReturnsNull()
    {
        // Arrange
        var settings = CreateSettings();
        var sut      = CreateSut(settings);
        var raw      = BuildRawToken(settings, DateTime.UtcNow.AddMinutes(-30),
            secretKey: "a-completely-different-signing-key-value!!");

        // Act
        var principal = sut.GetPrincipalFromExpiredToken(raw);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithWrongIssuer_ReturnsNull()
    {
        // Arrange
        var settings = CreateSettings();
        var sut      = CreateSut(settings);
        var raw      = BuildRawToken(settings, DateTime.UtcNow.AddMinutes(-30), issuer: "someone-else");

        // Act
        var principal = sut.GetPrincipalFromExpiredToken(raw);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithWrongAudience_ReturnsNull()
    {
        // Arrange
        var settings = CreateSettings();
        var sut      = CreateSut(settings);
        var raw      = BuildRawToken(settings, DateTime.UtcNow.AddMinutes(-30), audience: "someone-else");

        // Act
        var principal = sut.GetPrincipalFromExpiredToken(raw);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithMalformedToken_ReturnsNull()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var principal = sut.GetPrincipalFromExpiredToken("this-is-not-a-jwt");

        // Assert
        Assert.Null(principal);
    }
}
