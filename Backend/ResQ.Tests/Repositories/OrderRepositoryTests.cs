using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Catalog;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Orders;
using ResQ.API.Repositories.Orders;

namespace ResQ.Tests.Repositories;

public class OrderRepositoryTests : IDisposable
{
    private readonly ResQDbContext _db;
    private readonly OrderRepository _sut;

    public OrderRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ResQDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new ResQDbContext(options);
        _sut = new OrderRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<ConsumerProfile> SeedConsumerAsync(string firstName = "Ana")
    {
        var user = new User { Email = $"{firstName.ToLower()}@example.com", CreatedAt = DateTime.UtcNow };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var consumer = new ConsumerProfile
        {
            UserId    = user.Id,
            FirstName = firstName,
            LastName  = "López",
            CreatedAt = DateTime.UtcNow
        };
        _db.ConsumerProfiles.Add(consumer);
        await _db.SaveChangesAsync();
        return consumer;
    }

    private async Task<MerchantProfile> SeedMerchantAsync(string name = "La Panadería")
    {
        var merchant = new MerchantProfile
        {
            BusinessName = name,
            Cuit         = "30-12345678-9",
            Address      = "Av. 123",
            ContactPhone = "351-000000",
            CreatedAt    = DateTime.UtcNow
        };
        _db.MerchantProfiles.Add(merchant);
        await _db.SaveChangesAsync();
        return merchant;
    }

