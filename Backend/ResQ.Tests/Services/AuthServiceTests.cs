using FluentResults;
using Microsoft.Extensions.Options;
using Moq;
using ResQ.API.DTOs.Auth;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Settings;
using ResQ.API.Repositories.Auth;
using ResQ.API.Services.Auth;
using ResQ.API.Services.Email;
using ResQ.API.Services.Jwt;
using ResQ.API.Services.Password;

namespace ResQ.Tests.Services;

public class AuthServiceTests
{
    // ─── Mocks ────────────────────────────────────────────────────────────────

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUserRoleRepository> _userRoles = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IConsumerProfileRepository> _consumerProfiles = new();
    private readonly Mock<IMerchantProfileRepository> _merchantProfiles = new();
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetTokens = new();
    private readonly Mock<IPasswordService> _password = new();
    private readonly Mock<IJwtService> _jwt = new();
    private readonly Mock<IEmailService> _email = new();

    private readonly IOptions<JwtSettings> _jwtOptions = Options.Create(new JwtSettings
    {
        AccessTokenExpirationMinutes = 15,
        RefreshTokenExpirationDays   = 7
    });

    private readonly IOptions<MpSettings> _mpOptions = Options.Create(new MpSettings
    {
        FrontendBaseUrl = "https://resq.test"
    });

