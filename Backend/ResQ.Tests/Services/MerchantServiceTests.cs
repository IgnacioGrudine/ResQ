using FluentResults;
using Microsoft.AspNetCore.Http;
using Moq;
using ResQ.API.DTOs.Admin;
using ResQ.API.DTOs.Merchants;
using ResQ.API.DTOs.Orders;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Catalog;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Orders;
using ResQ.API.Models.Reporting;
using ResQ.API.Models.Reviews;
using ResQ.API.Repositories.Auth;
using ResQ.API.Repositories.Catalog;
using ResQ.API.Repositories.Orders;
using ResQ.API.Repositories.Reviews;
using ResQ.API.Services.Merchants;
using ResQ.API.Services.Orders;
using ResQ.API.Services.Reporting;
using ResQ.API.Services.Storage;

namespace ResQ.Tests.Services;

public class MerchantServiceTests
{
    private readonly Mock<IMerchantProfileRepository> _merchants = new();
    private readonly Mock<IMerchantCategoryRepository> _merchantCategories = new();
    private readonly Mock<IReviewRepository> _reviews = new();
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IOrderService> _orderService = new();
    private readonly Mock<IImageStorageService> _imageStorage = new();
    private readonly Mock<IReportFileFactory> _reportFactory = new();

    public MerchantServiceTests()
    {
        // Pass-through default so tests that don't care about URL resolution aren't forced to stub it.
        _imageStorage.Setup(s => s.ResolvePublicUrl(It.IsAny<string?>()))
                     .Returns((string? s) => s);
    }

    private MerchantService CreateSut() => new(
        _merchants.Object,
        _merchantCategories.Object,
        _reviews.Object,
        _orders.Object,
        _products.Object,
        _orderService.Object,
        _imageStorage.Object,
        _reportFactory.Object);

    // ─── Builders ─────────────────────────────────────────────────────────────

    private static MerchantProfile BuildMerchant(int id = 10, MpConnectionStatus mpStatus = MpConnectionStatus.Connected) => new()
    {
        Id                 = id,
        BusinessName       = "Panadería Don José",
        Cuit               = "30-12345678-9",
        Address            = "Calle Falsa 123",
        Latitude           = -31.4m,
        Longitude          = -64.2m,
        ContactPhone       = "351-1234567",
        MpConnectionStatus = mpStatus,
        MerchantCategories = [],
        Products           = [],
        Reviews            = []
    };

    private static Product BuildProduct(int id = 1, int merchantId = 10, bool isActive = true, decimal salePrice = 400m) => new()
    {
        Id              = id,
        MerchantId      = merchantId,
        Name            = $"Pack {id}",
        ProductType     = ProductType.SurprisePack,
        OriginalPrice   = 1000m,
        SalePrice       = salePrice,
        StockQuantity   = 5,
        PickupTimeStart = new TimeOnly(18, 0),
        PickupTimeEnd   = new TimeOnly(21, 0),
        IsActive        = isActive,
        CreatedAt       = DateTime.UtcNow
    };

    private static Review BuildReview(int id, int merchantId, byte rating, string? comment = null, DateTime? createdAt = null) => new()
    {
        Id         = id,
        MerchantId = merchantId,
        Rating     = rating,
        Comment    = comment,
        CreatedAt  = createdAt ?? DateTime.UtcNow
    };

    private static Order BuildOrder(
        int id, int merchantId, OrderStatus status, decimal merchantEarnings, DateTime createdAt,
        int[] quantities, decimal totalAmount = 0) => new()
    {
        Id               = id,
        MerchantId       = merchantId,
        OrderStatus      = status,
        MerchantEarnings = merchantEarnings,
        TotalAmount      = totalAmount,
        CreatedAt        = createdAt,
        OrderDetails     = quantities.Select((q, i) => new OrderDetail
        {
            Id        = id * 100 + i,
            ProductId = i + 1,
            Quantity  = q,
            UnitPrice = 100m,
            Product   = new Product { Id = i + 1, Name = $"Pack {i + 1}", OriginalPrice = 200m, SalePrice = 100m }
        }).ToList()
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // GetCatalogAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCatalogAsync_ReturnsMerchantsMappedWithCategoriesRatingAndMinPrice()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Panadería" };
        var merchant = BuildMerchant(10);
        merchant.PhotoUrl = "merchants/10/photo.jpg";
        merchant.MerchantCategories = [new MerchantCategory { MerchantId = 10, CategoryId = 1, Category = category }];
        merchant.Products = [BuildProduct(1, 10, salePrice: 500m), BuildProduct(2, 10, salePrice: 300m)];
        merchant.Reviews = [BuildReview(1, 10, 4), BuildReview(2, 10, 5)];

        _merchants.Setup(m => m.GetAllWithCatalogDataAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync([merchant]);
        _imageStorage.Setup(s => s.ResolvePublicUrl("merchants/10/photo.jpg"))
                     .Returns("https://cdn.resq.com/merchants/10/photo.jpg");

        var sut = CreateSut();

        // Act
        var result = await sut.GetCatalogAsync();

        // Assert
        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value);
        Assert.Equal("Panadería Don José", item.BusinessName);
        Assert.Equal("https://cdn.resq.com/merchants/10/photo.jpg", item.PhotoUrl);
        var categoryName = Assert.Single(item.Categories);
        Assert.Equal("Panadería", categoryName);
        Assert.Equal(4.5m, item.AverageRating);
        Assert.Equal(2, item.ReviewCount);
        Assert.Equal(300m, item.MinSalePrice);
        Assert.Equal(2, item.ActiveProductCount);
    }

