using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ResQ.API.Common.Errors;
using ResQ.API.Controllers;
using ResQ.API.DTOs.Catalog;
using ResQ.API.DTOs.Merchants;
using ResQ.API.DTOs.Shared;
using ResQ.API.Services.Catalog;

namespace ResQ.Tests.Controllers;

public class CatalogControllerTests
{
    private readonly Mock<ICatalogService> _catalogService = new();

    // The catalog endpoints are entirely public and unauthenticated, so no
    // ClaimsPrincipal / ControllerContext setup is needed to exercise them.
    private CatalogController CreateSut() => new(_catalogService.Object);

    private static PackListItemResponse BuildPack(int id = 1, string name = "Pack Sorpresa") => new()
    {
        Id              = id,
        Name            = name,
        ProductType     = "SurprisePack",
        OriginalPrice   = 1000m,
        SalePrice       = 400m,
        StockQuantity   = 5,
        PickupTimeStart = new TimeOnly(18, 0),
        PickupTimeEnd   = new TimeOnly(21, 0),
        MerchantId      = 10,
        MerchantName    = "Comercio Test"
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // GetCategories
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCategories_WhenServiceReturnsOk_Returns200WithCategories()
    {
        // Arrange
        var categories = new List<CategoryResponse> { new() { Id = 1, Name = "Panadería" } };
        _catalogService.Setup(s => s.GetCategoriesAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Ok<IEnumerable<CategoryResponse>>(categories));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetCategories(CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<CategoryResponse>>(result.Value).ToList();
        Assert.Single(items);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetCatalog
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCatalog_WhenServiceReturnsOk_Returns200WithMerchants()
    {
        // Arrange
        var merchants = new List<MerchantListItemResponse> { new() { Id = 1, BusinessName = "Comercio A" } };
        _catalogService.Setup(s => s.GetCatalogAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Ok<IEnumerable<MerchantListItemResponse>>(merchants));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetCatalog(CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<MerchantListItemResponse>>(result.Value).ToList();
        Assert.Single(items);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMerchantDetail
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMerchantDetail_WhenServiceReturnsOk_Returns200WithDetail()
    {
        // Arrange
        var detail = new MerchantDetailResponse { Id = 5, BusinessName = "Comercio B" };
        _catalogService.Setup(s => s.GetMerchantDetailAsync(5, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Ok(detail));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetMerchantDetail(5, CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsType<MerchantDetailResponse>(result.Value);
        Assert.Equal("Comercio B", returned.BusinessName);
    }

    [Fact]
    public async Task GetMerchantDetail_WhenMerchantNotFound_Returns404()
    {
        // Arrange
        _catalogService.Setup(s => s.GetMerchantDetailAsync(99, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Fail<MerchantDetailResponse>(new NotFoundError("Merchant 99 not found.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetMerchantDetail(99, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetPacks
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPacks_WhenServiceReturnsOk_Returns200WithPacks()
    {
        // Arrange
        var packs = new List<PackListItemResponse> { BuildPack(1), BuildPack(2) };
        _catalogService.Setup(s => s.GetPacksAsync(
                            null, null, null, null, null, null, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Ok<IEnumerable<PackListItemResponse>>(packs));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetPacks(null, null, null, null, null, null, CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<PackListItemResponse>>(result.Value).ToList();
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task GetPacks_PassesAllQueryParametersToService()
    {
        // Arrange
        _catalogService.Setup(s => s.GetPacksAsync(
                            -31.42, -64.18, "sushi", 3, 500m, 10, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Ok<IEnumerable<PackListItemResponse>>([]));

        var sut = CreateSut();

        // Act
        await sut.GetPacks(-31.42, -64.18, "sushi", 3, 500m, 10, CancellationToken.None);

        // Assert
        _catalogService.Verify(s => s.GetPacksAsync(
            -31.42, -64.18, "sushi", 3, 500m, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetPackById
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPackById_WhenServiceReturnsOk_Returns200WithPack()
    {
        // Arrange
        var pack = BuildPack(1);
        _catalogService.Setup(s => s.GetPackByIdAsync(1, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Ok(pack));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetPackById(1, CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsType<PackListItemResponse>(result.Value);
        Assert.Equal(1, returned.Id);
    }

    [Fact]
    public async Task GetPackById_WhenPackNotFound_Returns404()
    {
        // Arrange
        _catalogService.Setup(s => s.GetPackByIdAsync(99, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Fail<PackListItemResponse>(new NotFoundError("Pack 99 not found.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetPackById(99, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }
}
