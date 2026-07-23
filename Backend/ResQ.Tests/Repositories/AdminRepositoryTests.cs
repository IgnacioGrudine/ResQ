using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Catalog;
using ResQ.API.Models.Enums;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Models.Orders;
using ResQ.API.Repositories.Admin;

namespace ResQ.Tests.Repositories;

public class AdminRepositoryTests : IDisposable
{
    private readonly ResQDbContext _db;
    private readonly AdminRepository _sut;

    public AdminRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ResQDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new ResQDbContext(options);
        _sut = new AdminRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<User> SeedUserAsync(string email, bool isActive = true, DateTime? createdAt = null)
    {
        var user = new User
        {
            Email        = email,
            PasswordHash = "hash",
            IsActive     = isActive,
            CreatedAt    = createdAt ?? DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private async Task SeedUserRoleAsync(int userId, Role role)
    {
        _db.UserRoles.Add(new UserRole { UserId = userId, Role = role, CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();
    }

    private async Task<ConsumerProfile> SeedConsumerAsync(int userId, string firstName = "Ana", DateTime? createdAt = null)
    {
        var consumer = new ConsumerProfile
        {
            UserId    = userId,
            FirstName = firstName,
            LastName  = "López",
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
        _db.ConsumerProfiles.Add(consumer);
        await _db.SaveChangesAsync();
        return consumer;
    }

    private async Task<MerchantProfile> SeedMerchantAsync(
        int userId, string businessName = "Comercio",
        MpConnectionStatus mpStatus = MpConnectionStatus.Disconnected, DateTime? createdAt = null)
    {
        var merchant = new MerchantProfile
        {
            UserId             = userId,
            BusinessName       = businessName,
            Cuit               = "30-12345678-9",
            Address            = "Calle 123",
            ContactPhone       = "351-000000",
            MpConnectionStatus = mpStatus,
            CreatedAt          = createdAt ?? DateTime.UtcNow
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

    private async Task SeedMerchantCategoryAsync(int merchantId, int categoryId)
    {
        _db.MerchantCategories.Add(new MerchantCategory { MerchantId = merchantId, CategoryId = categoryId });
        await _db.SaveChangesAsync();
    }

    private async Task<Order> SeedOrderAsync(
        int consumerId, int merchantId, OrderStatus status = OrderStatus.Paid,
        decimal totalAmount = 1000m, decimal platformFee = 100m, decimal merchantEarnings = 900m,
        DateTime? createdAt = null, string pickupCode = "ABC123")
    {
        var order = new Order
        {
            ConsumerId        = consumerId,
            MerchantId        = merchantId,
            TotalAmount       = totalAmount,
            PlatformFee       = platformFee,
            MerchantEarnings  = merchantEarnings,
            ExternalReference = Guid.NewGuid().ToString(),
            OrderStatus       = status,
            PickupCode        = pickupCode,
            CreatedAt         = createdAt ?? DateTime.UtcNow
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    private async Task<Product> SeedProductAsync(int merchantId, string name = "Pack", bool isActive = true)
    {
        var product = new Product
        {
            MerchantId      = merchantId,
            Name            = name,
            ProductType     = ProductType.SurprisePack,
            OriginalPrice   = 1000m,
            SalePrice       = 400m,
            StockQuantity   = 5,
            PickupTimeStart = new TimeOnly(18, 0),
            PickupTimeEnd   = new TimeOnly(21, 0),
            IsActive        = isActive,
            CreatedAt       = DateTime.UtcNow
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    private async Task SeedOrderDetailAsync(int orderId, int productId, int quantity = 1, decimal unitPrice = 400m)
    {
        _db.OrderDetails.Add(new OrderDetail
        {
            OrderId   = orderId,
            ProductId = productId,
            Quantity  = quantity,
            UnitPrice = unitPrice,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    private async Task SeedReviewAsync(int orderId, int merchantId, byte rating = 5)
    {
        _db.Reviews.Add(new ResQ.API.Models.Reviews.Review
        {
            OrderId    = orderId,
            MerchantId = merchantId,
            Rating     = rating,
            CreatedAt  = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    private async Task SeedMpCredentialAsync(int merchantId, bool isActive = true, DateTime? expiresAt = null)
    {
        _db.MerchantMpCredentials.Add(new MerchantMpCredential
        {
            MerchantId           = merchantId,
            MpUserId             = 123,
            AccessToken          = "enc-access",
            RefreshToken         = "enc-refresh",
            AccessTokenExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(30),
            IsActive             = isActive,
            CreatedAt            = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    private async Task<MpTokenRefreshLog> SeedMpLogAsync(int merchantId, bool success = true, DateTime? createdAt = null)
    {
        var log = new MpTokenRefreshLog { MerchantId = merchantId, Success = success, CreatedAt = createdAt ?? DateTime.UtcNow };
        _db.MpTokenRefreshLogs.Add(log);
        await _db.SaveChangesAsync();
        return log;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetOrdersInRangeAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetOrdersInRangeAsync_ReturnsOnlyOrdersWithinInclusiveRange()
    {
        // Arrange
        var user     = await SeedUserAsync("owner@test.com");
        var merchant = await SeedMerchantAsync(user.Id);
        var consumer = await SeedConsumerAsync(user.Id);

        var from = new DateTime(2026, 1, 1);
        var to   = new DateTime(2026, 1, 31);

        await SeedOrderAsync(consumer.Id, merchant.Id, createdAt: new DateTime(2025, 12, 31)); // before range
        var inRange = await SeedOrderAsync(consumer.Id, merchant.Id, createdAt: new DateTime(2026, 1, 15));
        await SeedOrderAsync(consumer.Id, merchant.Id, createdAt: new DateTime(2026, 2, 1)); // after range

        // Act
        var result = (await _sut.GetOrdersInRangeAsync(from, to)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(inRange.Id, result[0].Id);
        Assert.Equal(merchant.BusinessName, result[0].Merchant.BusinessName);
    }

    [Fact]
    public async Task GetOrdersInRangeAsync_EagerlyLoadsOrderDetailsAndProduct()
    {
        // Arrange
        var user     = await SeedUserAsync("owner@test.com");
        var merchant = await SeedMerchantAsync(user.Id);
        var consumer = await SeedConsumerAsync(user.Id);
        var product  = await SeedProductAsync(merchant.Id, "Pack Sorpresa");
        var order    = await SeedOrderAsync(consumer.Id, merchant.Id, createdAt: new DateTime(2026, 1, 10));
        await SeedOrderDetailAsync(order.Id, product.Id);

        // Act
        var result = (await _sut.GetOrdersInRangeAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31))).ToList();

        // Assert
        Assert.Single(result);
        Assert.Single(result[0].OrderDetails);
        Assert.Equal("Pack Sorpresa", result[0].OrderDetails.First().Product.Name);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetAllMerchantsWithStatsAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAllMerchantsWithStatsAsync_ReturnsAllMerchantsWithRelatedDataLoaded()
    {
        // Arrange
        var user     = await SeedUserAsync("owner@test.com");
        var merchant = await SeedMerchantAsync(user.Id, "Panadería A");
        var consumer = await SeedConsumerAsync(user.Id);
        var product  = await SeedProductAsync(merchant.Id);
        var order    = await SeedOrderAsync(consumer.Id, merchant.Id);
        await SeedReviewAsync(order.Id, merchant.Id);
        await SeedMpCredentialAsync(merchant.Id);

        // Act
        var result = (await _sut.GetAllMerchantsWithStatsAsync()).ToList();

        // Assert
        Assert.Single(result);
        var m = result[0];
        Assert.Equal("Panadería A", m.BusinessName);
        Assert.NotNull(m.User);
        Assert.Single(m.Reviews);
        Assert.Single(m.Products);
        Assert.NotNull(m.MpCredential);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CountConsumersAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CountConsumersAsync_WhenActiveIsNull_CountsAllConsumers()
    {
        // Arrange
        var activeUser   = await SeedUserAsync("active@test.com", isActive: true);
        var inactiveUser = await SeedUserAsync("inactive@test.com", isActive: false);
        await SeedConsumerAsync(activeUser.Id);
        await SeedConsumerAsync(inactiveUser.Id);

        // Act
        var result = await _sut.CountConsumersAsync(null);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task CountConsumersAsync_WhenActiveIsTrue_CountsOnlyActiveConsumers()
    {
        // Arrange
        var activeUser   = await SeedUserAsync("active@test.com", isActive: true);
        var inactiveUser = await SeedUserAsync("inactive@test.com", isActive: false);
        await SeedConsumerAsync(activeUser.Id);
        await SeedConsumerAsync(inactiveUser.Id);

        // Act
        var result = await _sut.CountConsumersAsync(true);

        // Assert
        Assert.Equal(1, result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CountNewConsumersAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CountNewConsumersAsync_CountsOnlyConsumersCreatedWithinRange()
    {
        // Arrange
        var u1 = await SeedUserAsync("u1@test.com");
        var u2 = await SeedUserAsync("u2@test.com");
        await SeedConsumerAsync(u1.Id, createdAt: new DateTime(2026, 1, 10));
        await SeedConsumerAsync(u2.Id, createdAt: new DateTime(2026, 2, 10));

        // Act
        var result = await _sut.CountNewConsumersAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        // Assert
        Assert.Equal(1, result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CountNewMerchantsAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CountNewMerchantsAsync_CountsOnlyMerchantsCreatedWithinRange()
    {
        // Arrange
        var u1 = await SeedUserAsync("u1@test.com");
        var u2 = await SeedUserAsync("u2@test.com");
        await SeedMerchantAsync(u1.Id, createdAt: new DateTime(2026, 1, 10));
        await SeedMerchantAsync(u2.Id, createdAt: new DateTime(2026, 2, 10));

        // Act
        var result = await _sut.CountNewMerchantsAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        // Assert
        Assert.Equal(1, result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMerchantsPageAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMerchantsPageAsync_FiltersByActiveState()
    {
        // Arrange
        var activeUser   = await SeedUserAsync("active@test.com", isActive: true);
        var inactiveUser = await SeedUserAsync("inactive@test.com", isActive: false);
        await SeedMerchantAsync(activeUser.Id, "Activo");
        await SeedMerchantAsync(inactiveUser.Id, "Inactivo");

        // Act
        var (items, total) = await _sut.GetMerchantsPageAsync(true, null, null, 1, 10);

        // Assert
        Assert.Equal(1, total);
        Assert.Equal("Activo", items[0].BusinessName);
    }

    [Fact]
    public async Task GetMerchantsPageAsync_FiltersByCategoryId()
    {
        // Arrange
        var u1 = await SeedUserAsync("u1@test.com");
        var u2 = await SeedUserAsync("u2@test.com");
        var m1 = await SeedMerchantAsync(u1.Id, "Con categoría");
        await SeedMerchantAsync(u2.Id, "Sin categoría");
        var category = await SeedCategoryAsync("Sushi");
        await SeedMerchantCategoryAsync(m1.Id, category.Id);

        // Act
        var (items, total) = await _sut.GetMerchantsPageAsync(null, category.Id, null, 1, 10);

        // Assert
        Assert.Equal(1, total);
        Assert.Equal("Con categoría", items[0].BusinessName);
    }

    [Fact]
    public async Task GetMerchantsPageAsync_FiltersByMpStatus()
    {
        // Arrange
        var u1 = await SeedUserAsync("u1@test.com");
        var u2 = await SeedUserAsync("u2@test.com");
        await SeedMerchantAsync(u1.Id, "Conectado", MpConnectionStatus.Connected);
        await SeedMerchantAsync(u2.Id, "Desconectado", MpConnectionStatus.Disconnected);

        // Act
        var (items, total) = await _sut.GetMerchantsPageAsync(null, null, "Connected", 1, 10);

        // Assert
        Assert.Equal(1, total);
        Assert.Equal("Conectado", items[0].BusinessName);
    }

    [Fact]
    public async Task GetMerchantsPageAsync_PaginatesResults()
    {
        // Arrange
        for (var i = 1; i <= 5; i++)
        {
            var user = await SeedUserAsync($"u{i}@test.com");
            await SeedMerchantAsync(user.Id, $"Comercio {i}");
        }

        // Act
        var (items, total) = await _sut.GetMerchantsPageAsync(null, null, null, 2, 2);

        // Assert
        Assert.Equal(5, total);
        Assert.Equal(2, items.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMerchantDetailAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMerchantDetailAsync_WhenFound_ReturnsMerchantWithOrdersAndProducts()
    {
        // Arrange
        var user     = await SeedUserAsync("owner@test.com");
        var merchant = await SeedMerchantAsync(user.Id, "Panadería A");
        var consumer = await SeedConsumerAsync(user.Id);
        await SeedProductAsync(merchant.Id);
        await SeedOrderAsync(consumer.Id, merchant.Id);

        // Act
        var result = await _sut.GetMerchantDetailAsync(merchant.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Panadería A", result.BusinessName);
        Assert.Single(result.Products);
        Assert.Single(result.Orders);
    }

    [Fact]
    public async Task GetMerchantDetailAsync_WhenNotFound_ReturnsNull()
    {
        // Act
        var result = await _sut.GetMerchantDetailAsync(9999);

        // Assert
        Assert.Null(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMpLogsByMerchantAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMpLogsByMerchantAsync_ReturnsLogsForGivenMerchantOrderedNewestFirst()
    {
        // Arrange
        var u1 = await SeedUserAsync("u1@test.com");
        var u2 = await SeedUserAsync("u2@test.com");
        var m1 = await SeedMerchantAsync(u1.Id);
        var m2 = await SeedMerchantAsync(u2.Id);

        await SeedMpLogAsync(m1.Id, success: true, createdAt: DateTime.UtcNow.AddDays(-2));
        var newest = await SeedMpLogAsync(m1.Id, success: false, createdAt: DateTime.UtcNow.AddDays(-1));
        await SeedMpLogAsync(m2.Id, success: true);

        // Act
        var result = (await _sut.GetMpLogsByMerchantAsync(m1.Id)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(newest.Id, result[0].Id);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetUsersPageAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetUsersPageAsync_FiltersByRole()
    {
        // Arrange
        var consumerUser = await SeedUserAsync("consumer@test.com");
        var merchantUser = await SeedUserAsync("merchant@test.com");
        await SeedUserRoleAsync(consumerUser.Id, Role.Consumer);
        await SeedUserRoleAsync(merchantUser.Id, Role.Merchant);

        // Act
        var (items, total) = await _sut.GetUsersPageAsync(Role.Merchant, null, 1, 10);

        // Assert
        Assert.Equal(1, total);
        Assert.Equal(merchantUser.Id, items[0].Id);
    }

    [Fact]
    public async Task GetUsersPageAsync_FiltersByActiveState()
    {
        // Arrange
        await SeedUserAsync("active@test.com", isActive: true);
        await SeedUserAsync("inactive@test.com", isActive: false);

        // Act
        var (items, total) = await _sut.GetUsersPageAsync(null, false, 1, 10);

        // Assert
        Assert.Equal(1, total);
        Assert.Equal("inactive@test.com", items[0].Email);
    }

    [Fact]
    public async Task GetUsersPageAsync_PaginatesResults()
    {
        // Arrange
        for (var i = 1; i <= 5; i++)
            await SeedUserAsync($"u{i}@test.com");

        // Act
        var (items, total) = await _sut.GetUsersPageAsync(null, null, 1, 2);

        // Assert
        Assert.Equal(5, total);
        Assert.Equal(2, items.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetUserWithRolesAndProfilesAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetUserWithRolesAndProfilesAsync_WhenFound_ReturnsUserWithRolesAndConsumerProfile()
    {
        // Arrange
        var user = await SeedUserAsync("ana@test.com");
        await SeedUserRoleAsync(user.Id, Role.Consumer);
        await SeedConsumerAsync(user.Id, "Ana");

        // Act
        var result = await _sut.GetUserWithRolesAndProfilesAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.UserRoles);
        Assert.NotNull(result.ConsumerProfile);
        Assert.Equal("Ana", result.ConsumerProfile!.FirstName);
    }

    [Fact]
    public async Task GetUserWithRolesAndProfilesAsync_WhenNotFound_ReturnsNull()
    {
        // Act
        var result = await _sut.GetUserWithRolesAndProfilesAsync(9999);

        // Assert
        Assert.Null(result);
    }
}