    [Fact]
    public async Task GetCatalogAsync_WhenMerchantHasNoProductsOrReviews_ReturnsZeroedAggregates()
    {
        // Arrange
        var merchant = BuildMerchant(11);
        _merchants.Setup(m => m.GetAllWithCatalogDataAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync([merchant]);

        var sut = CreateSut();

        // Act
        var result = await sut.GetCatalogAsync();

        // Assert
        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value);
        Assert.Equal(0, item.AverageRating);
        Assert.Equal(0, item.MinSalePrice);
        Assert.Empty(item.Categories);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByIdAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByIdAsync_WhenMerchantNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _merchants.Setup(m => m.GetByIdWithPublicDetailAsync(99, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((MerchantProfile?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.GetByIdAsync(99);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrado", result.Errors[0].Message);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsDetailWithCategoriesProductsAndReviewsOrderedByNewest()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Sushi" };
        var merchant = BuildMerchant(10);
        merchant.MerchantCategories = [new MerchantCategory { MerchantId = 10, CategoryId = 1, Category = category }];
        merchant.Products = [BuildProduct(1, 10)];
        var older = BuildReview(1, 10, 3, createdAt: DateTime.UtcNow.AddDays(-2));
        var newer = BuildReview(2, 10, 5, createdAt: DateTime.UtcNow.AddDays(-1));
        merchant.Reviews = [older, newer];

        _merchants.Setup(m => m.GetByIdWithPublicDetailAsync(10, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(merchant);

        var sut = CreateSut();

        // Act
        var result = await sut.GetByIdAsync(10);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Sushi", result.Value.Categories[0].Name);
        Assert.Single(result.Value.ActiveProducts);
        Assert.Equal(2, result.Value.RecentReviews.Count);
        Assert.Equal(newer.Id, result.Value.RecentReviews[0].Id);
        Assert.Equal(older.Id, result.Value.RecentReviews[1].Id);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMyProfileAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMyProfileAsync_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _merchants.Setup(m => m.GetByIdWithCategoriesAsync(99, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((MerchantProfile?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.GetMyProfileAsync(99);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrado", result.Errors[0].Message);
    }

    [Fact]
    public async Task GetMyProfileAsync_WhenFound_ReturnsMappedProfile()
    {
        // Arrange
        var category = new Category { Id = 2, Name = "Café" };
        var merchant = BuildMerchant(10);
        merchant.Cuit = "30-99999999-1";
        merchant.MerchantCategories = [new MerchantCategory { MerchantId = 10, CategoryId = 2, Category = category }];

        _merchants.Setup(m => m.GetByIdWithCategoriesAsync(10, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(merchant);

        var sut = CreateSut();

        // Act
        var result = await sut.GetMyProfileAsync(10);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("30-99999999-1", result.Value.Cuit);
        Assert.Equal("Connected", result.Value.MpConnectionStatus);
        Assert.Single(result.Value.Categories);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UpdateMyProfileAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateMyProfileAsync_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _merchants.Setup(m => m.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((MerchantProfile?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateMyProfileAsync(99, new UpdateMerchantProfileRequest());

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrado", result.Errors[0].Message);
    }

    [Fact]
    public async Task UpdateMyProfileAsync_WhenCategoryIdsEmpty_UpdatesFieldsWithoutTouchingCategories()
    {
        // Arrange
        var merchant = BuildMerchant(10);
        var request = new UpdateMerchantProfileRequest
        {
            BusinessName = "Nuevo nombre",
            Address      = "Nueva dirección",
            Latitude     = -31.5m,
            Longitude    = -64.3m,
            ContactPhone = "351-7654321",
            CategoryIds  = []
        };

        _merchants.Setup(m => m.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _merchants.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var refreshed = BuildMerchant(10);
        refreshed.BusinessName = "Nuevo nombre";
        _merchants.Setup(m => m.GetByIdWithCategoriesAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(refreshed);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateMyProfileAsync(10, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Nuevo nombre", result.Value.BusinessName);
        Assert.Equal("Nueva dirección", merchant.Address);
        _merchants.Verify(m => m.Update(merchant), Times.Once);
        _merchantCategories.Verify(c => c.GetByMerchantIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _merchantCategories.Verify(c => c.DeleteRange(It.IsAny<IEnumerable<MerchantCategory>>()), Times.Never);
    }

    [Fact]
    public async Task UpdateMyProfileAsync_WhenCategoryIdsProvided_ReplacesCategoriesWithDistinctSet()
    {
        // Arrange
        var merchant = BuildMerchant(10);
        var existing = new List<MerchantCategory> { new() { MerchantId = 10, CategoryId = 99 } };
        var request = new UpdateMerchantProfileRequest { CategoryIds = [1, 2, 2] };

        _merchants.Setup(m => m.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _merchantCategories.Setup(c => c.GetByMerchantIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _merchants.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _merchants.Setup(m => m.GetByIdWithCategoriesAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(BuildMerchant(10));

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateMyProfileAsync(10, request);

        // Assert
        Assert.True(result.IsSuccess);
        _merchantCategories.Verify(c => c.DeleteRange(existing), Times.Once);
        _merchantCategories.Verify(c => c.AddAsync(
            It.Is<MerchantCategory>(mc => mc.MerchantId == 10 && mc.CategoryId == 1), It.IsAny<CancellationToken>()), Times.Once);
        _merchantCategories.Verify(c => c.AddAsync(
            It.Is<MerchantCategory>(mc => mc.MerchantId == 10 && mc.CategoryId == 2), It.IsAny<CancellationToken>()), Times.Once);
        _merchantCategories.Verify(c => c.AddAsync(It.IsAny<MerchantCategory>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMyOrdersAsync / ConfirmPickupAsync — pure delegation to IOrderService
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMyOrdersAsync_DelegatesToOrderService()
    {
        // Arrange
        var expected = Result.Ok<IEnumerable<MerchantOrderSummaryResponse>>([new MerchantOrderSummaryResponse { Id = 1 }]);
        _orderService.Setup(s => s.GetMerchantOrdersAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var sut = CreateSut();

        // Act
        var result = await sut.GetMyOrdersAsync(10);

        // Assert
        Assert.Same(expected, result);
        _orderService.Verify(s => s.GetMerchantOrdersAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmPickupAsync_DelegatesToOrderService()
    {
        // Arrange
        var expected = Result.Ok(new MerchantOrderSummaryResponse { Id = 1, PickupCode = "ABC123" });
        _orderService.Setup(s => s.ConfirmPickupAsync(10, "ABC123", It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var sut = CreateSut();

        // Act
        var result = await sut.ConfirmPickupAsync(10, "ABC123");

        // Assert
        Assert.Same(expected, result);
        _orderService.Verify(s => s.ConfirmPickupAsync(10, "ABC123", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetDashboardAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetDashboardAsync_WhenMerchantNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _merchants.Setup(m => m.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((MerchantProfile?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.GetDashboardAsync(99);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrado", result.Errors[0].Message);
    }

    [Fact]
    public async Task GetDashboardAsync_WhenFound_ComputesMetricsFromOrdersProductsAndReviews()
    {
        // Arrange
        var merchant = BuildMerchant(10);
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        var paidToday       = BuildOrder(1, 10, OrderStatus.Paid, 100m, today, [2]);
        var pickedUpToday   = BuildOrder(2, 10, OrderStatus.PickedUp, 200m, today, [1]);
        var cancelledBefore = BuildOrder(3, 10, OrderStatus.Cancelled, 999m, yesterday, [5]);

        _orders.Setup(o => o.GetByMerchantIdAsync(10, It.IsAny<CancellationToken>()))
               .ReturnsAsync([paidToday, pickedUpToday, cancelledBefore]);
        _products.Setup(p => p.GetByMerchantIdAsync(10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync([BuildProduct(1, 10, isActive: true), BuildProduct(2, 10, isActive: false)]);
        _reviews.Setup(r => r.GetByMerchantIdAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync([BuildReview(1, 10, 4), BuildReview(2, 10, 5)]);
        _merchants.Setup(m => m.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);

        var sut = CreateSut();

        // Act
        var result = await sut.GetDashboardAsync(10);

        // Assert
        Assert.True(result.IsSuccess);
        var dto = result.Value;
        Assert.Equal("Panadería Don José", dto.BusinessName);
        Assert.Equal(1, dto.ActiveOrders);
        Assert.Equal(300m, dto.TodayIncome);
        Assert.Equal(3, dto.PacksSoldToday);
        Assert.Equal(2, dto.TotalSales);
        Assert.Equal(300m, dto.TotalIncome);
        Assert.Equal(3.0m, dto.KgFoodRescued);
        Assert.Equal(4.5m, dto.AverageRating);
        Assert.Equal(2, dto.ReviewCount);
        Assert.Equal(1, dto.ActivePackCount);
        Assert.Equal("Connected", dto.MpConnectionStatus);
        Assert.Equal(7, dto.WeeklySales.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMyReviewsAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMyReviewsAsync_ReturnsAllReviewsMappedToResponse()
    {
        // Arrange
        _reviews.Setup(r => r.GetByMerchantIdAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync([BuildReview(1, 10, 5, "Excelente")]);

        var sut = CreateSut();

        // Act
        var result = await sut.GetMyReviewsAsync(10);

        // Assert
        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value);
        Assert.Equal((byte)5, item.Rating);
        Assert.Equal("Excelente", item.Comment);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UploadPhotoAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UploadPhotoAsync_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _merchants.Setup(m => m.GetByIdWithCategoriesAsync(99, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((MerchantProfile?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.UploadPhotoAsync(99, Mock.Of<IFormFile>());

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrado", result.Errors[0].Message);
    }

    [Fact]
    public async Task UploadPhotoAsync_WhenFound_UploadsAndUpdatesPhotoUrl()
    {
        // Arrange
        const string storedPath = "merchants/10/photo.jpg";
        var merchant = BuildMerchant(10);
        var mockFile = new Mock<IFormFile>();

        _merchants.Setup(m => m.GetByIdWithCategoriesAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _imageStorage.Setup(s => s.UploadAsync(mockFile.Object, "merchants/10", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(storedPath);
        _imageStorage.Setup(s => s.ResolvePublicUrl(storedPath)).Returns("https://cdn.resq.com/merchants/10/photo.jpg");
        _merchants.Setup(m => m.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.UploadPhotoAsync(10, mockFile.Object);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("https://cdn.resq.com/merchants/10/photo.jpg", result.Value.PhotoUrl);
        Assert.Equal(storedPath, merchant.PhotoUrl);
        _merchants.Verify(m => m.Update(merchant), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetAnalyticsAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAnalyticsAsync_WhenMerchantNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _merchants.Setup(m => m.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((MerchantProfile?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.GetAnalyticsAsync(99, new DashboardQuery());

        // Assert
        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task GetAnalyticsAsync_WhenFound_ComputesKpisGrowthAndTopProducts()
    {
        // Arrange
        var merchant = BuildMerchant(10);
        _merchants.Setup(m => m.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);

        var query = new DashboardQuery { From = new DateTime(2026, 1, 1), To = new DateTime(2026, 1, 10) };

        var productA = new Product { Id = 1, Name = "Pack A" };
        var productB = new Product { Id = 2, Name = "Pack B" };

        var orderA = new Order
        {
            Id = 1, MerchantId = 10, CreatedAt = new DateTime(2026, 1, 5), OrderStatus = OrderStatus.Paid,
            MerchantEarnings = 500m, TotalAmount = 600m,
            OrderDetails = [new OrderDetail { ProductId = 1, Product = productA, Quantity = 2, UnitPrice = 300m }]
        };
        var orderB = new Order
        {
            Id = 2, MerchantId = 10, CreatedAt = new DateTime(2026, 1, 6), OrderStatus = OrderStatus.PickedUp,
            MerchantEarnings = 300m, TotalAmount = 400m,
            OrderDetails = [new OrderDetail { ProductId = 2, Product = productB, Quantity = 1, UnitPrice = 400m }]
        };
        var orderC = new Order
        {
            Id = 3, MerchantId = 10, CreatedAt = new DateTime(2026, 1, 7), OrderStatus = OrderStatus.Cancelled,
            MerchantEarnings = 0m, TotalAmount = 100m, OrderDetails = []
        };
        var previousOrder = new Order
        {
            Id = 4, MerchantId = 10, CreatedAt = new DateTime(2025, 12, 25), OrderStatus = OrderStatus.Paid,
            MerchantEarnings = 200m, TotalAmount = 250m, OrderDetails = []
        };

        _orders.Setup(o => o.GetByMerchantIdAsync(10, It.IsAny<CancellationToken>()))
               .ReturnsAsync([orderA, orderB, orderC, previousOrder]);
        _reviews.Setup(r => r.GetByMerchantIdAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync([BuildReview(1, 10, 5), BuildReview(2, 10, 5), BuildReview(3, 10, 3)]);

        var sut = CreateSut();

        // Act
        var result = await sut.GetAnalyticsAsync(10, query);

        // Assert
        Assert.True(result.IsSuccess);
        var dto = result.Value;
        Assert.Equal("01/01/2026 — 10/01/2026", dto.RangeLabel);
        Assert.Equal(1000m, dto.Gmv);
        Assert.Equal(800m, dto.Earnings);
        Assert.Equal(500m, dto.AverageOrderValue);
        Assert.Equal(200m, dto.PreviousEarnings);
        Assert.Equal(300m, dto.EarningsGrowthPct);
        Assert.Equal(1, dto.CompletedOrders);
        Assert.Equal(1, dto.CancelledOrders);
        Assert.Equal(33.3m, dto.CancellationRate);
        Assert.Equal(3, dto.PacksSold);
        Assert.Equal(4.3m, dto.AverageRating);
        Assert.Equal(3, dto.ReviewCount);
        Assert.Equal(2, dto.TopProducts.Count);
        Assert.Equal("Pack A", dto.TopProducts[0].Name);
        Assert.Equal(2, dto.TopProducts[0].UnitsSold);
        Assert.Equal(10, dto.ActivitySeries.Count);
    }

    [Fact]
    public async Task GetAnalyticsAsync_WhenPreviousPeriodHasNoEarnings_EarningsGrowthPctIsNull()
    {
        // Arrange
        var merchant = BuildMerchant(10);
        _merchants.Setup(m => m.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        var query = new DashboardQuery { From = new DateTime(2026, 1, 1), To = new DateTime(2026, 1, 10) };

        _orders.Setup(o => o.GetByMerchantIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _reviews.Setup(r => r.GetByMerchantIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var sut = CreateSut();

        // Act
        var result = await sut.GetAnalyticsAsync(10, query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.EarningsGrowthPct);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GenerateSalesReportAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateSalesReportAsync_WhenMerchantNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _merchants.Setup(m => m.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((MerchantProfile?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.GenerateSalesReportAsync(99, new ReportQuery());

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrado", result.Errors[0].Message);
    }

    [Fact]
    public async Task GenerateSalesReportAsync_WhenFound_BuildsReportModelAndReturnsRenderedFile()
    {
        // Arrange
        var merchant = BuildMerchant(10);
        _merchants.Setup(m => m.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);

        var query = new ReportQuery
        {
            From = new DateTime(2026, 1, 1), To = new DateTime(2026, 1, 10), Format = ReportFormat.Excel
        };

        var order = new Order
        {
            Id = 1, MerchantId = 10, CreatedAt = new DateTime(2026, 1, 5), OrderStatus = OrderStatus.PickedUp,
            MerchantEarnings = 300m, TotalAmount = 400m,
            Consumer = new ConsumerProfile { FirstName = "Ana", LastName = "Pérez" },
            OrderDetails = []
        };
        _orders.Setup(o => o.GetByMerchantIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync([order]);

        var expectedFile = new ReportFile([1, 2, 3], "application/vnd.openxmlformats", "resq-ventas.xlsx");
        _reportFactory.Setup(f => f.Create(It.IsAny<ReportModel>(), ReportFormat.Excel, It.IsAny<string>()))
                      .Returns(expectedFile);

        var sut = CreateSut();

        // Act
        var result = await sut.GenerateSalesReportAsync(10, query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Same(expectedFile, result.Value);
        _reportFactory.Verify(f => f.Create(
            It.Is<ReportModel>(m => m.Title.Contains("Panadería Don José")),
            ReportFormat.Excel,
            "resq-ventas_20260101-20260110"), Times.Once);
    }
}
