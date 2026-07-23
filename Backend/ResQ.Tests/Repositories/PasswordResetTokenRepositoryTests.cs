using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Repositories.Auth;

namespace ResQ.Tests.Repositories;

public class PasswordResetTokenRepositoryTests : IDisposable
{
    private readonly ResQDbContext _db;
    private readonly PasswordResetTokenRepository _sut;

    public PasswordResetTokenRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ResQDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new ResQDbContext(options);
        _sut = new PasswordResetTokenRepository(_db);
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

    private async Task<PasswordResetToken> SeedTokenAsync(
        int userId,
        string tokenHash,
        bool isUsed = false,
        DateTime? expiresAt = null)
    {
        var token = new PasswordResetToken
        {
            UserId    = userId,
            TokenHash = tokenHash,
            IsUsed    = isUsed,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        };
        _db.PasswordResetTokens.Add(token);
        await _db.SaveChangesAsync();
        return token;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByTokenHashAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByTokenHashAsync_WhenTokenExists_ReturnsTokenWithUserLoaded()
    {
        // Arrange
        var user = await SeedUserAsync("owner@test.com");
        await SeedTokenAsync(user.Id, "hash-abc");

        // Act
        var result = await _sut.GetByTokenHashAsync("hash-abc");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
        Assert.NotNull(result.User);
        Assert.Equal("owner@test.com", result.User.Email);
    }

    [Fact]
    public async Task GetByTokenHashAsync_WhenTokenNotFound_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByTokenHashAsync("missing-hash");

        // Assert
        Assert.Null(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetActiveByUserIdAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetActiveByUserIdAsync_ReturnsOnlyUnusedUnexpiredTokensForUser()
    {
        // Arrange
        var user1 = await SeedUserAsync("u1@test.com");
        var user2 = await SeedUserAsync("u2@test.com");
        await SeedTokenAsync(user1.Id, "active-1");
        await SeedTokenAsync(user1.Id, "active-2");
        await SeedTokenAsync(user2.Id, "other-user");

        // Act
        var result = (await _sut.GetActiveByUserIdAsync(user1.Id)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Equal(user1.Id, t.UserId));
    }

    [Fact]
    public async Task GetActiveByUserIdAsync_ExcludesUsedTokens()
    {
        // Arrange
        var user = await SeedUserAsync();
        await SeedTokenAsync(user.Id, "used", isUsed: true);
        await SeedTokenAsync(user.Id, "unused");

        // Act
        var result = (await _sut.GetActiveByUserIdAsync(user.Id)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("unused", result[0].TokenHash);
    }

    [Fact]
    public async Task GetActiveByUserIdAsync_ExcludesExpiredTokens()
    {
        // Arrange
        var user = await SeedUserAsync();
        await SeedTokenAsync(user.Id, "expired", expiresAt: DateTime.UtcNow.AddHours(-1));
        await SeedTokenAsync(user.Id, "active", expiresAt: DateTime.UtcNow.AddHours(1));

        // Act
        var result = (await _sut.GetActiveByUserIdAsync(user.Id)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("active", result[0].TokenHash);
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
