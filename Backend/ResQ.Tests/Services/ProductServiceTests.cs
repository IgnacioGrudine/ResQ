using FluentResults;
using Microsoft.AspNetCore.Http;
using Moq;
using ResQ.API.DTOs.Products;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Catalog;
using ResQ.API.Models.Enums;
using ResQ.API.Repositories.Auth;
using ResQ.API.Repositories.Catalog;
using ResQ.API.Services.Products;
using ResQ.API.Services.Storage;

namespace ResQ.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IMerchantProfileRepository> _merchants = new();
    private readonly Mock<IImageStorageService> _imageStorage = new();

    private ProductService CreateSut() => new(_products.Object, _merchants.Object, _imageStorage.Object);

    /// <summary>Makes the merchant gate pass: the given merchant has a live Mercado Pago connection.</summary>
    private void SetupConnectedMerchant(int merchantId, MpConnectionStatus status = MpConnectionStatus.Connected) =>
        _merchants.Setup(m => m.GetByIdAsync(merchantId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new MerchantProfile { Id = merchantId, MpConnectionStatus = status });

    private static Product BuildProduct(int id = 1, int merchantId = 10, bool isActive = true) => new()
    {
        Id              = id,
        MerchantId      = merchantId,
        Name            = "Pack Sorpresa",
        Description     = "Variado",
        ProductType     = ProductType.SurprisePack,
        OriginalPrice   = 1000m,
        SalePrice       = 400m,
        StockQuantity   = 5,
        PickupTimeStart = new TimeOnly(18, 0),
        PickupTimeEnd   = new TimeOnly(21, 0),
        IsActive        = isActive,
        CreatedAt       = DateTime.UtcNow
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMyProductsAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMyProductsAsync_ReturnsAllProductsMappedToResponse()
    {
        // Arrange
        var merchantId = 10;
        var storedProducts = new List<Product> { BuildProduct(1, merchantId), BuildProduct(2, merchantId) };
        _products.Setup(p => p.GetByMerchantIdAsync(merchantId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(storedProducts);

        var sut = CreateSut();

        // Act
        var result = await sut.GetMyProductsAsync(merchantId);

        // Assert
        Assert.True(result.IsSuccess);
        var items = result.Value.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("Pack Sorpresa", items[0].Name);
    }

    [Fact]
    public async Task GetMyProductsAsync_WhenNoProducts_ReturnsEmptyCollection()
    {
        // Arrange
        _products.Setup(p => p.GetByMerchantIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync([]);

        var sut = CreateSut();

        // Act
        var result = await sut.GetMyProductsAsync(99);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CreateProductAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateProductAsync_CreatesProductWithCorrectPropertiesAndReturnsOk()
    {
        // Arrange
        var merchantId = 10;
        var request = new CreateProductRequest
        {
            Name            = "Menú del día",
            Description     = "Plato principal + postre",
            ProductType     = ProductType.ExplicitItem,
            OriginalPrice   = 2000m,
            SalePrice       = 800m,
            StockQuantity   = 3,
            PickupTimeStart = new TimeOnly(19, 0),
            PickupTimeEnd   = new TimeOnly(22, 0)
        };

        SetupConnectedMerchant(merchantId);
        _products.Setup(p => p.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _products.Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.CreateProductAsync(merchantId, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(request.Name, result.Value.Name);
        Assert.Equal(request.SalePrice, result.Value.SalePrice);
        Assert.True(result.Value.IsActive);
        _products.Verify(p => p.AddAsync(
            It.Is<Product>(pr => pr.MerchantId == merchantId && pr.Name == request.Name),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_WhenMpNotConnected_ReturnsBadRequestAndDoesNotPersist()
    {
        // Arrange
        var merchantId = 10;
        SetupConnectedMerchant(merchantId, MpConnectionStatus.Disconnected);

        var request = new CreateProductRequest
        {
            Name            = "Menú del día",
            ProductType     = ProductType.SurprisePack,
            OriginalPrice   = 2000m,
            SalePrice       = 800m,
            StockQuantity   = 3,
            PickupTimeStart = new TimeOnly(19, 0),
            PickupTimeEnd   = new TimeOnly(22, 0)
        };

        var sut = CreateSut();

        // Act
        var result = await sut.CreateProductAsync(merchantId, request);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("Mercado Pago", result.Errors[0].Message);
        _products.Verify(p => p.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UpdateProductAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateProductAsync_WhenProductNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _products.Setup(p => p.GetByIdForMerchantAsync(99, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Product?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateProductAsync(10, 99, new UpdateProductRequest());

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrado", result.Errors[0].Message);
    }

    [Fact]
    public async Task UpdateProductAsync_WhenProductFound_UpdatesPropertiesAndReturnsOk()
    {
        // Arrange
        var product = BuildProduct(1, 10);
        var request = new UpdateProductRequest
        {
            Name            = "Nuevo nombre",
            Description     = "Descripción actualizada",
            ProductType     = ProductType.SurprisePack,
            OriginalPrice   = 1500m,
            SalePrice       = 600m,
            StockQuantity   = 10,
            PickupTimeStart = new TimeOnly(17, 0),
            PickupTimeEnd   = new TimeOnly(20, 0)
        };

        _products.Setup(p => p.GetByIdForMerchantAsync(1, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);
        _products.Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateProductAsync(10, 1, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(request.Name, result.Value.Name);
        Assert.Equal(request.SalePrice, result.Value.SalePrice);
        _products.Verify(p => p.Update(product), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DeleteProductAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteProductAsync_WhenProductNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _products.Setup(p => p.GetByIdForMerchantAsync(99, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Product?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteProductAsync(10, 99);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrado", result.Errors[0].Message);
    }

    [Fact]
    public async Task DeleteProductAsync_WhenProductFound_DeletesAndReturnsOk()
    {
        // Arrange
        var product = BuildProduct(1, 10);
        _products.Setup(p => p.GetByIdForMerchantAsync(1, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);
        _products.Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteProductAsync(10, 1);

        // Assert
        Assert.True(result.IsSuccess);
        _products.Verify(p => p.Delete(product), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToggleProductAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToggleProductAsync_WhenProductNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _products.Setup(p => p.GetByIdForMerchantAsync(99, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Product?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.ToggleProductAsync(10, 99);

        // Assert
        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task ToggleProductAsync_WhenProductIsActive_SetsIsActiveToFalse()
    {
        // Arrange
        var product = BuildProduct(1, 10, isActive: true);
        _products.Setup(p => p.GetByIdForMerchantAsync(1, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);
        _products.Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.ToggleProductAsync(10, 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
    }

    [Fact]
    public async Task ToggleProductAsync_WhenProductIsInactive_SetsIsActiveToTrue()
    {
        // Arrange
        var product = BuildProduct(1, 10, isActive: false);
        SetupConnectedMerchant(10);
        _products.Setup(p => p.GetByIdForMerchantAsync(1, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);
        _products.Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.ToggleProductAsync(10, 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task ToggleProductAsync_WhenActivatingAndMpNotConnected_ReturnsBadRequestAndStaysInactive()
    {
        // Arrange
        var product = BuildProduct(1, 10, isActive: false);
        SetupConnectedMerchant(10, MpConnectionStatus.Disconnected);
        _products.Setup(p => p.GetByIdForMerchantAsync(1, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);

        var sut = CreateSut();

        // Act
        var result = await sut.ToggleProductAsync(10, 1);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("Mercado Pago", result.Errors[0].Message);
        Assert.False(product.IsActive);
        _products.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ToggleProductAsync_WhenDeactivating_DoesNotRequireMpConnection()
    {
        // Arrange — an active pack can always be turned off, even if MP is disconnected.
        var product = BuildProduct(1, 10, isActive: true);
        SetupConnectedMerchant(10, MpConnectionStatus.Disconnected);
        _products.Setup(p => p.GetByIdForMerchantAsync(1, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);
        _products.Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.ToggleProductAsync(10, 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UploadImageAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UploadImageAsync_WhenProductNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _products.Setup(p => p.GetByIdForMerchantAsync(99, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Product?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.UploadImageAsync(10, 99, Mock.Of<IFormFile>());

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrado", result.Errors[0].Message);
    }

    [Fact]
    public async Task UploadImageAsync_WhenProductFound_UploadsAndUpdatesImageUrl()
    {
        // Arrange
        const string newUrl = "https://storage.example.com/products/10/image.jpg";
        var product = BuildProduct(1, 10);
        var mockFile = new Mock<IFormFile>();

        _products.Setup(p => p.GetByIdForMerchantAsync(1, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);
        _imageStorage.Setup(s => s.UploadAsync(mockFile.Object, "products/10", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(newUrl);
        _products.Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.UploadImageAsync(10, 1, mockFile.Object);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newUrl, result.Value.ImageUrl);
        _products.Verify(p => p.Update(product), Times.Once);
    }
}