    private async Task<Order> SeedOrderAsync(
        int consumerId,
        int merchantId,
        OrderStatus status       = OrderStatus.Pending,
        string pickupCode        = "ABC123",
        string? externalReference = null)
    {
        var order = new Order
        {
            ConsumerId        = consumerId,
            MerchantId        = merchantId,
            TotalAmount       = 500m,
            PlatformFee       = 50m,
            MerchantEarnings  = 450m,
            ExternalReference = externalReference ?? Guid.NewGuid().ToString(),
            OrderStatus       = status,
            PickupCode        = pickupCode,
            CreatedAt         = DateTime.UtcNow
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByConsumerIdAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByConsumerIdAsync_ReturnsOnlyOrdersForGivenConsumer()
    {
        // Arrange
        var c1 = await SeedConsumerAsync("Ana");
        var c2 = await SeedConsumerAsync("Luis");
        var m  = await SeedMerchantAsync();

        await SeedOrderAsync(c1.Id, m.Id);
        await SeedOrderAsync(c1.Id, m.Id);
        await SeedOrderAsync(c2.Id, m.Id);

        // Act
        var result = (await _sut.GetByConsumerIdAsync(c1.Id)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, o => Assert.Equal(c1.Id, o.ConsumerId));
    }

    [Fact]
    public async Task GetByConsumerIdAsync_WhenConsumerHasNoOrders_ReturnsEmptyList()
    {
        // Arrange
        var consumer = await SeedConsumerAsync();

        // Act
        var result = await _sut.GetByConsumerIdAsync(consumer.Id);

        // Assert
        Assert.Empty(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByMerchantIdAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByMerchantIdAsync_ReturnsOnlyOrdersForGivenMerchant()
    {
        // Arrange
        var c  = await SeedConsumerAsync();
        var m1 = await SeedMerchantAsync("Panadería A");
        var m2 = await SeedMerchantAsync("Panadería B");

        await SeedOrderAsync(c.Id, m1.Id);
        await SeedOrderAsync(c.Id, m2.Id);

        // Act
        var result = (await _sut.GetByMerchantIdAsync(m1.Id)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(m1.Id, result[0].MerchantId);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByPickupCodeAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByPickupCodeAsync_WhenMatchFound_ReturnsOrder()
    {
        // Arrange
        var consumer = await SeedConsumerAsync();
        var merchant = await SeedMerchantAsync();
        var order    = await SeedOrderAsync(consumer.Id, merchant.Id, pickupCode: "PICK99");

        // Act
        var result = await _sut.GetByPickupCodeAsync(merchant.Id, "PICK99");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
    }

    [Fact]
    public async Task GetByPickupCodeAsync_WhenCodeBelongsToDifferentMerchant_ReturnsNull()
    {
        // Arrange
        var consumer = await SeedConsumerAsync();
        var m1       = await SeedMerchantAsync("A");
        var m2       = await SeedMerchantAsync("B");
        await SeedOrderAsync(consumer.Id, m1.Id, pickupCode: "CODE01");

        // Act
        var result = await _sut.GetByPickupCodeAsync(m2.Id, "CODE01");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPickupCodeAsync_WhenCodeNotFound_ReturnsNull()
    {
        // Arrange
        var consumer = await SeedConsumerAsync();
        var merchant = await SeedMerchantAsync();
        await SeedOrderAsync(consumer.Id, merchant.Id, pickupCode: "REAL01");

        // Act
        var result = await _sut.GetByPickupCodeAsync(merchant.Id, "WRONG1");

        // Assert
        Assert.Null(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByExternalReferenceAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByExternalReferenceAsync_WhenFound_ReturnsOrder()
    {
        // Arrange
        var consumer  = await SeedConsumerAsync();
        var merchant  = await SeedMerchantAsync();
        var extRef    = "ext-uuid-1234";
        var order     = await SeedOrderAsync(consumer.Id, merchant.Id, externalReference: extRef);

        // Act
        var result = await _sut.GetByExternalReferenceAsync(extRef);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(extRef, result.ExternalReference);
    }

    [Fact]
    public async Task GetByExternalReferenceAsync_WhenNotFound_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByExternalReferenceAsync("non-existent-ref");

        // Assert
        Assert.Null(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByIdForConsumerAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByIdForConsumerAsync_WhenOrderBelongsToConsumer_ReturnsOrder()
    {
        // Arrange
        var consumer = await SeedConsumerAsync();
        var merchant = await SeedMerchantAsync();
        var order    = await SeedOrderAsync(consumer.Id, merchant.Id);

        // Act
        var result = await _sut.GetByIdForConsumerAsync(order.Id, consumer.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdForConsumerAsync_WhenOrderBelongsToDifferentConsumer_ReturnsNull()
    {
        // Arrange
        var c1      = await SeedConsumerAsync("Ana");
        var c2      = await SeedConsumerAsync("Luis");
        var merchant = await SeedMerchantAsync();
        var order   = await SeedOrderAsync(c1.Id, merchant.Id);

        // Act
        var result = await _sut.GetByIdForConsumerAsync(order.Id, c2.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdForConsumerAsync_WhenOrderNotFound_ReturnsNull()
    {
        // Arrange
        var consumer = await SeedConsumerAsync();

        // Act
        var result = await _sut.GetByIdForConsumerAsync(9999, consumer.Id);

        // Assert
        Assert.Null(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GenericRepository — AddAsync / Update
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsOrder()
    {
        // Arrange
        var consumer = await SeedConsumerAsync();
        var merchant = await SeedMerchantAsync();
        var order = new Order
        {
            ConsumerId        = consumer.Id,
            MerchantId        = merchant.Id,
            TotalAmount       = 800m,
            PlatformFee       = 80m,
            MerchantEarnings  = 720m,
            ExternalReference = "new-ext-ref",
            OrderStatus       = OrderStatus.Pending,
            PickupCode        = "NEW001",
            CreatedAt         = DateTime.UtcNow
        };

        // Act
        await _sut.AddAsync(order);
        await _sut.SaveChangesAsync();

        // Assert
        var saved = await _db.Orders.FindAsync(order.Id);
        Assert.NotNull(saved);
        Assert.Equal("NEW001", saved.PickupCode);
    }

    [Fact]
    public async Task Update_ThenSaveChanges_PersistsOrderStatusChange()
    {
        // Arrange
        var consumer = await SeedConsumerAsync();
        var merchant = await SeedMerchantAsync();
        var order    = await SeedOrderAsync(consumer.Id, merchant.Id, OrderStatus.Pending);

        // Act
        order.OrderStatus = OrderStatus.Paid;
        order.UpdatedAt   = DateTime.UtcNow;
        _sut.Update(order);
        await _sut.SaveChangesAsync();

        // Assert
        var updated = await _db.Orders.FindAsync(order.Id);
        Assert.NotNull(updated);
        Assert.Equal(OrderStatus.Paid, updated.OrderStatus);
    }
}
