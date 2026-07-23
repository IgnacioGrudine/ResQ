using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Catalog;
using ResQ.API.Repositories.Catalog;

namespace ResQ.Tests.Repositories;

public class CategoryRepositoryTests : IDisposable
{
    private readonly ResQDbContext _db;
    private readonly CategoryRepository _sut;

    public CategoryRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ResQDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new ResQDbContext(options);
        _sut = new CategoryRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<Category> SeedCategoryAsync(string name = "Panadería")
    {
        var category = new Category { Name = name, CreatedAt = DateTime.UtcNow };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

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

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByIdsAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByIdsAsync_ReturnsOnlyCategoriesMatchingGivenIds()
    {
        // Arrange
        var c1 = await SeedCategoryAsync("Panadería");
        var c2 = await SeedCategoryAsync("Sushi");
        var c3 = await SeedCategoryAsync("Pizza");

        // Act
        var result = (await _sut.GetByIdsAsync([c1.Id, c3.Id])).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Id == c1.Id);
        Assert.Contains(result, c => c.Id == c3.Id);
        Assert.DoesNotContain(result, c => c.Id == c2.Id);
    }

    [Fact]
    public async Task GetByIdsAsync_WhenNoIdsMatch_ReturnsEmptyCollection()
    {
        // Arrange
        await SeedCategoryAsync();

        // Act
        var result = await _sut.GetByIdsAsync([9999]);

        // Assert
        Assert.Empty(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // IsInUseAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task IsInUseAsync_WhenMerchantIsAssignedToCategory_ReturnsTrue()
    {
        // Arrange
        var category = await SeedCategoryAsync();
        var merchant = await SeedMerchantAsync();
        _db.MerchantCategories.Add(new MerchantCategory { MerchantId = merchant.Id, CategoryId = category.Id });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.IsInUseAsync(category.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsInUseAsync_WhenNoMerchantIsAssigned_ReturnsFalse()
    {
        // Arrange
        var category = await SeedCategoryAsync();

        // Act
        var result = await _sut.IsInUseAsync(category.Id);

        // Assert
        Assert.False(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GenericRepository — GetByIdAsync / GetAllAsync / AddAsync / Update / Delete
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByIdAsync_WhenCategoryExists_ReturnsCategory()
    {
        // Arrange
        var category = await SeedCategoryAsync("Sushi");

        // Act
        var result = await _sut.GetByIdAsync(category.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Sushi", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCategoryDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(9999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllPersistedCategories()
    {
        // Arrange
        await SeedCategoryAsync("Panadería");
        await SeedCategoryAsync("Sushi");

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsCategory()
    {
        // Arrange
        var category = new Category { Name = "Cafetería" };

        // Act
        await _sut.AddAsync(category);
        await _sut.SaveChangesAsync();

        // Assert
        var saved = await _db.Categories.FindAsync(category.Id);
        Assert.NotNull(saved);
        Assert.Equal("Cafetería", saved.Name);
    }

    [Fact]
    public async Task Update_ThenSaveChanges_PersistsModifiedName()
    {
        // Arrange
        var category = await SeedCategoryAsync("Vieja");
        category.Name = "Nueva";

        // Act
        _sut.Update(category);
        await _sut.SaveChangesAsync();

        // Assert
        var updated = await _db.Categories.FindAsync(category.Id);
        Assert.Equal("Nueva", updated!.Name);
    }

    [Fact]
    public async Task Delete_ThenSaveChanges_RemovesCategory()
    {
        // Arrange
        var category = await SeedCategoryAsync("ToDelete");

        // Act
        _sut.Delete(category);
        await _sut.SaveChangesAsync();

        // Assert
        var deleted = await _db.Categories.FindAsync(category.Id);
        Assert.Null(deleted);
    }
}
