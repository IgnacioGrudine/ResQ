using FluentResults;
using Moq;
using ResQ.API.Common.Errors;
using ResQ.API.DTOs.Catalog;
using ResQ.API.DTOs.Merchants;
using ResQ.API.DTOs.Shared;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Catalog;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Reviews;
using ResQ.API.Repositories.Catalog;
using ResQ.API.Services.Catalog;
using ResQ.API.Services.Merchants;
using ResQ.API.Services.Storage;

namespace ResQ.Tests.Services;

public class CatalogServiceTests
{
    private readonly Mock<IMerchantService> _merchantService = new();
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IImageStorageService> _imageStorage = new();

    private CatalogService CreateSut() =>
        new(_merchantService.Object, _products.Object, _categories.Object, _imageStorage.Object);

    private static Category BuildCategory(int id = 1, string name = "Panadería") => new()
    {
        Id   = id,
        Name = name
    };

    private static Product BuildProduct(
        int id = 1,
        string name = "Pack Sorpresa",
        decimal salePrice = 400m,
        string? imageUrl = null,
        int merchantId = 10,
        string merchantName = "Comercio Test",
        decimal merchantLat = -31.4201m,
        decimal merchantLon = -64.1888m,
        IEnumerable<MerchantCategory>? merchantCategories = null,
        IEnumerable<Review>? reviews = null) => new()
    {
        Id              = id,
        Name            = name,
        Description     = "Variado",
        ImageUrl        = imageUrl,
        ProductType     = ProductType.SurprisePack,
        OriginalPrice   = 1000m,
        SalePrice       = salePrice,
        StockQuantity   = 5,
        PickupTimeStart = new TimeOnly(18, 0),
        PickupTimeEnd   = new TimeOnly(21, 0),
        IsActive        = true,
        Merchant = new MerchantProfile
        {
            Id                 = merchantId,
            BusinessName       = merchantName,
            Latitude           = merchantLat,
            Longitude          = merchantLon,
            MerchantCategories = merchantCategories?.ToList() ?? [],
            Reviews            = reviews?.ToList() ?? []
        }
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // GetCategoriesAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCategoriesAsync_ReturnsAllCategoriesMappedToResponse()
    {
        // Arrange
        _categories.Setup(c => c.GetAllAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync([BuildCategory(1, "Panadería"), BuildCategory(2, "Sushi")]);

        var sut = CreateSut();

        // Act
        var result = await sut.GetCategoriesAsync();

        // Assert
        Assert.True(result.IsSuccess);
        var items = result.Value.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("Panadería", items[0].Name);
        Assert.Equal("Sushi", items[1].Name);
    }

    [Fact]
    public async Task GetCategoriesAsync_WhenNoCategories_ReturnsEmptyCollection()
    {
        // Arrange
        _categories.Setup(c => c.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var sut = CreateSut();

        // Act
        var result = await sut.GetCategoriesAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetCatalogAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCatalogAsync_DelegatesToMerchantServiceAndReturnsItsResult()
    {
        // Arrange
        var merchants = new List<MerchantListItemResponse> { new() { Id = 1, BusinessName = "Comercio A" } };
        _merchantService.Setup(m => m.GetCatalogAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Ok<IEnumerable<MerchantListItemResponse>>(merchants));

        var sut = CreateSut();

        // Act
        var result = await sut.GetCatalogAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        _merchantService.Verify(m => m.GetCatalogAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMerchantDetailAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMerchantDetailAsync_DelegatesToMerchantServiceWithGivenId()
    {
        // Arrange
        var detail = new MerchantDetailResponse { Id = 5, BusinessName = "Comercio B" };
        _merchantService.Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Ok(detail));

        var sut = CreateSut();

        // Act
        var result = await sut.GetMerchantDetailAsync(5);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Comercio B", result.Value.BusinessName);
    }

    [Fact]
    public async Task GetMerchantDetailAsync_WhenMerchantServiceFails_ReturnsFailure()
    {
        // Arrange
        _merchantService.Setup(m => m.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Fail<MerchantDetailResponse>(new NotFoundError("Merchant 99 not found.")));

        var sut = CreateSut();

        // Act
        var result = await sut.GetMerchantDetailAsync(99);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("not found", result.Errors[0].Message);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetPacksAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPacksAsync_WithoutFilters_ReturnsAllPacksSortedByPriceAscending()
    {
        // Arrange
        var products = new List<Product>
        {
            BuildProduct(1, salePrice: 500m),
            BuildProduct(2, salePrice: 200m),
            BuildProduct(3, salePrice: 300m)
        };
        _products.Setup(p => p.GetAllActiveWithMerchantAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);

        var sut = CreateSut();

        // Act
        var result = await sut.GetPacksAsync(null, null, null, null, null, null);

        // Assert
        Assert.True(result.IsSuccess);
        var items = result.Value.ToList();
        Assert.Equal(3, items.Count);
        Assert.Equal(new[] { 2, 3, 1 }, items.Select(i => i.Id).ToArray());
    }

    [Fact]
    public async Task GetPacksAsync_WithSearch_FiltersByPackOrMerchantNameCaseInsensitively()
    {
        // Arrange
        var products = new List<Product>
        {
            BuildProduct(1, name: "Sushi Deluxe", merchantName: "Cafe Roma"),
            BuildProduct(2, name: "Bakery Special", merchantName: "La Sushi House"),
            BuildProduct(3, name: "Pizza Familiar", merchantName: "Pizzeria Don Pepe")
        };
        _products.Setup(p => p.GetAllActiveWithMerchantAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);

        var sut = CreateSut();

        // Act
        var result = await sut.GetPacksAsync(null, null, "sushi", null, null, null);

        // Assert
        Assert.True(result.IsSuccess);
        var ids = result.Value.Select(i => i.Id).ToList();
        Assert.Equal(2, ids.Count);
        Assert.Contains(1, ids);
        Assert.Contains(2, ids);
    }

    [Fact]
    public async Task GetPacksAsync_WithCategoryId_FiltersByMerchantCategoryAssignment()
    {
        // Arrange
        var products = new List<Product>
        {
            BuildProduct(1, merchantCategories: [new MerchantCategory { CategoryId = 1 }]),
            BuildProduct(2, merchantCategories: [new MerchantCategory { CategoryId = 2 }])
        };
        _products.Setup(p => p.GetAllActiveWithMerchantAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);

        var sut = CreateSut();

        // Act
        var result = await sut.GetPacksAsync(null, null, null, 1, null, null);

        // Assert
        Assert.True(result.IsSuccess);
        var items = result.Value.ToList();
        Assert.Single(items);
        Assert.Equal(1, items[0].Id);
    }

    [Fact]
    public async Task GetPacksAsync_WithMaxPrice_FiltersOutPacksAbovePriceInclusive()
    {
        // Arrange
        var products = new List<Product>
        {
            BuildProduct(1, salePrice: 100m),
            BuildProduct(2, salePrice: 200m),
            BuildProduct(3, salePrice: 300m)
        };
        _products.Setup(p => p.GetAllActiveWithMerchantAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);

        var sut = CreateSut();

        // Act
        var result = await sut.GetPacksAsync(null, null, null, null, 200m, null);

        // Assert
        Assert.True(result.IsSuccess);
        var ids = result.Value.Select(i => i.Id).ToList();
        Assert.Equal(2, ids.Count);
        Assert.DoesNotContain(3, ids);
    }

    [Fact]
    public async Task GetPacksAsync_WithoutLocation_DistanceKmIsNull()
    {
        // Arrange
        var products = new List<Product> { BuildProduct(1) };
        _products.Setup(p => p.GetAllActiveWithMerchantAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);

        var sut = CreateSut();

        // Act
        var result = await sut.GetPacksAsync(null, null, null, null, null, null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Single().DistanceKm);
    }

    [Fact]
    public async Task GetPacksAsync_WithLocation_ComputesDistanceAndSortsByProximity()
    {
        // Arrange — merchant "Near" is very close to the consumer, "Far" is in Buenos Aires.
        var products = new List<Product>
        {
            BuildProduct(1, name: "Far",  merchantLat: -34.6037m, merchantLon: -58.3816m),
            BuildProduct(2, name: "Near", merchantLat: -31.4210m, merchantLon: -64.1890m)
        };
        _products.Setup(p => p.GetAllActiveWithMerchantAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);

        var sut = CreateSut();

        // Act
        var result = await sut.GetPacksAsync(-31.4201, -64.1888, null, null, null, null);

        // Assert
        Assert.True(result.IsSuccess);
        var items = result.Value.ToList();
        Assert.Equal(2, items.Count);
        Assert.All(items, i => Assert.NotNull(i.DistanceKm));
        Assert.Equal("Near", items[0].Name);
        Assert.Equal("Far", items[1].Name);
    }

    [Fact]
    public async Task GetPacksAsync_WithMaxDistance_FiltersOutPacksFartherThanLimit()
    {
        // Arrange
        var products = new List<Product>
        {
            BuildProduct(1, name: "Far",  merchantLat: -34.6037m, merchantLon: -58.3816m),
            BuildProduct(2, name: "Near", merchantLat: -31.4210m, merchantLon: -64.1890m)
        };
        _products.Setup(p => p.GetAllActiveWithMerchantAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);

        var sut = CreateSut();

        // Act
        var result = await sut.GetPacksAsync(-31.4201, -64.1888, null, null, null, 50);

        // Assert
        Assert.True(result.IsSuccess);
        var items = result.Value.ToList();
        Assert.Single(items);
        Assert.Equal("Near", items[0].Name);
    }

    [Fact]
    public async Task GetPacksAsync_MapsMerchantAverageRatingFromReviews()
    {
        // Arrange
        var products = new List<Product>
        {
            BuildProduct(1, reviews: [new Review { Rating = 4 }, new Review { Rating = 5 }])
        };
        _products.Setup(p => p.GetAllActiveWithMerchantAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);

        var sut = CreateSut();

        // Act
        var result = await sut.GetPacksAsync(null, null, null, null, null, null);

        // Assert
        var item = result.Value.Single();
        Assert.Equal(4.5m, item.MerchantAverageRating);
        Assert.Equal(2, item.MerchantReviewCount);
    }

    [Fact]
    public async Task GetPacksAsync_WhenMerchantHasNoReviews_AverageRatingIsZero()
    {
        // Arrange
        var products = new List<Product> { BuildProduct(1) };
        _products.Setup(p => p.GetAllActiveWithMerchantAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);

        var sut = CreateSut();

        // Act
        var result = await sut.GetPacksAsync(null, null, null, null, null, null);

        // Assert
        var item = result.Value.Single();
        Assert.Equal(0, item.MerchantAverageRating);
        Assert.Equal(0, item.MerchantReviewCount);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetPackByIdAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPackByIdAsync_WhenPackNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _products.Setup(p => p.GetByIdWithMerchantAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Product?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.GetPackByIdAsync(99);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("not found", result.Errors[0].Message);
    }

    [Fact]
    public async Task GetPackByIdAsync_WhenFound_ReturnsMappedPackWithResolvedImageUrl()
    {
        // Arrange
        const string storedUrl = "products/1/photo.jpg";
        const string publicUrl = "https://cdn.resq.com/products/1/photo.jpg";
        var product = BuildProduct(1, imageUrl: storedUrl);
        _products.Setup(p => p.GetByIdWithMerchantAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _imageStorage.Setup(s => s.ResolvePublicUrl(storedUrl)).Returns(publicUrl);

        var sut = CreateSut();

        // Act
        var result = await sut.GetPackByIdAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(publicUrl, result.Value.ImageUrl);
        Assert.Equal("Comercio Test", result.Value.MerchantName);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CreateCategoryAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateCategoryAsync_TrimsNameAndPersistsCategory()
    {
        // Arrange
        _categories.Setup(c => c.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);
        _categories.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.CreateCategoryAsync("  Panadería  ");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Panadería", result.Value.Name);
        _categories.Verify(c => c.AddAsync(
            It.Is<Category>(cat => cat.Name == "Panadería"), It.IsAny<CancellationToken>()), Times.Once);
        _categories.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UpdateCategoryAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateCategoryAsync_WhenCategoryNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _categories.Setup(c => c.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateCategoryAsync(99, "Nuevo Nombre");

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("not found", result.Errors[0].Message);
    }

    [Fact]
    public async Task UpdateCategoryAsync_WhenFound_UpdatesTrimmedNameAndReturnsOk()
    {
        // Arrange
        var category = BuildCategory(1, "Vieja");
        _categories.Setup(c => c.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _categories.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateCategoryAsync(1, "  Nueva  ");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Nueva", result.Value.Name);
        _categories.Verify(c => c.Update(category), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DeleteCategoryAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteCategoryAsync_WhenCategoryNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _categories.Setup(c => c.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteCategoryAsync(99);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("not found", result.Errors[0].Message);
    }

    [Fact]
    public async Task DeleteCategoryAsync_WhenCategoryInUse_ReturnsConflictErrorAndDoesNotDelete()
    {
        // Arrange
        var category = BuildCategory(1);
        _categories.Setup(c => c.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _categories.Setup(c => c.IsInUseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteCategoryAsync(1);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("asignada", result.Errors[0].Message);
        _categories.Verify(c => c.Delete(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCategoryAsync_WhenNotInUse_DeletesAndReturnsOk()
    {
        // Arrange
        var category = BuildCategory(1);
        _categories.Setup(c => c.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _categories.Setup(c => c.IsInUseAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _categories.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteCategoryAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        _categories.Verify(c => c.Delete(category), Times.Once);
    }
}
