using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Catalog;
using ResQ.API.Models.Enums;
using ResQ.API.Repositories.Catalog;

namespace ResQ.Tests.Repositories;

public class ProductRepositoryTests : IDisposable
{
    private readonly ResQDbContext _db;
    private readonly ProductRepository _sut;

    public ProductRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ResQDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new ResQDbContext(options);
        _sut = new ProductRepository(_db);
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

    private async Task<Product> SeedProductAsync(
        int merchantId,
        string name          = "Pack",
        bool isActive        = true,
        int stockQuantity    = 5,
        DateTime? createdAt  = null)
    {
        var product = new Product
        {
            MerchantId      = merchantId,
            Name            = name,
            ProductType     = ProductType.SurprisePack,
            OriginalPrice   = 1000m,
            SalePrice       = 400m,
            StockQuantity   = stockQuantity,
            PickupTimeStart = new TimeOnly(18, 0),
            PickupTimeEnd   = new TimeOnly(21, 0),
            IsActive        = isActive,
            CreatedAt       = createdAt ?? DateTime.UtcNow
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByMerchantIdAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByMerchantIdAsync_ReturnsOnlyProductsForGivenMerchant()
    {
        // Arrange
        var m1 = await SeedMerchantAsync(1);
        var m2 = await SeedMerchantAsync(2);
        await SeedProductAsync(m1.Id, "Pack A");
        await SeedProductAsync(m1.Id, "Pack B");
        await SeedProductAsync(m2.Id, "Pack C");

        // Act
        var result = (await _sut.GetByMerchantIdAsync(m1.Id)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Equal(m1.Id, p.MerchantId));
    }

    [Fact]
    public async Task GetByMerchantIdAsync_WhenMerchantHasNoProducts_ReturnsEmptyList()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();

        // Act
        var result = await _sut.GetByMerchantIdAsync(merchant.Id);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByMerchantIdAsync_ActiveProductsReturnedBeforeInactive()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();
        var now = DateTime.UtcNow;
        await SeedProductAsync(merchant.Id, "Inactive", isActive: false, createdAt: now.AddHours(-1));
        await SeedProductAsync(merchant.Id, "Active", isActive: true, createdAt: now.AddHours(-2));

        // Act
        var result = (await _sut.GetByMerchantIdAsync(merchant.Id)).ToList();

        // Assert
        Assert.Equal("Active", result[0].Name);
        Assert.Equal("Inactive", result[1].Name);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByIdForMerchantAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByIdForMerchantAsync_WhenProductBelongsToMerchant_ReturnsProduct()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();
        var product  = await SeedProductAsync(merchant.Id);

        // Act
        var result = await _sut.GetByIdForMerchantAsync(product.Id, merchant.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(product.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdForMerchantAsync_WhenProductBelongsToDifferentMerchant_ReturnsNull()
    {
        // Arrange
        var m1      = await SeedMerchantAsync(1);
        var m2      = await SeedMerchantAsync(2);
        var product = await SeedProductAsync(m1.Id);

        // Act
        var result = await _sut.GetByIdForMerchantAsync(product.Id, m2.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdForMerchantAsync_WhenProductNotFound_ReturnsNull()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();

        // Act
        var result = await _sut.GetByIdForMerchantAsync(9999, merchant.Id);

        // Assert
        Assert.Null(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetAllActiveWithMerchantAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAllActiveWithMerchantAsync_ReturnsOnlyActiveProductsWithStock()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();
        await SeedProductAsync(merchant.Id, "Active with stock",  isActive: true,  stockQuantity: 5);
        await SeedProductAsync(merchant.Id, "Inactive",           isActive: false, stockQuantity: 5);
        await SeedProductAsync(merchant.Id, "Active no stock",    isActive: true,  stockQuantity: 0);

        // Act
        var result = (await _sut.GetAllActiveWithMerchantAsync()).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("Active with stock", result[0].Name);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByIdWithMerchantAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByIdWithMerchantAsync_WhenProductExists_ReturnsProduct()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();
        var product  = await SeedProductAsync(merchant.Id);

        // Act
        var result = await _sut.GetByIdWithMerchantAsync(product.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(product.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdWithMerchantAsync_WhenProductNotFound_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdWithMerchantAsync(9999);

        // Assert
        Assert.Null(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GenericRepository — AddAsync / Delete
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsProduct()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();
        var product = new Product
        {
            MerchantId      = merchant.Id,
            Name            = "Nuevo pack",
            ProductType     = ProductType.SurprisePack,
            OriginalPrice   = 500m,
            SalePrice       = 200m,
            StockQuantity   = 3,
            PickupTimeStart = new TimeOnly(17, 0),
            PickupTimeEnd   = new TimeOnly(20, 0),
            IsActive        = true,
            CreatedAt       = DateTime.UtcNow
        };

        // Act
        await _sut.AddAsync(product);
        await _sut.SaveChangesAsync();

        // Assert
        var saved = await _db.Products.FindAsync(product.Id);
        Assert.NotNull(saved);
        Assert.Equal("Nuevo pack", saved.Name);
    }

    [Fact]
    public async Task Delete_ThenSaveChanges_RemovesProduct()
    {
        // Arrange
        var merchant = await SeedMerchantAsync();
        var product  = await SeedProductAsync(merchant.Id, "ToDelete");

        // Act
        _sut.Delete(product);
        await _sut.SaveChangesAsync();

        // Assert
        var deleted = await _db.Products.FindAsync(product.Id);
        Assert.Null(deleted);
    }
}
