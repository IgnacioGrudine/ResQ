using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Catalog;
using ResQ.API.Models.Enums;
using ResQ.API.Repositories.Auth;

namespace ResQ.Tests.Repositories;

public class MerchantProfileRepositoryTests : IDisposable
{
    private readonly ResQDbContext _db;
    private readonly MerchantProfileRepository _sut;

    public MerchantProfileRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ResQDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new ResQDbContext(options);
        _sut = new MerchantProfileRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<User> SeedUserAsync(int seed = 1)
    {
        var user = new User
        {
            Email     = $"merchant{seed}@example.com",
            IsActive  = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Seeds a merchant profile with its required <see cref="User"/> navigation linked,
    /// since <c>MerchantProfile.UserId</c> is a required FK and EF Core InMemory's
    /// <c>Include(...).ThenInclude(...)</c> will silently drop rows without it.
    /// </summary>
    private async Task<MerchantProfile> SeedMerchantAsync(int seed = 1, MpConnectionStatus mpStatus = MpConnectionStatus.Connected)
    {
        var user = await SeedUserAsync(seed);
        var merchant = new MerchantProfile
        {
            UserId             = user.Id,
            BusinessName       = $"Comercio {seed}",
            Cuit               = $"30-{seed:D8}-9",
            Address            = $"Calle {seed}",
            ContactPhone       = "351-000000",
            MpConnectionStatus = mpStatus,
            CreatedAt          = DateTime.UtcNow
        };
        _db.MerchantProfiles.Add(merchant);
        await _db.SaveChangesAsync();
        return merchant;
    }

    private async Task<Category> SeedCategoryAsync(string name = "Panadería")
    {
        var category = new Category { Name = name };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    private async Task SeedMerchantCategoryAsync(int merchantId, int categoryId)
    {
        _db.MerchantCategories.Add(new MerchantCategory { MerchantId = merchantId, CategoryId = categoryId });
        await _db.SaveChangesAsync();
    }

    private async Task<Product> SeedProductAsync(int merchantId, bool isActive = true, int stockQuantity = 5)
    {
        var product = new Product
        {
            MerchantId      = merchantId,
            Name            = "Pack",
            ProductType     = ProductType.SurprisePack,
            OriginalPrice   = 1000m,
            SalePrice       = 400m,
            StockQuantity   = stockQuantity,
            PickupTimeStart = new TimeOnly(18, 0),
            PickupTimeEnd   = new TimeOnly(21, 0),
            IsActive        = isActive,
            CreatedAt       = DateTime.UtcNow
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByUserIdAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByUserIdAsync_WhenExists_ReturnsProfile()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();

        // Act
        var result = await _sut.GetByUserIdAsync(merchant.UserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(merchant.Id, result.Id);
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
    // ExistsByCuitAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExistsByCuitAsync_WhenCuitIsRegistered_ReturnsTrue()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();

        // Act
        var result = await _sut.ExistsByCuitAsync(merchant.Cuit);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsByCuitAsync_WhenCuitIsNotRegistered_ReturnsFalse()
    {
        // Act
        var result = await _sut.ExistsByCuitAsync("30-00000000-0");

        // Assert
        Assert.False(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetAllWithCatalogDataAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAllWithCatalogDataAsync_ReturnsOnlyMerchantsWithActiveInStockProducts()
    {
        // Arrange
        var withStock    = await SeedMerchantAsync(1);
        var withoutStock = await SeedMerchantAsync(2);
        var inactive     = await SeedMerchantAsync(3);
        await SeedProductAsync(withStock.Id, isActive: true,  stockQuantity: 5);
        await SeedProductAsync(withoutStock.Id, isActive: true,  stockQuantity: 0);
        await SeedProductAsync(inactive.Id, isActive: false, stockQuantity: 5);

        var category = await SeedCategoryAsync();
        await SeedMerchantCategoryAsync(withStock.Id, category.Id);

        // Act
        var result = (await _sut.GetAllWithCatalogDataAsync()).ToList();

        // Assert
        var item = Assert.Single(result);
        Assert.Equal(withStock.Id, item.Id);
        Assert.Single(item.MerchantCategories);
        Assert.Equal("Panadería", item.MerchantCategories.First().Category.Name);
        Assert.Single(item.Products);
    }

    [Fact]
    public async Task GetAllWithCatalogDataAsync_WhenNoMerchantsQualify_ReturnsEmptyList()
    {
        // Arrange
        await SeedMerchantAsync();

        // Act
        var result = await _sut.GetAllWithCatalogDataAsync();

        // Assert
        Assert.Empty(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByIdWithPublicDetailAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByIdWithPublicDetailAsync_WhenExists_ReturnsMerchantWithCategoriesAndProducts()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();
        var category = await SeedCategoryAsync("Sushi");
        await SeedMerchantCategoryAsync(merchant.Id, category.Id);
        await SeedProductAsync(merchant.Id, isActive: true, stockQuantity: 3);
        await SeedProductAsync(merchant.Id, isActive: false, stockQuantity: 3);

        // Act
        var result = await _sut.GetByIdWithPublicDetailAsync(merchant.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.MerchantCategories);
        Assert.Single(result.Products); // inactive product excluded by the filtered Include
    }

    [Fact]
    public async Task GetByIdWithPublicDetailAsync_WhenNotFound_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdWithPublicDetailAsync(9999);

        // Assert
        Assert.Null(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByIdWithCategoriesAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByIdWithCategoriesAsync_WhenExists_ReturnsMerchantWithCategories()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();
        var category = await SeedCategoryAsync("Café");
        await SeedMerchantCategoryAsync(merchant.Id, category.Id);

        // Act
        var result = await _sut.GetByIdWithCategoriesAsync(merchant.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.MerchantCategories);
        Assert.Equal("Café", result.MerchantCategories.First().Category.Name);
    }

    [Fact]
    public async Task GetByIdWithCategoriesAsync_WhenNotFound_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdWithCategoriesAsync(9999);

        // Assert
        Assert.Null(result);
    }
}
