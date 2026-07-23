using Microsoft.Extensions.Options;
using Moq;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Settings;
using ResQ.API.Repositories.Auth;
using ResQ.API.Services.Auth;
using ResQ.API.Services.Jwt;

namespace ResQ.Tests.Services;

/// <summary>
/// GoogleAuthService delegates ID token verification to the static
/// <c>Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync</c>, which performs real
/// cryptographic validation against Google's public certificates over the network. That call
/// sits behind no injectable abstraction, so it cannot be mocked. Only the deterministic,
/// offline-verifiable failure path — a malformed ID token that fails Google's own JWT format
/// check before any network call is attempted — can be exercised here. The success paths (new
/// user creation, existing user linking, role resolution, token issuance) all require a real,
/// currently-valid Google-signed JWT and are not unit-testable without introducing a wrapper
/// interface around GoogleJsonWebSignature in production code.
/// </summary>
public class GoogleAuthServiceTests
{
    // ─── Mocks ────────────────────────────────────────────────────────────────

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUserRoleRepository> _userRoles = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IConsumerProfileRepository> _consumerProfiles = new();
    private readonly Mock<IJwtService> _jwt = new();

    private readonly IOptions<JwtSettings> _jwtOptions = Options.Create(new JwtSettings
    {
        AccessTokenExpirationMinutes = 15,
        RefreshTokenExpirationDays   = 7
    });

    private readonly IOptions<GoogleSettings> _googleOptions = Options.Create(new GoogleSettings
    {
        ClientId = "test-client-id.apps.googleusercontent.com"
    });

    private GoogleAuthService CreateSut() => new(
        _users.Object,
        _userRoles.Object,
        _refreshTokens.Object,
        _consumerProfiles.Object,
        _jwt.Object,
        _jwtOptions,
        _googleOptions);

    // ═══════════════════════════════════════════════════════════════════════════
    // LoginWithGoogleAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task LoginWithGoogleAsync_WithMalformedIdToken_ReturnsUnauthorizedError()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        // "not-a-valid-google-id-token" has no '.' separators, so Google's own parser rejects
        // it as malformed before any network call is made — this is deterministic and offline.
        var result = await sut.LoginWithGoogleAsync("not-a-valid-google-id-token");

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("inválido", result.Errors[0].Message);
        _users.Verify(u => u.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _jwt.Verify(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<Role>>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task LoginWithGoogleAsync_WithEmptyIdToken_ReturnsUnauthorizedError()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.LoginWithGoogleAsync(string.Empty);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("inválido", result.Errors[0].Message);
        _refreshTokens.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