    private AuthService CreateSut() => new(
        _users.Object,
        _userRoles.Object,
        _refreshTokens.Object,
        _consumerProfiles.Object,
        _merchantProfiles.Object,
        _passwordResetTokens.Object,
        _password.Object,
        _jwt.Object,
        _email.Object,
        _jwtOptions,
        _mpOptions);

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void SetupJwtIssue(string accessToken = "at", string refreshToken = "rt")
    {
        _jwt.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<Role>>(), It.IsAny<int?>()))
            .Returns(accessToken);
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns(refreshToken);
        _refreshTokens.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _refreshTokens.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RegisterConsumerAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RegisterConsumerAsync_WhenEmailAlreadyRegistered_ReturnsConflictError()
    {
        // Arrange
        var request = new RegisterConsumerRequest { Email = "test@test.com", Password = "pass" };
        _users.Setup(u => u.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
              .ReturnsAsync(true);

        var sut = CreateSut();

        // Act
        var result = await sut.RegisterConsumerAsync(request);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("ya está registrado", result.Errors[0].Message);
    }

    [Fact]
    public async Task RegisterConsumerAsync_WhenEmailNotRegistered_CreatesUserAndReturnsOk()
    {
        // Arrange
        var request = new RegisterConsumerRequest
        {
            Email       = "new@test.com",
            Password    = "pass123",
            FirstName   = "Ana",
            LastName    = "García",
            PhoneNumber = "111-222"
        };

        _users.Setup(u => u.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
              .ReturnsAsync(false);
        _users.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);
        _users.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _userRoles.Setup(r => r.AddAsync(It.IsAny<UserRole>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        _consumerProfiles.Setup(cp => cp.AddAsync(It.IsAny<ConsumerProfile>(), It.IsAny<CancellationToken>()))
                         .Returns(Task.CompletedTask);
        _password.Setup(p => p.Hash(request.Password)).Returns("hashed");
        SetupJwtIssue();

        var sut = CreateSut();

        // Act
        var result = await sut.RegisterConsumerAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("at", result.Value.AccessToken);
        Assert.Equal("rt", result.Value.RefreshToken);
        Assert.Equal("Consumer", result.Value.Role);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RegisterMerchantAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RegisterMerchantAsync_WhenEmailAlreadyRegistered_ReturnsConflictError()
    {
        // Arrange
        var request = new RegisterMerchantRequest { Email = "merchant@test.com", Password = "pass" };
        _users.Setup(u => u.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
              .ReturnsAsync(true);

        var sut = CreateSut();

        // Act
        var result = await sut.RegisterMerchantAsync(request);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("ya está registrado", result.Errors[0].Message);
    }

    [Fact]
    public async Task RegisterMerchantAsync_WhenEmailNotRegistered_CreatesUserAndReturnsOk()
    {
        // Arrange
        var request = new RegisterMerchantRequest
        {
            Email        = "shop@test.com",
            Password     = "pass123",
            BusinessName = "La Panadería",
            Cuit         = "30-12345678-9",
            Address      = "Av. Siempre Viva 123",
            Latitude     = -31.4m,
            Longitude    = -64.2m,
            ContactPhone = "351-000000"
        };

        _users.Setup(u => u.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
              .ReturnsAsync(false);
        _users.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);
        _users.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _userRoles.Setup(r => r.AddAsync(It.IsAny<UserRole>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        _merchantProfiles.Setup(mp => mp.AddAsync(It.IsAny<MerchantProfile>(), It.IsAny<CancellationToken>()))
                         .Returns(Task.CompletedTask);
        _password.Setup(p => p.Hash(request.Password)).Returns("hashed");
        SetupJwtIssue();

        var sut = CreateSut();

        // Act
        var result = await sut.RegisterMerchantAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Merchant", result.Value.Role);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LoginAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ReturnsUnauthorizedError()
    {
        // Arrange
        var request = new LoginRequest("missing@test.com", "pass");
        _users.Setup(u => u.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
              .ReturnsAsync((User?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.LoginAsync(request);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("Credenciales inválidas", result.Errors[0].Message);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordInvalid_ReturnsUnauthorizedError()
    {
        // Arrange
        var request = new LoginRequest("user@test.com", "wrongpass");
        var user = new User { Id = 1, Email = "user@test.com", PasswordHash = "hash", IsActive = true };

        _users.Setup(u => u.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);
        _password.Setup(p => p.Verify(request.Password, user.PasswordHash)).Returns(false);

        var sut = CreateSut();

        // Act
        var result = await sut.LoginAsync(request);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("Credenciales inválidas", result.Errors[0].Message);
    }

    [Fact]
    public async Task LoginAsync_WhenAccountInactive_ReturnsUnauthorizedError()
    {
        // Arrange
        var request = new LoginRequest("inactive@test.com", "pass");
        var user = new User { Id = 1, Email = "inactive@test.com", PasswordHash = "hash", IsActive = false };

        _users.Setup(u => u.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);
        _password.Setup(p => p.Verify(request.Password, user.PasswordHash)).Returns(true);

        var sut = CreateSut();

        // Act
        var result = await sut.LoginAsync(request);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("desactivada", result.Errors[0].Message);
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsValid_IssuesTokensAndReturnsOk()
    {
        // Arrange
        var request = new LoginRequest("user@test.com", "pass");
        var user = new User { Id = 1, Email = "user@test.com", PasswordHash = "hash", IsActive = true };
        var roles = new List<UserRole> { new() { UserId = 1, Role = Role.Consumer } };
        var profile = new ConsumerProfile { Id = 5, UserId = 1 };

        _users.Setup(u => u.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);
        _password.Setup(p => p.Verify(request.Password, user.PasswordHash)).Returns(true);
        _userRoles.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(roles);
        _consumerProfiles.Setup(cp => cp.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(profile);
        SetupJwtIssue();

        var sut = CreateSut();

        // Act
        var result = await sut.LoginAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("at", result.Value.AccessToken);
        Assert.Equal("Consumer", result.Value.Role);
        Assert.Equal(profile.Id, result.Value.ProfileId);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RefreshTokenAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RefreshTokenAsync_WhenTokenNotFound_ReturnsUnauthorizedError()
    {
        // Arrange
        _refreshTokens.Setup(r => r.GetByTokenAsync("bad-token", It.IsAny<CancellationToken>()))
                      .ReturnsAsync((RefreshToken?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.RefreshTokenAsync("bad-token");

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("inválido", result.Errors[0].Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenTokenRevoked_ReturnsUnauthorizedError()
    {
        // Arrange
        var stored = new RefreshToken
        {
            Token     = "rt",
            IsRevoked = true,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            User      = new User { UserRoles = [new UserRole { Role = Role.Consumer }] }
        };
        _refreshTokens.Setup(r => r.GetByTokenAsync("rt", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(stored);

        var sut = CreateSut();

        // Act
        var result = await sut.RefreshTokenAsync("rt");

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("expirado o revocado", result.Errors[0].Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenTokenExpired_ReturnsUnauthorizedError()
    {
        // Arrange
        var stored = new RefreshToken
        {
            Token     = "rt",
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            User      = new User { UserRoles = [new UserRole { Role = Role.Consumer }] }
        };
        _refreshTokens.Setup(r => r.GetByTokenAsync("rt", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(stored);

        var sut = CreateSut();

        // Act
        var result = await sut.RefreshTokenAsync("rt");

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("expirado o revocado", result.Errors[0].Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenTokenValid_RotatesAndReturnsNewTokens()
    {
        // Arrange
        var user = new User
        {
            Id        = 1,
            UserRoles = [new UserRole { Role = Role.Consumer }]
        };
        var stored = new RefreshToken
        {
            Token     = "old-rt",
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            User      = user
        };
        var profile = new ConsumerProfile { Id = 3, UserId = 1 };

        _refreshTokens.Setup(r => r.GetByTokenAsync("old-rt", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(stored);
        _refreshTokens.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        _refreshTokens.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _consumerProfiles.Setup(cp => cp.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(profile);
        _jwt.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<Role>>(), It.IsAny<int?>()))
            .Returns("new-at");
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns("new-rt");

        var sut = CreateSut();

        // Act
        var result = await sut.RefreshTokenAsync("old-rt");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new-at", result.Value.AccessToken);
        Assert.Equal("new-rt", result.Value.RefreshToken);
        Assert.True(stored.IsRevoked);
        Assert.Equal("new-rt", stored.ReplacedByToken);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LogoutAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task LogoutAsync_WhenTokenNotFound_ReturnsOkWithoutUpdate()
    {
        // Arrange
        _refreshTokens.Setup(r => r.GetByTokenAsync("missing", It.IsAny<CancellationToken>()))
                      .ReturnsAsync((RefreshToken?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.LogoutAsync("missing");

        // Assert
        Assert.True(result.IsSuccess);
        _refreshTokens.Verify(r => r.Update(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_WhenTokenFound_RevokesTokenAndReturnsOk()
    {
        // Arrange
        var stored = new RefreshToken { Token = "rt", IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddDays(7) };
        _refreshTokens.Setup(r => r.GetByTokenAsync("rt", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(stored);
        _refreshTokens.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.LogoutAsync("rt");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(stored.IsRevoked);
        _refreshTokens.Verify(r => r.Update(stored), Times.Once);
    }
}
