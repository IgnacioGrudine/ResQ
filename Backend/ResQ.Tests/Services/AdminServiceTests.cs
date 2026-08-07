using FluentResults;
using Moq;
using ResQ.API.Common.Errors;
using ResQ.API.DTOs.Admin;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Catalog;
using ResQ.API.Models.Enums;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Models.Orders;
using ResQ.API.Models.Reporting;
using ResQ.API.Models.Reviews;
using ResQ.API.Repositories.Admin;
using ResQ.API.Repositories.Auth;
using ResQ.API.Repositories.Orders;
using ResQ.API.Services.Admin;
using ResQ.API.Services.Reporting;

namespace ResQ.Tests.Services;

public class AdminServiceTests
{
    private readonly Mock<IAdminRepository> _admin = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IReportFileFactory> _reportFactory = new();

    private AdminService CreateSut() => new(_admin.Object, _users.Object, _orders.Object, _reportFactory.Object);

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static MerchantProfile BuildMerchant(
        int id = 1,
        string businessName = "Panadería A",
        bool userIsActive = true,
        MpConnectionStatus mpStatus = MpConnectionStatus.Connected,
        List<Review>? reviews = null,
        List<Product>? products = null,
        List<MerchantCategory>? categories = null,
        MerchantMpCredential? mpCredential = null,
        List<Order>? orders = null,
        DateTime? createdAt = null,
        int userId = 0) => new()
    {
        Id                 = id,
        UserId             = userId == 0 ? id * 100 : userId,
        BusinessName       = businessName,
        Cuit               = "30-12345678-9",
        Address            = "Calle 123",
        ContactPhone       = "351-000000",
        MpConnectionStatus = mpStatus,
        User               = new User { Id = userId == 0 ? id * 100 : userId, Email = $"merchant{id}@test.com", IsActive = userIsActive },
        Reviews            = reviews ?? [],
        Products           = products ?? [],
        MerchantCategories = categories ?? [],
        MpCredential       = mpCredential,
        Orders             = orders ?? [],
        CreatedAt          = createdAt ?? DateTime.UtcNow
    };

    private static Order BuildOrder(
        int id, int merchantId, MerchantProfile merchant, OrderStatus status,
        decimal totalAmount, decimal platformFee, decimal merchantEarnings,
        DateTime createdAt, ConsumerProfile? consumer = null,
        List<OrderDetail>? details = null, Review? review = null) => new()
    {
        Id                = id,
        MerchantId        = merchantId,
        Merchant          = merchant,
        ConsumerId        = consumer?.Id ?? 0,
        Consumer          = consumer!,
        OrderStatus       = status,
        TotalAmount       = totalAmount,
        PlatformFee       = platformFee,
        MerchantEarnings  = merchantEarnings,
        CreatedAt         = createdAt,
        ExternalReference = Guid.NewGuid().ToString(),
        PickupCode        = "ABC123",
        OrderDetails      = details ?? [],
        Review            = review
    };

