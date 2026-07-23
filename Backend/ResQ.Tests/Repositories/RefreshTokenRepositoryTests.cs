using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;
using ResQ.API.Repositories.Auth;

namespace ResQ.Tests.Repositories;

public class RefreshTokenRepositoryTests : IDisposable
{
    private readonly ResQDbContext _db;
    private readonly RefreshTokenRepository _sut;

    public RefreshTokenRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ResQDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new ResQDbContext(options);
        _sut = new RefreshTokenRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<User> SeedUserAsync(string email = "test@test.com")
    {
        var user = new User
        {
            Email        = email,
            PasswordHash = "hash",
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private async Task<RefreshToken> SeedRefreshTokenAsync(
        int userId,
        string token,
        bool isRevoked = false,
        DateTime? expiresAt = null)
    {
        var refreshToken = new RefreshToken
        {
            UserId    = userId,
            Token     = token,
            IsRevoked = isRevoked,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();
        return refreshToken;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByTokenAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByTokenAsync_WhenTokenExists_ReturnsTokenWithUserAndRolesLoaded()
    {
        // Arrange
        var user = await SeedUserAsync("owner@test.com");
        _db.UserRoles.Add(new UserRole { UserId = user.Id, Role = Role.Consumer, CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();
        await SeedRefreshTokenAsync(user.Id, "rt-token");

        // Act
        var result = await _sut.GetByTokenAsync("rt-token");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
        Assert.NotNull(result.User);
        Assert.Equal("owner@test.com", result.User.Email);
        Assert.Single(result.User.UserRoles);
    }

    [Fact]
    public async Task GetByTokenAsync_WhenTokenNotFound_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByTokenAsync("missing-token");

        // Assert
        Assert.Null(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetActiveByUserIdAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetActiveByUserIdAsync_ReturnsOnlyNonRevokedUnexpiredTokensForUser()
    {
        // Arrange
        var user1 = await SeedUserAsync("u1@test.com");
        var user2 = await SeedUserAsync("u2@test.com");
        await SeedRefreshTokenAsync(user1.Id, "active-1");
        await SeedRefreshTokenAsync(user1.Id, "active-2");
        await SeedRefreshTokenAsync(user2.Id, "other-user");

        // Act
        var result = (await _sut.GetActiveByUserIdAsync(user1.Id)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, rt => Assert.Equal(user1.Id, rt.UserId));
    }

    [Fact]
    public async Task GetActiveByUserIdAsync_ExcludesRevokedTokens()
    {
        // Arrange
        var user = await SeedUserAsync();
        await SeedRefreshTokenAsync(user.Id, "revoked", isRevoked: true);
        await SeedRefreshTokenAsync(user.Id, "active");

        // Act
        var result = (await _sut.GetActiveByUserIdAsync(user.Id)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("active", result[0].Token);
    }

    [Fact]
    public async Task GetActiveByUserIdAsync_ExcludesExpiredTokens()
    {
        // Arrange
        var user = await SeedUserAsync();
        await SeedRefreshTokenAsync(user.Id, "expired", expiresAt: DateTime.UtcNow.AddDays(-1));
        await SeedRefreshTokenAsync(user.Id, "active", expiresAt: DateTime.UtcNow.AddDays(1));

        // Act
        var result = (await _sut.GetActiveByUserIdAsync(user.Id)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("active", result[0].Token);
    }

    [Fact]
    public async Task GetActiveByUserIdAsync_WhenNoActiveTokens_ReturnsEmpty()
    {
        // Arrange
        var user = await SeedUserAsync();

        // Act
        var result = await _sut.GetActiveByUserIdAsync(user.Id);

        // Assert
        Assert.Empty(result);
    }
}
