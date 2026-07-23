using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Repositories.Auth;

namespace ResQ.Tests.Repositories;

public class ConsumerProfileRepositoryTests : IDisposable
{
    private readonly ResQDbContext _db;
    private readonly ConsumerProfileRepository _sut;

    public ConsumerProfileRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ResQDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new ResQDbContext(options);
        _sut = new ConsumerProfileRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<User> SeedUserAsync(int seed = 1)
    {
        var user = new User
        {
            Email     = $"consumer{seed}@example.com",
            IsActive  = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Seeds a consumer profile with its required <see cref="User"/> navigation linked,
    /// since <c>ConsumerProfile.UserId</c> is a required FK and EF Core InMemory's
    /// <c>Include(cp => cp.User)</c> will silently drop rows without it.
    /// </summary>
    private async Task<ConsumerProfile> SeedConsumerAsync(int seed = 1)
    {
        var user = await SeedUserAsync(seed);
        var consumer = new ConsumerProfile
        {
            UserId      = user.Id,
            FirstName   = $"Consumidor{seed}",
            LastName    = "Apellido",
            PhoneNumber = "351-000000",
            CreatedAt   = DateTime.UtcNow
        };
        _db.ConsumerProfiles.Add(consumer);
        await _db.SaveChangesAsync();
        return consumer;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByUserIdAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByUserIdAsync_WhenExists_ReturnsProfile()
    {
        // Arrange
        var consumer = await SeedConsumerAsync();

        // Act
        var result = await _sut.GetByUserIdAsync(consumer.UserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(consumer.Id, result.Id);
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenNotExists_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByUserIdAsync(9999);

        // Assert
        Assert.Null(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByIdWithUserAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByIdWithUserAsync_WhenExists_ReturnsProfileWithUserLoaded()
    {
        // Arrange
        var consumer = await SeedConsumerAsync();

        // Act
        var result = await _sut.GetByIdWithUserAsync(consumer.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.User);
        Assert.Equal($"consumer1@example.com", result.User.Email);
    }

    [Fact]
    public async Task GetByIdWithUserAsync_WhenNotFound_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdWithUserAsync(9999);

        // Assert
        Assert.Null(result);
    }
}
