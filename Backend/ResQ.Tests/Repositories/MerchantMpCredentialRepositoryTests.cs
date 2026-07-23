using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Repositories.MercadoPago;

namespace ResQ.Tests.Repositories;

public class MerchantMpCredentialRepositoryTests : IDisposable
{
    private readonly ResQDbContext _db;
    private readonly MerchantMpCredentialRepository _sut;

    public MerchantMpCredentialRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ResQDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new ResQDbContext(options);
        _sut = new MerchantMpCredentialRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<MerchantProfile> SeedMerchantAsync(int seed = 1)
    {
        var merchant = new MerchantProfile
        {
            BusinessName = $"Comercio {seed}",
            Cuit         = $"30-{seed:D8}-9",
            Address      = $"Calle {seed}",
            ContactPhone = "351-000000",
            CreatedAt    = DateTime.UtcNow
        };
        _db.MerchantProfiles.Add(merchant);
        await _db.SaveChangesAsync();
        return merchant;
    }

    private async Task<MerchantMpCredential> SeedCredentialAsync(
        int merchantId, bool isActive = true, DateTime? expiresAt = null)
    {
        var credential = new MerchantMpCredential
        {
            MerchantId           = merchantId,
            MpUserId              = 123456,
            AccessToken          = "encrypted-access",
            RefreshToken         = "encrypted-refresh",
            AccessTokenExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(30),
            IsActive             = isActive,
            CreatedAt            = DateTime.UtcNow
        };
        _db.MerchantMpCredentials.Add(credential);
        await _db.SaveChangesAsync();
        return credential;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByMerchantIdAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByMerchantIdAsync_WhenCredentialExists_ReturnsCredential()
    {
        // Arrange
        var merchant   = await SeedMerchantAsync();
        var credential = await SeedCredentialAsync(merchant.Id);

        // Act
        var result = await _sut.GetByMerchantIdAsync(merchant.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(credential.Id, result.Id);
    }

    [Fact]
    public async Task GetByMerchantIdAsync_WhenNoCredentialExists_ReturnsNull()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();

        // Act
        var result = await _sut.GetByMerchantIdAsync(merchant.Id);

        // Assert
        Assert.Null(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetExpiringSoonAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetExpiringSoonAsync_ReturnsOnlyActiveCredentialsExpiringWithinThreshold()
    {
        // Arrange
        var m1 = await SeedMerchantAsync(1);
        var m2 = await SeedMerchantAsync(2);
        var m3 = await SeedMerchantAsync(3);
        var m4 = await SeedMerchantAsync(4);

        var expiringSoonActive  = await SeedCredentialAsync(m1.Id, isActive: true,  expiresAt: DateTime.UtcNow.AddDays(3));
        await SeedCredentialAsync(m2.Id, isActive: true,  expiresAt: DateTime.UtcNow.AddDays(30)); // outside threshold
        await SeedCredentialAsync(m3.Id, isActive: false, expiresAt: DateTime.UtcNow.AddDays(1));  // inactive, excluded
        var alreadyExpiredActive = await SeedCredentialAsync(m4.Id, isActive: true,  expiresAt: DateTime.UtcNow.AddDays(-1));

        // Act
        var result = (await _sut.GetExpiringSoonAsync(7)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Id == expiringSoonActive.Id);
        Assert.Contains(result, c => c.Id == alreadyExpiredActive.Id);
    }

    [Fact]
    public async Task GetExpiringSoonAsync_WhenNoneExpiring_ReturnsEmpty()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();
        await SeedCredentialAsync(merchant.Id, isActive: true, expiresAt: DateTime.UtcNow.AddDays(60));

        // Act
        var result = await _sut.GetExpiringSoonAsync(7);

        // Assert
        Assert.Empty(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AddRefreshLogAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AddRefreshLogAsync_ThenSaveChanges_PersistsLog()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();
        var log = new MpTokenRefreshLog
        {
            MerchantId = merchant.Id,
            Success    = true,
            CreatedAt  = DateTime.UtcNow
        };

        // Act
        await _sut.AddRefreshLogAsync(log);
        await _sut.SaveChangesAsync();

        // Assert
        var saved = await _db.MpTokenRefreshLogs.FirstOrDefaultAsync(l => l.MerchantId == merchant.Id);
        Assert.NotNull(saved);
        Assert.True(saved.Success);
    }
}