    private static User BuildUser(
        int id, string email, bool isActive = true, List<Role>? roles = null,
        ConsumerProfile? consumerProfile = null, MerchantProfile? merchantProfile = null,
        DateTime? createdAt = null) => new()
    {
        Id               = id,
        Email            = email,
        IsActive         = isActive,
        UserRoles        = (roles ?? []).Select(r => new UserRole { UserId = id, Role = r }).ToList(),
        ConsumerProfile  = consumerProfile,
        MerchantProfile  = merchantProfile,
        CreatedAt        = createdAt ?? DateTime.UtcNow
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // GetDashboardAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetDashboardAsync_ComputesMoneyOrderAndMerchantMetrics()
    {
        // Arrange
        var query = new DashboardQuery
        {
            From        = new DateTime(2026, 1, 1),
            To          = new DateTime(2026, 1, 3),
            Granularity = ReportGranularity.Day
        };

        var merchant = BuildMerchant(
            id: 1,
            businessName: "Panadería A",
            userIsActive: true,
            mpStatus: MpConnectionStatus.Connected,
            reviews: [new Review { Rating = 5 }],
            products: [new Product { IsActive = true }, new Product { IsActive = false }],
            categories: [new MerchantCategory { Category = new Category { Name = "Panadería" } }]);

        var o1 = BuildOrder(1, merchant.Id, merchant, OrderStatus.Paid, 1000m, 100m, 900m, new DateTime(2026, 1, 1, 10, 0, 0));
        var o2 = BuildOrder(2, merchant.Id, merchant, OrderStatus.PickedUp, 2000m, 200m, 1800m, new DateTime(2026, 1, 2, 10, 0, 0));
        var o3 = BuildOrder(3, merchant.Id, merchant, OrderStatus.Cancelled, 500m, 50m, 450m, new DateTime(2026, 1, 3, 10, 0, 0));

        _admin.Setup(a => a.GetOrdersInRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync([o1, o2, o3]);
        _admin.Setup(a => a.GetAllMerchantsWithStatsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([merchant]);
        _admin.Setup(a => a.CountConsumersAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(50);
        _admin.Setup(a => a.CountConsumersAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync(40);
        _admin.Setup(a => a.CountNewConsumersAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(5);
        _admin.Setup(a => a.CountNewMerchantsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.GetDashboardAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        var dto = result.Value;
        Assert.Equal("01/01/2026 — 03/01/2026", dto.RangeLabel);
        Assert.Equal(3000m, dto.Gmv);
        Assert.Equal(300m, dto.PlatformRevenue);
        Assert.Equal(2700m, dto.MerchantEarnings);
        Assert.Equal(1500m, dto.AverageOrderValue);
        Assert.Equal(3, dto.TotalOrders);
        Assert.Equal(1, dto.CompletedOrders);
        Assert.Equal(1, dto.CancelledOrders);
        Assert.Equal(33.3m, dto.CancellationRate);
        Assert.Equal(50m, dto.PickupRate);
        Assert.Equal(1, dto.TotalMerchants);
        Assert.Equal(1, dto.ActiveMerchants);
        Assert.Equal(1, dto.MerchantsWithMp);
        Assert.Equal(1, dto.NewMerchants);
        Assert.Equal(50, dto.TotalConsumers);
        Assert.Equal(40, dto.ActiveConsumers);
        Assert.Equal(5, dto.NewConsumers);
        Assert.Equal(5m, dto.AverageRating);
        Assert.Equal(1, dto.ReviewCount);
        Assert.Equal(1, dto.ActivePacks);
        Assert.Equal(3, dto.ActivitySeries.Count);
        Assert.Equal(2, dto.ActivitySeries.Sum(p => p.Orders));
        Assert.Single(dto.TopMerchantsByRevenue);
        Assert.Equal("Panadería A", dto.TopMerchantsByRevenue[0].BusinessName);
        Assert.Single(dto.CategoryDistribution);
        Assert.Equal("Panadería", dto.CategoryDistribution[0].Category);
        Assert.Empty(dto.Alerts);
    }

    [Fact]
    public async Task GetDashboardAsync_WhenNoOrdersOrMerchants_ReturnsZeroedMetricsWithoutDivideByZero()
    {
        // Arrange
        var query = new DashboardQuery { From = new DateTime(2026, 1, 1), To = new DateTime(2026, 1, 1) };

        _admin.Setup(a => a.GetOrdersInRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _admin.Setup(a => a.GetAllMerchantsWithStatsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _admin.Setup(a => a.CountConsumersAsync(It.IsAny<bool?>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _admin.Setup(a => a.CountNewConsumersAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _admin.Setup(a => a.CountNewMerchantsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var sut = CreateSut();

        // Act
        var result = await sut.GetDashboardAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        var dto = result.Value;
        Assert.Equal(0m, dto.Gmv);
        Assert.Equal(0m, dto.AverageOrderValue);
        Assert.Equal(0m, dto.CancellationRate);
        Assert.Equal(0m, dto.PickupRate);
        Assert.Equal(0m, dto.AverageRating);
        Assert.Empty(dto.TopMerchantsByRevenue);
        Assert.Empty(dto.Alerts);
    }

    [Fact]
    public async Task GetDashboardAsync_BuildsAlertsForExpiredExpiringAndDisconnectedMerchants()
    {
        // Arrange
        var m1 = BuildMerchant(id: 1, mpStatus: MpConnectionStatus.Connected,
            mpCredential: new MerchantMpCredential { IsActive = true, AccessTokenExpiresAt = DateTime.UtcNow.AddDays(-1) });
        var m2 = BuildMerchant(id: 2, mpStatus: MpConnectionStatus.Connected,
            mpCredential: new MerchantMpCredential { IsActive = true, AccessTokenExpiresAt = DateTime.UtcNow.AddDays(3) });
        var m3 = BuildMerchant(id: 3, mpStatus: MpConnectionStatus.Disconnected, mpCredential: null);

        var query = new DashboardQuery { From = DateTime.UtcNow.AddDays(-1), To = DateTime.UtcNow };

        _admin.Setup(a => a.GetOrdersInRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _admin.Setup(a => a.GetAllMerchantsWithStatsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([m1, m2, m3]);
        _admin.Setup(a => a.CountConsumersAsync(It.IsAny<bool?>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _admin.Setup(a => a.CountNewConsumersAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _admin.Setup(a => a.CountNewMerchantsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var sut = CreateSut();

        // Act
        var result = await sut.GetDashboardAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        var alerts = result.Value.Alerts;
        Assert.Equal(3, alerts.Count);
        Assert.Contains(alerts, a => a.Severity == "critical" && a.MerchantId == m1.Id);
        Assert.Contains(alerts, a => a.Severity == "warning" && a.MerchantId == m2.Id);
        Assert.Contains(alerts, a => a.Severity == "info" && a.MerchantId == null);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMerchantsAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMerchantsAsync_ReturnsMappedPagedMerchants()
    {
        // Arrange
        var merchant = BuildMerchant(
            id: 1,
            businessName: "Sushi Bar",
            reviews: [new Review { Rating = 4 }],
            categories: [new MerchantCategory { Category = new Category { Name = "Sushi" } }],
            orders: []);
        merchant.Orders =
        [
            BuildOrder(1, merchant.Id, merchant, OrderStatus.PickedUp, 1000m, 200m, 800m, DateTime.UtcNow)
        ];

        var query = new MerchantListQuery { Page = 1, PageSize = 10 };

        _admin.Setup(a => a.GetMerchantsPageAsync(null, null, null, 1, 10, It.IsAny<CancellationToken>()))
              .ReturnsAsync(([merchant], 1));

        var sut = CreateSut();

        // Act
        var result = await sut.GetMerchantsAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        var item = result.Value.Items[0];
        Assert.Equal("Sushi Bar", item.BusinessName);
        Assert.Equal(1, item.TotalOrders);
        Assert.Equal(200m, item.TotalRevenue);
        Assert.Equal(4m, item.AverageRating);
        Assert.Equal(1, result.Value.TotalItems);
    }

    [Fact]
    public async Task GetMerchantsAsync_NormalizesInvalidPagingParameters()
    {
        // Arrange
        var query = new MerchantListQuery { Page = 0, PageSize = 0 };

        _admin.Setup(a => a.GetMerchantsPageAsync(null, null, null, 1, 10, It.IsAny<CancellationToken>()))
              .ReturnsAsync(([], 0));

        var sut = CreateSut();

        // Act
        var result = await sut.GetMerchantsAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(10, result.Value.PageSize);
        _admin.Verify(a => a.GetMerchantsPageAsync(null, null, null, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMerchantDetailAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMerchantDetailAsync_WhenMerchantNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _admin.Setup(a => a.GetMerchantDetailAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((MerchantProfile?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.GetMerchantDetailAsync(99);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrado", result.Errors[0].Message);
    }

    [Fact]
    public async Task GetMerchantDetailAsync_WhenFound_ReturnsFullDetailWithOrders()
    {
        // Arrange
        var consumer = new ConsumerProfile { Id = 5, FirstName = "Ana", LastName = "López" };
        var merchant = BuildMerchant(id: 1, businessName: "Panadería A", reviews: [new Review { Rating = 4 }]);

        var olderOrder = BuildOrder(1, merchant.Id, merchant, OrderStatus.Paid, 1000m, 100m, 900m, DateTime.UtcNow.AddDays(-2), consumer);
        var newerOrder = BuildOrder(2, merchant.Id, merchant, OrderStatus.PickedUp, 2000m, 200m, 1800m, DateTime.UtcNow.AddDays(-1), consumer);
        merchant.Orders = [olderOrder, newerOrder];

        _admin.Setup(a => a.GetMerchantDetailAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);

        var sut = CreateSut();

        // Act
        var result = await sut.GetMerchantDetailAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        var dto = result.Value;
        Assert.Equal(1, dto.TotalOrders);
        Assert.Equal(3000m, dto.TotalGmv);
        Assert.Equal(300m, dto.TotalRevenue);
        Assert.Equal(2, dto.RecentOrders.Count);
        Assert.Equal(newerOrder.Id, dto.RecentOrders[0].Id);
        Assert.Equal("Ana López", dto.RecentOrders[0].ConsumerName);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SetMerchantStatusAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SetMerchantStatusAsync_WhenMerchantNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _admin.Setup(a => a.GetMerchantDetailAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((MerchantProfile?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.SetMerchantStatusAsync(99, false);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrado", result.Errors[0].Message);
    }

    [Fact]
    public async Task SetMerchantStatusAsync_WhenMerchantFound_TogglesOwningUserStatus()
    {
        // Arrange
        var merchant = BuildMerchant(id: 1, userId: 7);
        var owningUser = BuildUser(7, "merchant@test.com", isActive: true, roles: [Role.Merchant]);

        _admin.Setup(a => a.GetMerchantDetailAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _users.Setup(u => u.GetWithRolesAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(owningUser);
        _users.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.SetMerchantStatusAsync(1, false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(owningUser.IsActive);
        _users.Verify(u => u.Update(owningUser), Times.Once);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetMerchantStatusAsync_WhenOwningUserIsAdmin_ReturnsBadRequestError()
    {
        // Arrange
        var merchant = BuildMerchant(id: 1, userId: 7);
        var owningUser = BuildUser(7, "admin@test.com", roles: [Role.Admin]);

        _admin.Setup(a => a.GetMerchantDetailAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _users.Setup(u => u.GetWithRolesAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(owningUser);

        var sut = CreateSut();

        // Act
        var result = await sut.SetMerchantStatusAsync(1, false);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("administrador", result.Errors[0].Message);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetUsersAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetUsersAsync_MapsDisplayNameByProfileType()
    {
        // Arrange
        var consumerUser = BuildUser(1, "ana@test.com", roles: [Role.Consumer],
            consumerProfile: new ConsumerProfile { FirstName = "Ana", LastName = "López" });
        var merchantUser = BuildUser(2, "shop@test.com", roles: [Role.Merchant],
            merchantProfile: new MerchantProfile { Id = 10, BusinessName = "Panadería A" });
        var adminUser = BuildUser(3, "admin@test.com", roles: [Role.Admin]);

        var query = new UserListQuery { Page = 1, PageSize = 10 };

        _admin.Setup(a => a.GetUsersPageAsync(null, null, 1, 10, It.IsAny<CancellationToken>()))
              .ReturnsAsync(([consumerUser, merchantUser, adminUser], 3));

        var sut = CreateSut();

        // Act
        var result = await sut.GetUsersAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        var items = result.Value.Items;
        Assert.Equal("Ana López", items[0].DisplayName);
        Assert.Equal("Consumer", items[0].Role);
        Assert.Equal("Panadería A", items[1].DisplayName);
        Assert.Equal(10, items[1].ProfileId);
        Assert.Equal("admin", items[2].DisplayName);
        Assert.Equal("Admin", items[2].Role);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SetUserStatusAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SetUserStatusAsync_WhenUserNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _users.Setup(u => u.GetWithRolesAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.SetUserStatusAsync(99, true);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrado", result.Errors[0].Message);
    }

    [Fact]
    public async Task SetUserStatusAsync_WhenTargetIsAdmin_ReturnsBadRequestErrorAndDoesNotUpdate()
    {
        // Arrange
        var adminUser = BuildUser(1, "admin@test.com", roles: [Role.Admin]);
        _users.Setup(u => u.GetWithRolesAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(adminUser);

        var sut = CreateSut();

        // Act
        var result = await sut.SetUserStatusAsync(1, false);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("administrador", result.Errors[0].Message);
        _users.Verify(u => u.Update(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task SetUserStatusAsync_WhenTargetIsRegularUser_UpdatesActiveStateAndSaves()
    {
        // Arrange
        var user = BuildUser(1, "consumer@test.com", isActive: true, roles: [Role.Consumer]);
        _users.Setup(u => u.GetWithRolesAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.SetUserStatusAsync(1, false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(user.IsActive);
        Assert.NotNull(user.UpdatedAt);
        _users.Verify(u => u.Update(user), Times.Once);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetUserOrdersAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetUserOrdersAsync_WhenUserNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _admin.Setup(a => a.GetUserWithRolesAndProfilesAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.GetUserOrdersAsync(99);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrado", result.Errors[0].Message);
    }

    [Fact]
    public async Task GetUserOrdersAsync_WhenUserIsNotConsumer_ReturnsNotFoundError()
    {
        // Arrange
        var user = BuildUser(1, "shop@test.com", roles: [Role.Merchant]);
        _admin.Setup(a => a.GetUserWithRolesAndProfilesAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var sut = CreateSut();

        // Act
        var result = await sut.GetUserOrdersAsync(1);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no es un consumidor", result.Errors[0].Message);
    }

    [Fact]
    public async Task GetUserOrdersAsync_WhenConsumer_ReturnsMappedOrderHistory()
    {
        // Arrange
        var consumerProfile = new ConsumerProfile { Id = 5 };
        var user = BuildUser(1, "ana@test.com", roles: [Role.Consumer], consumerProfile: consumerProfile);
        var merchant = BuildMerchant(id: 2, businessName: "Panadería A");

        var reviewedOrder = BuildOrder(1, merchant.Id, merchant, OrderStatus.PickedUp, 1000m, 100m, 900m, DateTime.UtcNow,
            details: [new OrderDetail { Quantity = 2, UnitPrice = 500m, Product = new Product { Name = "Pack Mixto" } }],
            review: new Review { Rating = 5 });

        _admin.Setup(a => a.GetUserWithRolesAndProfilesAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _orders.Setup(o => o.GetByConsumerIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync([reviewedOrder]);

        var sut = CreateSut();

        // Act
        var result = await sut.GetUserOrdersAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        var order = result.Value.Single();
        Assert.Equal("Panadería A", order.MerchantName);
        Assert.True(order.HasReview);
        Assert.Single(order.Items);
        Assert.Equal("Pack Mixto", order.Items[0].ProductName);
        Assert.Equal(2, order.Items[0].Quantity);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GenerateGlobalReportAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateGlobalReportAsync_BuildsReportModelAndDelegatesToReportFactory()
    {
        // Arrange
        var query = new ReportQuery { From = new DateTime(2026, 1, 1), To = new DateTime(2026, 1, 2), Format = ReportFormat.Pdf };
        var merchant = BuildMerchant(id: 1);

        var o1 = BuildOrder(1, merchant.Id, merchant, OrderStatus.Paid, 1000m, 100m, 900m, new DateTime(2026, 1, 1));
        var o2 = BuildOrder(2, merchant.Id, merchant, OrderStatus.Cancelled, 500m, 50m, 450m, new DateTime(2026, 1, 2));

        var expectedFile = new ReportFile([1, 2, 3], "application/pdf", "resq-reporte-global.pdf");

        _admin.Setup(a => a.GetOrdersInRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync([o1, o2]);
        _reportFactory.Setup(f => f.Create(
                It.Is<ReportModel>(m => m.Title == "Reporte financiero global" && m.Kpis.Count == 7 && m.Rows.Count == 2),
                ReportFormat.Pdf,
                It.Is<string>(s => s.StartsWith("resq-reporte-global_"))))
            .Returns(expectedFile);

        var sut = CreateSut();

        // Act
        var result = await sut.GenerateGlobalReportAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedFile, result.Value);
        _reportFactory.Verify(f => f.Create(It.IsAny<ReportModel>(), ReportFormat.Pdf, It.IsAny<string>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GenerateMerchantRankingReportAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateMerchantRankingReportAsync_BuildsRankingReportAndDelegatesToReportFactory()
    {
        // Arrange
        var query = new ReportQuery { From = new DateTime(2026, 1, 1), To = new DateTime(2026, 1, 2), Format = ReportFormat.Excel };
        var merchant = BuildMerchant(id: 1, businessName: "Panadería A");
        var order = BuildOrder(1, merchant.Id, merchant, OrderStatus.PickedUp, 1000m, 100m, 900m, new DateTime(2026, 1, 1));

        var expectedFile = new ReportFile([1], "application/xlsx", "resq-ranking-comercios.xlsx");

        _admin.Setup(a => a.GetOrdersInRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync([order]);
        _admin.Setup(a => a.GetAllMerchantsWithStatsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([merchant]);
        _reportFactory.Setup(f => f.Create(
                It.Is<ReportModel>(m => m.Title == "Ranking de comercios" && m.Rows.Count == 1),
                ReportFormat.Excel,
                It.Is<string>(s => s.StartsWith("resq-ranking-comercios_"))))
            .Returns(expectedFile);

        var sut = CreateSut();

        // Act
        var result = await sut.GenerateMerchantRankingReportAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedFile, result.Value);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GenerateMerchantReportAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateMerchantReportAsync_WhenMerchantNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _admin.Setup(a => a.GetMerchantDetailAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((MerchantProfile?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.GenerateMerchantReportAsync(99, new ReportQuery());

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrado", result.Errors[0].Message);
    }

    [Fact]
    public async Task GenerateMerchantReportAsync_WhenFound_FiltersOrdersByRangeAndDelegatesToReportFactory()
    {
        // Arrange
        var query = new ReportQuery { From = new DateTime(2026, 1, 1), To = new DateTime(2026, 1, 5), Format = ReportFormat.Pdf };
        var merchant = BuildMerchant(id: 3, businessName: "Panadería A");

        var inRange    = BuildOrder(1, merchant.Id, merchant, OrderStatus.PickedUp, 1000m, 100m, 900m, new DateTime(2026, 1, 3));
        var outOfRange = BuildOrder(2, merchant.Id, merchant, OrderStatus.PickedUp, 2000m, 200m, 1800m, new DateTime(2026, 2, 1));
        merchant.Orders = [inRange, outOfRange];

        var expectedFile = new ReportFile([9], "application/pdf", "resq-comercio-3.pdf");

        _admin.Setup(a => a.GetMerchantDetailAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        _reportFactory.Setup(f => f.Create(
                It.Is<ReportModel>(m => m.Title.Contains("Panadería A") && m.Rows.Count == 1),
                ReportFormat.Pdf,
                It.Is<string>(s => s.StartsWith("resq-comercio-3_"))))
            .Returns(expectedFile);

        var sut = CreateSut();

        // Act
        var result = await sut.GenerateMerchantReportAsync(3, query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedFile, result.Value);
    }
}
