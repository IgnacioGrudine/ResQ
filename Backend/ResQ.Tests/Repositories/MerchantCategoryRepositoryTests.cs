using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Catalog;
using ResQ.API.Repositories.Catalog;

namespace ResQ.Tests.Repositories;

public class MerchantCategoryRepositoryTests : IDisposable
{
    private readonly ResQDbContext _db;
    private readonly MerchantCategoryRepository _sut;

    public MerchantCategoryRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ResQDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new ResQDbContext(options);
        _sut = new MerchantCategoryRepository(_db);
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

    private async Task<Category> SeedCategoryAsync(string name = "Panadería")
    {
        var category = new Category { Name = name, CreatedAt = DateTime.UtcNow };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    private async Task<MerchantCategory> SeedAssignmentAsync(int merchantId, int categoryId)
    {
        var assignment = new MerchantCategory { MerchantId = merchantId, CategoryId = categoryId };
        _db.MerchantCategories.Add(assignment);
        await _db.SaveChangesAsync();
        return assignment;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByMerchantIdAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByMerchantIdAsync_ReturnsOnlyAssignmentsForGivenMerchant()
    {
        // Arrange
        var m1 = await SeedMerchantAsync(1);
        var m2 = await SeedMerchantAsync(2);
        var c1 = await SeedCategoryAsync("Panadería");
        var c2 = await SeedCategoryAsync("Sushi");
        await SeedAssignmentAsync(m1.Id, c1.Id);
        await SeedAssignmentAsync(m1.Id, c2.Id);
        await SeedAssignmentAsync(m2.Id, c1.Id);

        // Act
        var result = (await _sut.GetByMerchantIdAsync(m1.Id)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, mc => Assert.Equal(m1.Id, mc.MerchantId));
    }

    [Fact]
    public async Task GetByMerchantIdAsync_WhenMerchantHasNoAssignments_ReturnsEmptyCollection()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();

        // Act
        var result = await _sut.GetByMerchantIdAsync(merchant.Id);

        // Assert
        Assert.Empty(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AddAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsAssignment()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();
        var category = await SeedCategoryAsync();
        var assignment = new MerchantCategory { MerchantId = merchant.Id, CategoryId = category.Id };

        // Act
        await _sut.AddAsync(assignment);
        await _db.SaveChangesAsync();

        // Assert
        var saved = await _db.MerchantCategories
            .FirstOrDefaultAsync(mc => mc.MerchantId == merchant.Id && mc.CategoryId == category.Id);
        Assert.NotNull(saved);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DeleteRange
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteRange_ThenSaveChanges_RemovesGivenAssignments()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();
        var c1 = await SeedCategoryAsync("Panadería");
        var c2 = await SeedCategoryAsync("Sushi");
        var a1 = await SeedAssignmentAsync(merchant.Id, c1.Id);
        var a2 = await SeedAssignmentAsync(merchant.Id, c2.Id);

        // Act
        _sut.DeleteRange([a1]);
        await _db.SaveChangesAsync();

        // Assert
        var remaining = await _sut.GetByMerchantIdAsync(merchant.Id);
        var remainingList = remaining.ToList();
        Assert.Single(remainingList);
        Assert.Equal(a2.CategoryId, remainingList[0].CategoryId);
    }
}
