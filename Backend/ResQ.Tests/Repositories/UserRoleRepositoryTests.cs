using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;
using ResQ.API.Repositories.Auth;

namespace ResQ.Tests.Repositories;

public class UserRoleRepositoryTests : IDisposable
{
    private readonly ResQDbContext _db;
    private readonly UserRoleRepository _sut;

    public UserRoleRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ResQDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new ResQDbContext(options);
        _sut = new UserRoleRepository(_db);
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

    private async Task<UserRole> SeedUserRoleAsync(int userId, Role role)
    {
        var userRole = new UserRole
        {
            UserId    = userId,
            Role      = role,
            CreatedAt = DateTime.UtcNow
        };
        _db.UserRoles.Add(userRole);
        await _db.SaveChangesAsync();
        return userRole;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByUserIdAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByUserIdAsync_ReturnsAllRolesForUser()
    {
        // Arrange
        var user = await SeedUserAsync();
        await SeedUserRoleAsync(user.Id, Role.Consumer);
        await SeedUserRoleAsync(user.Id, Role.Merchant);

        // Act
        var result = (await _sut.GetByUserIdAsync(user.Id)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, ur => ur.Role == Role.Consumer);
        Assert.Contains(result, ur => ur.Role == Role.Merchant);
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenUserHasNoRoles_ReturnsEmpty()
    {
        // Arrange
        var user = await SeedUserAsync();

        // Act
        var result = await _sut.GetByUserIdAsync(user.Id);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByUserIdAsync_DoesNotReturnRolesOfOtherUsers()
    {
        // Arrange
        var user1 = await SeedUserAsync("u1@test.com");
        var user2 = await SeedUserAsync("u2@test.com");
        await SeedUserRoleAsync(user1.Id, Role.Consumer);
        await SeedUserRoleAsync(user2.Id, Role.Merchant);

        // Act
        var result = (await _sut.GetByUserIdAsync(user1.Id)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(Role.Consumer, result[0].Role);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ExistsAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExistsAsync_WhenRoleAssigned_ReturnsTrue()
    {
        // Arrange
        var user = await SeedUserAsync();
        await SeedUserRoleAsync(user.Id, Role.Consumer);

        // Act
        var result = await _sut.ExistsAsync(user.Id, Role.Consumer);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WhenRoleNotAssigned_ReturnsFalse()
    {
        // Arrange
        var user = await SeedUserAsync();

        // Act
        var result = await _sut.ExistsAsync(user.Id, Role.Consumer);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExistsAsync_WhenUserHasDifferentRole_ReturnsFalse()
    {
        // Arrange
        var user = await SeedUserAsync();
        await SeedUserRoleAsync(user.Id, Role.Merchant);

        // Act
        var result = await _sut.ExistsAsync(user.Id, Role.Consumer);

        // Assert
        Assert.False(result);
    }
}
