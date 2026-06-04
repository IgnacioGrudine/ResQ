using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;
using ResQ.API.Repositories.Auth;

namespace ResQ.Tests.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly ResQDbContext _db;
    private readonly UserRepository _sut;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ResQDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new ResQDbContext(options);
        _sut = new UserRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<User> SeedUserAsync(string email = "test@test.com", bool isActive = true)
    {
        var user = new User
        {
            Email        = email.ToLower(),
            PasswordHash = "hash",
            IsActive     = isActive,
            CreatedAt    = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private async Task SeedUserWithRoleAsync(string email, Role role)
    {
        var user = await SeedUserAsync(email);
        _db.UserRoles.Add(new UserRole
        {
            UserId    = user.Id,
            Role      = role,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByEmailAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByEmailAsync_WhenUserExists_ReturnsUser()
    {
        // Arrange
        await SeedUserAsync("user@test.com");

        // Act
        var result = await _sut.GetByEmailAsync("user@test.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user@test.com", result.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenUserNotExists_ReturnsNull()
    {
        // Arrange
        await SeedUserAsync("other@test.com");

        // Act
        var result = await _sut.GetByEmailAsync("missing@test.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmailAsync_NormalizesEmailToLowercase()
    {
        // Arrange
        await SeedUserAsync("user@test.com");

        // Act
        var result = await _sut.GetByEmailAsync("USER@TEST.COM");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenDatabaseEmpty_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByEmailAsync("any@test.com");

        // Assert
        Assert.Null(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ExistsByEmailAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExistsByEmailAsync_WhenUserExists_ReturnsTrue()
    {
        // Arrange
        await SeedUserAsync("exists@test.com");

        // Act
        var result = await _sut.ExistsByEmailAsync("exists@test.com");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsByEmailAsync_WhenUserNotExists_ReturnsFalse()
    {
        // Act
        var result = await _sut.ExistsByEmailAsync("nobody@test.com");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExistsByEmailAsync_IsCaseInsensitive()
    {
        // Arrange
        await SeedUserAsync("user@test.com");

        // Act
        var result = await _sut.ExistsByEmailAsync("USER@TEST.COM");

        // Assert
        Assert.True(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetWithRolesAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetWithRolesAsync_WhenUserExistsWithRoles_ReturnsUserWithRolesLoaded()
    {
        // Arrange
        await SeedUserWithRoleAsync("roleuser@test.com", Role.Merchant);
        var user = await _db.Users.FirstAsync(u => u.Email == "roleuser@test.com");

        // Act
        var result = await _sut.GetWithRolesAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.UserRoles);
        Assert.Equal(Role.Merchant, result.UserRoles.First().Role);
    }

    [Fact]
    public async Task GetWithRolesAsync_WhenUserNotFound_ReturnsNull()
    {
        // Act
        var result = await _sut.GetWithRolesAsync(9999);

        // Assert
        Assert.Null(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByIdAsync (inherited from GenericRepository)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ReturnsCorrectUser()
    {
        // Arrange
        var seeded = await SeedUserAsync("byid@test.com");

        // Act
        var result = await _sut.GetByIdAsync(seeded.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(seeded.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserNotFound_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetAllAsync (inherited from GenericRepository)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        // Arrange
        await SeedUserAsync("a@test.com");
        await SeedUserAsync("b@test.com");

        // Act
        var result = (await _sut.GetAllAsync()).ToList();

        // Assert
        Assert.Equal(2, result.Count);
    }
}
